using System.Net;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Extension.Configuration;

/// <summary>API versioning (<c>AddAppVersioning</c>), on an anonymous endpoint.</summary>
public sealed partial class ApiEndpointConfigurationTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        private const string Route = "/misc/anti-forgery-token";

        [Fact]
        public async Task Request_NoVersion_UsesDefaultVersionAndReportsSupportedVersions()
        {
            using var response = await CreateClient().GetAsync(Route, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            response.Headers.GetValues("api-supported-versions").ShouldContain("1.0");
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("1")]
        public async Task Request_SupportedVersionInQueryString_Succeeds(string version)
        {
            using var response = await CreateClient().GetAsync($"{Route}?api-version={version}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Request_SupportedVersionInHeader_Succeeds()
        {
            using var response = await CreateClient().WithApiVersionHeader("1.0").GetAsync(Route, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Request_UnsupportedVersionInQueryString_Returns400()
        {
            using var response = await CreateClient().GetAsync($"{Route}?api-version=2.0", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Request_UnsupportedVersionInHeader_Returns400()
        {
            using var response = await CreateClient().WithApiVersionHeader("2.0").GetAsync(Route, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }
}
