namespace MyApp.Services.Tests.TestInfrastructure;

/// <summary>
/// Integration classes share the container database and empty it before each test: they must not run in parallel.
/// Unit classes stay outside of this collection and keep running in parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection
{
    public const string Name = "Database";
}
