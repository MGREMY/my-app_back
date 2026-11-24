using System.Net;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;

namespace MyApp.Host.Api.Tests.Extension.Configuration;

/// <summary>
///     Problem details on every error (<c>AddAppProblemDetails</c> / <c>UseAppProblemDetails</c>): responses sent without
///     body by the framework get one, and every problem identifies its occurrence.
/// </summary>
public sealed partial class ProblemDetailsConfigurationTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        [Fact]
        public async Task Get_Anonymous_Returns401Problem()
        {
            using var response = await CreateClient().AsAnonymous().GetAsync($"/users/{TestUsers.Bob.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            (await ReadProblemAsync(response)).Status.ShouldBe(401);
        }

        [Fact]
        public async Task Delete_AsUserWithoutAdminRole_Returns403Problem()
        {
            using var response = await CreateClient().AsUser(TestUsers.Alice).DeleteAsync($"/users/{TestUsers.Bob.Id}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await ReadProblemAsync(response)).Status.ShouldBe(403);
        }

        [Fact]
        public async Task Get_UnknownRoute_Returns404Problem()
        {
            using var response = await CreateClient().AsUser(TestUsers.Alice).GetAsync("/unknown-route", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await ReadProblemAsync(response)).Status.ShouldBe(404);
        }

        [Fact]
        public async Task Get_ServiceError_ReturnsProblemWithInstanceAndTraceId()
        {
            var path = $"/users/{Guid.NewGuid()}";

            using var response = await CreateClient().AsUser(TestUsers.Alice).GetAsync(path, Ct);

            var problem = await ReadProblemAsync(response);
            problem.Instance.ShouldBe(path);
            problem.Extensions.ShouldContainKey("traceId");
        }
    }
}
