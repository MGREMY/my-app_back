using Asp.Versioning;
using MyApp.Host.Api.Endpoints;

namespace MyApp.Host.Api.Extension.Configuration;

public static class ApiEndpointConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppVersioning()
        {
            builder.Services
                .AddApiVersioning(options =>
                {
                    options.ApiVersionReader = ApiVersionReader.Combine(
                        new QueryStringApiVersionReader(),
                        new HeaderApiVersionReader("X-Api-Version"));
                    options.DefaultApiVersion = new ApiVersion(1.0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    // Replace the placeholder with the actual version
                    options.SubstituteApiVersionInUrl = true;
                }).AddOpenApi();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseAppEndpoints()
        {
            app.MapAuth().ToV1();
            app.MapMisc().ToV1();
            app.MapUser().ToV1();

            return app;
        }
    }
}