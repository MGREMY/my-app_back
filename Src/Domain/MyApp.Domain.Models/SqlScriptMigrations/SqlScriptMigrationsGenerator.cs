using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.Domain.Models.SqlScriptMigrations;

/// <summary>
///     Design-time migration code generator: makes every migration created by <c>dotnet ef migrations add</c>
///     inherit <see cref="SqlScriptMigration" /> instead of <c>Migration</c>, so generated files never need manual edits.
/// </summary>
internal sealed class SqlScriptMigrationsGenerator(
    MigrationsCodeGeneratorDependencies dependencies,
    CSharpMigrationsGeneratorDependencies csharpDependencies)
    : CSharpMigrationsGenerator(dependencies, csharpDependencies)
{
    public override string GenerateMigration(
        string? migrationNamespace,
        string migrationName,
        IReadOnlyList<MigrationOperation> upOperations,
        IReadOnlyList<MigrationOperation> downOperations)
    {
        var code = base.GenerateMigration(migrationNamespace, migrationName, upOperations, downOperations);

        return code.Replace(
            $"public partial class {migrationName} : Migration",
            $"public partial class {migrationName} : global::{typeof(SqlScriptMigration).FullName}");
    }
}

/// <summary>
///     Registers the design-time services of the migrations assembly (picked up by the EF Core tools).
/// </summary>
public sealed class SqlScriptDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        services.AddSingleton<IMigrationsCodeGenerator, SqlScriptMigrationsGenerator>();
    }
}
