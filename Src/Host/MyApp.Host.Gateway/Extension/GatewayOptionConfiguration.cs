namespace MyApp.Host.Gateway.Extension;

public static class GatewayOptionConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        ///     Bind and validate the Gateway configuration from the <see cref="GatewayOption.SectionKey" /> section.
        ///     Single source of truth for every configuration that needs it.
        /// </summary>
        /// <exception cref="InvalidOperationException">If one or more settings are missing</exception>
        public GatewayOption GetAppConfig()
        {
            var config = builder.Configuration
                .GetSection(GatewayOption.SectionKey)
                .Get<GatewayOption>() ?? new GatewayOption();

            config.Clusters = new Dictionary<string, string>(config.Clusters, StringComparer.OrdinalIgnoreCase);

            List<string> missingSettings = [];

            foreach (var (clusterId, address) in config.Clusters)
                Require(address, $"Clusters:{clusterId}");

            if (missingSettings.Count > 0)
                throw new InvalidOperationException($"Missing configuration: {string.Join(", ", missingSettings)}");

            return config;

            void Require(string value, string path)
            {
                if (string.IsNullOrWhiteSpace(value))
                    missingSettings.Add($"{GatewayOption.SectionKey}:{path}");
            }
        }
    }
}