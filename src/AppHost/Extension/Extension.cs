namespace AppHost.Extension;

public static class Extension
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IDistributedApplicationBuilder
    {
        public IResourceBuilder<PostgresDatabaseResource> AddPostgresContainer()
        {
            return builder.AddPostgres(
                    name: "postgresServer",
                    userName: builder.AddParameterFromConfiguration(
                        name: "Z-PARAM-POSTGRES-USER",
                        configurationKey: "api_config:postgres:user",
                        secret: true),
                    password: builder.AddParameterFromConfiguration(
                        name: "Z-PARAM-POSTGRES-PASSWORD",
                        configurationKey: "api_config:postgres:password",
                        secret: true),
                    port: int.TryParse(builder.Configuration["api_config:postgres:port"], out var port)
                        ? port
                        : throw new NullReferenceException(nameof(port))
                )
                .WithDataVolume()
                .AddDatabase(name: "postgres", databaseName: "myApp");
        }

        public IResourceBuilder<RedisResource> AddRedisContainer()
        {
            return builder.AddRedis(
                name: "redis",
                password: builder.AddParameterFromConfiguration(
                    name: "Z-PARAM-REDIS-PASSWORD",
                    configurationKey: "api_config:redis:password",
                    secret: true),
                port: int.TryParse(builder.Configuration["api_config:redis:port"], out var port)
                    ? port
                    : throw new NullReferenceException(nameof(port))
            );
        }

        public IResourceBuilder<KeycloakResource> AddKeycloakContainer()
        {
            return builder.AddKeycloak(
                    name: "keycloak",
                    adminUsername: builder.AddParameterFromConfiguration(
                        name: "Z-PARAM-KEYCLOAK-ADMIN-USERNAME",
                        configurationKey: "api_config:keycloak:admin_username",
                        secret: true),
                    adminPassword: builder.AddParameterFromConfiguration(
                        name: "Z-PARAM-KEYCLOAK-ADMIN-PASSWORD",
                        configurationKey: "api_config:keycloak:admin_password",
                        secret: true),
                    port: int.TryParse(builder.Configuration["api_config:keycloak:port"], out var port)
                        ? port
                        : throw new NullReferenceException(nameof(port))
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
                httpPort: int.TryParse(builder.Configuration["api_config:mailPit:http_port"], out var httpPort)
                    ? httpPort
                    : throw new NullReferenceException(nameof(httpPort)),
                smtpPort: int.TryParse(builder.Configuration["api_config:mailPit:smtp_port"], out var smtpPort)
                    ? smtpPort
                    : throw new NullReferenceException(nameof(smtpPort))
            );
        }
    }
}