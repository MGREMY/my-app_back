namespace MyApp.Host.Api.Extension.Configuration;

public static class HealthCheckConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppHealthChecks()
        {
            var config = builder.GetAppConfig();

            builder.Services
                .AddHealthChecks()
                .AddDiskStorageHealthCheck(options => { options.CheckAllDrives = true; }, "system:diskStorage")
                .AddNpgSql(config.Database.ConnectionString, name: "services:PostgresSQL")
                .AddRedis(config.Cache.ConnectionString, "services:Redis");

            return builder;
        }
    }
}