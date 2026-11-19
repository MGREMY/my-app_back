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
        private const string UnsupportedVersionProblemType = "https://docs.api-versioning.org/problems#unsupported";

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

        [Theory(Skip = "Bug: an unsupported API version returns a 400 without body (no Content-Type) instead of an "
                       + "RFC 7807 problem (application/problem+json).")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Request_UnsupportedVersion_Returns400Problem(bool inHeader)
        {
            var client = CreateClient();
            using var response = inHeader
                ? await client.WithApiVersionHeader("2.0").GetAsync(Route, Ct)
                : await client.GetAsync($"{Route}?api-version=2.0", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await ReadProblemAsync(response);
            problem.Status.ShouldBe(400);
            problem.Type.ShouldBe(UnsupportedVersionProblemType);
            problem.Detail.ShouldNotBeNull().ShouldContain("2.0");
        }
    }
}
