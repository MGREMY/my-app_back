using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MyApp.Domain.Models.Tests.TestMigrations;

/// <summary>Context owning <see cref="ScriptedMigration"/> (scripts in SqlScripts/20260101000000_ScriptedMigration).</summary>
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
public sealed class FailingMigrationDbContext(DbContextOptions<FailingMigrationDbContext> options) : DbContext(options)
{
    public static FailingMigrationDbContext Create(string connectionString)
    {
        return new FailingMigrationDbContext(new DbContextOptionsBuilder<FailingMigrationDbContext>()
            .UseNpgsql(connectionString)
            .Options);
    }
}

/// <summary>Context owning <see cref="PartialMigration"/> (scripts in SqlScripts/20260104000000_PartialMigration).</summary>
public sealed class PartialMigrationDbContext(DbContextOptions<PartialMigrationDbContext> options) : DbContext(options)
{
    /// <param name="connectionString">Database to migrate.</param>
    /// <param name="warnings">Receives the messages of the non-transactional migration operation warning.</param>
    public static PartialMigrationDbContext Create(string connectionString, ICollection<string> warnings)
    {
        return new PartialMigrationDbContext(new DbContextOptionsBuilder<PartialMigrationDbContext>()
            .UseNpgsql(connectionString)
            .LogTo(warnings.Add, [RelationalEventId.NonTransactionalMigrationOperationWarning])
            .Options);
    }
}
