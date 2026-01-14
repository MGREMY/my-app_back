namespace Host.Api.Extension.Configuration;

public static class CorsConfiguration
{
    public class ApiCorsConfiguration
    {
        public IEnumerable<string> Origins { get; set; } = [];
    }

    public static WebApplication UseApiCors(
        this WebApplication app,
        Action<ApiCorsConfiguration> configure)
    {
        ApiCorsConfiguration apiConfiguration = new();
        configure(apiConfiguration);

        app.UseCors(options =>
        {
            options
                .WithOrigins(apiConfiguration.Origins.ToArray())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition");
        });

        return app;
    }
}