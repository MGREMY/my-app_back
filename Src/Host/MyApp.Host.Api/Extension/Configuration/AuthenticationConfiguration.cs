using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MyApp.Host.Api.Extension.Configuration;

public static class AuthenticationConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppAuthentication()
        {
            var config = builder.GetAppConfig();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.IncludeErrorDetails = true;
                    options.Authority = config.Auth.Domain;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = config.Auth.Audience,
                        ValidIssuer = config.Auth.Domain
                    };
                    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                });

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseAppAuthentication()
        {
            app.UseAuthentication();

            return app;
        }
    }
}
