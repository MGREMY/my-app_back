using Microsoft.EntityFrameworkCore;
using MyApp.Services.Generators.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using static MyApp.Services.Abstractions.Requests.FilterServiceRequest;
using static MyApp.Services.Generators.Tests.TestModels.PaginationTestData;

namespace MyApp.Services.Generators.Tests.Filters;

/// <summary>Paths are dotted, case-insensitive and ignore "_"; inherited members and keyword names are usable.</summary>
public sealed partial class PropertyPathFilterTests
{
    [IntegrationTest]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : PaginationIntegrationTest(containers)
    {
        [Theory]
        [InlineData("isActive")]
        [InlineData("IsActive")]
        [InlineData("ISACTIVE")]
        [InlineData("is_active")]
        [InlineData("_isActive")]
        public async Task Filter_PathCasingAndUnderscores_AreIgnored(string path)
        {
            var names = await FilterAsync(Requests.Filter(path, Operator.Equal, "true"));

            names.ShouldBe([Alice, Carol, Eve]);
        }

        [Theory]
        [InlineData("country.name")]
        [InlineData("Country.Name")]
        [InlineData("COUNTRY.NAME")]
        [InlineData("country.na_me")]
        public async Task Filter_NavigationPathCasing_IsIgnored(string path)
        {
            var names = await FilterAsync(Requests.Filter(path, Operator.Equal, "France"));

            names.ShouldBe([Alice, Dave]);
        }

        [Fact]
        public async Task Filter_InheritedProperty_IsFilterable()
        {
            var id = await Db.Authors.Where(x => x.Name == Carol).Select(x => x.Id).SingleAsync(Ct);

            var names = await FilterAsync(Requests.Filter("id", Operator.Equal, id.ToString()));

            names.ShouldBe([Carol]);
        }

        [Theory]
        [InlineData("@class")]
        [InlineData("@CLASS")]
        public async Task Filter_KeywordProperty_KeepsItsAt(string path)
        {
            var names = await FilterAsync(Requests.Filter(path, Operator.Equal, "admin"));

            names.ShouldBe([Alice]);
        }
    }
}
