using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>
/// Reference navigations become joins (countries: Alice and Dave France, Carol Japan, Eve Canada, Bob none; mentors:
/// Bob -> Alice -> nobody, Carol -> Bob, Dave -> Carol, Eve -> Dave).
/// </summary>
public sealed partial class ReferenceFilterTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        public static TheoryData<string, Operator, string, string[]> Cases() => new()
        {
            { "country.name", Operator.Equal, "France", [Alice, Dave] },
            { "country.continent.name", Operator.Equal, "Asia", [Carol] },
            { "country.continent.name", Operator.StartWith, "a", [Carol, Eve] },
            { "mentor.name", Operator.Equal, Alice, [Bob] },
            { "mentor.isActive", Operator.Equal, "true", [Bob, Dave] },
            { "mentor.age", Operator.GreaterThanOrEqual, "30", [Dave, Eve] },
            { "mentor.mentor.name", Operator.Equal, Alice, [Carol] },
            // Four navigations: the maximum depth
            { "mentor.mentor.mentor.mentor.name", Operator.Equal, Alice, [Eve] }
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Filter_ThroughReference_ReturnsMatchingAuthors(string path, Operator op, string value, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, value));

            names.ShouldBe(expected);
        }

        // Bug: a negative operator on a non-nullable property reached through a missing optional reference matches (C#
        // null semantics), contrary to AGENTS.md 6.5: Bob without country is returned
        [Fact]
        public async Task Filter_NotEqualThroughMissingReference_DoesNotMatch()
        {
            // Bob has no country: like a null property, a missing navigation never matches a negative operator
            var names = await FilterAsync(Requests.Filter("country.name", Operator.NotEqual, "France"));

            names.ShouldBe([Carol, Eve]);
        }

        // Bug: a negative operator on a non-nullable property reached through a missing optional reference matches (C#
        // null semantics), contrary to AGENTS.md 6.5: Bob without country is returned
        [Fact]
        public async Task Filter_NotContainsThroughMissingReference_DoesNotMatch()
        {
            var names = await FilterAsync(Requests.Filter("country.name", Operator.NotContains, "fr"));

            names.ShouldBe([Carol, Eve]);
        }

        [Fact]
        public async Task Filter_NotEqualThroughMissingReferenceOnNullable_DoesNotMatch()
        {
            // Nullable property: the null check of negative operators also covers the missing mentor (Alice)
            var names = await FilterAsync(Requests.Filter("mentor.rank", Operator.NotEqual, "10"));

            names.ShouldBe([Dave, Eve]);
        }
    }
}
