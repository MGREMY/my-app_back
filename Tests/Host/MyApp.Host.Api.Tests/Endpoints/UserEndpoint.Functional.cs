using Microsoft.EntityFrameworkCore;
using System.Net;
using MyApp.Host.Api.Requests;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Services;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;

namespace MyApp.Host.Api.Tests.Endpoints;

public sealed partial class UserEndpointTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        private const string Route = "/users";

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
            new(new PaginationApiRequest(FilterRequests: [InvalidFilter("userName", nested: [InvalidFilter("email"), InvalidFilter("")])]),
                "FilterRequests[0].Filters[1].PropertyName")
        ];

        private static readonly SortApiRequest[] ByUserName = [new("userName", false)];

        // QUERY /users

        [Fact]
        public async Task Query_AsUser_ReturnsRequestedPage()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().AsUser(TestUsers.Bob).QueryAsJsonAsync(Route, new PaginationApiRequest(1, 2, ByUserName), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.Data.Select(x => x.UserName).ShouldBe(["alice", "alina"]);
            page.Data[0].Email.ShouldBe(TestUsers.Alice.Email);
            page.PageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(2);
            page.TotalPages.ShouldBe(2);
            page.HasNextPage.ShouldBeTrue();
            page.HasPreviousPage.ShouldBeFalse();
        }

        [Fact]
        public async Task Query_Filter_ReturnsMatchingUsersOnly()
        {
            await SeedUsersAsync();
            // Page size 1: 2 pages when filtered (alice, alina), one per active user without filter.
            var request = new PaginationApiRequest(1, 1, ByUserName,
                [new FilterApiRequest("userName", FilterApiRequest.Operator.Contains, "ALI", FilterApiRequest.Logic.And)]);

            using var response = await CreateClient().AsUser(TestUsers.Bob).QueryAsJsonAsync(Route, request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var page = (await response.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(Ct)).ShouldNotBeNull();
            page.Data.ShouldHaveSingleItem().Id.ShouldBe(TestUsers.Alice.Id);
            page.TotalPages.ShouldBe(2);
            page.HasNextPage.ShouldBeTrue();
        }

        [Theory]
        [MemberData(nameof(InvalidPaginations))]
        public async Task Query_InvalidPagination_Returns400ValidationProblem(PaginationApiRequest request, string errorKey)
        {
            using var response = await CreateClient().AsUser(TestUsers.Alice).QueryAsJsonAsync(Route, request, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await ReadValidationProblemAsync(response);
            problem.Title.ShouldBe("Validation failed");
            problem.Errors.Keys.ShouldBe([errorKey]);
        }

        [Fact]
        public async Task Query_UnknownFilterProperty_Returns400InvalidPaginationProblem()
        {
            var request = new PaginationApiRequest(FilterRequests:
                [new FilterApiRequest("unknown", FilterApiRequest.Operator.Equal, "x", FilterApiRequest.Logic.And)]);

            using var response = await CreateClient().AsUser(TestUsers.Alice).QueryAsJsonAsync(Route, request, Ct);

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
            var user = (await SeedUsersAsync()).Single(x => x.Id == TestUsers.Bob.Id);

            using var response = await CreateClient().AsUser(TestUsers.Alice).GetAsync(ById(user.Id), Ct);

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
            await SeedUsersAsync();

            using var response = await CreateClient().AsUser(TestUsers.Alice).GetAsync(ById(Guid.NewGuid()), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await ReadServiceErrorAsync(response, ServiceConstant.Error.user_not_found)).Status.ShouldBe(404);
        }

        [Fact]
        public async Task GetById_NotAGuid_Returns400Problem()
        {
            using var response = await CreateClient().AsUser(TestUsers.Alice).GetAsync(ById("not-a-guid"), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Status.ShouldBe(400);
        }

        [Fact]
        public async Task GetById_Anonymous_Returns401()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().GetAsync(ById(TestUsers.Bob.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // DELETE /users/{id}

        [Fact]
        public async Task DeleteById_AsAdmin_SoftDeletesUserAndReturns204()
        {
            await SeedUsersAsync();
            var client = CreateClient().AsAdmin(TestUsers.Alice);
            (await client.GetAsync(ById(TestUsers.Bob.Id), Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

            using var response = await client.DeleteAsync(ById(TestUsers.Bob.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            (await FindUserIncludingDeletedAsync(TestUsers.Bob.Id)).ShouldNotBeNull().IsDeleted.ShouldBeTrue();
            // The cached user is invalidated.
            (await client.GetAsync(ById(TestUsers.Bob.Id), Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteById_UnknownId_Returns404Problem()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().AsAdmin(TestUsers.Alice).DeleteAsync(ById(Guid.NewGuid()), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            await ReadServiceErrorAsync(response, ServiceConstant.Error.user_not_found);
            (await WithDbAsync(db => db.Users.CountAsync(Ct))).ShouldBe(TestUsers.Active.Count);
        }

        [Fact]
        public async Task DeleteById_AlreadyDeletedUser_Returns404Problem()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().AsAdmin(TestUsers.Alice).DeleteAsync(ById(TestUsers.Dave.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            await ReadServiceErrorAsync(response, ServiceConstant.Error.user_not_found);
        }

        [Fact]
        public async Task DeleteById_AdminDeletingOwnAccount_Returns400ProblemAndKeepsUser()
        {
            await SeedUsersAsync();
            var admin = TestUsers.Alice;

            using var response = await CreateClient().AsAdmin(admin).DeleteAsync(ById(admin.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemAsync(response)).Status.ShouldBe(400);
            (await FindUserIncludingDeletedAsync(admin.Id)).ShouldNotBeNull().IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task DeleteById_AsUserWithoutAdminRole_Returns403AndKeepsUser()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().AsUser(TestUsers.Alice).DeleteAsync(ById(TestUsers.Bob.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await FindUserIncludingDeletedAsync(TestUsers.Bob.Id)).ShouldNotBeNull().IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task DeleteById_Anonymous_Returns401()
        {
            await SeedUsersAsync();

            using var response = await CreateClient().AsAnonymous().DeleteAsync(ById(TestUsers.Bob.Id), Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        private static string ById(object id) => $"{Route}/{id}";

        private static FilterApiRequest InvalidFilter(
            string propertyName,
            FilterApiRequest.Operator @operator = FilterApiRequest.Operator.Equal,
            FilterApiRequest.Logic logic = FilterApiRequest.Logic.And,
            IEnumerable<FilterApiRequest>? nested = null)
        {
            return new FilterApiRequest(propertyName, @operator, "x", logic, nested);
        }
    }
}
