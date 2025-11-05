using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder
            .ConfigureOpenTelemetry(ServiceDefaultConstant.HealthEndpointPath, ServiceDefaultConstant.AliveEndpointPath)
            .ConfigureHealthCheck()
            .ConfigureLog();

        return builder;
    }

    public static WebApplication MapDefaultEndpoint(
        this WebApplication application,
        string healthEndpointPath,
        string aliveEndpointPath)
    {
        if (application.Environment.IsDevelopment())
        {
            application.MapHealthChecks(healthEndpointPath, new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            });

            application.MapHealthChecks(aliveEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("self")
            });
        }

        return application;
    }
}