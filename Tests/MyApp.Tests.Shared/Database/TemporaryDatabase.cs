using Npgsql;

namespace MyApp.Tests.Shared.Database;

/// <summary>
/// Empty database created by <see cref="CreateAsync"/> on a PostgreSQL server and dropped on dispose.
/// For tests that need a pristine database (applying migrations), where emptying the tables is not enough.
/// </summary>
public sealed class TemporaryDatabase : IAsyncDisposable
{
    public string ConnectionString { get; }

    private readonly string _serverConnectionString;
    private readonly string _name;

    private TemporaryDatabase(string serverConnectionString, string name, string connectionString)
    {
        _serverConnectionString = serverConnectionString;
        _name = name;
        ConnectionString = connectionString;
    }

    /// <summary>Creates a database with a unique name on the server of <paramref name="serverConnectionString"/>.</summary>
    /// <param name="serverConnectionString">Connection string of the server (the container default database).</param>
    /// <param name="ct">Cancellation token of the creation.</param>
    public static async Task<TemporaryDatabase> CreateAsync(string serverConnectionString, CancellationToken ct = default)
    {
        var name = $"test_{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection(serverConnectionString))
        {
            await connection.OpenAsync(ct);
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync(ct);
        }

        var connectionString = new NpgsqlConnectionStringBuilder(serverConnectionString) { Database = name }.ConnectionString;

        return new TemporaryDatabase(serverConnectionString, name, connectionString);
    }

    public async ValueTask DisposeAsync()
    {
        // Pooled connections of the tests would keep the database in use.
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(_serverConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
