namespace AppHost.Extension;

public static class PostgresContainer
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IDistributedApplicationBuilder
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
    }
}