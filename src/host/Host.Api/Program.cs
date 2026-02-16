using System.Net.Mime;
using Domain.Model;
using Domain.Model.Extension;
using FastEndpoints;
using Host.Api;
using Host.Api.Extension.Configuration;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable format // @formatter:off
builder
    .AddServiceDefaults()
    .AddApiAuthorization()
    .AddApiAuthentication(options =>
    {
        options.Domain = builder.Configuration["AuthDomain"] ?? string.Empty;
        options.Audience = builder.Configuration["AuthAudience"] ?? string.Empty;
    })
    .AddMyAppApiService();
#pragma warning restore format // @formatter:on

builder.Services
    .AddCors()
    .AddFastEndpoints(options =>
    {
        options.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
    })
    .AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "XSRF-TOKEN";
        options.Cookie.HttpOnly = false;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder
    .AddApiHealthChecks()
    .AddApiOpenApi();

var app = builder.Build();

app.UseRouting();

app
    .UseApiCors(options => { options.Origins = builder.Configuration["AuthOrigins"]?.Split(';') ?? []; })
    .UseAuthentication()
    .UseAuthorization()
    .UseDomainExceptionHandler()
    .UseAntiforgeryFE()
    .UseFastEndpoints(options =>
    {
        options.Versioning.Prefix = "v";
        options.Versioning.DefaultVersion = 1;
        options.Versioning.PrependToRoute = true;
    });

app
    .MapApiOpenApiEndpoints()
    .MapDefaultEndpoint(ServiceDefaultConstant.HealthEndpointPath, ServiceDefaultConstant.AliveEndpointPath)
    .UseRequestLocalization(options =>
    {
        string[] supportedCultures = ["en", "fr"];
        options.SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
        options.ApplyCurrentCultureToResponseHeaders = true;
    });

if (app.Environment.IsDevelopment())
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        await using (var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>())
        {
            await dbContext.MigrateAndExecSqlScriptAsync(scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}

app.Run();