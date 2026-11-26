using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.AuthServices;
using MyApp.Services.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;

namespace MyApp.Services.Tests.Services.UserServices;

public sealed partial class UserGetByIdTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : ServiceIntegrationTest(containers)
    {
        [Fact]
        public async Task ExecuteAsync_ExistingUser_ReturnsUser()
        {
            var bob = (await SeedUsersAsync()).Single(x => x.Id == TestUsers.Bob.Id);

            var result = await GetUserByIdAsync(bob.Id);

            var response = result.Value.ShouldBeOfType<UserServiceResponse>();
            response.Id.ShouldBe(bob.Id);
            response.UserName.ShouldBe(bob.UserName);
            response.Email.ShouldBe(bob.Email);
            response.CreatedAtUtc.ShouldBe(bob.CreatedAtUtc, TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public async Task ExecuteAsync_UnknownId_ReturnsNotFound()
        {
            await SeedUsersAsync();

            var result = await GetUserByIdAsync(Guid.NewGuid());

            result.Value.ShouldBeServiceError<NotFoundError>().Code.ShouldBe(ServiceConstant.Error.user_not_found);
        }

        [Fact]
        public async Task ExecuteAsync_SoftDeletedUser_ReturnsNotFound()
        {
            await SeedUsersAsync();

            var result = await GetUserByIdAsync(TestUsers.Dave.Id);

            result.Value.ShouldBeServiceError<NotFoundError>().Code.ShouldBe(ServiceConstant.Error.user_not_found);
        }

        [Fact]
        public async Task ExecuteAsync_SecondCall_ReturnsCachedUser()
        {
            await SeedUsersAsync();
            await GetUserByIdAsync(TestUsers.Bob.Id);
            await RenameInDatabaseAsync(TestUsers.Bob.Id, "renamed");

            var result = await GetUserByIdAsync(TestUsers.Bob.Id);

            result.Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe(TestUsers.Bob.UserName);
        }

        [Fact]
        public async Task ExecuteAsync_CachedByAnotherInstance_ReturnsUserFromRedis()
        {
            await SeedUsersAsync();
            await GetUserByIdAsync(TestUsers.Bob.Id);
            await RenameInDatabaseAsync(TestUsers.Bob.Id, "renamed");

            // New provider: empty in-memory cache, same Redis.
            var result = await GetUserByIdAsync(TestUsers.Bob.Id, CreateProvider());

            result.Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe(TestUsers.Bob.UserName);
        }

        [Fact]
        public async Task ExecuteAsync_CachedNullThenUserSynced_ReturnsUser()
        {
            // docs/archi/cache.md: the service that creates the data (AuthSyncUser) must overwrite the cached null. The read
            // happens before the users are inserted (directly in the database), so a null is cached for an existing id.
            var bob = TestUsers.Bob;
            (await GetUserByIdAsync(bob.Id)).Value.ShouldBeServiceError<NotFoundError>();
            await SeedUsersAsync();

            await RunAsync<IAuthSyncUser, ServiceResponse>(x => x.ExecuteAsync(new IAuthSyncUser.Request(bob.AuthId, bob.UserName, bob.Email), Ct));

            (await GetUserByIdAsync(bob.Id)).Value.ShouldBeOfType<UserServiceResponse>().Id.ShouldBe(bob.Id);
            (await GetUserByIdAsync(bob.Id, CreateProvider())).Value.ShouldBeOfType<UserServiceResponse>().Id.ShouldBe(bob.Id);
        }
    }
}
