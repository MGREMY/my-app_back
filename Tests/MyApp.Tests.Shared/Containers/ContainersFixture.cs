using MyApp.Tests.Shared.Database;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace MyApp.Tests.Shared.Containers;

/// <summary>
/// Starts PostgreSQL and Redis once for a whole test assembly. Only the projects declaring
/// <c>[assembly: AssemblyFixture(typeof(ContainersFixture))]</c> use it; tests receive it through their constructor.
/// Images match the ones used by the AppHost in development.
/// </summary>
/// <remarks>
/// xUnit creates and initializes assembly fixtures even when a filter only selects unit tests, so the containers are not
/// started by xUnit (the fixture deliberately does not implement <see cref="IAsyncLifetime"/>) but lazily:
/// tests needing them call <see cref="EnsureStartedAsync"/> first (typically in <c>InitializeAsync</c>). A unit-only run
/// (<c>--filter-trait "Category=Unit"</c>) therefore never needs Docker.
/// </remarks>
public sealed class ContainersFixture : IAsyncDisposable
{
    /// <summary>Available once <see cref="EnsureStartedAsync"/> has completed.</summary>
    public string DatabaseConnectionString => Started(_postgres.GetConnectionString);

    /// <summary>Available once <see cref="EnsureStartedAsync"/> has completed.</summary>
    public string CacheConnectionString => Started(_redis.GetConnectionString);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:8.6").Build();
    private readonly Lazy<Task> _start;
    private readonly SemaphoreSlim _resetLock = new(1, 1);
    private DatabaseReset? _databaseReset;
    private ConnectionMultiplexer? _cache;

    public ContainersFixture()
    {
        _start = new Lazy<Task>(() => Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync()));
    }

    /// <summary>Starts both containers on the first call; later calls wait for the same start.</summary>
    public Task EnsureStartedAsync()
    {
        return _start.Value;
    }

    /// <summary>
    /// Empties the default database (every table except the migration history) and the Redis cache, so each test starts
    /// from a clean state. Call it once the schema exists: the tables to empty are read on the first call and reused
    /// for the whole run (the schema of a test assembly does not change during the run).
    /// </summary>
    /// <remarks>
    /// Only resets the containers: an API host shared by several tests keeps its in-memory (L1) cache, which its base
    /// class clears itself.
    /// </remarks>
    /// <param name="ct">Cancellation token of the reset, waiting for a reset in progress included.</param>
    public async Task ResetAsync(CancellationToken ct = default)
    {
        await _resetLock.WaitAsync(ct);
        try
        {
            _databaseReset ??= await DatabaseReset.CreateAsync(DatabaseConnectionString, ct);
            _cache ??= await ConnectionMultiplexer.ConnectAsync($"{CacheConnectionString},allowAdmin=true");

            await _databaseReset.ResetAsync();
            foreach (var server in _cache.GetServers())
                await server.FlushAllDatabasesAsync();
        }
        finally
        {
            _resetLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_databaseReset is not null)
            await _databaseReset.DisposeAsync();
        if (_cache is not null)
            await _cache.DisposeAsync();

        _resetLock.Dispose();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    private string Started(Func<string> value)
    {
        if (!_start.IsValueCreated || !_start.Value.IsCompletedSuccessfully)
            throw new InvalidOperationException($"Call {nameof(EnsureStartedAsync)} before using the containers.");

        return value();
    }
}
