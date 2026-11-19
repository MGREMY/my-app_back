using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>Numbers and dates: every operator, on the non-nullable property and on its nullable twin.</summary>
public sealed partial class ComparableFilterTests
{
    [IntegrationTest]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        // (non-nullable path, nullable path, value of Carol): every value is f(rank), Bob and Eve have null twins
        private static readonly (string Path, string NullablePath, string CarolValue)[] Types =
        [
            ("age", "rank", "30"),
            ("followers", "views", "3000000000000"),
            ("balance", "bonus", "3.25"),
            ("score", "rating", "3.5"),
            ("createdAt", "updatedAt", "2024-01-03T10:00:00Z"),
            ("joinedAt", "lastSeenAt", "2024-03-01T08:00:00+00:00"),
            ("birthDate", "deathDate", "1990-01-03")
        ];

        // Expected authors when comparing to Carol (rank 3), all ranks then nullable twins (no Bob, no Eve)
        private static readonly (Operator Operator, string[] Expected, string[] ExpectedNullable)[] Operators =
        [
            (Operator.Equal, [Carol], [Carol]),
            (Operator.NotEqual, [Alice, Bob, Dave, Eve], [Alice, Dave]),
            (Operator.GreaterThan, [Dave, Eve], [Dave]),
            (Operator.GreaterThanOrEqual, [Carol, Dave, Eve], [Carol, Dave]),
            (Operator.LessThan, [Alice, Bob], [Alice]),
            (Operator.LessThanOrEqual, [Alice, Bob, Carol], [Alice, Carol])
        ];

        public static TheoryData<string, Operator, string, string[]> Matrix()
        {
            var data = new TheoryData<string, Operator, string, string[]>();
            foreach (var (path, nullablePath, value) in Types)
            foreach (var (op, expected, expectedNullable) in Operators)
            {
                data.Add(path, op, value, expected);
                data.Add(nullablePath, op, value, expectedNullable);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Matrix))]
        public async Task Filter_ComparableOperator_ReturnsMatchingAuthors(string path, Operator op, string value, string[] expected)
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

            names.ShouldBe([Eve]);
        }
    }
}
