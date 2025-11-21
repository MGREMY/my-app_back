using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyApp.Domain.Model.Extension;

public static class AppDbContextExtension
{
    /// <summary>
    /// Apply script before and after pending migrations.
    ///
    /// Each migration SQL script must be in a folder with the same name as the migration
    /// (copy/paste the filename from the migration without the extension).
    ///
    /// Each script must be in either the "Before" or "After" folder ; depending
    /// on when you want it to be executed.
    /// </summary>
    /// <param name="dbContext"><see cref="AppDbContext"/> from where the migration must be executed</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> mandatory logger factory</param>
    /// <param name="ct"><see cref="CancellationToken"/> cancellation token</param>
    /// <returns>If all pending migrations have successfully been applied or not</returns>
    ///
    /// <remarks>
    /// If any migration or SQL script didn't work correctly, every pending migrations and SQL script are rolled back
    /// in order to prevent data loss.
    /// </remarks>
    public static async Task<bool> MigrateAndExecSqlScriptAsync(
        this AppDbContext dbContext,
        ILoggerFactory loggerFactory,
        CancellationToken ct = default)
    {
        var logger = loggerFactory.CreateLogger(typeof(AppDbContextExtension));
        var migrator = dbContext.GetInfrastructure().GetRequiredService<IMigrator>();

        logger.LogInformation("Preparing migrations");
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(ct)
            .ContinueWith(x => x.Result.ToList(), ct);
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct)
            .ContinueWith(x => x.Result.ToArray(), ct);
        var sqlScriptDirectory = Path.Combine(AppContext.BaseDirectory, "SqlScript");
        var sqlScriptDirectoryExists = Directory.Exists(sqlScriptDirectory);

        if (!sqlScriptDirectoryExists) logger.LogWarning("No SqlScript folder found. No SqlScript will be executed");

        logger.LogInformation("Starting migrations ; Total count of migrations: {count}", pendingMigrations.Length);

        // Creating a transaction ; if something fails, everything is rollback
        var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var pendingMigration in pendingMigrations)
            {
                var currentMigrationSqlScriptDirectory = Path.Combine(
                    sqlScriptDirectory,
                    pendingMigration);
                var beforeCurrentMigrationSqlScriptDirectory = Path.Combine(
                    currentMigrationSqlScriptDirectory,
                    "Before");
                var afterCurrentMigrationSqlScriptDirectory = Path.Combine(
                    currentMigrationSqlScriptDirectory,
                    "After");

                if (sqlScriptDirectoryExists)
                {
                    if (Directory.Exists(beforeCurrentMigrationSqlScriptDirectory))
                    {
                        logger.LogInformation("Starting sql scripts in {directoryName} directory",
                            beforeCurrentMigrationSqlScriptDirectory);

                        await FindAndExecuteSqlScriptInDirectory(beforeCurrentMigrationSqlScriptDirectory);
                    }
                }

                logger.LogInformation("Starting migration {name}", pendingMigration);
                var migrationScript = migrator.GenerateScript(
                    appliedMigrations.LastOrDefault(),
                    pendingMigration,
                    MigrationsSqlGenerationOptions.NoTransactions);
                await dbContext.Database.ExecuteSqlRawAsync(migrationScript, ct);
                appliedMigrations.Add(pendingMigration);

                if (sqlScriptDirectoryExists)
                {
                    if (Directory.Exists(afterCurrentMigrationSqlScriptDirectory))
                    {
                        logger.LogInformation("Starting sql scripts in {directoryName} directory",
                            afterCurrentMigrationSqlScriptDirectory);

                        await FindAndExecuteSqlScriptInDirectory(afterCurrentMigrationSqlScriptDirectory);
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogCritical("Error while migrating the database: {exception}", e);
            logger.LogCritical("Exiting and rollbock all changes made to avoid conflicts or lost data");

            await transaction.RollbackAsync(ct);

            Environment.Exit(1);

            return false;
        }

        await transaction.CommitAsync(ct);

        logger.LogInformation("Ending migrations");
        
        return true;

        async Task FindAndExecuteSqlScriptInDirectory(string directory)
        {
            foreach (var sqlScript in Directory.GetFiles(
                         path: directory,
                         searchPattern: "*.sql",
                         searchOption: SearchOption.AllDirectories))
            {
                logger.LogInformation("Starting {sqlScript}", sqlScript);
                await dbContext.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(sqlScript, ct), ct);
            }
        }
    }
}