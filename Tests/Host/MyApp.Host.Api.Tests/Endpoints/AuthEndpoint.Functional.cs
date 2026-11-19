using System.Net;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Services.Resources;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Endpoints;

public sealed partial class AuthEndpointTests
{
    [FunctionalTest]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        [Fact]
        public async Task SyncUser_NewUser_CreatesUserFromIdentityAndReturns204()
        {
            using var response = await CreateClient().AsUser("auth-1", "john", "john@example.test").PostAsync("/auth/sync-user", null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            var user = (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem();
            user.UserName.ShouldBe("john");
            user.Email.ShouldBe("john@example.test");
            user.IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task SyncUser_ExistingUser_Returns204WithoutDuplicate()
        {
            await SeedUserAsync("auth-1");

            using var response = await CreateClient().AsUser("auth-1").PostAsync("/auth/sync-user", null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem();
        }

        [Fact]
        public async Task SyncUser_ThenGetById_ReturnsSyncedUser()
        {
            var client = CreateClient().AsUser("auth-1", "john");
            await client.PostAsync("/auth/sync-user", null, Ct);
            var user = (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem();

            using var response = await client.GetAsync($"/users/{user.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SyncUser_DeletedUser_Returns403Problem()
        {
            await SeedUserAsync("auth-1", deleted: true);

            using var response = await CreateClient().AsUser("auth-1").PostAsync("/auth/sync-user", null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await ReadProblemAsync(response)).Title.ShouldBe(SharedResource.user_already_deleted);
        }

        [Fact]
        public async Task SyncUser_Anonymous_Returns401()
        {
            using var response = await CreateClient().AsAnonymous().PostAsync("/auth/sync-user", null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldBeEmpty();
        }
    }
}
