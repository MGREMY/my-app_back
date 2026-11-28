using MyApp.Host.Gateway.ReverseProxy;
using Yarp.ReverseProxy.Configuration;

namespace MyApp.Host.Gateway.Extension;

public static class ReverseProxyConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppReverseProxy()
        {
            var config = builder.GetAppConfig();

            builder.Configuration
                .AddJsonFile(GatewayConstant.ReverseProxyConfigPath, optional: false, reloadOnChange: false);

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            builder.Services.AddSingleton<IProxyConfigFilter>(new ClusterAddressConfigFilter(config.Clusters));

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseAppReverseProxy()
        {
            app.MapReverseProxy();

            return app;
        }
    }
}