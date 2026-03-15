using Asp.Versioning.Builder;
using Host.Api.Endpoint;

namespace Host.Api.Extension.Configuration;

public static class ApiEndpointConfiguration
{
    public static WebApplication UseAppEndpoints(this WebApplication app, ApiVersionSet apiVersionSet)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}")
            .WithTags("api")
            .WithApiVersionSet(apiVersionSet)
            .RequireAuthorization();

        group
            .UseAuthApi()
            .UseMiscApi()
            .UseUserApi();

        return app;
    }
}