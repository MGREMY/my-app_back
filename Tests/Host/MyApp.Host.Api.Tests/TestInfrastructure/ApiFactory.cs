using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using MyApp.Domain.Models;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Database;

namespace MyApp.Host.Api.Tests.TestInfrastructure;

/// <summary>
/// The real API (<c>Program</c>) on the PostgreSQL and Redis containers, in the <see cref="EnvironmentName"/>
/// environment: not Development, so neither appsettings.Development.json nor the startup migration are used. Every
/// required <c>Api:*</c> setting is given explicitly; the schema is created once with <see cref="DatabaseSchema"/>.
/// </summary>
/// <remarks>
/// Shared by the <see cref="DatabaseCollection"/> and started lazily by <see cref="EnsureStartedAsync"/>, so a
/// unit-only run never starts Docker nor the host.
/// BFF switch: authentication is replaced here (<see cref="TestAuthHandler"/>); with
/// <see cref="ApiClientAuthExtensions"/>, it is the only code to adapt.
/// </remarks>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string EnvironmentName = "Testing";
    public const string CorsOrigin = "https://front.example.test";

    /// <summary>Test-only endpoint answering 204 when the antiforgery token of a POST is valid, 400 otherwise.</summary>
    public const string AntiforgeryProbePath = "/__tests/antiforgery";

    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), $"myapp-api-tests-{Guid.NewGuid():N}");
    private readonly Lock _lock = new();
    private ContainersFixture? _containers;
    private Task? _start;

    public Task EnsureStartedAsync(ContainersFixture containers)
    {
        lock (_lock)
            return _start ??= StartAsync(containers);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var containers = _containers ?? throw new InvalidOperationException($"Call {nameof(EnsureStartedAsync)} first.");

        builder
            .UseEnvironment(EnvironmentName)
            .UseSetting("Api:Database:ConnectionString", containers.DatabaseConnectionString)
            .UseSetting("Api:Cache:ConnectionString", containers.CacheConnectionString)
            .UseSetting("Api:Auth:Domain", "https://auth.example.test/realms/test")
            .UseSetting("Api:Auth:Audience", "myApp-tests")
            .UseSetting("Api:Cors:Origins:0", CorsOrigin)
            .UseSetting("Api:Services:DataPath", _dataPath)
            .ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);

                // Program sets JWT bearer as default: the test scheme replaces it for every operation.
                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                });

                services.AddTransient<IStartupFilter, AntiforgeryProbeStartupFilter>();
            });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (Directory.Exists(_dataPath))
            Directory.Delete(_dataPath, recursive: true);
    }

    private async Task StartAsync(ContainersFixture containers)
    {
        await containers.EnsureStartedAsync();
        _containers = containers;

        await using var scope = Services.CreateAsyncScope();
        await DatabaseSchema.CreateAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>
    /// No API endpoint validates antiforgery tokens yet: this probe validates the token sent by
    /// <see cref="ApiClientAuthExtensions.WithAntiforgeryTokenAsync"/>, to test the documented flow end to end.
    /// </summary>
    private sealed class AntiforgeryProbeStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Map(AntiforgeryProbePath, probe => probe.Run(async context =>
                {
                    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                    context.Response.StatusCode = await antiforgery.IsRequestValidAsync(context)
                        ? StatusCodes.Status204NoContent
                        : StatusCodes.Status400BadRequest;
                }));

                next(app);
            };
        }
    }
}
