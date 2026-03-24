using System.Diagnostics.CodeAnalysis;
using Domain.Model;
using Domain.Service.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Domain.Service.Extension;

[ExcludeFromCodeCoverage]
public static class Extension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMyAppServices(
            string postgresConnectionString,
            string redisConnectionString,
            Action<ServiceOption> options)
        {
            return services
                .AddLocalization()
                .AddServiceOption(options)
                .AddDatabase(postgresConnectionString)
                .AddCache(redisConnectionString)
                .AddServices();
        }

        private IServiceCollection AddServiceOption(Action<ServiceOption> options)
        {
            return services.Configure(options);
        }

        private IServiceCollection AddDatabase(string postgresConnectionString)
        {
            services.AddDbContextPool<AppDbContext>(options =>
            {
                options.UseLazyLoadingProxies();
                options.UseNpgsql(postgresConnectionString,
                    npgOptions => { npgOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName); });
            });

            return services;
        }

        private IServiceCollection AddCache(string redisConnectionString)
        {
            services.AddHybridCache(options =>
            {
                options.ReportTagMetrics = true;
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),
                    LocalCacheExpiration = TimeSpan.FromMinutes(5),
                };
            }).Services.AddStackExchangeRedisCache(options => { options.Configuration = redisConnectionString; });

            return services;
        }

        private IServiceCollection AddServices()
        {
            const string skipInterfaceNameContaining = "dto";

            var definedServiceTypes = typeof(Extension).Assembly.DefinedTypes
                .Where(x => x is { IsClass: true, IsAbstract: false })
                .ToArray();

            var definedServiceInterfaces = typeof(Contract.DomainException).Assembly.DefinedTypes
                .Where(x =>
                    x is { IsInterface: true }
                    && !x.FullName!.Contains(skipInterfaceNameContaining, StringComparison.InvariantCultureIgnoreCase)
                )
                .ToArray();

            foreach (var serviceInterface in definedServiceInterfaces)
            {
                foreach (var definedServiceType in definedServiceTypes)
                {
                    if (!definedServiceType.ImplementedInterfaces.Contains(serviceInterface)) continue;

                    services.AddScoped(serviceInterface, definedServiceType);
                    break;
                }
            }

            return services
                .AddHttpClient()
                .AddScoped<ICacheService, CacheService>();
        }
    }
}