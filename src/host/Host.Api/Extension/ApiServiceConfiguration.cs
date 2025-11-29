using Domain.Service.Extension;

namespace Host.Api.Extension;

public static class ApiServiceConfiguration
{
    public static TBuilder AddMyAppApiService<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var postgresConnectionString = builder.Configuration.GetConnectionString("postgres");
        var redisConnectionString = builder.Configuration.GetConnectionString("redis");

        ArgumentNullException.ThrowIfNull(postgresConnectionString);
        ArgumentNullException.ThrowIfNull(redisConnectionString);

        builder.Services.AddMyAppServices(postgresConnectionString, redisConnectionString);

        return builder;
    }
}