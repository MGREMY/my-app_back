using System.Net;
using System.Net.Http.Json;
using MyApp.Host.Api.Requests;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Services.Resources;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Endpoints;

public sealed partial class UserEndpointTests
{
    [FunctionalTest]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        private static readonly SortApiRequest[] ByUserName = [new("userName", false)];

        // QUERY /users

        [Fact]
        public async Task Query_AsUser_ReturnsRequestedPage()
        {
            await SeedUserAsync("c");
            await SeedUserAsync("a");
            await SeedUserAsync("b");

            using var response = await CreateClient().AsUser().QueryAsJsonAsync("/users", new PaginationApiRequest(1, 2, ByUserName), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.Data.Select(x => x.UserName).ShouldBe(["user-a", "user-b"]);
            page.Data[0].Email.ShouldBe("a@example.test");
            page.PageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(2);
            page.TotalPages.ShouldBe(2);
            page.HasNextPage.ShouldBeTrue();
            page.HasPreviousPage.ShouldBeFalse();
        }

        [Fact]
        public async Task Query_Filter_ReturnsMatchingUsersOnly()
        {
            await SeedUserAsync("alice");
            await SeedUserAsync("bob");
            var request = new PaginationApiRequest(FilterRequests:
                [new FilterApiRequest("userName", FilterApiRequest.Operator.Contains, "ALI", FilterApiRequest.Logic.And)]);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync("/users", request, Ct);

            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.Data.ShouldHaveSingleItem().UserName.ShouldBe("user-alice");
        }

        [Fact]
        public async Task Query_SoftDeletedUser_IsNotListed()
        {
            await SeedUserAsync("alive");
            await SeedUserAsync("gone", deleted: true);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync("/users", new PaginationApiRequest(), Ct);

            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.Data.ShouldHaveSingleItem().UserName.ShouldBe("user-alive");
        }

        [Fact]
        public async Task Query_InvalidPagination_Returns400ValidationProblem()
        {
            var request = new PaginationApiRequest(0, 15, [new SortApiRequest("", false)]);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync("/users", request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await ReadValidationProblemAsync(response);
            problem.Title.ShouldBe("Validation failed");
            problem.Errors.Keys.ShouldBe(["PageNumber", "SortRequests[0].PropertyName"], ignoreOrder: true);
        }

        [Fact]
        public async Task Query_UnknownFilterProperty_Returns400InvalidPaginationProblem()
        {
            var request = new PaginationApiRequest(FilterRequests:
                [new FilterApiRequest("unknown", FilterApiRequest.Operator.Equal, "x", FilterApiRequest.Logic.And)]);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync("/users", request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await ReadProblemAsync(response);
            problem.Title.ShouldBe("Invalid pagination request");
            problem.Detail.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Query_UnknownSortProperty_Returns400InvalidPaginationProblem()
        {
            using var response = await CreateClient().AsUser()
                .QueryAsJsonAsync("/users", new PaginationApiRequest(SortRequests: [new SortApiRequest("unknown", false)]), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Title.ShouldBe("Invalid pagination request");
        }

        [Fact]
        public async Task Query_UnparsableFilterValue_Returns400InvalidPaginationProblem()
        {
            var request = new PaginationApiRequest(FilterRequests:
                [new FilterApiRequest("createdAtUtc", FilterApiRequest.Operator.GreaterThan, "not-a-date", FilterApiRequest.Logic.And)]);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync("/users", request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Title.ShouldBe("Invalid pagination request");
        }

        [Fact]
        public async Task Query_Anonymous_Returns401()
        {
            using var response = await CreateClient().AsAnonymous().QueryAsJsonAsync("/users", new PaginationApiRequest(), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Post_ListRoute_Returns405()
        {
            // Paginated lists only answer the QUERY method.
            using var response = await CreateClient().AsUser().PostAsJsonAsync("/users", new PaginationApiRequest(), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        }

        // GET /users/{id}

        [Fact]
        public async Task GetById_ExistingUser_ReturnsUser()
        {
            var user = await SeedUserAsync("auth-1");

            using var response = await CreateClient().AsUser().GetAsync($"/users/{user.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var body = (await response.Content.ReadFromJsonAsync<UserResponse>(Ct)).ShouldNotBeNull();
            body.Id.ShouldBe(user.Id);
            body.UserName.ShouldBe(user.UserName);
            body.Email.ShouldBe(user.Email);
            body.CreatedAtUtc.ShouldBe(user.CreatedAtUtc, TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public async Task GetById_UnknownId_Returns404Problem()
        {
            using var response = await CreateClient().AsUser().GetAsync($"/users/{Guid.NewGuid()}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            var problem = await ReadProblemAsync(response);
            problem.Status.ShouldBe(404);
            problem.Title.ShouldBe(SharedResource.user_not_found);
        }

        [Fact]
        public async Task GetById_SoftDeletedUser_Returns404()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            using var response = await CreateClient().AsUser().GetAsync($"/users/{user.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_NotAGuid_Returns400()
        {
            using var response = await CreateClient().AsUser().GetAsync("/users/not-a-guid", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetById_Anonymous_Returns401()
        {
            var user = await SeedUserAsync("auth-1");

            using var response = await CreateClient().GetAsync($"/users/{user.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // DELETE /users/{id}

        [Fact]
        public async Task DeleteById_AsAdmin_SoftDeletesUserAndReturns204()
        {
            var user = await SeedUserAsync("auth-1");
            var client = CreateClient().AsAdmin();
            (await client.GetAsync($"/users/{user.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

            using var response = await client.DeleteAsync($"/users/{user.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem().IsDeleted.ShouldBeTrue();
            // The cached user is invalidated.
            (await client.GetAsync($"/users/{user.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteById_UnknownId_Returns404Problem()
        {
            using var response = await CreateClient().AsAdmin().DeleteAsync($"/users/{Guid.NewGuid()}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await ReadProblemAsync(response)).Title.ShouldBe(SharedResource.user_not_found);
        }

        [Fact]
        public async Task DeleteById_AlreadyDeletedUser_Returns404()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            using var response = await CreateClient().AsAdmin().DeleteAsync($"/users/{user.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteById_AsUserWithoutAdminRole_Returns403AndKeepsUser()
        {
            var user = await SeedUserAsync("auth-1");

            using var response = await CreateClient().AsUser("auth-2").DeleteAsync($"/users/{user.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem().IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task DeleteById_Anonymous_Returns401()
        {
            var user = await SeedUserAsync("auth-1");

            using var response = await CreateClient().AsAnonymous().DeleteAsync($"/users/{user.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
