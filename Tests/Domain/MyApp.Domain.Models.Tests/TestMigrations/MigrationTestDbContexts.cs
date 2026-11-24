using Microsoft.EntityFrameworkCore;

namespace MyApp.Domain.Models.Tests.TestMigrations;

/// <summary>Context owning <see cref="ScriptedMigration"/> (scripts in SqlScripts/20260101000000_ScriptedMigration).</summary>
/// <param name="options">Options built by <see cref="Create"/>.</param>
public sealed class ScriptedMigrationDbContext(DbContextOptions<ScriptedMigrationDbContext> options) : DbContext(options)
{
    public static ScriptedMigrationDbContext Create(string connectionString)
    {
        return new ScriptedMigrationDbContext(new DbContextOptionsBuilder<ScriptedMigrationDbContext>()
            .UseNpgsql(connectionString)
            .Options);
    }
}

/// <summary>Context owning <see cref="FailingMigration"/> (scripts in SqlScripts/20260102000000_FailingMigration).</summary>
/// <param name="options">Options built by <see cref="Create"/>.</param>
public sealed class FailingMigrationDbContext(DbContextOptions<FailingMigrationDbContext> options) : DbContext(options)
{
    public static FailingMigrationDbContext Create(string connectionString)
    {
        return new FailingMigrationDbContext(new DbContextOptionsBuilder<FailingMigrationDbContext>()
            .UseNpgsql(connectionString)
            .Options);
    }
}

