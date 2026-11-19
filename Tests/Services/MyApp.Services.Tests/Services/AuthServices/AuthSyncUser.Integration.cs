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
    [Trait(TestCategory.Name, TestCategory.Integration)]
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

        [Fact]
        public async Task ExecuteAsync_ExistingAuthIdWithNewProfile_DoesNotModifyUser()
        {
            // The sync only inserts a user the first time: an existing user's profile is never updated from the request.
            var user = await SeedUserAsync("auth-1");

            var result = await SyncAsync(new IAuthSyncUser.Request("auth-1", "new-name", "new@example.test"));

            result.Value.ShouldBeOfType<ServiceSuccessResponse>();
            var stored = (await FindUserIncludingDeletedAsync(user.Id)).ShouldNotBeNull();
            stored.UserName.ShouldBe(user.UserName);
            stored.Email.ShouldBe(user.Email);
            stored.HasBeenModified.ShouldBeFalse();
            stored.UpdatedAtUtc.ShouldBeNull();
            (await GetByIdAsync(user.Id, CreateProvider())).Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe(user.UserName);
        }

        [Fact(Skip = "Bug: non-deterministic race, concurrent first syncs of the same AuthId all try to insert and the losers throw DbUpdateException (unique index on auth_id) instead of returning a 409")]
        public async Task ExecuteAsync_ConcurrentFirstSyncOfSameAuthId_CreatesOneUserAndReturns409ToOthers()
        {
            const int parallelCalls = 10;
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = Enumerable.Range(0, parallelCalls)
                .Select(_ => Task.Run(async () =>
                {
                    await start.Task;
                    try
                    {
                        return (await SyncAsync(new IAuthSyncUser.Request("auth-1", "john", "john@example.test"))).Value;
                    }
                    catch (Exception exception)
                    {
                        // Collected instead of rethrown so the assertion lists what every call did.
                        return (object?)exception;
                    }
                }, Ct))
                .ToArray();

            start.SetResult();
            var outcomes = await Task.WhenAll(calls);

            outcomes.OfType<Exception>().Select(x => x.GetType().Name).ShouldBeEmpty();
            outcomes.OfType<ServiceSuccessResponse>().ShouldNotBeEmpty();
            outcomes.Where(x => x is not ServiceSuccessResponse)
                .ShouldAllBe(x => x is ServiceErrorResponse && ((ServiceErrorResponse)x).StatusCode == 409);
            (await WithDbAsync(db => db.Users.CountAsync(x => x.AuthId == "auth-1", Ct))).ShouldBe(1);
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
