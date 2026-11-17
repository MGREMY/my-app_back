using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Models;
using MyApp.Services.Abstractions;

namespace MyApp.Services.Extensions;

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
                    LocalCacheExpiration = TimeSpan.FromMinutes(5)
                };
            }).Services.AddStackExchangeRedisCache(options => { options.Configuration = redisConnectionString; });

            return services;
        }

        private IServiceCollection AddServices()
        {
            var definedServiceTypes = typeof(Extension).Assembly.DefinedTypes
                .Where(x => x is { IsClass: true, IsAbstract: false })
                .ToArray();

            var definedServiceInterfaces = typeof(ServiceResponse).Assembly.DefinedTypes
                .Where(x => x is { IsInterface: true })
                .ToArray();

            foreach (var serviceInterface in definedServiceInterfaces)
            foreach (var definedServiceType in definedServiceTypes)
            {
                if (!definedServiceType.ImplementedInterfaces.Contains(serviceInterface)) continue;

                services.AddScoped(serviceInterface, definedServiceType);
                break;
            }

            return services.AddHttpClient();
        }
    }
}