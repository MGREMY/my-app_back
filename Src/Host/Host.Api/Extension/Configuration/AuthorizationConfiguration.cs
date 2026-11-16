namespace Host.Api.Extension.Configuration;

public static class AuthorizationConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppAuthorization()
        {
            builder.Services
                .AddAuthorizationBuilder()
                .AddPolicy(ApiConstant.AuthorizationPolicies.Admin,
                    policy => { policy.RequireRole(ApiConstant.Role.Admin); });

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseAppAuthorization()
        {
            app.UseAuthorization();

            return app;
        }
    }
}