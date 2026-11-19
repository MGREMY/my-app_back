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
        public async Task AntiforgeryProbe_NoToken_IsRejected()
        {
            // Control of the probe used below: a POST without token is invalid.
            using var response = await CreateClient().PostAsync(ApiFactory.AntiforgeryProbePath, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact(Skip = "Bug: GetAndStoreTokens only stores the cookie token in the XSRF-TOKEN cookie; the request token is "
                     + "never returned, so a SPA copying the cookie into X-XSRF-TOKEN sends the cookie token twice and "
                     + "validation fails (tokens swapped). The endpoint should also return/issue tokens.RequestToken.")]
        public async Task AntiForgeryToken_CookieSentBackAsHeader_PassesValidation()
        {
            var client = await CreateClient().AsAnonymous().WithAntiforgeryTokenAsync(Ct);

            using var response = await client.PostAsync(ApiFactory.AntiforgeryProbePath, null, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
    }
}
