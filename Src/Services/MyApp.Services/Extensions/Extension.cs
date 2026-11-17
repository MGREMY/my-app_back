using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Services.Abstractions;

namespace MyApp.Services.Extensions;

public static class Extension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Register the application services only.
        ///     Infrastructure (database, distributed cache) must be registered by the host,
        ///     e.g. with <c>AddMyAppPersistence</c> and <see cref="AddMyAppCache" />.
        /// </summary>
        public IServiceCollection AddMyAppServices(Action<ServiceOption> options)
        {
            return services
                .AddLocalization()
                .AddServiceOption(options)
                .AddServices();
        }

        /// <summary>
        ///     Register the hybrid cache backed by Redis as distributed (L2) cache.
        /// </summary>
        /// <param name="redisConnectionString">Redis connection string</param>
        public IServiceCollection AddMyAppCache(string redisConnectionString)
        {
            services.AddHybridCache(options =>
            {
                options.ReportTagMetrics = true;
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),
                    LocalCacheExpiration = TimeSpan.FromMinutes(5)
                };
            }).Services.AddStackExchangeRedisCache(options => { options.Configuration = redisConnectionString; });

            return services;
        }

        private IServiceCollection AddServiceOption(Action<ServiceOption> options)
        {
            return services.Configure(options);
        }

        private IServiceCollection AddServices()
        {
            var definedServiceTypes = typeof(Extension).Assembly.DefinedTypes
                .Where(x => x is { IsClass: true, IsAbstract: false })
                .ToArray();

            var definedServiceInterfaces = typeof(ServiceResponse).Assembly.DefinedTypes
                .Where(x => x is { IsInterface: true, IsGenericTypeDefinition: false })
                .ToArray();

            foreach (var serviceInterface in definedServiceInterfaces)
            {
                var implementations = definedServiceTypes
                    .Where(x => x.ImplementedInterfaces.Contains(serviceInterface))
                    .ToArray();

                services.AddScoped(serviceInterface, implementations switch
                {
                    [var single] => single,
                    [] => throw new InvalidOperationException(
                        $"No implementation found for service '{serviceInterface.FullName}'."),
                    _ => throw new InvalidOperationException(
                        $"Multiple implementations found for service '{serviceInterface.FullName}': {string.Join(", ", implementations.Select(x => x.FullName))}."),
                });
            }

            return services.AddHttpClient();
        }
    }
}