using System.Net;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Endpoints;

public sealed partial class MiscEndpointTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        private const string Route = "/misc";
        private const string AntiForgeryTokenRoute = $"{Route}/anti-forgery-token";

        [Fact]
        public async Task AntiForgeryToken_Anonymous_Returns204AndIssuesXsrfCookie()
        {
            using var response = await CreateClient().AsAnonymous().GetAsync(AntiForgeryTokenRoute, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            ApiClientAuthExtensions.AntiforgeryCookie(response).ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task AntiForgeryToken_CookieSentBackAsHeader_PassesValidation()
        {
            var client = await CreateClient().AsAnonymous().WithAntiforgeryTokenAsync(Ct);

            using var response = await client.PostAsync(ApiFactory.AntiforgeryProbePath, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
    }
}
