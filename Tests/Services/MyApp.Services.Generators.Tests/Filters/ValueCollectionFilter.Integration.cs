using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>
/// Collections of values, the item is the value. Tags (List&lt;string&gt;): Alice [ef, dotnet], Bob [], Carol [cooking],
/// Dave [EF, travel], Eve [music, ef]. Holidays (DateOnly[]): Alice [12-25], Bob [], Carol [07-14, 12-25],
/// Dave [01-01], Eve [] (2024).
/// </summary>
public sealed partial class ValueCollectionFilterTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        public static TheoryData<string, Operator, string, string[]> Cases() => new()
        {
            { "tags", Operator.Equal, "ef", [Alice, Eve] },
            { "tags", Operator.Contains, "EF", [Alice, Dave, Eve] },
            { "tags.$all", Operator.NotEqual, "ef", [Bob, Carol, Dave] },
            { "tags", Operator.NotContains, "e", [Carol, Eve] },
            { "tags.$none", Operator.Equal, "ef", [Bob, Carol, Dave] },
            { "tags.$all", Operator.Contains, "o", [Bob, Carol] },
            { "tags.$count", Operator.Equal, "0", [Bob] },
            { "tags.$count", Operator.GreaterThanOrEqual, "2", [Alice, Dave, Eve] },
            { "holidays", Operator.Equal, "2024-12-25", [Alice, Carol] },
            { "holidays", Operator.GreaterThan, "2024-06-01", [Alice, Carol] },
            { "holidays.$none", Operator.LessThan, "2024-06-01", [Alice, Bob, Carol, Eve] },
            { "holidays.$all", Operator.GreaterThanOrEqual, "2024-07-01", [Alice, Bob, Carol, Eve] },
            { "holidays.$all", Operator.NotEqual, "2024-12-25", [Bob, Dave, Eve] },
            { "holidays.$count", Operator.Equal, "0", [Bob, Eve] },
            // Through a reference: the mentors' tags
            { "mentor.tags", Operator.Equal, "ef", [Bob] }
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Filter_ValueCollection_ReturnsMatchingAuthors(string path, Operator op, string value, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, value));

            names.ShouldBe(expected);
        }

        // NotEqual without quantifier or with $none: Any(p => p != @value) on a value collection
        public static TheoryData<string, Operator, string, string[]> AnyNotEqualCases() => new()
        {
            { "tags", Operator.NotEqual, "ef", [Alice, Carol, Dave, Eve] },
            { "tags.$none", Operator.NotEqual, "ef", [Bob] },
            { "holidays", Operator.NotEqual, "2024-12-25", [Carol, Dave] }
        };

        // Bug: Any(p => p != @value) on a collection of values (text[], date[]) is not translated by EF Core / Npgsql:
        // InvalidOperationException (500) instead of a result
        [Theory]
        [MemberData(nameof(AnyNotEqualCases))]
        public async Task Filter_ValueCollectionAnyNotEqual_ReturnsMatchingAuthors(string path, Operator op, string value, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, value));

            names.ShouldBe(expected);
        }
    }
}
