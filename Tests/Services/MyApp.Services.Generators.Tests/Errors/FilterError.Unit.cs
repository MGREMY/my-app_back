using MyApp.Services.Abstractions.Requests;
using MyApp.Services.Generators.Tests.TestModels;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestInfrastructure.Requests;

namespace MyApp.Services.Generators.Tests.Errors;

/// <summary>
/// Invalid filters throw <see cref="InvalidPaginationRequestException" /> while the query is built: no database needed.
/// </summary>
public sealed partial class FilterErrorTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        private static readonly IQueryable<Author> Authors = Array.Empty<Author>().AsQueryable();

        public static TheoryData<string, Operator, string?> InvalidFilters() => new()
        {
            // Unknown paths
            { "unknown", Operator.Equal, "x" },
            { "country.unknown", Operator.Equal, "x" },
            { "posts.unknown", Operator.Equal, "x" },
            { "country_name", Operator.Equal, "France" },
            // Ignored properties: binary column, unsupported type, keyword without "@", explicit interface implementation
            { "avatar", Operator.Equal, "AQID" },
            { "timeout", Operator.Equal, "01:00:00" },
            { "class", Operator.Equal, "admin" },
            { "auditNote", Operator.Equal, "audited" },
            // A class of another assembly (System.Uri) is never followed as a navigation, nor filtered as a scalar
            { "website", Operator.Equal, "https://alice.example/" },
            { "website.host", Operator.Equal, "alice.example" },
            // A scalar ends the path, a navigation needs one
            { "name.length", Operator.Equal, "5" },
            { "country", Operator.Equal, "France" },
            { "posts", Operator.Equal, "x" },
            // Operator not supported by the type
            { "name", Operator.GreaterThan, "a" },
            { "nickname", Operator.LessThanOrEqual, "a" },
            { "age", Operator.Contains, "1" },
            { "createdAt", Operator.StartWith, "2024" },
            { "isActive", Operator.GreaterThan, "true" },
            { "status", Operator.LessThan, "Draft" },
            { "externalId", Operator.GreaterThanOrEqual, "00000001-0000-0000-0000-000000000000" },
            { "posts.$count", Operator.Contains, "1" },
            { "holidays", Operator.EndWith, "25" },
            // Unparsable values ("" and "null" are ordinary, unparsable values on non-string types)
            { "age", Operator.Equal, "abc" },
            { "age", Operator.Equal, "" },
            { "rank", Operator.Equal, "null" },
            { "age", Operator.Equal, "2147483648" },
            { "followers", Operator.Equal, "1.5" },
            { "balance", Operator.Equal, "1e" },
            { "createdAt", Operator.Equal, "not a date" },
            { "joinedAt", Operator.Equal, "2024-13-01" },
            { "birthDate", Operator.Equal, "2024-02-30" },
            { "isActive", Operator.Equal, "yes" },
            { "status", Operator.Equal, "Deleted" },
            { "externalId", Operator.Equal, "123" },
            { "posts.$count", Operator.Equal, "many" },
            { "holidays", Operator.Equal, "christmas" },
            // A null value on a non-nullable property, or with an operator other than Equal / NotEqual
            { "name", Operator.Equal, null },
            { "age", Operator.Equal, null },
            { "isActive", Operator.NotEqual, null },
            { "posts.$count", Operator.Equal, null },
            { "tags", Operator.Equal, null },
            { "rank", Operator.GreaterThan, null },
            { "nickname", Operator.Contains, null },
            // More than four navigations
            { "mentor.mentor.mentor.mentor.mentor.name", Operator.Equal, "Alice" },
            { "posts.author.posts.author.posts.title", Operator.Equal, "x" },
            { "posts.author.posts.author.tags", Operator.Equal, "ef" },
            { "posts.author.posts.author.posts.$count", Operator.Equal, "1" },
            // "$" segments: only right after a collection, $count ends the path, $none / $all need an item property
            { "$count", Operator.Equal, "1" },
            { "country.$none.name", Operator.Equal, "France" },
            { "name.$count", Operator.Equal, "1" },
            { "posts.$count.title", Operator.Equal, "1" },
            { "posts.$none", Operator.Equal, "x" },
            { "posts.$all", Operator.Equal, "x" },
            { "posts.$any.title", Operator.Equal, "x" },
            { "posts.$none.$all.title", Operator.Equal, "x" },
            // Collections of values: nothing may follow the collection or its quantifier
            { "tags.length", Operator.Equal, "2" },
            { "tags.$none.label", Operator.Equal, "ef" },
            { "tags.$all.x", Operator.Equal, "ef" }
        };

        [Theory]
        [MemberData(nameof(InvalidFilters))]
        public void ProcessPaginationRequest_InvalidFilter_ThrowsInvalidPaginationRequestException(string path, Operator op, string? value)
        {
            var request = Page(filters: [Filter(path, op, value)]);

            Should.Throw<InvalidPaginationRequestException>(() => Authors.ProcessPaginationRequest(request, out _));
        }

        // An enum value must be the name of a defined member, with its exact case
        public static TheoryData<string, string> InvalidEnumValues() => new()
        {
            { "status", "archived" },
            { "status", "PUBLISHED" },
            { "status", "1" },
            { "status", "42" },
            { "previousStatus", "draft" },
            { "previousStatus", "0" }
        };

        [Theory]
        [MemberData(nameof(InvalidEnumValues))]
        public void ProcessPaginationRequest_EnumValueNotADefinedName_ThrowsInvalidPaginationRequestException(string path, string value)
        {
            var request = Page(filters: [Filter(path, Operator.Equal, value)]);

            Should.Throw<InvalidPaginationRequestException>(() => Authors.ProcessPaginationRequest(request, out _));
        }

        [Fact]
        public void ProcessPaginationRequest_InvalidNestedFilter_ThrowsInvalidPaginationRequestException()
        {
            var request = Page(filters: [Filter("name", Operator.Equal, "Alice", Logic.And, Filter("unknown", Operator.Equal, "x"))]);

            Should.Throw<InvalidPaginationRequestException>(() => Authors.ProcessPaginationRequest(request, out _));
        }

        [Fact]
        public void ProcessPaginationRequest_UnknownLogic_ThrowsInvalidPaginationRequestException()
        {
            var request = Page(filters: [Filter("name", Operator.Equal, "Alice"), Filter("name", Operator.Equal, "Bob", (Logic)42)]);

            Should.Throw<InvalidPaginationRequestException>(() => Authors.ProcessPaginationRequest(request, out _));
        }

        [Fact]
        public void ProcessPaginationRequest_UnknownPath_NamesThePathInTheMessage()
        {
            var request = Page(filters: [Filter("Country.Unknown", Operator.Equal, "x")]);

            var exception = Should.Throw<InvalidPaginationRequestException>(() => Authors.ProcessPaginationRequest(request, out _));

            exception.Message.ShouldContain("Country.Unknown");
        }

        [Theory]
        [InlineData("mentor.mentor.mentor.mentor.name")]
        [InlineData("posts.author.posts.author.name")]
        [InlineData("mentor.mentor.mentor.posts.$count")]
        public void ProcessPaginationRequest_FourNavigations_IsAccepted(string path)
        {
            var request = Page(filters: [Filter(path, Operator.Equal, path.EndsWith("$count") ? "1" : "Alice")]);

            Should.NotThrow(() => Authors.ProcessPaginationRequest(request, out _));
        }
    }
}
