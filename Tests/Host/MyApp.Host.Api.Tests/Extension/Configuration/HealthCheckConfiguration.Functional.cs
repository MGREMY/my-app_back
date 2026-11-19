using System.Net;
using System.Text.Json;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Extension.Configuration;

/// <summary>Health checks of the API (<c>AddAppHealthChecks</c>) on the endpoints mapped by the service defaults.</summary>
public sealed partial class HealthCheckConfigurationTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        [Fact]
        public async Task Health_Anonymous_ReturnsHealthyReportOfEveryCheck()
        {
            using var response = await CreateClient().AsAnonymous().GetAsync("/health", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using var report = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
            report.RootElement.GetProperty("status").GetString().ShouldBe("Healthy");
            report.RootElement.GetProperty("entries").EnumerateObject().Select(x => x.Name)
                .ShouldBe(["self", "system:diskStorage", "services:PostgresSQL", "services:Redis"], ignoreOrder: true);
        }

        [Fact]
        public async Task Alive_Anonymous_ReturnsHealthyFromSelfCheckOnly()
        {
            using var response = await CreateClient().AsAnonymous().GetAsync("/alive", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync(Ct)).ShouldBe("Healthy");
        }
    }
}
