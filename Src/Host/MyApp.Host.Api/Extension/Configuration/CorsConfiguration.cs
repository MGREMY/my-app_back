namespace MyApp.Host.Api.Extension.Configuration;

public static class CorsConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppCors()
        {
            var config = builder.GetAppConfig();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .WithOrigins(config.Cors.Origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("Content-Disposition");
                });
            });

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseAppCors()
        {
            app.UseCors();

            return app;
        }
    }
}
