namespace Host.Api.Extension.Configuration;

public static class CorsConfiguration
{
    public class ApiCorsConfiguration
    {
        public IEnumerable<string> Origins { get; set; } = [];
    }

    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppCors()
        {
            builder.Services.AddCors();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseAppCors(Action<ApiCorsConfiguration> configure)
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
}