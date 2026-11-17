using Scalar.AspNetCore;

namespace MyApp.Host.Api.Extension.Configuration;

public static class OpenApiConfiguration
{
    extension(WebApplication app)
    {
        public WebApplication UseAppOpenApi()
        {
            if (!app.Environment.IsDevelopment()) return app;

            app.MapOpenApi().WithDocumentPerVersion();
            app.MapScalarApiReference(options =>
            {
                var descriptions = app.DescribeApiVersions();

                for (var i = 0; i < descriptions.Count; i++)
                {
                    var description = descriptions[i];
                    var isDefault = i == descriptions.Count - 1;

                    // isDefault is used to mark the default API version in Scalar.
                    // This decides which version is selected by default when users visit the Scalar UI.
                    options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
                }

                options
                    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
                    .WithTheme(ScalarTheme.Kepler);
            });

            return app;
        }
    }
}