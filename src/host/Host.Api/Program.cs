using Domain.Model;
using Domain.Model.Extension;
using FastEndpoints;
using Host.Api.Extension;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable format // @formatter:off
builder
    .AddServiceDefaults()
    .AddApiAuthentication(options =>
    {
        options.Domain = builder.Configuration["AuthDomain"] ?? string.Empty;
        options.Audience = builder.Configuration["AuthAudience"] ?? string.Empty;
    })
    .AddApiAuthorization(options =>
    {
        options.Origins = builder.Configuration["AuthOrigins"]?.Split(';') ?? [];
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
    .UseDomainExceptionHandler()
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