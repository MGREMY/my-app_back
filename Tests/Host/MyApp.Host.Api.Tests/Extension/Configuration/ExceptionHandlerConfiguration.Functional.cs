using System.Net;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Extension.Configuration;

/// <summary>Unhandled exceptions (<c>UseAppDomainExceptionHandler</c>), through <see cref="ApiFactory.ExceptionProbePath"/>.</summary>
public sealed partial class ExceptionHandlerConfigurationTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        [Fact]
        public async Task UnhandledException_Returns500WithoutLeakingTheException()
        {
            using var response = await CreateClient().GetAsync(ApiFactory.ExceptionProbePath, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            (await response.Content.ReadAsStringAsync(Ct)).ShouldNotContain(ApiFactory.ExceptionProbeMessage);
        }

        // Bug: the exception handler returns TypedResults.InternalServerError(), a 500 without body, instead of an RFC
        // 7807 problem (application/problem+json).
        [Fact]
        public async Task UnhandledException_Returns500Problem()
        {
            using var response = await CreateClient().GetAsync(ApiFactory.ExceptionProbePath, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            var problem = await ReadProblemAsync(response);
            problem.Status.ShouldBe(500);
            (problem.Detail ?? string.Empty).ShouldNotContain(ApiFactory.ExceptionProbeMessage);
        }
    }
}
