namespace Host.Api.Extension.Configuration;

public static class AuthorizationConfiguration
{
    public static TBuilder AddAppAuthorization<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddAuthorizationBuilder()
            .AddPolicy(ApiConstant.AuthorizationPolicies.Admin, policy =>
            {
                policy.RequireRole(ApiConstant.Role.Admin);
            });

        return builder;
    }

    public static WebApplication UseAppAuthorization(this WebApplication app)
    {
        app.UseAuthorization();

        return app;
    }
}