using FastEndpoints.Swagger;
using Scalar.AspNetCore;

namespace Host.Api.Extension;

public static class OpenApiConfiguration
{
    public static TBuilder AddApiOpenApi<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .SwaggerDocument(MakeDocument(1, "MyApp v1"));

        return builder;

        static Action<DocumentOptions> MakeDocument(int apiVersion, string title)
        {
            return o =>
            {
                o.MaxEndpointVersion = apiVersion;
                o.ShortSchemaNames = true;
                o.DocumentSettings = s =>
                {
                    s.MarkNonNullablePropsAsRequired();
                    s.DocumentName = $"v{apiVersion}";
                    s.Title = title;
                    s.Description = $"Release {apiVersion}";
                    s.Version = $"v{apiVersion}";
                };
            };
        }
    }

    public static WebApplication MapApiOpenApiEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerGen(c => { c.Path = "/openapi/{documentName}.json"; });
            app.MapScalarApiReference(options =>
            {
                options
                    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
                    .WithTheme(ScalarTheme.Kepler);
            });
        }

        return app;
    }
}