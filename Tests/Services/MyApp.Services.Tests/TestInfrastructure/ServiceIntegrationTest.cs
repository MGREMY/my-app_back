using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Database;

namespace MyApp.Services.Tests.TestInfrastructure;

/// <summary>
/// Base of the service integration tests: real composition on the PostgreSQL and Redis containers, empty database
/// before each test. Each test gets its own service provider, hence its own in-memory (L1) cache; Redis (L2) is shared.
/// </summary>
public abstract class ServiceIntegrationTest(ContainersFixture containers) : IAsyncLifetime
{
    private readonly List<ServiceProvider> _providers = [];
    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), $"myapp-tests-{Guid.NewGuid():N}");

    /// <summary>Provider of the test, as one API instance.</summary>
    protected ServiceProvider Provider { get; private set; } = null!;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await containers.EnsureStartedAsync();

        Provider = CreateProvider();

        await using (var scope = Provider.CreateAsyncScope())
            await DatabaseSchema.CreateAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), Ct);

        await using var reset = await DatabaseReset.CreateAsync(containers.DatabaseConnectionString, Ct);
        await reset.ResetAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var provider in _providers)
            await provider.DisposeAsync();

        if (Directory.Exists(_dataPath))
            Directory.Delete(_dataPath, recursive: true);
    }

    /// <summary>Another provider on the same infrastructure, as a second API instance (empty L1 cache).</summary>
    protected ServiceProvider CreateProvider()
    {
        var provider = ServiceComposition.Build(containers.DatabaseConnectionString, containers.CacheConnectionString, _dataPath);
        _providers.Add(provider);

        return provider;
    }

    /// <summary>Resolves <typeparamref name="TService"/> in a new scope and runs <paramref name="action"/>.</summary>
    protected async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action, ServiceProvider? provider = null)
        where TService : notnull
    {
        await using var scope = (provider ?? Provider).CreateAsyncScope();

        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    /// <summary>Runs <paramref name="action"/> on a fresh context, bypassing services and cache.</summary>
    protected async Task<TResult> WithDbAsync<TResult>(Func<AppDbContext, Task<TResult>> action)
    {
        await using var scope = Provider.CreateAsyncScope();

        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected Task<User> SeedUserAsync(string authId, bool deleted = false)
    {
        return WithDbAsync(async db =>
        {
            var user = new User { AuthId = authId, UserName = $"user-{authId}", Email = $"{authId}@example.test" }.SetCreatedAtData();
            if (deleted)
                user.SetSoftDeletableData();

            db.Users.Add(user);
            await db.SaveChangesAsync(Ct);

            return user;
        });
    }

    /// <summary>Changes the user name in the database only (the cache is not touched).</summary>
    protected Task<int> RenameInDatabaseAsync(Guid id, string userName)
    {
        return WithDbAsync(db => db.Users
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(u => u.UserName, userName), Ct));
    }

    protected Task<User?> FindUserIncludingDeletedAsync(Guid id)
    {
        return WithDbAsync(db => db.Users
            .AsNoTracking()
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .FirstOrDefaultAsync(x => x.Id == id, Ct));
    }
}
