using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using ServiceDefaults.Extension;

namespace ServiceDefaults;

public static class Extensions
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        /// Add OpenTelemetry, self Health Check and Serilog to the application
        /// </summary>
        /// <param name="builder"></param>
        /// <typeparam name="TBuilder"><see cref="IHostApplicationBuilder"/></typeparam>
        /// <returns><see cref="IHostApplicationBuilder"/></returns>
        public TBuilder AddServiceDefaults()
        {
            builder
                .ConfigureOpenTelemetry(ServiceDefaultConstant.HealthEndpointPath,
                    ServiceDefaultConstant.AliveEndpointPath)
                .ConfigureHealthCheck()
                .ConfigureLog();

            return builder;
        }
    }

    extension(WebApplication application)
    {
        /// <summary>
        /// Map Health and Alive endpoint to the Application
        /// </summary>
        /// <param name="application"><see cref="WebApplication"/></param>
        /// <param name="healthEndpointPath">Endpoint for Health Check</param>
        /// <param name="aliveEndpointPath">Endpoint for Alive Check</param>
        /// <returns><see cref="WebApplication"/></returns>
        public WebApplication MapDefaultEndpoint(
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
}