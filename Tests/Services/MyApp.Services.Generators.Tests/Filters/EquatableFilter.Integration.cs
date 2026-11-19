using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>bool, enums and Guid: Equal and NotEqual, on the non-nullable property and on its nullable twin.</summary>
public sealed partial class EquatableFilterTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        public static TheoryData<string, Operator, string, string[]> Cases() => new()
        {
            { "isActive", Operator.Equal, "true", [Alice, Carol, Eve] },
            { "isActive", Operator.NotEqual, "true", [Bob, Dave] },
            { "isActive", Operator.Equal, "False", [Bob, Dave] },
            { "isVerified", Operator.Equal, "true", [Alice, Dave] },
            { "isVerified", Operator.NotEqual, "true", [Carol] },
            { "status", Operator.Equal, "Published", [Bob, Dave] },
            { "status", Operator.Equal, "archived", [Carol] },
            { "status", Operator.NotEqual, "Draft", [Bob, Carol, Dave] },
            { "previousStatus", Operator.Equal, "Draft", [Alice] },
            { "previousStatus", Operator.NotEqual, "Draft", [Carol, Dave] },
            { "externalId", Operator.Equal, ExternalId(3).ToString(), [Carol] },
            { "externalId", Operator.NotEqual, ExternalId(3).ToString(), [Alice, Bob, Dave, Eve] },
            { "externalRef", Operator.Equal, ExternalId(1).ToString("B"), [Alice] },
            { "externalRef", Operator.NotEqual, ExternalId(1).ToString(), [Carol, Dave] }
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Filter_EquatableOperator_ReturnsMatchingAuthors(string path, Operator op, string value, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, value));

            names.ShouldBe(expected);
        }
    }
}
