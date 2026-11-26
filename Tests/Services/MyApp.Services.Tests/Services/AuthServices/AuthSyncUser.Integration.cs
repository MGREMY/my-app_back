using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Models;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.AuthServices;
using MyApp.Services.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;

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
            await SeedUsersAsync();
            var before = DateTimeOffset.UtcNow;

            var result = await SyncAsync(new IAuthSyncUser.Request("auth-new", "john", "john@example.test"));

            result.Value.ShouldBeOfType<ServiceSuccessResponse>();
            (await CountUsersIncludingDeletedAsync()).ShouldBe(TestUsers.All.Count + 1);
            var user = await WithDbAsync(db => db.Users.AsNoTracking().SingleAsync(x => x.AuthId == "auth-new", Ct));
            user.UserName.ShouldBe("john");
            user.Email.ShouldBe("john@example.test");
            user.Id.ShouldNotBe(Guid.Empty);
            user.CreatedAtUtc.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
            user.HasBeenModified.ShouldBeFalse();
            user.IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_ExistingAuthId_DoesNotCreateAnotherUser()
        {
            await SeedUsersAsync();
            var bob = TestUsers.Bob;

            var result = await SyncAsync(new IAuthSyncUser.Request(bob.AuthId, bob.UserName, bob.Email));

            result.Value.ShouldBeOfType<ServiceSuccessResponse>();
            (await WithDbAsync(db => db.Users.Where(x => x.AuthId == bob.AuthId).Select(x => x.Id).ToListAsync(Ct))).ShouldBe([bob.Id]);
            (await WithDbAsync(db => db.Users.CountAsync(Ct))).ShouldBe(TestUsers.Active.Count);
        }

        [Fact]
        public async Task ExecuteAsync_ExistingAuthIdWithNewProfile_DoesNotModifyUser()
        {
            // The sync only inserts a user the first time: an existing user's profile is never updated from the request.
            await SeedUsersAsync();
            var bob = TestUsers.Bob;

            var result = await SyncAsync(new IAuthSyncUser.Request(bob.AuthId, "new-name", "new@example.test"));

            result.Value.ShouldBeOfType<ServiceSuccessResponse>();
            var stored = (await FindUserIncludingDeletedAsync(bob.Id)).ShouldNotBeNull();
            stored.UserName.ShouldBe(bob.UserName);
            stored.Email.ShouldBe(bob.Email);
            stored.HasBeenModified.ShouldBeFalse();
            stored.UpdatedAtUtc.ShouldBeNull();
            (await GetUserByIdAsync(bob.Id, CreateProvider())).Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe(bob.UserName);
        }

        [Fact]
        public async Task ExecuteAsync_ConcurrentFirstSyncOfSameAuthId_CreatesOneUserAndReturns409ToOthers()
        {
            const int parallelCalls = 10;
            // Every call reads "no user" before any of them inserts: the race happens on every run, not by chance.
            var barrier = new ReadBarrierInterceptor("auth_id", parallelCalls);
            var racingProvider = CreateProvider(services => services.ConfigureDbContext<AppDbContext>(o => o.AddInterceptors(barrier)));

            var outcomes = await Task.WhenAll(Enumerable.Range(0, parallelCalls).Select(_ => Task.Run(async () =>
            {
                try
                {
                    return (await SyncAsync(new IAuthSyncUser.Request("auth-1", "john", "john@example.test"), racingProvider)).Value;
                }
                catch (Exception exception)
                {
                    // Collected instead of rethrown so the assertion lists what every call did.
                    return exception;
                }
            }, Ct)));

            barrier.Reads.ShouldBe(parallelCalls);
            outcomes.OfType<Exception>().Select(x => x.GetType().Name).ShouldBeEmpty();
            outcomes.OfType<ServiceSuccessResponse>().ShouldHaveSingleItem();
            outcomes.Where(x => x is not ServiceSuccessResponse)
                .ShouldAllBe(x => x is ServiceErrorResponse && ((ServiceErrorResponse)x).Value is ConflictError);
            (await WithDbAsync(db => db.Users.CountAsync(x => x.AuthId == "auth-1", Ct))).ShouldBe(1);
        }

        [Fact]
        public async Task ExecuteAsync_SoftDeletedUser_ReturnsForbiddenWithoutRecreatingUser()
        {
            await SeedUsersAsync();
            var dave = TestUsers.Dave;

            var result = await SyncAsync(new IAuthSyncUser.Request(dave.AuthId, dave.UserName, dave.Email));

            result.Value.ShouldBeServiceError<ForbiddenError>().Code.ShouldBe(ServiceConstant.Error.user_already_deleted);
            var ids = await WithDbAsync(db => db.Users
                .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
                .Where(x => x.AuthId == dave.AuthId)
                .Select(x => x.Id)
                .ToListAsync(Ct));
            ids.ShouldBe([dave.Id]);
        }

        private Task<ServiceResponse> SyncAsync(IAuthSyncUser.Request request, ServiceProvider? provider = null)
        {
            return RunAsync<IAuthSyncUser, ServiceResponse>(x => x.ExecuteAsync(request, Ct), provider);
        }
    }
}
