namespace MyApp.Host.Api.Extension;

public static class AuthorizationConfiguration
{
    public class ApiAuthorizationConfiguration
    {
        public IEnumerable<string> Origins { get; set; } = [];
    }

    public static TBuilder AddApiAuthorization<TBuilder>(
        this TBuilder builder,
        Action<ApiAuthorizationConfiguration> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ApiAuthorizationConfiguration apiConfiguration = new();
        configure(apiConfiguration);

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy
                .WithOrigins(apiConfiguration.Origins.ToArray())
                .AllowAnyHeader()
                .AllowCredentials()
            );
        });

        builder.Services.AddAuthorizationBuilder();

        return builder;
    }

    public static WebApplication UseApiAuthorization(this WebApplication app)
    {
        app.UseCors();

        return app;
    }
}