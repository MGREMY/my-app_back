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
    public static IServiceCollection AddMyAppServices(
        this IServiceCollection services,
        string postgresConnectionString,
        string redisConnectionString)
    {
        return services
            .AddLocalization()
            .AddDatabase(postgresConnectionString)
            .AddCache(redisConnectionString)
            .AddServices();
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        string postgresConnectionString)
    {
        services.AddDbContextPool<AppDbContext>(options =>
        {
            options.UseLazyLoadingProxies();
            options.UseNpgsql(postgresConnectionString,
                npgOptions => { npgOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName); });
        });

        return services;
    }

    private static IServiceCollection AddCache(
        this IServiceCollection services,
        string redisConnectionString)
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

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        return services
            .AddHttpClient()
            .AddScoped<ICacheService, CacheService>()
            .AddScoped<IAuthSyncUserService, AuthSyncUserService>()
            .AddScoped<IUserGetService, UserGetService>()
            .AddScoped<IUserGetByIdService, UserGetByIdService>();
    }
}