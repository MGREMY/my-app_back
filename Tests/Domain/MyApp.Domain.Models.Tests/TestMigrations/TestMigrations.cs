using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyApp.Domain.Models.SqlScriptMigrations;

namespace MyApp.Domain.Models.Tests.TestMigrations;

// Hand-written equivalents of generated migrations (the template ships without migrations).

[DbContext(typeof(ScriptedMigrationDbContext))]
[Migration(Id)]
public sealed class ScriptedMigration : SqlScriptMigration
{
    public const string Id = "20260101000000_ScriptedMigration";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "scripted_entity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_scripted_entity", x => x.id));

        // Requires the log table created by the Before scripts.
        migrationBuilder.Sql("INSERT INTO script_log (name) VALUES ('migration');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "scripted_entity");
    }
}

[DbContext(typeof(FailingMigrationDbContext))]
[Migration(Id)]
public sealed class FailingMigration : SqlScriptMigration
{
    public const string Id = "20260102000000_FailingMigration";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("INSERT INTO failing_log (name) VALUES ('migration');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}


/// <summary>
///     Class and file names unrelated to the scripts folder: scripts are looked up by the <see cref="MigrationAttribute"/>
///     id, so it gets the scripts of <see cref="ScriptedMigration"/>.
/// </summary>
[Migration(ScriptedMigration.Id)]
public sealed class MigrationNamedDifferently : SqlScriptMigration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }
}

/// <summary>Migration without scripts folder: only the generated operations run.</summary>
[Migration("20260103000000_UnscriptedMigration")]
public sealed class UnscriptedMigration : SqlScriptMigration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("SELECT 'unscripted';");
    }
}

/// <summary>Invalid migration: no <see cref="MigrationAttribute"/>, ignored by EF Core but rejected by SqlScriptMigration.</summary>
public sealed class MigrationWithoutAttribute : SqlScriptMigration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }
}
