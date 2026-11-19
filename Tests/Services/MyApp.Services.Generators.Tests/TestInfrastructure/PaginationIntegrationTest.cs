using Microsoft.EntityFrameworkCore;
using MyApp.Services.Abstractions.Requests;
using MyApp.Services.Generators.Tests.TestModels;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Database;

namespace MyApp.Services.Generators.Tests.TestInfrastructure;

/// <summary>
/// Base of the integration tests: runs the generated <c>ProcessPaginationRequest</c> on the real PostgreSQL container,
/// so every generated expression must be translated by EF Core / Npgsql.
/// </summary>
/// <remarks>
/// Tests only read <see cref="PaginationTestData" />: the database is emptied and seeded once per run, by the first
/// test of the <see cref="DatabaseCollection" />.
/// </remarks>
public abstract class PaginationIntegrationTest(ContainersFixture containers) : IAsyncLifetime
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private static bool _seeded;

    protected PaginationTestDbContext Db { get; private set; } = null!;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await containers.EnsureStartedAsync();
        Db = PaginationTestDbContext.Create(containers.DatabaseConnectionString);

        await SeedLock.WaitAsync(Ct);
        try
        {
            if (!_seeded)
            {
                await DatabaseSchema.CreateAsync(Db, Ct);
                await using (var reset = await DatabaseReset.CreateAsync(containers.DatabaseConnectionString, Ct))
                    await reset.ResetAsync();

                await PaginationTestData.SeedAsync(Db, Ct);
                Db.ChangeTracker.Clear();
                _seeded = true;
            }
        }
        finally
        {
            SeedLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        return Db.DisposeAsync();
    }

    /// <summary>Names of the authors matching <paramref name="filters" />, in name order.</summary>
    protected async Task<string[]> FilterAsync(params FilterServiceRequest[] filters)
    {
        var names = await Db.Authors
            .AsNoTracking()
            .ProcessPaginationRequest(Requests.Page(filters: filters), out _)
            .Select(x => x.Name)
            .ToListAsync(Ct);

        return names.Order(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Names of the authors in the order of <paramref name="sorts" />.</summary>
    protected Task<List<string>> SortAsync(params SortServiceRequest[] sorts)
    {
        return Db.Authors
            .AsNoTracking()
            .ProcessPaginationRequest(Requests.Page(sorts: sorts), out _)
            .Select(x => x.Name)
            .ToListAsync(Ct);
    }
}
