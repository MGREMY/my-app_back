using Microsoft.EntityFrameworkCore;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Resources;
using MyApp.Services.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;

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
            await SeedUsersAsync();
            var before = DateTimeOffset.UtcNow;

            var result = await DeleteAsync(TestUsers.Bob.Id);

            result.Value.ShouldBeOfType<ServiceSuccessResponse>();
            var stored = (await FindUserIncludingDeletedAsync(TestUsers.Bob.Id)).ShouldNotBeNull();
            stored.IsDeleted.ShouldBeTrue();
            stored.DeletedAtUtc.ShouldNotBeNull().ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
            // Only that user is deleted.
            (await WithDbAsync(db => db.Users.CountAsync(Ct))).ShouldBe(TestUsers.Active.Count - 1);
        }

        [Fact]
        public async Task ExecuteAsync_UserCachedInRedis_RemovesRedisEntry()
        {
            await SeedUsersAsync();
            await GetUserByIdAsync(TestUsers.Bob.Id, CreateProvider());

            await DeleteAsync(TestUsers.Bob.Id);

            // Fresh instance: empty in-memory cache, so the answer comes from Redis or the database.
            (await GetUserByIdAsync(TestUsers.Bob.Id, CreateProvider())).Value.ShouldBeOfType<ServiceErrorResponse>().StatusCode.ShouldBe(404);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownId_Returns404()
        {
            await SeedUsersAsync();

            var result = await DeleteAsync(Guid.NewGuid());

            var error = result.Value.ShouldBeOfType<ServiceErrorResponse>();
            error.StatusCode.ShouldBe(404);
            error.Message.ShouldBe(SharedResource.user_not_found);
            (await WithDbAsync(db => db.Users.CountAsync(Ct))).ShouldBe(TestUsers.Active.Count);
        }

        [Fact]
        public async Task ExecuteAsync_AlreadyDeletedUser_Returns400UserAlreadyDeleted()
        {
            await SeedUsersAsync();

            var result = await DeleteAsync(TestUsers.Dave.Id);

            var error = result.Value.ShouldBeOfType<ServiceErrorResponse>();
            error.StatusCode.ShouldBe(400);
            error.Message.ShouldBe(SharedResource.user_already_deleted);
        }

        [Fact]
        public async Task ExecuteAsync_AlreadyDeletedUser_KeepsDeletionDate()
        {
            var dave = (await SeedUsersAsync()).Single(x => x.Id == TestUsers.Dave.Id);

            var result = await DeleteAsync(dave.Id);

            result.Value.ShouldBeOfType<ServiceErrorResponse>();
            var stored = (await FindUserIncludingDeletedAsync(dave.Id)).ShouldNotBeNull();
            stored.DeletedAtUtc.ShouldNotBeNull().ShouldBe(dave.DeletedAtUtc!.Value, TimeSpan.FromMilliseconds(1));
        }

        private Task<ServiceResponse> DeleteAsync(Guid id)
        {
            return RunAsync<IUserDeleteById, ServiceResponse>(x => x.ExecuteAsync(id, Ct));
        }
    }
}
