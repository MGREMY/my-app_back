using Domain.Model;
using Domain.Model.Extension;
using FluentValidation;
using Host.Api.Extension.Configuration;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddAppAuthorization()
    .AddAppAuthentication(options =>
    {
        options.Domain = builder.Configuration["AuthDomain"] ?? string.Empty;
        options.Audience = builder.Configuration["AuthAudience"] ?? string.Empty;
    })
    .AddAppCors()
    .AddAppServices()
    .AddAppHealthChecks()
    .AddAppOpenApi()
    .AddAppVersioning();

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>()
    .AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "XSRF-TOKEN";
        options.Cookie.HttpOnly = false;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

app
    .UseAppCors(options => { options.Origins = builder.Configuration["AuthOrigins"]?.Split(';') ?? []; })
    .UseAppAuthentication()
    .UseAppAuthorization()
    .UseAppDomainExceptionHandler()
    .UseAppOpenApi()
    .MapDefaultEndpoint(ServiceDefaultConstant.HealthEndpointPath, ServiceDefaultConstant.AliveEndpointPath)
    .UseAppEndpoints()
    .UseAntiforgery()
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
            try
            {
                await dbContext.MigrateAndExecSqlScriptAsync(scope.ServiceProvider
                    .GetRequiredService<ILoggerFactory>());
            }
            catch
            {
                Environment.Exit(1);
            }
        }
    }
}

app.Run();