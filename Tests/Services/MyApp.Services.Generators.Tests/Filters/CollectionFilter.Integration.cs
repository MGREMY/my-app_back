using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>
/// Collections of entities, posts (likes) [tags]: Alice "Intro to EF" (10) [ef, intro] and "Advanced LINQ" (50) [linq],
/// Bob none, Carol "Cooking" (5) [], Dave "EF tips" (null) [ef] and "Travel" (20) [], Eve "Music" (30) [music, ef].
/// </summary>
public sealed partial class CollectionFilterTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        public static TheoryData<string, Operator, string, string[]> Cases() => new()
        {
            // Without quantifier: at least one item matches (EXISTS)
            { "posts.likes", Operator.GreaterThan, "25", [Alice, Eve] },
            { "posts.title", Operator.Equal, "Cooking", [Carol] },
            // Inverting the operator does not invert the meaning: at least one post is not "Cooking"
            { "posts.title", Operator.NotEqual, "Cooking", [Alice, Dave, Eve] },
            // $none: no item matches (NOT EXISTS)
            { "posts.$none.title", Operator.Equal, "Cooking", [Alice, Bob, Dave, Eve] },
            { "posts.$none.likes", Operator.GreaterThan, "25", [Bob, Carol, Dave] },
            // A null item never matches a negative operator, inside $none too
            { "posts.$none.likes", Operator.NotEqual, "20", [Bob, Dave] },
            // $all: every item matches, true on an empty collection (Bob)
            { "posts.$all.likes", Operator.GreaterThanOrEqual, "10", [Alice, Bob, Eve] },
            { "posts.$all.likes", Operator.NotEqual, "10", [Bob, Carol, Eve] },
            // $count: number of items
            { "posts.$count", Operator.Equal, "0", [Bob] },
            { "posts.$count", Operator.NotEqual, "1", [Alice, Bob, Dave] },
            { "posts.$count", Operator.GreaterThan, "1", [Alice, Dave] },
            { "posts.$count", Operator.LessThanOrEqual, "1", [Bob, Carol, Eve] },
            // Nested collections and nested quantifiers
            { "posts.tags.label", Operator.Equal, "ef", [Alice, Dave, Eve] },
            { "posts.tags.$count", Operator.GreaterThan, "1", [Alice, Eve] },
            { "posts.tags.$all.label", Operator.Equal, "ef", [Carol, Dave] },
            { "posts.$none.tags.label", Operator.Equal, "ef", [Bob, Carol] },
            { "posts.$all.tags.$none.label", Operator.Equal, "ef", [Bob, Carol] },
            // Back to the root entity (cycle), up to four navigations
            { "posts.author.name", Operator.Equal, Alice, [Alice] },
            { "posts.author.mentor.name", Operator.Equal, Carol, [Dave] },
            { "posts.author.posts.author.name", Operator.Equal, Eve, [Eve] }
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Filter_ThroughCollection_ReturnsMatchingAuthors(string path, Operator op, string value, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, value));

            names.ShouldBe(expected);
        }

        [Fact]
        public async Task Filter_AllCombinedWithCount_RequiresAtLeastOneItem()
        {
            var names = await FilterAsync(
                Requests.Filter("posts.$all.likes", Operator.GreaterThanOrEqual, "10"),
                Requests.Filter("posts.$count", Operator.GreaterThan, "0"));

            names.ShouldBe([Alice, Eve]);
        }
    }
}
