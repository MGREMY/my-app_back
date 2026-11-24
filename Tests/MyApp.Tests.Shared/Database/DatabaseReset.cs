using Npgsql;
using Respawn;
using Respawn.Graph;

namespace MyApp.Tests.Shared.Database;

/// <summary>
/// Empties every table of a PostgreSQL database (migration history excepted) between tests, with Respawn.
/// Create it once the schema exists (<see cref="DatabaseSchema.CreateAsync"/>) and reuse it: creating it reads the
/// schema to build the delete graph. Tests use <c>ContainersFixture.ResetAsync</c>, which keeps one per run.
/// </summary>
public sealed class DatabaseReset : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly Respawner _respawner;

    private DatabaseReset(NpgsqlConnection connection, Respawner respawner)
    {
        _connection = connection;
        _respawner = respawner;
    }

    public static async Task<DatabaseReset> CreateAsync(string connectionString, CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(ct);

            var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                TablesToIgnore = [new Table("__EFMigrationsHistory")]
            });

            return new DatabaseReset(connection, respawner);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public Task ResetAsync()
    {
        return _respawner.ResetAsync(_connection);
    }

    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}
