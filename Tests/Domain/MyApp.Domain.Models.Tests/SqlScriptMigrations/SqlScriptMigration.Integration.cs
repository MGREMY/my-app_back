using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Models.Tests.TestMigrations;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Database;
using Npgsql;

namespace MyApp.Domain.Models.Tests.SqlScriptMigrations;

public sealed partial class SqlScriptMigrationTests
{
    /// <summary>Each test applies migrations to its own empty database (emptying tables is not enough here).</summary>
    [IntegrationTest]
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
