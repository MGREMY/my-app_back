using Microsoft.EntityFrameworkCore;
using MyApp.Services.Abstractions.Requests;
using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Generators.Tests.TestInfrastructure.Requests;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Paging;

/// <summary>Paging is applied after the sorts; the count covers the filtered rows, before paging.</summary>
public sealed partial class PagingTests
{
    [IntegrationTest]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        [Theory]
        [InlineData(1, 2, new[] { Alice, Bob })]
        [InlineData(2, 2, new[] { Carol, Dave })]
        [InlineData(3, 2, new[] { Eve })]
        [InlineData(4, 2, new string[0])]
        [InlineData(1, 10, new[] { Alice, Bob, Carol, Dave, Eve })]
        public async Task ProcessPaginationRequest_Page_ReturnsSortedPage(int pageNumber, int pageSize, string[] expected)
        {
            var names = await Db.Authors
                .ProcessPaginationRequest(Page(pageNumber, pageSize, [Asc("age")]), out _)
                .Select(x => x.Name)
                .ToListAsync(Ct);

            names.ShouldBe(expected);
        }

        [Fact]
        public async Task ProcessPaginationRequest_Count_CountsFilteredRowsBeforePaging()
        {
            var request = Page(1, 2, [Asc("age")], [Filter("age", FilterServiceRequest.Operator.GreaterThanOrEqual, "20")]);

            var names = await Db.Authors.ProcessPaginationRequest(request, out var countAsync).Select(x => x.Name).ToListAsync(Ct);
            var count = await countAsync(Ct);

            names.ShouldBe([Bob, Carol]);
            count.ShouldBe(4);
        }
    }
}
