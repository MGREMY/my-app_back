using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace MyApp.Tests.Shared.Containers;

/// <summary>
/// Starts PostgreSQL and Redis once for a whole test assembly. Only the projects declaring
/// <c>[assembly: AssemblyFixture(typeof(ContainersFixture))]</c> start them; tests receive it through their constructor.
/// Images match the ones used by the AppHost in development.
/// </summary>
public sealed class ContainersFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:8.6").Build();

    public string DatabaseConnectionString => _postgres.GetConnectionString();
    public string CacheConnectionString => _redis.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
