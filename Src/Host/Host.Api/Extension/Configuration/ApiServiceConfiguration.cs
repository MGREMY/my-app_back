using Domain.Service.Extension;

namespace Host.Api.Extension.Configuration;

public static class ApiServiceConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppServices()
        {
            var postgresConnectionString = builder.Configuration.GetConnectionString("postgres");
            var redisConnectionString = builder.Configuration.GetConnectionString("redis");
            var dataPath = builder.Configuration.GetValue<string>("DataPath");

            ArgumentNullException.ThrowIfNull(postgresConnectionString);
            ArgumentNullException.ThrowIfNull(redisConnectionString);
            ArgumentNullException.ThrowIfNull(dataPath);

            builder.Services.AddMyAppServices(
                postgresConnectionString,
                redisConnectionString,
                config => { config.DataPath = dataPath; });

            return builder;
        }
    }
}