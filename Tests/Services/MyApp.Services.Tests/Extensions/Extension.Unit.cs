using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyApp.Services.Abstractions;
using MyApp.Services.Extensions;
using MyApp.Services.Tests.TestInfrastructure;

namespace MyApp.Services.Tests.Extensions;

public sealed partial class ExtensionTests
{
    /// <summary>Builds the real composition; nothing connects to PostgreSQL or Redis until a query runs.</summary>
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit : IDisposable
    {
        private readonly string _dataPath = Path.Combine(Path.GetTempPath(), $"myapp-tests-{Guid.NewGuid():N}");

        public static TheoryData<Type> ServiceInterfaces => new(typeof(ServiceResponse).Assembly.DefinedTypes
            .Where(x => x is { IsInterface: true, IsGenericTypeDefinition: false })
            .Select(x => x.AsType()));

        public void Dispose()
        {
            if (Directory.Exists(_dataPath))
                Directory.Delete(_dataPath, recursive: true);
        }

        [Fact]
        public void AddMyAppServices_FullComposition_PassesProviderValidation()
        {
            // ValidateOnBuild + ValidateScopes: every registered service can be built with scoped lifetimes respected.
            using var provider = Build();
            using var scope = provider.CreateScope();
            using var otherScope = provider.CreateScope();

            foreach (var serviceInterface in ServiceInterfaces.Select(x => x.Data))
            {
                var service = scope.ServiceProvider.GetRequiredService(serviceInterface);

                scope.ServiceProvider.GetRequiredService(serviceInterface).ShouldBeSameAs(service, serviceInterface.FullName);
                otherScope.ServiceProvider.GetRequiredService(serviceInterface).ShouldNotBeSameAs(service, serviceInterface.FullName);
            }
        }

        [Theory]
        [MemberData(nameof(ServiceInterfaces))]
        public void AddMyAppServices_AbstractionInterface_RegistersSingleScopedImplementation(Type serviceInterface)
        {
            var services = new ServiceCollection().AddMyAppServices(_ => { });

            var descriptor = services.Where(x => x.ServiceType == serviceInterface).ShouldHaveSingleItem();

            descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
            descriptor.ImplementationType.ShouldNotBeNull().IsAssignableTo(serviceInterface).ShouldBeTrue();
        }

        private ServiceProvider Build()
        {
            return ServiceComposition.Build("Host=localhost;Database=unused", "localhost:1", _dataPath);
        }
    }
}
