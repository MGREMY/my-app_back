namespace Host.Api.Extension;

public static class AuthorizationConfiguration
{
    public static TBuilder AddApiAuthorization<TBuilder>(this TBuilder builder)
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
}