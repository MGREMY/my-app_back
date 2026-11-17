namespace MyApp.Host.Api.Extension.Configuration;

public static class ApiOptionConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        ///     Bind and validate the API configuration from the <see cref="ApiOption.SectionKey" /> section.
        ///     Single source of truth for every configuration that needs it.
        /// </summary>
        /// <exception cref="InvalidOperationException">If one or more settings are missing</exception>
        public ApiOption GetAppConfig()
        {
            var config = builder.Configuration
                .GetSection(ApiOption.SectionKey)
                .Get<ApiOption>() ?? new ApiOption();

            List<string> missingSettings = [];

            Require(config.Database.ConnectionString, "Database:ConnectionString");
            Require(config.Cache.ConnectionString, "Cache:ConnectionString");
            Require(config.Auth.Domain, "Auth:Domain");
            Require(config.Auth.Audience, "Auth:Audience");
            Require(config.Services.DataPath, "Services:DataPath");

            if (missingSettings.Count > 0)
                throw new InvalidOperationException(
                    $"Missing configuration: {string.Join(", ", missingSettings)}");

            return config;

            void Require(string value, string path)
            {
                if (string.IsNullOrWhiteSpace(value))
                    missingSettings.Add($"{ApiOption.SectionKey}:{path}");
            }
        }
    }
}
