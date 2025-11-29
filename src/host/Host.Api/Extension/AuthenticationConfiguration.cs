using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Host.Api.Extension;

public static class AuthenticationConfiguration
{
    public class ApiAuthenticationOptions
    {
        public string Domain { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
    }

    public static TBuilder AddApiAuthentication<TBuilder>(
        this TBuilder builder,
        Action<ApiAuthenticationOptions> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ApiAuthenticationOptions apiConfiguration = new();
        configure(apiConfiguration);

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.IncludeErrorDetails = true;
                options.Authority = apiConfiguration.Domain;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudience = apiConfiguration.Audience,
                    ValidIssuer = apiConfiguration.Domain,
                };
                options.RequireHttpsMetadata = false;
            });

        return builder;
    }
}