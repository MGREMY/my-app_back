using Xunit.v3;

namespace MyApp.Tests.Shared.Categories;

/// <summary>
/// Base of the category attributes: adds the <see cref="TestCategory.Name"/> trait with the given value.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public abstract class CategoryAttribute : Attribute, ITraitAttribute
{
    protected CategoryAttribute(string category)
    {
        Category = category;
    }

    public string Category { get; }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        return [new KeyValuePair<string, string>(TestCategory.Name, Category)];
    }
}
