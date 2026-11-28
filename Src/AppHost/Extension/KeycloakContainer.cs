namespace AppHost.Extension;

public static class KeycloakContainer
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IDistributedApplicationBuilder
    {
        public IResourceBuilder<KeycloakResource> AddKeycloakContainer()
        {
            return builder.AddKeycloak(
                    "myAppKeycloak",
                    adminUsername: builder.AddParameterFromConfiguration(
                        "Z-PARAM-KEYCLOAK-ADMIN-USERNAME",
                        "api_config:keycloak:admin_username",
                        true),
                    adminPassword: builder.AddParameterFromConfiguration(
                        "Z-PARAM-KEYCLOAK-ADMIN-PASSWORD",
                        "api_config:keycloak:admin_password",
                        true),
                    port: int.TryParse(builder.Configuration["api_config:keycloak:port"], out var port)
                        ? port
                        : throw new InvalidOperationException(nameof(port))
                )
                .WithDataVolume()
                // Note:
                // Using this causes crash if `authorizationSettings` section is not deleted from JSON file
                // When updating it. Please be careful
                .WithRealmImport("./Configuration/Keycloak/Realm");
        }
    }
}