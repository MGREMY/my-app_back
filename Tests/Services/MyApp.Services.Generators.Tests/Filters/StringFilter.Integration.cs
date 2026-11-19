using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>
/// Strings (nicknames: Alice "Ali", Bob null, Carol "", Dave "null", Eve "ALIEN"): case-sensitive Equal, case-insensitive
/// text operators, "" and "null" as ordinary values, negative operators never matching a null property.
/// </summary>
public sealed partial class StringFilterTests
{
    [IntegrationTest]
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

        [Theory]
        [InlineData("50%")]
        [InlineData("a_i")]
        public async Task Filter_ContainsWithLikeWildcards_MatchesLiterally(string value)
        {
            var names = await FilterAsync(Requests.Filter("nickname", Operator.Contains, value));

            names.ShouldBeEmpty();
        }
    }
}
