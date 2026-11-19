using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain.Models.Extensions;
using MyApp.Services.Extensions;

namespace MyApp.Services.Tests.TestInfrastructure;

/// <summary>Same composition as the host (ApiServiceConfiguration), with the given infrastructure.</summary>
internal static class ServiceComposition
{
    public static ServiceProvider Build(string databaseConnectionString, string cacheConnectionString, string dataPath)
    {
        return new ServiceCollection()
            .AddLogging()
            .AddMyAppPersistence(databaseConnectionString)
            .AddMyAppCache(cacheConnectionString)
            .AddMyAppServices(options => { options.DataPath = dataPath; })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
