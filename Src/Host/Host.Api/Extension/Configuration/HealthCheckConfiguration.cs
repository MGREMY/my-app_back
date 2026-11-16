namespace Host.Api.Extension.Configuration;

public static class HealthCheckConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppHealthChecks()
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
}