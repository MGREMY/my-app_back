using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Data;

namespace MyApp.Host.Api.Tests.TestInfrastructure;

/// <summary>
/// Base of the functional tests: the shared <see cref="ApiFactory"/> host, an empty database before each test.
/// Clients are anonymous until <c>AsUser()</c> / <c>AsAdmin()</c> (<see cref="ApiClientAuthExtensions"/>).
/// </summary>
/// <param name="containers">Containers of the assembly, reset before each test.</param>
/// <param name="factory">API host shared by the classes of the Database collection.</param>
public abstract class ApiFunctionalTest(ContainersFixture containers, ApiFactory factory) : IAsyncLifetime
{
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected ApiFactory Factory => factory;

    private readonly List<HttpClient> _clients = [];

    public async ValueTask InitializeAsync()
    {
        await factory.EnsureStartedAsync(containers);

        await containers.ResetAsync(Ct);
        // The host is shared by every test: its in-memory (L1) cache survives the Redis flush. "*" invalidates every entry.
        await factory.Services.GetRequiredService<HybridCache>().RemoveByTagAsync("*", Ct);
    }

    public ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
            client.Dispose();

        return ValueTask.CompletedTask;
    }

    /// <summary>New anonymous client (own cookies and default headers).</summary>
    protected HttpClient CreateClient()
    {
        var client = factory.CreateClient();
        _clients.Add(client);

        return client;
    }

    protected async Task<TResult> WithDbAsync<TResult>(Func<AppDbContext, Task<TResult>> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();

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

    /// <summary>Number of users in the database, soft-deleted ones included.</summary>
    protected Task<int> CountUsersIncludingDeletedAsync()
    {
        return WithDbAsync(db => db.Users.IgnoreQueryFilters([ModelConstant.SoftDeletionFilter]).CountAsync(Ct));
    }

    protected Task<User?> FindUserIncludingDeletedAsync(Guid id)
    {
        // Deleted rows are part of the expected state of these tests.
        return WithDbAsync(db => db.Users
            .AsNoTracking()
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .FirstOrDefaultAsync(x => x.Id == id, Ct));
    }

    protected Task<User[]> FindUsersIncludingDeletedAsync(string authId)
    {
        // Deleted rows are part of the expected state of these tests.
        return WithDbAsync(db => db.Users
            .AsNoTracking()
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .Where(x => x.AuthId == authId)
            .ToArrayAsync(Ct));
    }

    protected static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");

        return (await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct)).ShouldNotBeNull();
    }

    protected static async Task<HttpValidationProblemDetails> ReadValidationProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");

        return (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Ct)).ShouldNotBeNull();
    }
}
