namespace AppHost.Extension;

public static class Extension
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IDistributedApplicationBuilder
    {
        public IResourceBuilder<PostgresDatabaseResource> AddPostgresContainer()
        {
            return builder.AddPostgres(
                    "myAppPostgresServer",
                    builder.AddParameterFromConfiguration(
                        "Z-PARAM-POSTGRES-USER",
                        "api_config:postgres:user",
                        true),
                    builder.AddParameterFromConfiguration(
                        "Z-PARAM-POSTGRES-PASSWORD",
                        "api_config:postgres:password",
                        true),
                    int.TryParse(builder.Configuration["api_config:postgres:port"], out var port)
                        ? port
                        : throw new InvalidOperationException(nameof(port))
                )
                .WithDataVolume()
                .AddDatabase("postgres", "myApp");
        }

        public IResourceBuilder<RedisResource> AddRedisContainer()
        {
            var redis = builder.AddRedis(
                "myAppRedis",
                password: builder.AddParameterFromConfiguration(
                    "Z-PARAM-REDIS-PASSWORD",
                    "api_config:redis:password",
                    true),
                port: int.TryParse(builder.Configuration["api_config:redis:port"], out var port)
                    ? port
                    : throw new InvalidOperationException(nameof(port))
            );

            // Remove TLS certificate as Aspire is used only for development
#pragma warning disable ASPIRECERTIFICATES001
            redis.WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

            return redis;
        }

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

        public IResourceBuilder<MailPitContainerResource> AddMailPitContainer()
        {
            return builder.AddMailPit(
                "smtp",
                int.TryParse(builder.Configuration["api_config:mailPit:http_port"], out var httpPort)
                    ? httpPort
                    : throw new InvalidOperationException(nameof(httpPort)),
                int.TryParse(builder.Configuration["api_config:mailPit:smtp_port"], out var smtpPort)
                    ? smtpPort
                    : throw new InvalidOperationException(nameof(smtpPort))
            );
        }
    }
}