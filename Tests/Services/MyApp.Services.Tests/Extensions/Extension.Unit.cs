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

            provider.ShouldNotBeNull();
        }

        [Theory]
        [MemberData(nameof(ServiceInterfaces))]
        public void AddMyAppServices_AbstractionInterface_ResolvesImplementationOfServicesAssembly(Type serviceInterface)
        {
            using var provider = Build();
            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService(serviceInterface);

            service.GetType().Assembly.ShouldBe(typeof(Extension).Assembly);
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

        [Fact]
        public void AddMyAppServices_Abstractions_EveryInterfaceHasExactlyOneImplementation()
        {
            // The rule enforced at startup: a second implementation (or none) would make AddMyAppServices throw.
            var implementations = typeof(Extension).Assembly.DefinedTypes
                .Where(x => x is { IsClass: true, IsAbstract: false })
                .ToArray();

            var counts = ServiceInterfaces
                .Select(x => (Interface: x.Data, Count: implementations.Count(i => i.ImplementedInterfaces.Contains(x.Data))))
                .Where(x => x.Count != 1)
                .Select(x => $"{x.Interface.FullName}: {x.Count}");

            counts.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(typeof(IService<>))]
        [InlineData(typeof(IService<,>))]
        [InlineData(typeof(IServiceAsync<>))]
        [InlineData(typeof(IServiceAsync<,>))]
        public void AddMyAppServices_OpenGenericInterface_IsNotRegistered(Type openGeneric)
        {
            var services = new ServiceCollection().AddMyAppServices(_ => { });

            services.ShouldNotContain(x => x.ServiceType == openGeneric);
        }

        [Fact]
        public void AddMyAppServices_Options_ConfiguresServiceOption()
        {
            using var provider = Build();

            var options = provider.GetRequiredService<IOptions<ServiceOption>>().Value;

            options.DataPath.ShouldBe(_dataPath);
        }

        [Fact]
        public void AddMyAppCache_Resolved_UsesHybridCacheWithRedisDistributedCache()
        {
            using var provider = Build();

            provider.GetRequiredService<HybridCache>().ShouldNotBeNull();
            provider.GetRequiredService<IDistributedCache>().ShouldBeAssignableTo<RedisCache>();
        }

        private ServiceProvider Build()
        {
            return ServiceComposition.Build("Host=localhost;Database=unused", "localhost:1", _dataPath);
        }
    }
}
