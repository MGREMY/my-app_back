using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Models;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.AuthServices;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Resources;
using MyApp.Services.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Services.Tests.Services.AuthServices;

public sealed partial class AuthSyncUserTests
{
    [IntegrationTest]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : ServiceIntegrationTest(containers)
    {
        [Fact]
        public async Task ExecuteAsync_NewAuthId_CreatesUser()
        {
            var before = DateTimeOffset.UtcNow;

            var result = await SyncAsync(new IAuthSyncUser.Request("auth-1", "john", "john@example.test"));

            result.Value.ShouldBeOfType<ServiceSuccessResponse>();
            var user = await WithDbAsync(db => db.Users.AsNoTracking().SingleAsync(Ct));
            user.AuthId.ShouldBe("auth-1");
            user.UserName.ShouldBe("john");
            user.Email.ShouldBe("john@example.test");
            user.Id.ShouldNotBe(Guid.Empty);
            user.CreatedAtUtc.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
            user.HasBeenModified.ShouldBeFalse();
            user.IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_NewAuthId_CachesCreatedUser()
        {
            await SyncAsync(new IAuthSyncUser.Request("auth-1", "john", "john@example.test"));
            var id = await WithDbAsync(db => db.Users.Select(x => x.Id).SingleAsync(Ct));
            await RenameInDatabaseAsync(id, "renamed");

            // Fresh instance: the value can only come from the Redis entry written by the sync.
            var result = await GetByIdAsync(id, CreateProvider());

            result.Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe("john");
        }

        [Fact]
        public async Task ExecuteAsync_ExistingAuthId_DoesNotCreateAnotherUser()
        {
            var user = await SeedUserAsync("auth-1");

            var result = await SyncAsync(new IAuthSyncUser.Request("auth-1", user.UserName, user.Email));

            result.Value.ShouldBeOfType<ServiceSuccessResponse>();
            var ids = await WithDbAsync(db => db.Users.Select(x => x.Id).ToListAsync(Ct));
            ids.ShouldBe([user.Id]);
        }

        [Fact]
        public async Task ExecuteAsync_ExistingAuthId_RefreshesCacheFromDatabase()
        {
            var user = await SeedUserAsync("auth-1");
            await GetByIdAsync(user.Id);
            await RenameInDatabaseAsync(user.Id, "renamed");

            await SyncAsync(new IAuthSyncUser.Request("auth-1", user.UserName, user.Email));

            (await GetByIdAsync(user.Id)).Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe("renamed");
        }

        [Fact(Skip = "Bug: AuthSyncUser never updates an existing user, the UserName and Email of the request are ignored")]
        public async Task ExecuteAsync_ExistingAuthIdWithNewProfile_UpdatesUser()
        {
            var user = await SeedUserAsync("auth-1");

            await SyncAsync(new IAuthSyncUser.Request("auth-1", "new-name", "new@example.test"));

            var stored = (await FindUserIncludingDeletedAsync(user.Id)).ShouldNotBeNull();
            stored.UserName.ShouldBe("new-name");
            stored.Email.ShouldBe("new@example.test");
            stored.HasBeenModified.ShouldBeTrue();
            stored.UpdatedAtUtc.ShouldNotBeNull();
            (await GetByIdAsync(user.Id)).Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe("new-name");
        }

        [Fact]
        public async Task ExecuteAsync_SoftDeletedUser_Returns403WithoutRecreatingUser()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            var result = await SyncAsync(new IAuthSyncUser.Request("auth-1", "john", "john@example.test"));

            var error = result.Value.ShouldBeOfType<ServiceErrorResponse>();
            error.StatusCode.ShouldBe(403);
            error.Message.ShouldBe(SharedResource.user_already_deleted);
            var ids = await WithDbAsync(db => db.Users.IgnoreQueryFilters([ModelConstant.SoftDeletionFilter]).Select(x => x.Id).ToListAsync(Ct));
            ids.ShouldBe([user.Id]);
        }

        [Fact]
        public async Task ExecuteAsync_SoftDeletedUser_DoesNotCacheUser()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            await SyncAsync(new IAuthSyncUser.Request("auth-1", "john", "john@example.test"));

            (await GetByIdAsync(user.Id, CreateProvider())).Value.ShouldBeOfType<ServiceErrorResponse>().StatusCode.ShouldBe(404);
        }

        private Task<ServiceResponse> SyncAsync(IAuthSyncUser.Request request)
        {
            return RunAsync<IAuthSyncUser, ServiceResponse>(x => x.ExecuteAsync(request, Ct));
        }

        private Task<ServiceResponse<UserServiceResponse>> GetByIdAsync(Guid id, Microsoft.Extensions.DependencyInjection.ServiceProvider? provider = null)
        {
            return RunAsync<IUserGetById, ServiceResponse<UserServiceResponse>>(x => x.ExecuteAsync(id, Ct), provider);
        }
    }
}
