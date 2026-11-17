using MyApp.Domain.Models.Extensions;
using MyApp.Services.Extensions;

namespace MyApp.Host.Api.Extension.Configuration;

public static class ApiServiceConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppServices()
        {
            var config = builder.GetAppConfig();

            builder.Services
                .AddMyAppPersistence(config.Database.ConnectionString)
                .AddMyAppCache(config.Cache.ConnectionString)
                .AddMyAppServices(options => { options.DataPath = config.Services.DataPath; });

            return builder;
        }
    }
}
