using System.Linq.Expressions;

namespace MyApp.Domain.Specifications.Tests.TestSpecifications;

public sealed class ItemIsPositiveSpec : ISpecification<Item>
{
    public Expression<Func<Item, bool>> Criteria => x => x.Value > 0;
}

public sealed class ItemIsEvenSpec : ISpecification<Item>
{
    public Expression<Func<Item, bool>> Criteria => x => x.Value % 2 == 0;
}

public sealed class ItemHasNameSpec : ISpecification<Item>
{
    public Expression<Func<Item, bool>> Criteria => x => x.Name != null;
}

/// <summary>Throws on a null name: only safe when combined after <see cref="ItemHasNameSpec"/> with short-circuit.</summary>
public sealed class ItemHasLongNameSpec : ISpecification<Item>
{
    public Expression<Func<Item, bool>> Criteria => x => x.Name!.Length > 3;
}

/// <summary>Nested lambda reusing the outer parameter name and type would be corrupted by a naive replacer.</summary>
public sealed class ItemHasPositiveChildSpec : ISpecification<Item>
{
    public Expression<Func<Item, bool>> Criteria => x => x.Children!.Any(c => c > 0);
}
