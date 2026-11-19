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
        [Fact]
        public async Task Sort_Ascending_ReturnsAscendingOrder()
        {
            var names = await SortAsync(Asc("name"));

            names.ShouldBe(All);
        }

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

        [Fact]
        public async Task Sort_ThroughReference_JoinsTheNavigation()
        {
            var names = await SortAsync(Asc("Country.Name"), Asc("name"));

            names.ShouldBe([Eve, Alice, Dave, Carol, Bob]);
        }

        [Fact]
        public async Task Sort_DescendingThroughReference_PutsMissingReferenceFirst()
        {
            // Bob has no country: null first when descending, like a null column
            var names = await SortAsync(Desc("country.name"), Asc("name"));

            names.ShouldBe([Bob, Carol, Alice, Dave, Eve]);
        }

        [Fact]
        public async Task Sort_DescendingThroughSelfReference_FollowsTheMentor()
        {
            // Mentors: Eve -> Dave, Dave -> Carol, Carol -> Bob, Bob -> Alice, Alice none (first)
            var names = await SortAsync(Desc("mentor.name"));

            names.ShouldBe([Alice, Eve, Dave, Carol, Bob]);
        }

        [Fact]
        public async Task Sort_ThroughTwoReferences_JoinsBothNavigations()
        {
            var names = await SortAsync(Asc("country.continent.name"), Asc("name"));

            names.ShouldBe([Eve, Carol, Alice, Dave, Bob]);
        }

        [Fact]
        public async Task Sort_ThroughSelfReference_FollowsTheMentor()
        {
            var names = await SortAsync(Asc("mentor.name"));

            names.ShouldBe([Bob, Carol, Dave, Eve, Alice]);
        }

        [Fact]
        public async Task Sort_ThroughThreeSelfReferences_IsTheMaximumDepth()
        {
            // Dave -> Carol -> Bob -> Alice, Eve -> Dave -> Carol -> Bob, the others have no third mentor
            var names = await SortAsync(Asc("mentor.mentor.mentor.name"), Asc("name"));

            names.ShouldBe([Dave, Eve, Alice, Bob, Carol]);
        }
    }
}
