using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Models.SqlScriptMigrations;
using MyApp.Domain.Models.Tests.TestMigrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Design.Internal;

namespace MyApp.Domain.Models.Tests.SqlScriptMigrations;

public sealed partial class SqlScriptMigrationsGeneratorTests
{
    /// <summary>Builds the design-time services like the EF Core tools; no database connection is opened.</summary>
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit : IDisposable
    {
        private const string MigrationName = "AddProduct";

        private readonly ScriptedMigrationDbContext _context = ScriptedMigrationDbContext.Create("Host=localhost;Database=unused");
        private readonly ServiceProvider _services;

        public Unit()
        {
            // Same order as the EF Core tools: provider services, then the referenced design-time services.
            var services = new ServiceCollection()
                .AddEntityFrameworkDesignTimeServices()
                .AddDbContextDesignTimeServices(_context);
#pragma warning disable EF1001 // The EF Core tools also instantiate the provider design-time services.
            new NpgsqlDesignTimeServices().ConfigureDesignTimeServices(services);
#pragma warning restore EF1001
            new SqlScriptDesignTimeServices().ConfigureDesignTimeServices(services);
            _services = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            _services.Dispose();
            _context.Dispose();
        }

        [Fact]
        public void ScaffoldMigration_NewMigration_InheritsSqlScriptMigration()
        {
            var scaffolder = _services.GetRequiredService<IMigrationsScaffolder>();

            var migration = scaffolder.ScaffoldMigration(MigrationName, "MyApp.Domain.Models", "Migrations", language: null, dryRun: true);

            migration.MigrationCode.ShouldContain(
                $"public partial class {MigrationName} : global::{typeof(SqlScriptMigration).FullName}");
            migration.MigrationCode.ShouldNotContain($"{MigrationName} : Migration");
        }

        [Fact]
        public void Select_CSharp_ReturnsGeneratorRegisteredByDesignTimeServices()
        {
            var generator = _services.GetRequiredService<IMigrationsCodeGeneratorSelector>().Select(language: null);

            var code = generator.GenerateMigration("MyApp.Domain.Models.Migrations", MigrationName, [], []);

            code.ShouldContain($"public partial class {MigrationName} : global::{typeof(SqlScriptMigration).FullName}");
        }
    }
}
