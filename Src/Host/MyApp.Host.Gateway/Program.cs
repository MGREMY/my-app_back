using MyApp.Host.Gateway.Extension;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddAppForwardedHeaders()
    .AddAppReverseProxy();

var app = builder.Build();

app
    .UseAppForwardedHeaders()
    .MapDefaultEndpoint(ServiceDefaultConstant.HealthEndpointPath, ServiceDefaultConstant.AliveEndpointPath)
    .UseAppReverseProxy();

app.Run();