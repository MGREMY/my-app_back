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
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:8.6").Build();
    private readonly Lazy<Task> _start;

    public ContainersFixture()
    {
        _start = new Lazy<Task>(() => Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync()));
    }

    /// <summary>Available once <see cref="EnsureStartedAsync"/> has completed.</summary>
    public string DatabaseConnectionString => Started(_postgres.GetConnectionString);

    /// <summary>Available once <see cref="EnsureStartedAsync"/> has completed.</summary>
    public string CacheConnectionString => Started(_redis.GetConnectionString);

    /// <summary>Starts both containers on the first call; later calls wait for the same start.</summary>
    public Task EnsureStartedAsync()
    {
        return _start.Value;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    private string Started(Func<string> value)
    {
        if (!_start.IsValueCreated || !_start.Value.IsCompletedSuccessfully)
            throw new InvalidOperationException($"Call {nameof(EnsureStartedAsync)} before using the containers.");

        return value();
    }
}
