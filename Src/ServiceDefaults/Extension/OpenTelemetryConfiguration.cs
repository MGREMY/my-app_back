using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ServiceDefaults.Extension;

internal static class OpenTelemetryConfiguration
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        internal TBuilder ConfigureOpenTelemetry(
            string healthEndpointPath,
            string aliveEndpointPath)
        {
            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });

            builder.Services
                .AddOpenTelemetry()
                .WithMetrics(options =>
                {
                    options.AddAspNetCoreInstrumentation();
                    options.AddHttpClientInstrumentation();
                    options.AddRuntimeInstrumentation();
                })
                .WithTracing(options =>
                {
                    options.AddSource(builder.Environment.ApplicationName)
                        // ReSharper disable once VariableHidesOuterVariable
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Filter = context =>
                                !context.Request.Path.StartsWithSegments(aliveEndpointPath)
                                && !context.Request.Path.StartsWithSegments(healthEndpointPath);
                        })
                        .AddHttpClientInstrumentation();
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private TBuilder AddOpenTelemetryExporters()
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            return builder;
        }
    }
}