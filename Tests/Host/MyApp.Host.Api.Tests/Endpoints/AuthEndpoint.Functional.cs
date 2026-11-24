using System.Net;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Services;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;

namespace MyApp.Host.Api.Tests.Endpoints;

public sealed partial class AuthEndpointTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        private const string Route = "/auth";
        private const string SyncUserRoute = $"{Route}/sync-user";

        /// <summary>Identity of a first sign-in: not in <see cref="TestUsers"/>.</summary>
        private const string NewAuthId = "auth-new";

        [Fact]
        public async Task SyncUser_NewUser_CreatesUserFromIdentityAndReturns204()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().AsUser(NewAuthId, "john", "john@example.test").PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            (await CountUsersIncludingDeletedAsync()).ShouldBe(TestUsers.All.Count + 1);
            var user = (await FindUsersIncludingDeletedAsync(NewAuthId)).ShouldHaveSingleItem();
            user.UserName.ShouldBe("john");
            user.Email.ShouldBe("john@example.test");
            user.IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task SyncUser_ExistingUserWithChangedIdentity_KeepsStoredInfo()
        {
            // sync-user only inserts on the first call: it never updates an existing user.
            await SeedUsersAsync();
            var bob = TestUsers.Bob;

            using var response = await CreateClient().AsUser(bob.AuthId, "new-name", "new@example.test").PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            var user = (await FindUsersIncludingDeletedAsync(bob.AuthId)).ShouldHaveSingleItem();
            user.UserName.ShouldBe(bob.UserName);
            user.Email.ShouldBe(bob.Email);
            user.UpdatedAtUtc.ShouldBeNull();
        }

        [Fact]
        public async Task SyncUser_TokenWithUsernameAndFullName_StoresTheUsername()
        {
            await SeedUsersAsync();
            var identity = new TestUser(NewAuthId, "jdoe", "jdoe@example.test", [], DisplayName: "John Doe");

            using var response = await CreateClient().As(identity).PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            (await FindUsersIncludingDeletedAsync(NewAuthId)).ShouldHaveSingleItem().UserName.ShouldBe("jdoe");
        }

        [Fact]
        public async Task SyncUser_TokenWithoutUsername_Returns400ProblemAndCreatesNothing()
        {
            await SeedUsersAsync();
            var identity = new TestUser(NewAuthId, UserName: null, "jdoe@example.test", [], DisplayName: "John Doe");

            using var response = await CreateClient().As(identity).PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Status.ShouldBe(400);
            (await CountUsersIncludingDeletedAsync()).ShouldBe(TestUsers.All.Count);
        }

        [Fact]
        public async Task SyncUser_TokenWithoutEmail_Returns400ProblemAndCreatesNothing()
        {
            await SeedUsersAsync();
            var identity = new TestUser(NewAuthId, "jdoe", Email: null, []);

            using var response = await CreateClient().As(identity).PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Status.ShouldBe(400);
            (await CountUsersIncludingDeletedAsync()).ShouldBe(TestUsers.All.Count);
        }

        [Fact]
        public async Task SyncUser_DeletedUser_Returns403Problem()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().AsUser(TestUsers.Dave).PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            await ReadServiceErrorAsync(response, ServiceConstant.Error.user_already_deleted);
        }

        [Fact]
        public async Task SyncUser_Anonymous_Returns401()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().AsAnonymous().PostAsync(SyncUserRoute, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            (await CountUsersIncludingDeletedAsync()).ShouldBe(TestUsers.All.Count);
        }
    }
}
