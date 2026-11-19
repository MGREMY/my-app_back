using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>Filters are combined left to right without precedence; nested filters form a group.</summary>
public sealed partial class FilterLogicTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        [Fact]
        public async Task Filter_NoFilter_ReturnsEveryAuthor()
        {
            var names = await FilterAsync();

            names.ShouldBe(All);
        }

        [Fact]
        public async Task Filter_And_RequiresBothConditions()
        {
            var names = await FilterAsync(
                Requests.Filter("isActive", Operator.Equal, "true"),
                Requests.Filter("age", Operator.GreaterThanOrEqual, "30"));

            names.ShouldBe([Carol, Eve]);
        }

        [Fact]
        public async Task Filter_Or_AcceptsEitherCondition()
        {
            var names = await FilterAsync(
                Requests.Filter("name", Operator.Equal, Alice),
                Requests.Filter("name", Operator.Equal, Bob, Logic.Or));

            names.ShouldBe([Alice, Bob]);
        }

        [Fact]
        public async Task Filter_LogicOfFirstFilter_IsIgnored()
        {
            var names = await FilterAsync(Requests.Filter("name", Operator.Equal, Alice, Logic.Or));

            names.ShouldBe([Alice]);
        }

        [Fact]
        public async Task Filter_OrThenAnd_IsEvaluatedLeftToRight()
        {
            // (Alice OR Bob) AND age >= 20, not Alice OR (Bob AND age >= 20)
            var names = await FilterAsync(
                Requests.Filter("name", Operator.Equal, Alice),
                Requests.Filter("name", Operator.Equal, Bob, Logic.Or),
                Requests.Filter("age", Operator.GreaterThanOrEqual, "20"));

            names.ShouldBe([Bob]);
        }

        [Fact]
        public async Task Filter_NestedFilters_AreGrouped()
        {
            // Alice OR (Bob AND age >= 20)
            var names = await FilterAsync(
                Requests.Filter("name", Operator.Equal, Alice, Logic.And,
                    Requests.Filter("name", Operator.Equal, Bob, Logic.Or),
                    Requests.Filter("age", Operator.GreaterThanOrEqual, "20")));

            names.ShouldBe([Alice, Bob]);
        }

        [Theory]
        [InlineData(Logic.And, new[] { Bob })]
        [InlineData(Logic.Or, new[] { Alice, Bob, Carol, Dave, Eve })]
        public async Task Filter_NestedGroup_UsesLogicOfItsFirstFilter(Logic logic, string[] expected)
        {
            // age >= 20 <logic> (Alice OR Bob)
            var names = await FilterAsync(
                Requests.Filter("age", Operator.GreaterThanOrEqual, "20", Logic.And,
                    Requests.Filter("name", Operator.Equal, Alice, logic),
                    Requests.Filter("name", Operator.Equal, Bob, Logic.Or)));

            names.ShouldBe(expected);
        }
    }
}
