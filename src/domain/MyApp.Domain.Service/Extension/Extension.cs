using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Model;
using MyApp.Domain.Service.Contract.Service.AuthService;
using MyApp.Domain.Service.Contract.Service.UserService;
using MyApp.Domain.Service.Service;
using MyApp.Domain.Service.Service.AuthService;
using MyApp.Domain.Service.Service.UserService;

namespace MyApp.Domain.Service.Extension;

public static class Extension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMyAppServices(
            string postgresConnectionString,
            string redisConnectionString)
        {
            return services
                .AddLocalization()
                .AddDatabase(postgresConnectionString)
                .AddCache(redisConnectionString)
                .AddServices();
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
            return services
                .AddHttpClient()
                .AddScoped<ICacheService, CacheService>()
                .AddScoped<IAuthSyncUserService, AuthSyncUserService>()
                .AddScoped<IUserGetService, UserGetService>()
                .AddScoped<IUserGetByIdService, UserGetByIdService>();
        }
    }
}