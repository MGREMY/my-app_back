using Npgsql;

namespace MyApp.Tests.Shared.Database;

/// <summary>
/// Empty PostgreSQL database created on the server of <paramref name="serverConnectionString"/> and dropped on dispose.
/// For tests that need a pristine database (applying migrations), where emptying the tables is not enough.
/// </summary>
public sealed class TemporaryDatabase : IAsyncDisposable
{
    private readonly string _serverConnectionString;
    private readonly string _name;

    private TemporaryDatabase(string serverConnectionString, string name, string connectionString)
    {
        _serverConnectionString = serverConnectionString;
        _name = name;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

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
