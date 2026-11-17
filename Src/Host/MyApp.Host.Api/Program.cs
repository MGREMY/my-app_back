using FluentValidation;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Extensions;
using MyApp.Host.Api.Extension.Configuration;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddAppAuthorization()
    .AddAppAuthentication()
    .AddAppCors()
    .AddAppServices()
    .AddAppHealthChecks()
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
    .UseAppCors()
    .UseAppLocalization()
    .UseAppDomainExceptionHandler()
    .UseAppAuthentication()
    .UseAppAuthorization()
    .UseAppOpenApi()
    .MapDefaultEndpoint(ServiceDefaultConstant.HealthEndpointPath, ServiceDefaultConstant.AliveEndpointPath)
    .UseAppEndpoints()
    .UseAntiforgery();

if (app.Environment.IsDevelopment())
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.MigrateAndExecSqlScriptAsync(scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
    }

app.Run();