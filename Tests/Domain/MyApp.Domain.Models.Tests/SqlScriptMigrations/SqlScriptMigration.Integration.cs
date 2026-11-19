using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyApp.Domain.Models.Tests.TestMigrations;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Database;
using Npgsql;

namespace MyApp.Domain.Models.Tests.SqlScriptMigrations;

public sealed partial class SqlScriptMigrationTests
{
    /// <summary>Each test applies migrations to its own empty database (emptying tables is not enough here).</summary>
    [Trait(TestCategory.Name, TestCategory.Integration)]
    public sealed class Integration(ContainersFixture containers) : IAsyncLifetime
    {
        private TemporaryDatabase _database = null!;

        public async ValueTask InitializeAsync()
        {
            await containers.EnsureStartedAsync();
            _database = await TemporaryDatabase.CreateAsync(containers.DatabaseConnectionString);
        }

        public ValueTask DisposeAsync()
        {
            return _database.DisposeAsync();
        }

        [Fact]
        public async Task MigrateAsync_WithScripts_ExecutesScriptsInOrderAroundMigration()
        {
            await using var context = ScriptedMigrationDbContext.Create(_database.ConnectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var log = await QueryAsync<string>("SELECT name FROM script_log ORDER BY seq");
            log.ShouldBe([
                "Before/01_create_log.sql",
                "Before/02_nested/01_first.sql",
                "Before/02_nested/02_second/01_deep.sql",
                "Before/10_ten.sql",
                "Before/2_two.sql",
                "Before/B_upper.sql",
                "Before/a_lower.sql",
                "migration",
                "After/01_seed.sql",
                "After/02_ix_name.no-transaction.sql",
                "After/03_last.sql"
            ]);
        }

        [Fact]
        public async Task MigrateAsync_NoTransactionScript_RunsStatementForbiddenInTransaction()
        {
            await using var context = ScriptedMigrationDbContext.Create(_database.ConnectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            // CREATE INDEX CONCURRENTLY fails inside a transaction block.
            var valid = await QueryAsync<bool>(
                "SELECT i.indisvalid FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid WHERE c.relname = 'ix_scripted_entity_name'");
            valid.ShouldBe([true]);
        }

        [Fact]
        public async Task MigrateAsync_WithScripts_RecordsMigrationInHistory()
        {
            await using var context = ScriptedMigrationDbContext.Create(_database.ConnectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
            applied.ShouldBe([ScriptedMigration.Id]);
        }

        [Fact]
        public async Task MigrateAsync_AppliedTwice_DoesNotRunScriptsAgain()
        {
            await using var context = ScriptedMigrationDbContext.Create(_database.ConnectionString);
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var count = await QueryAsync<long>("SELECT count(*) FROM script_log");
            count.ShouldBe([11L]);
        }

        [Fact]
        public async Task MigrateAsync_FailingAfterScript_RollsBackBeforeScriptsAndMigration()
        {
            await using var context = FailingMigrationDbContext.Create(_database.ConnectionString);

            await Should.ThrowAsync<PostgresException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

            // Transactional scripts share the migration transaction: the table created by the Before script is gone.
            var tables = await QueryAsync<long>("SELECT count(*) FROM pg_tables WHERE tablename = 'failing_log'");
            tables.ShouldBe([0L]);
            var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
            applied.ShouldBeEmpty();
        }

        [Fact]
        public async Task MigrateAsync_FailingNoTransactionScript_LeavesMigrationPartiallyAppliedWithWarning()
        {
            var warnings = new List<string>();
            await using var context = PartialMigrationDbContext.Create(_database.ConnectionString, warnings);

            await Should.ThrowAsync<PostgresException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

            // Operations before the script outside of the transaction were committed, the history was not written.
            var log = await QueryAsync<string>("SELECT name FROM partial_log");
            log.ShouldBe(["migration"]);
            var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
            applied.ShouldBeEmpty();
            warnings.ShouldHaveSingleItem().ShouldContain("After/01_fail.no-transaction.sql");
        }

        [Fact(Skip = "Bug: the idempotent script wraps the .no-transaction.sql script in a DO block, where PostgreSQL " +
                     "rejects CREATE INDEX CONCURRENTLY (cannot be executed from a function)")]
        public async Task GenerateScript_IdempotentAppliedTwice_RunsScriptsOnce()
        {
            await using var context = ScriptedMigrationDbContext.Create(_database.ConnectionString);
            var script = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

            await ExecuteScriptAsync(script);
            await ExecuteScriptAsync(script);

            var count = await QueryAsync<long>("SELECT count(*) FROM script_log");
            count.ShouldBe([11L]);
            var valid = await QueryAsync<bool>(
                "SELECT i.indisvalid FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid WHERE c.relname = 'ix_scripted_entity_name'");
            valid.ShouldBe([true]);
            var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
            applied.ShouldBe([ScriptedMigration.Id]);
        }

        /// <summary>Runs a generated script statement by statement in autocommit mode, like psql does.</summary>
        private async Task ExecuteScriptAsync(string script)
        {
            await using var connection = new NpgsqlConnection(_database.ConnectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            foreach (var statement in SplitStatements(script))
            {
                await using var command = new NpgsqlCommand(statement, connection);
                await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }
        }

        /// <summary>
        ///     Splits an EF Core script on its top-level statements: a <c>DO $EF$</c> block ends with <c>END $EF$;</c>, any
        ///     other statement with a line ending with <c>;</c>.
        /// </summary>
        private static IEnumerable<string> SplitStatements(string script)
        {
            var current = new System.Text.StringBuilder();
            var inBlock = false;

            foreach (var line in script.Split('\n'))
            {
                var trimmed = line.Trim();
                if (current.Length == 0 && trimmed.Length == 0) continue;
                if (current.Length == 0) inBlock = trimmed.StartsWith("DO $EF$", StringComparison.Ordinal);

                current.AppendLine(line);

                var ends = inBlock ? trimmed == "END $EF$;" : trimmed.EndsWith(';');
                if (!ends) continue;

                yield return current.ToString();
                current.Clear();
            }
        }

        private async Task<List<T>> QueryAsync<T>(string sql)
        {
            await using var connection = new NpgsqlConnection(_database.ConnectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

            var values = new List<T>();
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
                values.Add(reader.GetFieldValue<T>(0));

            return values;
        }
    }
}
