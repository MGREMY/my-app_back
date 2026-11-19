using MyApp.Services.Abstractions.Requests;
using MyApp.Services.Generators.Tests.TestInfrastructure;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestInfrastructure.Requests;

namespace MyApp.Services.Generators.Tests.Errors;

/// <summary>
/// Properties of an unsupported type are ignored: the generated code compiles, and filters or sorts on them are
/// rejected like unknown paths. The model is compiled in memory and the generated code runs on LINQ to Objects.
/// </summary>
public sealed partial class UnsupportedTypeTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        private static string Model(string properties) =>
            $$"""
              namespace Sample;

              public class User
              {
                  public Guid Id { get; set; }
                  public string Name { get; set; } = "";
                  {{properties}}
              }

              [MyApp.Services.PaginationHandlerFor<User>]
              public sealed class UserHandler;
              """;

        [Theory]
        [InlineData("public System.Text.Json.JsonElement Metadata { get; set; }", "metadata", "{}")]
        [InlineData("public Dictionary<string, int> Counters { get; set; } = [];", "counters", "1")]
        [InlineData("public Dictionary<string, int> Counters { get; set; } = [];", "counters.key", "x")]
        [InlineData("public System.Uri? Website { get; set; }", "website", "https://alice.example/")]
        [InlineData("public System.Uri? Website { get; set; }", "website.host", "alice.example")]
        public void Generator_UnsupportedType_IsIgnored(string property, string path, string value)
        {
            AssertIgnored(GeneratorRunner.Run(Model(property)), path, value);
        }

        // Bug: a multidimensional array (int[,]) is treated as a collection of values: a filter on it throws
        // ArgumentException (500, Enumerable.Any cannot take an int[,]) instead of InvalidPaginationRequestException
        // (400)
        [Fact]
        public void Generator_MultidimensionalArray_IsIgnored()
        {
            // "1" is a valid int: a rejection means the path is unknown, not that the value is unparsable
            AssertIgnored(GeneratorRunner.Run(Model("public int[,] Grid { get; set; } = new int[0, 0];")), "grid", "1");
        }

        private static void AssertIgnored(GeneratorRunner.Result result, string path, string value)
        {
            result.CompilationErrors.ShouldBeEmpty();
            var user = result.Create("Sample.User", ("Name", "Alice"));

            Should.Throw<InvalidPaginationRequestException>(
                () => result.Paginate("Sample.User", [user], Page(filters: [Filter(path, Operator.Equal, value)])));
            Should.Throw<InvalidPaginationRequestException>(
                () => result.Paginate("Sample.User", [user], Page(sorts: [Asc(path)])));
            // The rest of the entity stays filterable
            result.Paginate("Sample.User", [user], Page(filters: [Filter("name", Operator.Equal, "Alice")])).ShouldBe([user]);
        }
    }
}
