using Asp.Versioning;
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
    .AddAppOpenApi();

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>()
    .AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "XSRF-TOKEN";
        options.Cookie.HttpOnly = false;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddApiVersioning(options =>
    {
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.DefaultApiVersion = new ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        // Replace the placeholder with the actual version
        options.SubstituteApiVersionInUrl = true;
    });

var app = builder.Build();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

app
    .UseAppAuthentication()
    .UseAppAuthorization()
    .UseAppDomainExceptionHandler()
    .UseAppOpenApi()
    .UseAppCors(options => { options.Origins = builder.Configuration["AuthOrigins"]?.Split(';') ?? []; })
    .MapDefaultEndpoint(ServiceDefaultConstant.HealthEndpointPath, ServiceDefaultConstant.AliveEndpointPath)
    .UseAppEndpoints(apiVersionSet)
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