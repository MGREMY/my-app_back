using System.Globalization;
using System.Net;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Services;
using MyApp.Services.Resources;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Extension.Configuration;

/// <summary>Localized error messages (<c>UseAppLocalization</c>): en by default, fr supported.</summary>
public sealed partial class RequestLocalizationConfigurationTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        [Fact]
        public async Task Error_NoCulture_ReturnsEnglishMessage()
        {
            (await NotFoundTitleAsync(CreateClient().AsUser())).ShouldBe("User not found");
        }

        [Theory]
        [InlineData("fr")]
        [InlineData("fr-FR")]
        public async Task Error_FrenchCulture_ReturnsFrenchMessage(string culture)
        {
            var title = await NotFoundTitleAsync(CreateClient().AsUser().WithCulture(culture));

            title.ShouldBe(Translation(ServiceConstant.Error.user_not_found, "fr"));
            title.ShouldNotBe(SharedResource.user_not_found);
        }

        [Fact]
        public async Task Error_UnsupportedCulture_FallsBackToEnglish()
        {
            (await NotFoundTitleAsync(CreateClient().AsUser().WithCulture("de"))).ShouldBe(SharedResource.user_not_found);
        }

        [Fact]
        public async Task ServiceError_FrenchCulture_ReturnsFrenchMessage()
        {
            await SeedUserAsync("auth-1", deleted: true);

            using var response = await CreateClient().AsUser("auth-1").WithCulture("fr").PostAsync("/auth/sync-user", null, Ct);

            (await ReadProblemAsync(response)).Title.ShouldBe(Translation(ServiceConstant.Error.user_already_deleted, "fr"));
        }

        private static async Task<string?> NotFoundTitleAsync(HttpClient client)
        {
            using var response = await client.GetAsync($"/users/{Guid.NewGuid()}", Ct);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

            return (await ReadProblemAsync(response)).Title;
        }

        private static string Translation(string key, string culture)
        {
            return SharedResource.ResourceManager.GetString(key, CultureInfo.GetCultureInfo(culture)).ShouldNotBeNull();
        }
    }
}
