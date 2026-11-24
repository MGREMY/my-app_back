namespace MyApp.Domain.Specifications.Tests.TestSpecifications;

/// <summary>In-memory type evaluated by the test specifications.</summary>
/// <param name="Value">Compared by the numeric test specifications.</param>
/// <param name="Name">Compared by the text test specifications; may be <c>null</c>.</param>
/// <param name="Children">Values of a nested collection, for specifications using nested lambdas; may be <c>null</c>.</param>
public sealed record Item(int Value, string? Name = null, IReadOnlyList<int>? Children = null);
