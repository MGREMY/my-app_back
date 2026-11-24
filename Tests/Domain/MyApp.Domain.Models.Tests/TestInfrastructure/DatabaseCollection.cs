namespace MyApp.Domain.Models.Tests.TestInfrastructure;

/// <summary>
/// Integration classes using the container default database empty it before each test: they must not run in parallel.
/// Classes working on their own <c>TemporaryDatabase</c> (migrations) and unit classes stay outside of this collection
/// and keep running in parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection
{
    public const string Name = "Database";
}
