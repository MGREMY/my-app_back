using Projects;

namespace AppHost.Extension;

public static class GatewayContainer
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IDistributedApplicationBuilder
    {
        public IResourceBuilder<ProjectResource> AddGatewayContainer(
            IResourceBuilder<ProjectResource> api)
        {
            return builder.AddProject<MyApp_Host_Gateway>("Gateway")
                .WithEnvironment("Gateway__Clusters__back", api.GetEndpoint("http"))
                .WithEnvironment("Gateway__Clusters__front", builder.Configuration["gateway_config:clusters:front"]);
        }
    }
}