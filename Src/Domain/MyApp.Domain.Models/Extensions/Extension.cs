using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.Domain.Models.Extensions;

public static class Extension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Register the <see cref="AppDbContext" /> backed by PostgreSQL.
        /// </summary>
        /// <param name="postgresConnectionString">PostgreSQL connection string</param>
        public IServiceCollection AddMyAppPersistence(string postgresConnectionString)
        {
            services.AddDbContextPool<AppDbContext>(options =>
            {
                options.UseLazyLoadingProxies();
                options.UseNpgsql(postgresConnectionString,
                    npgOptions => { npgOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName); });
            });

            return services;
        }
    }
}
