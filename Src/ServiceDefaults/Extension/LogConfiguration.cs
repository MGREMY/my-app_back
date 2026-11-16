using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;

namespace ServiceDefaults.Extension;

internal static class LogConfiguration
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        internal TBuilder ConfigureLog()
        {
            const string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

            var loggerConfiguration = new LoggerConfiguration()
                .Filter.ByExcluding(e => e.Exception?.GetType().Name == "DomainException")
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", builder.Environment.ApplicationName)
                .WriteTo.Console(
                    theme: SystemConsoleTheme.Colored,
                    outputTemplate: outputTemplate
                )
                .WriteTo.File(
                    "logs/log.log",
                    outputTemplate: outputTemplate,
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Day,
                    retainedFileTimeLimit: TimeSpan.FromDays(7)
                );

            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                loggerConfiguration
                    .WriteTo.OpenTelemetry(options =>
                    {
                        options.Endpoint = otlpEndpoint;
                        options.Protocol = OtlpProtocol.Grpc;
                        options.IncludedData =
                            IncludedData.TraceIdField |
                            IncludedData.SpanIdField |
                            IncludedData.MessageTemplateTextAttribute |
                            IncludedData.SpecRequiredResourceAttributes;

                        options.ResourceAttributes = new Dictionary<string, object>
                        {
                            ["service.name"] = builder.Environment.ApplicationName,
                            ["service.version"] = typeof(LogConfiguration).Assembly.GetName().Version?.ToString()
                                                  ?? "1.0.0",
                            ["deployment.environment"] = builder.Environment.EnvironmentName,
                        };
                    });
            }

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(loggerConfiguration.CreateLogger());

            return builder;
        }
    }
}