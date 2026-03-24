using Domain.Service.Extension;

namespace Host.Api.Extension.Configuration;

public static class ApiServiceConfiguration
{
    public static TBuilder AddAppServices<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
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