using Asp.Versioning;
using Host.Api.Endpoint;

namespace Host.Api.Extension.Configuration;

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

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseAppEndpoints()
        {
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1))
                .ReportApiVersions()
                .Build();

            var group = app.MapGroup("/api/v{version:apiVersion}")
                .WithTags("api")
                .WithApiVersionSet(apiVersionSet)
                .RequireAuthorization();

            group
                .UseAuthApi()
                .UseMiscApi()
                .UseUserApi();

            return app;
        }
    }
}