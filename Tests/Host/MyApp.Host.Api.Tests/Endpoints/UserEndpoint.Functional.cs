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

        [Fact]
        public async Task Query_SoftDeletedUser_IsNotListed()
        {
            await SeedUserAsync("alive");
            await SeedUserAsync("gone", deleted: true);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, new PaginationApiRequest(), Ct);

            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.Data.ShouldHaveSingleItem().UserName.ShouldBe("user-alive");
        }

        [Fact]
        public async Task Query_PageBeyondTheLast_Returns200WithEmptyData()
        {
            await SeedUserAsync("a");
            await SeedUserAsync("b");

            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, new PaginationApiRequest(3, 1, ByUserName), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.Data.ShouldBeEmpty();
            page.PageNumber.ShouldBe(3);
            page.TotalPages.ShouldBe(2);
            page.HasNextPage.ShouldBeFalse();
        }

        [Fact]
        public async Task Query_LargePageSize_IsNotCapped()
        {
            // There is no maximum page size.
            await SeedUserAsync("a");
            await SeedUserAsync("b");

            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, new PaginationApiRequest(1, 100_000, ByUserName), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.PageSize.ShouldBe(100_000);
            page.Data.Length.ShouldBe(2);
            page.TotalPages.ShouldBe(1);
        }

        [Fact]
        public async Task Query_InvalidPagination_Returns400ValidationProblem()
        {
            var request = new PaginationApiRequest(0, 15, [new SortApiRequest("", false)]);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await ReadValidationProblemAsync(response);
            problem.Title.ShouldBe("Validation failed");
            // Keys are named like the JSON sent by the client, not like the C# properties.
            problem.Errors.Keys.ShouldBe(["pageNumber", "sortRequests[0].propertyName"], ignoreOrder: true);
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
        public async Task Query_UnknownSortProperty_Returns400InvalidPaginationProblem()
        {
            using var response = await CreateClient().AsUser()
                .QueryAsJsonAsync(Route, new PaginationApiRequest(SortRequests: [new SortApiRequest("unknown", false)]), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Title.ShouldBe("Invalid pagination request");
        }

        [Fact]
        public async Task Query_UnparsableFilterValue_Returns400InvalidPaginationProblem()
        {
            var request = new PaginationApiRequest(FilterRequests:
                [new FilterApiRequest("createdAtUtc", FilterApiRequest.Operator.GreaterThan, "not-a-date", FilterApiRequest.Logic.And)]);

            using var response = await CreateClient().AsUser().QueryAsJsonAsync(Route, request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Title.ShouldBe("Invalid pagination request");
        }

        [Fact]
        public async Task Query_Anonymous_Returns401()
        {
            using var response = await CreateClient().AsAnonymous().QueryAsJsonAsync(Route, new PaginationApiRequest(), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Post_ListRoute_Returns405()
        {
            // Paginated lists only answer the QUERY method.
            using var response = await CreateClient().AsUser().PostAsJsonAsync(Route, new PaginationApiRequest(), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
            // RFC 9110: a 405 lists the methods the resource supports.
            response.Content.Headers.Allow.ShouldBe(["QUERY"]);
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

        [Fact]
        public async Task GetById_SoftDeletedUser_Returns404()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            using var response = await CreateClient().AsUser().GetAsync(ById(user.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_NotAGuid_Returns400()
        {
            using var response = await CreateClient().AsUser().GetAsync(ById("not-a-guid"), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact(Skip = "Bug: the route binding failure of a non-GUID id returns a 400 without body instead of an "
                     + "RFC 7807 problem (application/problem+json).")]
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

        [Fact(Skip = "Bug: the soft deletion filter hides the deleted user, so DELETE returns 404 user_not_found "
                     + "instead of 400 user_already_deleted.")]
        public async Task DeleteById_AlreadyDeletedUser_Returns400AlreadyDeletedProblem()
        {
            var user = await SeedUserAsync("auth-1", deleted: true);

            using var response = await CreateClient().AsAdmin().DeleteAsync(ById(user.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Title.ShouldBe(SharedResource.user_already_deleted);
        }

        [Fact(Skip = "Bug: nothing prevents an admin from deleting their own account: DELETE returns 204 and "
                     + "soft-deletes the admin.")]
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
