using MyApp.Host.Api.Extension;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Model;
using MyApp.Domain.Model.Extension;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable format // @formatter:off
builder
    .AddServiceDefaults()
    .AddApiAuthentication(options =>
    {
        options.Domain = builder.Configuration["auth_domain"] ?? string.Empty;
        options.Audience = builder.Configuration["auth_audience"] ?? string.Empty;
    })
    .AddApiAuthorization(options =>
    {
        options.Origins = builder.Configuration["auth_origins"]?.Split(';') ?? [];
    })
    .AddMyAppApiService();
#pragma warning restore format // @formatter:on

builder.Services.AddFastEndpoints();

builder
    .AddApiHealthChecks()
    .AddApiOpenApi();

var app = builder.Build();
app
    .UseApiAuthorization()
    .UseDefaultExceptionHandler()
    .UseFastEndpoints(options =>
    {
        options.Versioning.Prefix = "v";
        options.Versioning.DefaultVersion = 1;
        options.Versioning.PrependToRoute = true;
    });

app
    .MapApiOpenApiEndpoints()
    .MapDefaultEndpoint(ServiceDefaultConstant.HealthEndpointPath, ServiceDefaultConstant.AliveEndpointPath)
    .UseAuthentication()
    .UseAuthorization()
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