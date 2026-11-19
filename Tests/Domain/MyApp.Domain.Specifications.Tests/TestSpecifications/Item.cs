namespace MyApp.Domain.Specifications.Tests.TestSpecifications;

/// <summary>In-memory type evaluated by the test specifications.</summary>
public sealed record Item(int Value, string? Name = null, IReadOnlyList<int>? Children = null);
