using System.Diagnostics.CodeAnalysis;
using Domain.Model;
using Domain.Service.Contract.Service.AuthService;
using Domain.Service.Contract.Service.UserService;
using Domain.Service.Service;
using Domain.Service.Service.AuthService;
using Domain.Service.Service.UserService;
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
                // Auth
                .AddScoped<IAuthSyncUserService, AuthSyncUserService>()
                // User
                .AddScoped<IUserGetService, UserGetService>()
                .AddScoped<IUserGetByIdService, UserGetByIdService>()
                .AddScoped<IUserDeleteService, UserDeleteService>();
        }
    }
}