using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;
using MyApp.Tests.Shared.Database;

namespace MyApp.Services.Tests.TestInfrastructure;

/// <summary>
/// Base of the service integration tests: real composition on the PostgreSQL and Redis containers, empty database
/// before each test. Each test gets its own service provider, hence its own in-memory (L1) cache; Redis (L2) is shared.
/// </summary>
/// <param name="containers">Containers of the assembly, reset before each test.</param>
public abstract class ServiceIntegrationTest(ContainersFixture containers) : IAsyncLifetime
{
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    // The schema is created once per run, by the first test (the classes of the Database collection run one at a time).
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _schemaCreated;

    /// <summary>Provider of the test, as one API instance.</summary>
    protected ServiceProvider Provider { get; private set; } = null!;

    private readonly List<ServiceProvider> _providers = [];
    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), $"myapp-tests-{Guid.NewGuid():N}");

    public async ValueTask InitializeAsync()
    {
        await containers.EnsureStartedAsync();

        Provider = CreateProvider();

        await SchemaLock.WaitAsync(Ct);
        try
        {
            if (!_schemaCreated)
            {
                await using var scope = Provider.CreateAsyncScope();
                await DatabaseSchema.CreateAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), Ct);
                _schemaCreated = true;
            }
        }
        finally
        {
            SchemaLock.Release();
        }

        // Database and Redis; the in-memory (L1) cache is new with each provider.
        await containers.ResetAsync(Ct);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var provider in _providers)
            await provider.DisposeAsync();

        if (Directory.Exists(_dataPath))
            Directory.Delete(_dataPath, recursive: true);
    }

    /// <summary>Another provider on the same infrastructure, as a second API instance (empty L1 cache).</summary>
    /// <param name="configure">Test-only additions on top of the host composition (interceptors...).</param>
    protected ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null)
    {
        var provider = ServiceComposition.Build(containers.DatabaseConnectionString, containers.CacheConnectionString, _dataPath, configure);
        _providers.Add(provider);

        return provider;
    }

    /// <summary>Resolves <typeparamref name="TService"/> in a new scope and runs <paramref name="action"/>.</summary>
    /// <typeparam name="TService">Service to resolve.</typeparam>
    /// <typeparam name="TResult">Result of <paramref name="action"/>.</typeparam>
    /// <param name="action">Call to make on the service.</param>
    /// <param name="provider">Provider to resolve from (another API instance), <see cref="Provider"/> by default.</param>
    protected async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action, ServiceProvider? provider = null)
        where TService : notnull
    {
        await using var scope = (provider ?? Provider).CreateAsyncScope();

        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    /// <summary>Reads a user through <see cref="IUserGetById"/> (cache included), on <paramref name="provider"/> if given.</summary>
    /// <param name="id">Id of the user to read.</param>
    /// <param name="provider">Provider to resolve from (another API instance), <see cref="Provider"/> by default.</param>
    protected Task<ServiceResponse<UserServiceResponse>> GetUserByIdAsync(Guid id, ServiceProvider? provider = null)
    {
        return RunAsync<IUserGetById, ServiceResponse<UserServiceResponse>>(x => x.ExecuteAsync(id, Ct), provider);
    }

    /// <summary>Runs <paramref name="action"/> on a fresh context, bypassing services and cache.</summary>
    /// <typeparam name="TResult">Result of <paramref name="action"/>.</typeparam>
    /// <param name="action">Work to do on the context.</param>
    protected async Task<TResult> WithDbAsync<TResult>(Func<AppDbContext, Task<TResult>> action)
    {
        await using var scope = Provider.CreateAsyncScope();

        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>
    /// Inserts the whole <see cref="TestUsers"/> dataset (soft-deleted users included), as new entities.
    /// Returns them in the order of <see cref="TestUsers.All"/>, with their generated values (creation date...).
    /// </summary>
    protected Task<User[]> SeedUsersAsync()
    {
        return WithDbAsync(async db =>
        {
            var users = TestUsers.All.Select(ToUser).ToArray();
            db.Users.AddRange(users);
            await db.SaveChangesAsync(Ct);

            return users;
        });
    }

    /// <summary>New entity for <paramref name="data"/>, not attached to any context.</summary>
    /// <param name="data">User of <see cref="TestUsers"/>.</param>
    protected static User ToUser(TestUserData data)
    {
        var user = new User { Id = data.Id, AuthId = data.AuthId, UserName = data.UserName, Email = data.Email }.SetCreatedAtData();
        if (data.IsDeleted)
            user.SetSoftDeletableData();

        return user;
    }

    /// <summary>Changes the user name in the database only (the cache is not touched).</summary>
    /// <param name="id">Id of the user to rename, soft-deleted ones included.</param>
    /// <param name="userName">New user name.</param>
    protected Task<int> RenameInDatabaseAsync(Guid id, string userName)
    {
        return WithDbAsync(db => db.Users
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(u => u.UserName, userName), Ct));
    }

    /// <summary>Number of users in the database, soft-deleted ones included.</summary>
    protected Task<int> CountUsersIncludingDeletedAsync()
    {
        return WithDbAsync(db => db.Users.IgnoreQueryFilters([ModelConstant.SoftDeletionFilter]).CountAsync(Ct));
    }

    protected Task<User?> FindUserIncludingDeletedAsync(Guid id)
    {
        return WithDbAsync(db => db.Users
            .AsNoTracking()
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .FirstOrDefaultAsync(x => x.Id == id, Ct));
    }

    /// <summary>
    /// Retries <paramref name="assertion"/> until it passes, for effects propagated in the background (cache
    /// invalidations sent to the other instances by the backplane). After <paramref name="timeout"/> (5 s by default),
    /// the last failure is rethrown.
    /// </summary>
    /// <param name="assertion">Assertion to retry; only Shouldly failures are retried.</param>
    /// <param name="timeout">How long to retry.</param>
    protected static async Task EventuallyAsync(Func<Task> assertion, TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));

        while (true)
        {
            try
            {
                await assertion();
                return;
            }
            catch (ShouldAssertException) when (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), Ct);
            }
        }
    }
}
