using Microsoft.EntityFrameworkCore;
using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Generators.Tests.TestInfrastructure.Requests;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Sorts;

/// <summary>Sorts on scalars, chained with ThenBy, through reference navigations up to three levels.</summary>
public sealed partial class SortTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        // Followers and birthDate follow their own orders: a sort on the wrong column is detected
        public static TheoryData<string, string[]> ScalarOrders() => new()
        {
            { "name", All },
            { "age", All },
            { "followers", FollowersOrder },
            { "balance", All },
            { "score", All },
            { "createdAt", All },
            { "joinedAt", All },
            { "birthDate", BirthDateOrder },
            { "externalId", All }
        };

        [Theory]
        [MemberData(nameof(ScalarOrders))]
        public async Task Sort_AscendingOnScalar_ReturnsTheColumnOrder(string path, string[] expected)
        {
            var names = await SortAsync(Asc(path));

            names.ShouldBe(expected);
        }

        [Theory]
        [MemberData(nameof(ScalarOrders))]
        public async Task Sort_DescendingOnScalar_ReturnsReverseOrder(string path, string[] expected)
        {
            var names = await SortAsync(Desc(path));

            names.ShouldBe(expected.Reverse());
        }

        // Nulls follow PostgreSQL: last when ascending, first when descending (Bob and Eve have no rank / views)
        [Theory]
        [InlineData("rank", false, new[] { Alice, Carol, Dave, Bob, Eve })]
        [InlineData("rank", true, new[] { Bob, Eve, Dave, Carol, Alice })]
        [InlineData("views", false, new[] { Dave, Alice, Carol, Bob, Eve })]
        [InlineData("views", true, new[] { Bob, Eve, Carol, Alice, Dave })]
        public async Task Sort_NullableProperty_FollowsPostgreSqlNullsOrdering(string path, bool descending, string[] expected)
        {
            var names = await SortAsync(descending ? Desc(path) : Asc(path), Asc("name"));

            names.ShouldBe(expected);
        }

        [Fact]
        public async Task Sort_SeveralKeys_AppliesThemInOrder()
        {
            var names = await SortAsync(Desc("isActive"), Asc("name"));

            names.ShouldBe([Alice, Carol, Eve, Bob, Dave]);
        }

        [Fact]
        public async Task Sort_EnumThenDescendingName_AppliesBothDirections()
        {
            // Draft (Alice, Eve), Published (Bob, Dave), Archived (Carol)
            var names = await SortAsync(Asc("status"), Desc("name"));

            names.ShouldBe([Eve, Alice, Dave, Bob, Carol]);
        }

        [Fact]
        public async Task Sort_InheritedProperty_IsSortable()
        {
            var expected = await Db.Authors.OrderBy(x => x.Id).Select(x => x.Name).ToListAsync(Ct);

            var names = await SortAsync(Asc("id"));

            names.ShouldBe(expected);
        }

        // Bob has no country, Alice no mentor: a missing reference is ordered like a null column.
        // Mentors: Eve -> Dave, Dave -> Carol, Carol -> Bob, Bob -> Alice. Three mentors is the maximum depth.
        [Theory]
        [InlineData("Country.Name", false, new[] { Eve, Alice, Dave, Carol, Bob })]
        [InlineData("country.name", true, new[] { Bob, Carol, Alice, Dave, Eve })]
        [InlineData("country.continent.name", false, new[] { Eve, Carol, Alice, Dave, Bob })]
        [InlineData("mentor.name", false, new[] { Bob, Carol, Dave, Eve, Alice })]
        [InlineData("mentor.name", true, new[] { Alice, Eve, Dave, Carol, Bob })]
        [InlineData("mentor.mentor.mentor.name", false, new[] { Dave, Eve, Alice, Bob, Carol })]
        public async Task Sort_ThroughReferences_FollowsTheNavigations(string path, bool descending, string[] expected)
        {
            var names = await SortAsync(descending ? Desc(path) : Asc(path), Asc("name"));

            names.ShouldBe(expected);
        }
    }
}
