using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>
/// Strings (nicknames: Alice "Ali", Bob null, Carol "", Dave "null", Eve "ALIEN"): case-sensitive Equal, case-insensitive
/// text operators, "" and "null" as ordinary values, negative operators never matching a null property, LIKE wildcards
/// matched literally.
/// </summary>
public sealed partial class StringFilterTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        public static TheoryData<string, Operator, string, string[]> Cases() => new()
        {
            { "nickname", Operator.Equal, "Ali", [Alice] },
            { "nickname", Operator.Equal, "ali", [] },
            { "nickname", Operator.NotEqual, "Ali", [Carol, Dave, Eve] },
            { "nickname", Operator.Contains, "LI", [Alice, Eve] },
            { "nickname", Operator.NotContains, "li", [Carol, Dave] },
            { "nickname", Operator.StartWith, "ali", [Alice, Eve] },
            { "nickname", Operator.EndWith, "En", [Eve] },
            { "nickname", Operator.Equal, "", [Carol] },
            { "nickname", Operator.Equal, "null", [Dave] },
            { "nickname", Operator.NotEqual, "null", [Alice, Carol, Eve] },
            { "nickname", Operator.Contains, "", [Alice, Carol, Dave, Eve] },
            { "name", Operator.NotEqual, "Alice", [Bob, Carol, Dave, Eve] },
            { "name", Operator.Contains, "a", [Alice, Carol, Dave] },
            { "name", Operator.NotContains, "E", [Bob, Carol] }
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Filter_StringOperator_ReturnsMatchingAuthors(string path, Operator op, string value, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, value));

            names.ShouldBe(expected);
        }

        // Mottos: Alice "50% off", Bob null, Carol "500 days", Dave "a_c", Eve "abc". Read as LIKE wildcards, "%" and "_"
        // would also match Carol and Eve.
        public static TheoryData<Operator, string, string[]> WildcardCases() => new()
        {
            { Operator.Contains, "50%", [Alice] },
            { Operator.Contains, "a_c", [Dave] },
            { Operator.StartWith, "50%", [Alice] },
            { Operator.StartWith, "a_", [Dave] },
            { Operator.EndWith, "_c", [Dave] },
            { Operator.EndWith, "0% off", [Alice] },
            { Operator.NotContains, "a_c", [Alice, Carol, Eve] },
            { Operator.NotContains, "50%", [Carol, Dave, Eve] }
        };

        [Theory]
        [MemberData(nameof(WildcardCases))]
        public async Task Filter_TextOperatorWithLikeWildcards_MatchesLiterally(Operator op, string value, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter("motto", op, value));

            names.ShouldBe(expected);
        }
    }
}
