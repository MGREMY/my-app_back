using System.Net;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Extension.Configuration;

public sealed partial class OpenApiConfigurationTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        [Theory]
        [InlineData("/openapi/v1.json")]
        [InlineData("/scalar")]
        public async Task Documentation_OutsideDevelopment_IsNotMapped(string route)
        {
            using var response = await CreateClient().GetAsync(route, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }
}
