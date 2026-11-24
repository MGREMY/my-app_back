using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Requests;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;

namespace MyApp.Services.Tests.Services.UserServices;

public sealed partial class UserGetTests
{
    /// <summary>Pagination basics through the service; the filter/sort matrix belongs to the generator tests.</summary>
    /// <param name="containers">Containers of the assembly, reset before each test.</param>
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : ServiceIntegrationTest(containers)
    {
        private static readonly SortServiceRequest[] ByUserName = [new("userName", false)];

        [Fact]
        public async Task ExecuteAsync_FirstPage_ReturnsPageAndMetadata()
        {
            await SeedUsersAsync();

            var page = await GetAsync(new PaginationServiceRequest(1, 2, ByUserName, []));

            page.Data.Select(x => x.UserName).ShouldBe(["alice", "alina"]);
            page.PageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(2);
            page.TotalPages.ShouldBe(2);
            page.HasPreviousPage.ShouldBeFalse();
            page.HasNextPage.ShouldBeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_LastPage_HasPreviousButNoNextPage()
        {
            await SeedUsersAsync();

            var page = await GetAsync(new PaginationServiceRequest(2, 3, ByUserName, []));

            page.Data.Select(x => x.UserName).ShouldBe(["carl"]);
            page.TotalPages.ShouldBe(2);
            page.HasPreviousPage.ShouldBeTrue();
            page.HasNextPage.ShouldBeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_EmptyTable_ReturnsNoPage()
        {
            var page = await GetAsync(new PaginationServiceRequest(1, 10, [], []));

            page.Data.ShouldBeEmpty();
            page.TotalPages.ShouldBe(0);
            page.HasNextPage.ShouldBeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_SoftDeletedUsers_AreExcluded()
        {
            await SeedUsersAsync();

            var page = await GetAsync(new PaginationServiceRequest(1, 10, ByUserName, []));

            page.Data.Select(x => x.UserName).ShouldBe(["alice", "alina", "bob", "carl"]);
            page.Data.Count().ShouldBe(TestUsers.Active.Count);
        }

        [Fact]
        public async Task ExecuteAsync_Filter_CountsAndReturnsMatchingUsersOnly()
        {
            await SeedUsersAsync();
            var filter = new FilterServiceRequest("email", FilterServiceRequest.Operator.EndWith, "b@example.test", FilterServiceRequest.Logic.And, []);

            // Page size 1: an unfiltered count (every active user) would give several pages and a next page.
            var page = await GetAsync(new PaginationServiceRequest(1, 1, ByUserName, [filter]));

            page.Data.ShouldHaveSingleItem().Id.ShouldBe(TestUsers.Bob.Id);
            page.TotalPages.ShouldBe(1);
            page.HasNextPage.ShouldBeFalse();
        }

        private async Task<PaginationServiceResponse<UserServiceResponse>> GetAsync(PaginationServiceRequest request)
        {
            var result = await RunAsync<IUserGet, ServiceResponse<PaginationServiceResponse<UserServiceResponse>>>(
                x => x.ExecuteAsync(new IUserGet.Request(request), Ct));

            return result.Value.ShouldBeOfType<PaginationServiceResponse<UserServiceResponse>>();
        }
    }
}
