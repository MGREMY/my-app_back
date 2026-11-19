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

        [Theory]
        [InlineData("name")]
        [InlineData("age")]
        [InlineData("followers")]
        [InlineData("balance")]
        [InlineData("score")]
        [InlineData("createdAt")]
        [InlineData("joinedAt")]
        [InlineData("birthDate")]
        [InlineData("externalId")]
        public async Task Sort_DescendingOnScalar_ReturnsReverseOrder(string path)
        {
            var names = await SortAsync(Desc(path));

            names.ShouldBe(All.Reverse());
        }

        [Fact]
        public async Task Sort_NullableProperty_PutsNullsLastWhenAscending()
        {
            var names = await SortAsync(Asc("rank"), Asc("name"));

            names.ShouldBe([Alice, Carol, Dave, Bob, Eve]);
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
