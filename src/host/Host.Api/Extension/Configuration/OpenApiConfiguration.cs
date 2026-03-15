using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Host.Api.Extension.Configuration;

public static class OpenApiConfiguration
{
    public static TBuilder AddAppOpenApi<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, ct) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Description = "OAuth2 authentication using Keycloak",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                });

                document.SetReferenceHostDocument();

                return Task.CompletedTask;
            });
        });

        return builder;
    }

    public static WebApplication UseAppOpenApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.AddPreferredSecuritySchemes("Bearer");
            options.AddDocument("v1");
            options
                .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
                .WithTheme(ScalarTheme.Kepler);
        });

        return app;
    }
}