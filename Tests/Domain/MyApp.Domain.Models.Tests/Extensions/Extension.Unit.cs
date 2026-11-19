using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Models.Extensions;
using MyApp.Domain.Models.Models;

namespace MyApp.Domain.Models.Tests.Extensions;

public sealed partial class ExtensionTests
{
    /// <summary>Resolves the context without opening any connection.</summary>
    [UnitTest]
    public sealed class Unit
    {
        private const string ConnectionString = "Host=localhost;Database=unused";

        [Fact]
        public void AddMyAppPersistence_Resolved_UsesNpgsql()
        {
            using var scope = BuildProvider().CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.ProviderName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
            context.Database.GetConnectionString().ShouldBe(ConnectionString);
        }

        [Fact]
        public void AddMyAppPersistence_Resolved_EnablesLazyLoadingProxiesAndMigrationsAssembly()
        {
            using var scope = BuildProvider().CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var options = context.GetService<IDbContextOptions>();

            // CreateProxy throws when proxies are not enabled.
            var user = context.CreateProxy<User>(x => x.AuthId = "a");
            user.GetType().ShouldNotBe(typeof(User));
            RelationalOptionsExtension.Extract(options).MigrationsAssembly.ShouldBe(typeof(AppDbContext).Assembly.FullName);
        }

        [Fact]
        public void AddMyAppPersistence_ScopeDisposed_ReusesPooledContext()
        {
            var provider = BuildProvider();
            AppDbContext first;
            using (var scope = provider.CreateScope())
                first = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            using var secondScope = provider.CreateScope();
            var second = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();

            second.ShouldBeSameAs(first);
        }

        private static ServiceProvider BuildProvider()
        {
            return new ServiceCollection()
                .AddMyAppPersistence(ConnectionString)
                .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }
    }
}
