using Microsoft.EntityFrameworkCore;

namespace MyApp.Tests.Shared.Database;

public static class DatabaseSchema
{
    /// <summary>
    /// Creates the schema of <paramref name="context"/>: applies the migrations (SQL scripts included) when the model
    /// has some, otherwise creates it from the model (the template ships without migrations).
    /// </summary>
    public static async Task CreateAsync(DbContext context, CancellationToken ct = default)
    {
        if (context.Database.GetMigrations().Any())
            await context.Database.MigrateAsync(ct);
        else
            await context.Database.EnsureCreatedAsync(ct);
    }
}
