namespace MyApp.Services.Generators.Tests.TestInfrastructure;

/// <summary>
/// Integration classes share the container database, seeded once: they run one after the other.
/// Unit classes stay outside of this collection and keep running in parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection
{
    public const string Name = "Database";
}
