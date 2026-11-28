using Microsoft.AspNetCore.HttpOverrides;

namespace MyApp.Host.Gateway.Extension;

public static class ForwardedHeaderConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppForwardedHeaders()
        {
            var config = builder.GetAppConfig();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                           | ForwardedHeaders.XForwardedProto
                                           | ForwardedHeaders.XForwardedHost;

                foreach (var network in config.ForwardedHeaders.KnownNetworks)
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            });

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseAppForwardedHeaders()
        {
            app.UseForwardedHeaders();

            return app;
        }
    }
}