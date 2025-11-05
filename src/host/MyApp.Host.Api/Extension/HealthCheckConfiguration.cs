namespace MyApp.Host.Api.Extension;

public static class HealthCheckConfiguration
{
    public static TBuilder AddApiHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var postgresConnectionString = builder.Configuration.GetConnectionString("postgres");
        var redisConnectionString = builder.Configuration.GetConnectionString("redis");

        ArgumentNullException.ThrowIfNull(postgresConnectionString);
        ArgumentNullException.ThrowIfNull(redisConnectionString);

        builder.Services
            .AddHealthChecks()
            .AddDiskStorageHealthCheck(options => { options.CheckAllDrives = true; }, name: "system:diskStorage")
            .AddNpgSql(postgresConnectionString, name: "services:PostgresSQL")
            .AddRedis(redisConnectionString, name: "services:Redis");

        return builder;
    }
}