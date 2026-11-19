using MyApp.Domain.Models.Models;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.AuthServices;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Resources;
using MyApp.Services.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

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
            var user = await SeedUserAsync("auth-1");

            var result = await GetByIdAsync(user.Id);

            var response = result.Value.ShouldBeOfType<UserServiceResponse>();
            response.Id.ShouldBe(user.Id);
            response.UserName.ShouldBe(user.UserName);
            response.Email.ShouldBe(user.Email);
            response.CreatedAtUtc.ShouldBe(user.CreatedAtUtc, TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public async Task ExecuteAsync_UnknownId_Returns404()
        {
            var result = await GetByIdAsync(Guid.NewGuid());

            var error = result.Value.ShouldBeOfType<ServiceErrorResponse>();
            error.StatusCode.ShouldBe(404);
            error.Message.ShouldBe(SharedResource.user_not_found);
        }

        [Fact]
        public async Task ExecuteAsync_SoftDeletedUser_Returns404()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            var result = await GetByIdAsync(user.Id);

            result.Value.ShouldBeOfType<ServiceErrorResponse>().StatusCode.ShouldBe(404);
        }

        [Fact]
        public async Task ExecuteAsync_SecondCall_ReturnsCachedUser()
        {
            var user = await SeedUserAsync("auth-1");
            await GetByIdAsync(user.Id);
            await RenameInDatabaseAsync(user.Id, "renamed");

            var result = await GetByIdAsync(user.Id);

            result.Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe(user.UserName);
        }

        [Fact]
        public async Task ExecuteAsync_CachedByAnotherInstance_ReturnsUserFromRedis()
        {
            var user = await SeedUserAsync("auth-1");
            await GetByIdAsync(user.Id);
            await RenameInDatabaseAsync(user.Id, "renamed");

            // New provider: empty in-memory cache, same Redis.
            var result = await GetByIdAsync(user.Id, CreateProvider());

            result.Value.ShouldBeOfType<UserServiceResponse>().UserName.ShouldBe(user.UserName);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownIdCreatedAfterFirstCall_KeepsReturning404FromCachedNull()
        {
            // Documents the current, accepted behaviour (AGENTS.md 6.8): GetOrCreateAsync caches the null result, so a
            // row added without going through a service stays invisible until the entry expires. This may change with
            // a future cache rework; update this test then.
            var user = await CacheNullThenInsertInDatabaseAsync();

            var result = await GetByIdAsync(user.Id);

            result.Value.ShouldBeOfType<ServiceErrorResponse>().StatusCode.ShouldBe(404);
        }

        [Fact]
        public async Task ExecuteAsync_CachedNullThenUserSynced_ReturnsUser()
        {
            // AGENTS.md 6.8: the service that creates the data (AuthSyncUser) must overwrite the cached null. The id
            // is generated on insert, so the null is cached for a row that is then put back in the database.
            var user = await CacheNullThenInsertInDatabaseAsync();

            await RunAsync<IAuthSyncUser, ServiceResponse>(x => x.ExecuteAsync(new IAuthSyncUser.Request(user.AuthId, user.UserName, user.Email), Ct));

            (await GetByIdAsync(user.Id)).Value.ShouldBeOfType<UserServiceResponse>().Id.ShouldBe(user.Id);
            (await GetByIdAsync(user.Id, CreateProvider())).Value.ShouldBeOfType<UserServiceResponse>().Id.ShouldBe(user.Id);
        }

        /// <summary>Caches a null result for the id of a user, then inserts that user in the database only.</summary>
        private async Task<User> CacheNullThenInsertInDatabaseAsync()
        {
            var user = await SeedUserAsync("auth-1");
            await WithDbAsync(async db =>
            {
                db.Users.Remove(user);
                return await db.SaveChangesAsync(Ct);
            });
            (await GetByIdAsync(user.Id)).Value.ShouldBeOfType<ServiceErrorResponse>();
            await WithDbAsync(async db =>
            {
                db.Users.Add(new() { Id = user.Id, AuthId = user.AuthId, UserName = user.UserName, Email = user.Email, CreatedAtUtc = user.CreatedAtUtc });
                return await db.SaveChangesAsync(Ct);
            });

            return user;
        }

        private Task<ServiceResponse<UserServiceResponse>> GetByIdAsync(Guid id, Microsoft.Extensions.DependencyInjection.ServiceProvider? provider = null)
        {
            return RunAsync<IUserGetById, ServiceResponse<UserServiceResponse>>(x => x.ExecuteAsync(id, Ct), provider);
        }
    }
}
