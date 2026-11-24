using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Models.Extensions;
using MyApp.Services.Extensions;

namespace MyApp.Services.Tests.TestInfrastructure;

/// <summary>Same composition as the host (ApiServiceConfiguration), with the given infrastructure.</summary>
internal static class ServiceComposition
{
    /// <param name="databaseConnectionString">PostgreSQL connection string (the container database).</param>
    /// <param name="cacheConnectionString">Redis connection string (the container cache).</param>
    /// <param name="dataPath">Folder of the file storage.</param>
    /// <param name="configure">Test-only additions on top of the host composition (interceptors...).</param>
    public static ServiceProvider Build(
        string databaseConnectionString,
        string cacheConnectionString,
        string dataPath,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddMyAppPersistence(databaseConnectionString)
            .AddMyAppCache(cacheConnectionString)
            .AddMyAppServices(options => { options.DataPath = dataPath; });

        configure?.Invoke(services);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
