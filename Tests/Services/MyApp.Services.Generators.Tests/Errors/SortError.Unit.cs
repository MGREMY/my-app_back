using MyApp.Services.Abstractions.Requests;
using MyApp.Services.Generators.Tests.TestModels;
using static MyApp.Services.Generators.Tests.TestInfrastructure.Requests;

namespace MyApp.Services.Generators.Tests.Errors;

/// <summary>Only scalars are sortable, through references only, up to three levels.</summary>
public sealed partial class SortErrorTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        private static readonly IQueryable<Author> Authors = Array.Empty<Author>().AsQueryable();

        [Theory]
        [InlineData("unknown")]
        [InlineData("country.unknown")]
        // Collections (of entities or of values) are never sortable
        [InlineData("posts")]
        [InlineData("posts.likes")]
        [InlineData("posts.$count")]
        [InlineData("tags")]
        [InlineData("holidays")]
        // A navigation itself, ignored properties
        [InlineData("country")]
        [InlineData("avatar")]
        [InlineData("timeout")]
        [InlineData("auditNote")]
        // More than three references
        [InlineData("mentor.mentor.mentor.mentor.name")]
        [InlineData("mentor.mentor.country.continent.name")]
        public void ProcessPaginationRequest_UnsortablePath_ThrowsInvalidPaginationRequestException(string path)
        {
            Should.Throw<InvalidPaginationRequestException>(() => Authors.ProcessPaginationRequest(Page(sorts: [Asc(path)]), out _));
        }

        [Fact]
        public void ProcessPaginationRequest_UnsortableSecondKey_ThrowsInvalidPaginationRequestException()
        {
            Should.Throw<InvalidPaginationRequestException>(
                () => Authors.ProcessPaginationRequest(Page(sorts: [Asc("name"), Desc("posts.likes")]), out _));
        }

        [Theory]
        [InlineData("mentor.mentor.mentor.name")]
        [InlineData("mentor.country.continent.name")]
        [InlineData("@class")]
        [InlineData("deathDate")]
        public void ProcessPaginationRequest_SortablePath_IsAccepted(string path)
        {
            Should.NotThrow(() => Authors.ProcessPaginationRequest(Page(sorts: [Asc(path)]), out _));
        }
    }
}
