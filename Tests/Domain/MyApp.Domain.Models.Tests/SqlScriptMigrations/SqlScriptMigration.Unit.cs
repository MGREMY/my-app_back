using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MyApp.Domain.Models.Tests.TestMigrations;

namespace MyApp.Domain.Models.Tests.SqlScriptMigrations;

public sealed partial class SqlScriptMigrationTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        private static readonly string[] ExpectedBefore =
        [
            "Before/01_create_log.sql",
            "Before/02_nested/01_first.sql",
            "Before/02_nested/02_second/01_deep.sql",
            "Before/10_ten.sql",
            "Before/2_two.sql",
            "Before/B_upper.sql",
            "Before/a_lower.sql"
        ];

        private static readonly string[] ExpectedAfter =
        [
            "After/01_seed.sql",
            "After/02_ix_name.no-transaction.sql",
            "After/03_last.sql"
        ];

        [Fact]
        public void UpOperations_WithScripts_RunsBeforeScriptsThenMigrationThenAfterScripts()
        {
            var operations = new ScriptedMigration().UpOperations;

            operations.Count.ShouldBe(ExpectedBefore.Length + 2 + ExpectedAfter.Length);
            operations.Take(ExpectedBefore.Length).Select(ScriptName).ShouldBe(ExpectedBefore);
            operations[ExpectedBefore.Length].ShouldBeOfType<CreateTableOperation>().Name.ShouldBe("scripted_entity");
            operations[ExpectedBefore.Length + 1].ShouldBeOfType<SqlOperation>().Sql.ShouldContain("'migration'");
            operations.TakeLast(ExpectedAfter.Length).Select(ScriptName).ShouldBe(ExpectedAfter);
        }

        [Fact]
        public void UpOperations_NestedFolders_OrdersByOrdinalRelativePath()
        {
            // 02_nested/ is walked recursively, "10_" sorts before "2_" and uppercase before lowercase.
            var names = new ScriptedMigration().UpOperations.OfType<SqlOperation>().Select(ScriptName).ToArray();

            names.Where(x => x.StartsWith("Before/")).ShouldBe(ExpectedBefore);
        }

        [Fact]
        public void UpOperations_NoTransactionSuffix_SuppressesTransactionOfThatScriptOnly()
        {
            var operations = new ScriptedMigration().UpOperations.OfType<SqlOperation>().ToArray();

            operations.Where(x => x.SuppressTransaction).Select(ScriptName).ShouldBe(["After/02_ix_name.no-transaction.sql"]);
        }

        [Fact]
        public void UpOperations_OtherFilesAndMigrations_AreIgnored()
        {
            var operations = new ScriptedMigration().UpOperations.OfType<SqlOperation>().ToArray();

            // 04_ignored.txt (not .sql) and SqlScripts/20260101000000_ScriptedMigrationExtra (other id) contain 1/0.
            operations.ShouldNotContain(x => x.Sql.Contains("1/0"));
        }

        [Fact]
        public void UpOperations_WithoutScripts_ReturnsGeneratedOperationsOnly()
        {
            var operations = new UnscriptedMigration().UpOperations;

            operations.ShouldHaveSingleItem().ShouldBeOfType<SqlOperation>().Sql.ShouldBe("SELECT 'unscripted';");
        }

        [Fact]
        public void UpOperations_ReadTwice_ReturnsSameOperations()
        {
            var migration = new ScriptedMigration();

            var first = migration.UpOperations;
            var second = migration.UpOperations;

            second.ShouldBeSameAs(first);
        }

        [Fact]
        public void UpOperations_WithoutMigrationAttribute_ThrowsInvalidOperationException()
        {
            var migration = new MigrationWithoutAttribute();

            var exception = Should.Throw<InvalidOperationException>(() => migration.UpOperations);

            exception.Message.ShouldContain(nameof(MigrationWithoutAttribute));
        }

        [Fact]
        public void GenerateScript_Idempotent_ContainsScriptsAroundMigration()
        {
            using var context = ScriptedMigrationDbContext.Create("Host=localhost;Database=unused");

            var script = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

            var positions = new[] { "Before/01_create_log.sql", "CREATE TABLE scripted_entity", "After/03_last.sql" }
                .Select(x => script.IndexOf(x, StringComparison.Ordinal))
                .ToArray();
            positions.ShouldAllBe(x => x >= 0);
            positions.ShouldBeInOrder();
        }

        private static string ScriptName(MigrationOperation operation)
        {
            // Every test script starts with "-- <Stage>/<relative path>".
            return operation.ShouldBeOfType<SqlOperation>().Sql.Split('\n')[0]["-- ".Length..].Trim();
        }
    }
}
