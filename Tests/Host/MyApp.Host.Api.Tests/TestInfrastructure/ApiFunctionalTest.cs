using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Database;

namespace MyApp.Host.Api.Tests.TestInfrastructure;

/// <summary>
/// Base of the functional tests: the shared <see cref="ApiFactory"/> host, an empty database before each test.
/// Clients are anonymous until <c>AsUser()</c> / <c>AsAdmin()</c> (<see cref="ApiClientAuthExtensions"/>).
/// </summary>
public abstract class ApiFunctionalTest(ContainersFixture containers, ApiFactory factory) : IAsyncLifetime
{
    private readonly List<HttpClient> _clients = [];

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected ApiFactory Factory => factory;

    public async ValueTask InitializeAsync()
    {
        await factory.EnsureStartedAsync(containers);

        await using var reset = await DatabaseReset.CreateAsync(containers.DatabaseConnectionString, Ct);
        await reset.ResetAsync();
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

    protected Task<User> SeedUserAsync(string authId, bool deleted = false, string? userName = null)
    {
        return WithDbAsync(async db =>
        {
            var user = new User { AuthId = authId, UserName = userName ?? $"user-{authId}", Email = $"{authId}@example.test" }
                .SetCreatedAtData();
            if (deleted)
                user.SetSoftDeletableData();

            db.Users.Add(user);
            await db.SaveChangesAsync(Ct);

            return user;
        });
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
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        return (await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct)).ShouldNotBeNull();
    }

    protected static async Task<HttpValidationProblemDetails> ReadValidationProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        return (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Ct)).ShouldNotBeNull();
    }
}
