using System.Net;
using System.Net.Http.Json;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Services.Resources;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Endpoints;

public sealed partial class AuthEndpointTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        private const string Route = "/auth";
        private const string SyncUserRoute = $"{Route}/sync-user";

        [Fact]
        public async Task SyncUser_NewUser_CreatesUserFromIdentityAndReturns204()
        {
            using var response = await CreateClient().AsUser("auth-1", "john", "john@example.test").PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            var user = (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem();
            user.UserName.ShouldBe("john");
            user.Email.ShouldBe("john@example.test");
            user.IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task SyncUser_ExistingUserWithChangedIdentity_KeepsStoredInfo()
        {
            // sync-user only inserts on the first call: it never updates an existing user.
            var seeded = await SeedUserAsync("auth-1", userName: "old-name");

            using var response = await CreateClient().AsUser("auth-1", "new-name", "new@example.test").PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            var user = (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem();
            user.UserName.ShouldBe("old-name");
            user.Email.ShouldBe(seeded.Email);
            user.UpdatedAtUtc.ShouldBeNull();
        }

        [Fact]
        public async Task SyncUser_TokenWithUsernameAndFullName_StoresTheUsername()
        {
            var identity = new TestUser("auth-1", "jdoe", "jdoe@example.test", [], DisplayName: "John Doe");

            using var response = await CreateClient().As(identity).PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem().UserName.ShouldBe("jdoe");
        }

        [Fact]
        public async Task SyncUser_TokenWithoutUsername_Returns400ProblemAndCreatesNothing()
        {
            var identity = new TestUser("auth-1", UserName: null, "jdoe@example.test", [], DisplayName: "John Doe");

            using var response = await CreateClient().As(identity).PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Status.ShouldBe(400);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldBeEmpty();
        }

        [Fact]
        public async Task SyncUser_TokenWithoutEmail_Returns400ProblemAndCreatesNothing()
        {
            var identity = new TestUser("auth-1", "jdoe", Email: null, []);

            using var response = await CreateClient().As(identity).PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Status.ShouldBe(400);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldBeEmpty();
        }

        [Fact]
        public async Task SyncUser_DeletedUser_Returns403Problem()
        {
            await SeedUserAsync("auth-1", deleted: true);

            using var response = await CreateClient().AsUser("auth-1").PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await ReadProblemAsync(response)).Title.ShouldBe(SharedResource.user_already_deleted);
        }

        [Fact]
        public async Task SyncUser_Anonymous_Returns401()
        {
            using var response = await CreateClient().AsAnonymous().PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldBeEmpty();
        }
    }
}
