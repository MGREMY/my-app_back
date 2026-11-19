using Microsoft.Extensions.Configuration;
using MyApp.Host.Api.Extension.Configuration;

namespace MyApp.Host.Api.Tests.Extension.Configuration;

public sealed partial class ApiOptionConfigurationTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        private static readonly string[] RequiredKeys =
        [
            "Api:Database:ConnectionString",
            "Api:Cache:ConnectionString",
            "Api:Auth:Domain",
            "Api:Auth:Audience",
            "Api:Services:DataPath"
        ];

        public static TheoryData<string> RequiredKeyData => new(RequiredKeys);

        [Fact]
        public void GetAppConfig_CompleteConfiguration_BindsEverySetting()
        {
            var config = Builder(CompleteSettings()).GetAppConfig();

            config.Database.ConnectionString.ShouldBe("Host=db");
            config.Cache.ConnectionString.ShouldBe("redis:6379");
            config.Auth.Domain.ShouldBe("https://auth.example.test");
            config.Auth.Audience.ShouldBe("audience");
            config.Cors.Origins.ShouldBe(["https://a.example.test", "https://b.example.test"]);
            config.Services.DataPath.ShouldBe("data");
        }

        [Fact]
        public void GetAppConfig_NoCorsOrigin_IsValidWithEmptyOrigins()
        {
            var settings = CompleteSettings();
            settings.Remove("Api:Cors:Origins:0");
            settings.Remove("Api:Cors:Origins:1");

            Builder(settings).GetAppConfig().Cors.Origins.ShouldBeEmpty();
        }

        [Theory]
        [MemberData(nameof(RequiredKeyData))]
        public void GetAppConfig_MissingRequiredKey_ThrowsListingOnlyThatKey(string key)
        {
            var settings = CompleteSettings();
            settings.Remove(key);

            var exception = Should.Throw<InvalidOperationException>(() => Builder(settings).GetAppConfig());

            exception.Message.ShouldBe($"Missing configuration: {key}");
        }

        [Theory]
        [MemberData(nameof(RequiredKeyData))]
        public void GetAppConfig_WhitespaceRequiredKey_ThrowsListingThatKey(string key)
        {
            var settings = CompleteSettings();
            settings[key] = "  ";

            Should.Throw<InvalidOperationException>(() => Builder(settings).GetAppConfig()).Message.ShouldContain(key);
        }

        [Fact]
        public void GetAppConfig_EmptyConfiguration_ThrowsOnceListingEveryRequiredKey()
        {
            var exception = Should.Throw<InvalidOperationException>(() => Builder([]).GetAppConfig());

            exception.Message.ShouldBe($"Missing configuration: {string.Join(", ", RequiredKeys)}");
        }

        private static Dictionary<string, string?> CompleteSettings()
        {
            return new Dictionary<string, string?>
            {
                ["Api:Database:ConnectionString"] = "Host=db",
                ["Api:Cache:ConnectionString"] = "redis:6379",
                ["Api:Auth:Domain"] = "https://auth.example.test",
                ["Api:Auth:Audience"] = "audience",
                ["Api:Cors:Origins:0"] = "https://a.example.test",
                ["Api:Cors:Origins:1"] = "https://b.example.test",
                ["Api:Services:DataPath"] = "data"
            };
        }

        private static HostApplicationBuilder Builder(Dictionary<string, string?> settings)
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
            builder.Configuration.AddInMemoryCollection(settings);

            return builder;
        }
    }
}
