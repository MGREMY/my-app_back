using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>A JSON null value targets NULL: Equal is IS NULL, NotEqual is IS NOT NULL, on every nullable type.</summary>
public sealed partial class NullValueFilterTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        // Bob and Eve have every nullable twin set to null
        public static TheoryData<string> NullablePaths() =>
        [
            "isVerified", "previousStatus", "externalRef", "rank", "views", "bonus", "rating", "updatedAt", "lastSeenAt",
            "deathDate"
        ];

        [Theory]
        [MemberData(nameof(NullablePaths))]
        public async Task Filter_EqualNull_ReturnsNullRows(string path)
        {
            var names = await FilterAsync(Requests.Filter(path, Operator.Equal, null));

            names.ShouldBe([Bob, Eve]);
        }

        [Theory]
        [MemberData(nameof(NullablePaths))]
        public async Task Filter_NotEqualNull_ReturnsNonNullRows(string path)
        {
            var names = await FilterAsync(Requests.Filter(path, Operator.NotEqual, null));

            names.ShouldBe([Alice, Carol, Dave]);
        }

        [Fact]
        public async Task Filter_NullableStringEqualNull_ReturnsNullRowsOnly()
        {
            var names = await FilterAsync(Requests.Filter("nickname", Operator.Equal, null));

            names.ShouldBe([Bob]);
        }

        [Fact]
        public async Task Filter_NullableStringNotEqualNull_KeepsEmptyAndNullStrings()
        {
            var names = await FilterAsync(Requests.Filter("nickname", Operator.NotEqual, null));

            names.ShouldBe([Alice, Carol, Dave, Eve]);
        }

        // Rows without reference (Bob has no country, Alice no mentor): the foreign key, the navigation itself and a
        // property through the navigation all target the same rows
        public static TheoryData<string, Operator, string[]> MissingReferenceByForeignKey() => new()
        {
            { "countryId", Operator.Equal, [Bob] },
            { "countryId", Operator.NotEqual, [Alice, Carol, Dave, Eve] },
            { "mentorId", Operator.Equal, [Alice] }
        };

        [Theory]
        [MemberData(nameof(MissingReferenceByForeignKey))]
        public async Task Filter_ForeignKeyNull_TargetsRowsWithoutReference(string path, Operator op, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, null));

            names.ShouldBe(expected);
        }

        public static TheoryData<string, Operator, string[]> MissingReferenceByNavigation() => new()
        {
            { "country", Operator.Equal, [Bob] },
            { "country", Operator.NotEqual, [Alice, Carol, Dave, Eve] },
            { "country.name", Operator.Equal, [Bob] },
            { "country.name", Operator.NotEqual, [Alice, Carol, Dave, Eve] },
            { "country.continent.name", Operator.Equal, [Bob] },
            { "mentor", Operator.Equal, [Alice] }
        };

        // Bug: a null value on a navigation ("country") or on a non-nullable property through an optional reference
        // ("country.name") is rejected with InvalidPaginationRequestException instead of targeting the rows without
        // reference
        [Theory]
        [MemberData(nameof(MissingReferenceByNavigation))]
        public async Task Filter_NavigationNull_TargetsRowsWithoutReference(string path, Operator op, string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, null));

            names.ShouldBe(expected);
        }

        [Fact]
        public async Task Filter_OptionalReferenceEqualNull_TargetsMissingNavigation()
        {
            // Alice has no mentor, Carol's mentor (Bob) has no rank
            var names = await FilterAsync(Requests.Filter("mentor.rank", Operator.Equal, null));

            names.ShouldBe([Alice, Carol]);
        }
    }
}
