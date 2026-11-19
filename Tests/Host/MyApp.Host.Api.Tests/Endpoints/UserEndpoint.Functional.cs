using System.Net;
using System.Net.Http.Json;
using MyApp.Host.Api.Requests;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Services.Resources;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Endpoints;

public sealed partial class UserEndpointTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        private const string Route = "/users";

        private static readonly SortApiRequest[] ByUserName = [new("userName", false)];

        private static string ById(object id) => $"{Route}/{id}";

        // QUERY /users

        [Fact]
        public async Task Query_AsUser_ReturnsRequestedPage()
        {
            await SeedUserAsync("c");
            await SeedUserAsync("a");
            await SeedUserAsync("b");

            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, new PaginationApiRequest(1, 2, ByUserName), Ct);

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
            await SeedUserAsync("alina");
            await SeedUserAsync("bob");
            await SeedUserAsync("carl");
            // Page size 1: 2 pages when filtered (alice, alina), 4 without filter.
            var request = new PaginationApiRequest(1, 1, ByUserName,
                [new FilterApiRequest("userName", FilterApiRequest.Operator.Contains, "ALI", FilterApiRequest.Logic.And)]);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.Data.ShouldHaveSingleItem().UserName.ShouldBe("user-alice");
            page.TotalPages.ShouldBe(2);
            page.HasNextPage.ShouldBeTrue();
        }

        // One row per validation rule of PaginationApiRequest (and its child sort and filter requests).
        public static IEnumerable<TheoryDataRow<PaginationApiRequest, string>> InvalidPaginations =>
        [
            new(new PaginationApiRequest(PageNumber: 0), "PageNumber"),
            new(new PaginationApiRequest(PageSize: 0), "PageSize"),
            new(new PaginationApiRequest(SortRequests: [new SortApiRequest("id", false), new SortApiRequest(" ", false)]),
                "SortRequests[1].PropertyName"),
            new(new PaginationApiRequest(FilterRequests: [InvalidFilter(" ")]), "FilterRequests[0].PropertyName"),
            new(new PaginationApiRequest(FilterRequests: [InvalidFilter("userName", @operator: (FilterApiRequest.Operator)99)]),
                "FilterRequests[0].FilterOperator"),
            new(new PaginationApiRequest(FilterRequests: [InvalidFilter("userName", logic: (FilterApiRequest.Logic)2)]),
                "FilterRequests[0].FilterLogic"),
            // Bug: the validator only checks top-level filters, so an invalid nested filter reaches the generated code
            // and is not returned as a 400 validation problem.
            new(new PaginationApiRequest(FilterRequests: [InvalidFilter("userName", nested: [InvalidFilter("email"), InvalidFilter("")])]),
                "FilterRequests[0].Filters[1].PropertyName")
        ];

        [Theory]
        [MemberData(nameof(InvalidPaginations))]
        public async Task Query_InvalidPagination_Returns400ValidationProblem(PaginationApiRequest request, string errorKey)
        {
            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await ReadValidationProblemAsync(response);
            problem.Title.ShouldBe("Validation failed");
            problem.Errors.Keys.ShouldBe([errorKey]);
        }

        private static FilterApiRequest InvalidFilter(
            string propertyName,
            FilterApiRequest.Operator @operator = FilterApiRequest.Operator.Equal,
            FilterApiRequest.Logic logic = FilterApiRequest.Logic.And,
            IEnumerable<FilterApiRequest>? nested = null)
        {
            return new FilterApiRequest(propertyName, @operator, "x", logic, nested);
        }

        [Fact]
        public async Task Query_UnknownFilterProperty_Returns400InvalidPaginationProblem()
        {
            var request = new PaginationApiRequest(FilterRequests:
                [new FilterApiRequest("unknown", FilterApiRequest.Operator.Equal, "x", FilterApiRequest.Logic.And)]);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await ReadProblemAsync(response);
            problem.Title.ShouldBe("Invalid pagination request");
            problem.Detail.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Query_Anonymous_Returns401()
        {
            using var response = await CreateClient().AsAnonymous().QueryAsJsonAsync(Route, new PaginationApiRequest(), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // GET /users/{id}

        [Fact]
        public async Task GetById_ExistingUser_ReturnsUser()
        {
            var user = await SeedUserAsync("auth-1");

            using var response = await CreateClient().AsUser().GetAsync(ById(user.Id), Ct);

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
            using var response = await CreateClient().AsUser().GetAsync(ById(Guid.NewGuid()), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            var problem = await ReadProblemAsync(response);
            problem.Status.ShouldBe(404);
            problem.Title.ShouldBe(SharedResource.user_not_found);
        }

        // Bug: the route binding failure of a non-GUID id returns a 400 without body instead of an RFC 7807 problem
        // (application/problem+json).
        [Fact]
        public async Task GetById_NotAGuid_Returns400Problem()
        {
            using var response = await CreateClient().AsUser().GetAsync(ById("not-a-guid"), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Status.ShouldBe(400);
        }

        [Fact]
        public async Task GetById_Anonymous_Returns401()
        {
            var user = await SeedUserAsync("auth-1");

            using var response = await CreateClient().GetAsync(ById(user.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // DELETE /users/{id}

        [Fact]
        public async Task DeleteById_AsAdmin_SoftDeletesUserAndReturns204()
        {
            var user = await SeedUserAsync("auth-1");
            var client = CreateClient().AsAdmin();
            (await client.GetAsync(ById(user.Id), Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

            using var response = await client.DeleteAsync(ById(user.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem().IsDeleted.ShouldBeTrue();
            // The cached user is invalidated.
            (await client.GetAsync(ById(user.Id), Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteById_UnknownId_Returns404Problem()
        {
            using var response = await CreateClient().AsAdmin().DeleteAsync(ById(Guid.NewGuid()), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await ReadProblemAsync(response)).Title.ShouldBe(SharedResource.user_not_found);
        }

        // Bug: the soft deletion filter hides the deleted user, so DELETE returns 404 user_not_found instead of 400
        // user_already_deleted.
        [Fact]
        public async Task DeleteById_AlreadyDeletedUser_Returns400AlreadyDeletedProblem()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            using var response = await CreateClient().AsAdmin().DeleteAsync(ById(user.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Title.ShouldBe(SharedResource.user_already_deleted);
        }

        // Bug: nothing prevents an admin from deleting their own account: DELETE returns 204 and soft-deletes the
        // admin.
        [Fact]
        public async Task DeleteById_AdminDeletingOwnAccount_Returns400ProblemAndKeepsUser()
        {
            var admin = await SeedUserAsync("admin-1");

            using var response = await CreateClient().AsAdmin("admin-1").DeleteAsync(ById(admin.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Status.ShouldBe(400);
            (await FindUsersIncludingDeletedAsync("admin-1")).ShouldHaveSingleItem().IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task DeleteById_AsUserWithoutAdminRole_Returns403AndKeepsUser()
        {
            var user = await SeedUserAsync("auth-1");

            using var response = await CreateClient().AsUser("auth-2").DeleteAsync(ById(user.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await FindUsersIncludingDeletedAsync("auth-1")).ShouldHaveSingleItem().IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task DeleteById_Anonymous_Returns401()
        {
            var user = await SeedUserAsync("auth-1");

            using var response = await CreateClient().AsAnonymous().DeleteAsync(ById(user.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
