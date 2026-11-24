namespace MyApp.Host.Api.Tests.TestInfrastructure;

/// <summary>
/// Functional classes share one API host and the container database, emptied before each test: they must not run in
/// parallel. Unit classes stay outside of this collection and keep running in parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Database";
}
