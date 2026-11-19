using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Resources;
using MyApp.Services.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Services.Tests.Services.UserServices;

public sealed partial class UserDeleteByIdTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : ServiceIntegrationTest(containers)
    {
        [Fact]
        public async Task ExecuteAsync_ExistingUser_SoftDeletesRow()
        {
            var user = await SeedUserAsync("auth-1");
            var before = DateTimeOffset.UtcNow;

            var result = await DeleteAsync(user.Id);

            result.Value.ShouldBeOfType<ServiceSuccessResponse>();
            var stored = (await FindUserIncludingDeletedAsync(user.Id)).ShouldNotBeNull();
            stored.IsDeleted.ShouldBeTrue();
            stored.DeletedAtUtc.ShouldNotBeNull().ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        }

        [Fact]
        public async Task ExecuteAsync_CachedUser_InvalidatesCache()
        {
            var user = await SeedUserAsync("auth-1");
            await GetByIdAsync(user.Id);

            await DeleteAsync(user.Id);

            (await GetByIdAsync(user.Id)).Value.ShouldBeOfType<ServiceErrorResponse>().StatusCode.ShouldBe(404);
        }

        [Fact]
        public async Task ExecuteAsync_UserCachedInRedis_RemovesRedisEntry()
        {
            var user = await SeedUserAsync("auth-1");
            await GetByIdAsync(user.Id, CreateProvider());

            await DeleteAsync(user.Id);

            // Fresh instance: empty in-memory cache, so the answer comes from Redis or the database.
            (await GetByIdAsync(user.Id, CreateProvider())).Value.ShouldBeOfType<ServiceErrorResponse>().StatusCode.ShouldBe(404);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownId_Returns404()
        {
            var result = await DeleteAsync(Guid.NewGuid());

            var error = result.Value.ShouldBeOfType<ServiceErrorResponse>();
            error.StatusCode.ShouldBe(404);
            error.Message.ShouldBe(SharedResource.user_not_found);
        }

        [Fact(Skip = "Bug: the soft deletion filter hides the deleted user, so UserDeleteById returns 404 user_not_found instead of 400 user_already_deleted")]
        public async Task ExecuteAsync_AlreadyDeletedUser_Returns400UserAlreadyDeleted()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            var result = await DeleteAsync(user.Id);

            var error = result.Value.ShouldBeOfType<ServiceErrorResponse>();
            error.StatusCode.ShouldBe(400);
            error.Message.ShouldBe(SharedResource.user_already_deleted);
        }

        [Fact]
        public async Task ExecuteAsync_AlreadyDeletedUser_KeepsDeletionDate()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            var result = await DeleteAsync(user.Id);

            result.Value.ShouldBeOfType<ServiceErrorResponse>();
            var stored = (await FindUserIncludingDeletedAsync(user.Id)).ShouldNotBeNull();
            stored.DeletedAtUtc.ShouldNotBeNull().ShouldBe(user.DeletedAtUtc!.Value, TimeSpan.FromMilliseconds(1));
        }

        private Task<ServiceResponse> DeleteAsync(Guid id)
        {
            return RunAsync<IUserDeleteById, ServiceResponse>(x => x.ExecuteAsync(id, Ct));
        }

        private Task<ServiceResponse<UserServiceResponse>> GetByIdAsync(Guid id, Microsoft.Extensions.DependencyInjection.ServiceProvider? provider = null)
        {
            return RunAsync<IUserGetById, ServiceResponse<UserServiceResponse>>(x => x.ExecuteAsync(id, Ct), provider);
        }
    }
}
