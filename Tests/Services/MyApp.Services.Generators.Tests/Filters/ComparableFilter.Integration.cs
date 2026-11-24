using System.Globalization;
using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>Numbers and dates: every operator, on the non-nullable property and on its nullable twin.</summary>
public sealed partial class ComparableFilterTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        // (non-nullable path, nullable path, authors in ascending order, value of the middle author of that order).
        // Bob and Eve have null twins.
        private static readonly (string Path, string NullablePath, string[] Order, string MiddleValue)[] Types =
        [
            ("age", "rank", All, "30"),
            ("followers", "views", FollowersOrder, "3000000000000"),
            ("balance", "bonus", All, "3.25"),
            ("score", "rating", All, "3.5"),
            ("createdAt", "updatedAt", All, "2024-01-03T10:00:00Z"),
            ("joinedAt", "lastSeenAt", All, "2024-03-01T08:00:00+00:00"),
            ("birthDate", "deathDate", BirthDateOrder, "1990-01-03")
        ];

        // Authors expected when comparing to the middle author (index 2) of the ascending order
        private static readonly (Operator Operator, Func<string[], IEnumerable<string>> Expected)[] Operators =
        [
            (Operator.Equal, order => order[2..3]),
            (Operator.NotEqual, order => order.Where((_, i) => i != 2)),
            (Operator.GreaterThan, order => order[3..]),
            (Operator.GreaterThanOrEqual, order => order[2..]),
            (Operator.LessThan, order => order[..2]),
            (Operator.LessThanOrEqual, order => order[..3])
        ];

        public static TheoryData<string, Operator, string, string[]> Matrix()
        {
            var data = new TheoryData<string, Operator, string, string[]>();
            foreach (var (path, nullablePath, order, value) in Types)
            foreach (var (op, expected) in Operators)
            {
                var names = expected(order).Order(StringComparer.Ordinal).ToArray();
                data.Add(path, op, value, names);
                data.Add(nullablePath, op, value, names.Except([Bob, Eve]).ToArray());
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Matrix))]
        public async Task Filter_ComparableOperator_ReturnsMatchingAuthors(string path, Operator op, string value,
            string[] expected)
        {
            var names = await FilterAsync(Requests.Filter(path, op, value));

            names.ShouldBe(expected);
        }

        [Theory]
        [InlineData("joinedAt", "2024-03-01T10:00:00+02:00")]
        [InlineData("joinedAt", "2024-03-01T08:00:00")]
        [InlineData("createdAt", "2024-01-03T12:00:00+02:00")]
        [InlineData("createdAt", "2024-01-03T10:00:00")]
        public async Task Filter_DateWithOffsetOrWithoutZone_IsComparedInUtc(string path, string value)
        {
            var names = await FilterAsync(Requests.Filter(path, Operator.Equal, value));

            names.ShouldBe([Carol]);
        }

        [Fact]
        public async Task Filter_LongBeyondInt32_IsParsedAsLong()
        {
            var names = await FilterAsync(Requests.Filter("followers", Operator.GreaterThan, "4500000000000"));

            names.ShouldBe([FollowersOrder[^1]]);
        }

        [Theory]
        [InlineData("balance", "3.25")]
        [InlineData("score", "3.5")]
        [InlineData("createdAt", "2024-01-03T10:00:00Z")]
        public async Task Filter_UnderAnotherCulture_ParsesWithTheInvariantCulture(string path, string value)
        {
            // fr-FR uses "," as decimal separator: the value is still read with the invariant culture
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            try
            {
                var names = await FilterAsync(Requests.Filter(path, Operator.Equal, value));

                names.ShouldBe([Carol]);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }
    }
}