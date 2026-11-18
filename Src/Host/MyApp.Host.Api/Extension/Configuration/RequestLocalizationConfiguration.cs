namespace MyApp.Host.Api.Extension.Configuration;

public static class RequestLocalizationConfiguration
{
    extension(WebApplication app)
    {
        public WebApplication UseAppLocalization()
        {
            string[] supportedCultures = ["en", "fr"];

            app.UseRequestLocalization(options =>
            {
                options.SetDefaultCulture(supportedCultures[0])
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures);
                options.ApplyCurrentCultureToResponseHeaders = true;
            });

            return app;
        }
    }
}