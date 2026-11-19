using Microsoft.EntityFrameworkCore;
using MyApp.Services.Generators.Tests.TestModels;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestInfrastructure.Requests;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>
/// Filter values are sent as SQL parameters, never inlined in the SQL text. The SQL is only generated
/// (<c>ToQueryString</c>): no database needed.
/// </summary>
public sealed partial class FilterValueTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        [Theory]
        [InlineData("name", Operator.Equal, "Zorglub")]
        [InlineData("nickname", Operator.Contains, "Zorglub")]
        [InlineData("age", Operator.GreaterThan, "4242")]
        [InlineData("externalId", Operator.Equal, "4b1d0e2a-9c3f-4e5d-8a7b-6c5d4e3f2a1b")]
        [InlineData("birthDate", Operator.Equal, "1977-05-25")]
        [InlineData("posts.title", Operator.Equal, "Zorglub")]
        [InlineData("tags", Operator.Equal, "Zorglub")]
        public void ProcessPaginationRequest_FilterValue_IsSentAsSqlParameter(string path, Operator op, string value)
        {
            using var db = PaginationTestDbContext.Create("Host=localhost;Database=unused");

            var sql = db.Authors.ProcessPaginationRequest(Page(filters: [Filter(path, op, value)]), out _).ToQueryString();

            // ToQueryString declares the parameters (and their values) in "--" comment lines before the statement
            var statement = string.Join('\n', sql.Split('\n').Where(x => !x.StartsWith("--")));
            statement.ShouldContain("@");
            statement.ShouldNotContain(value, Case.Insensitive);
        }
    }
}
