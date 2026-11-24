namespace MyApp.Tests.Shared.Categories;

/// <summary>
/// Name and values of the trait used to categorize tests (<c>--filter-trait "Category=Unit"</c>).
/// Used as <c>[Trait(TestCategory.Name, TestCategory.Unit)]</c> on the nested <c>Unit</c>, <c>Integration</c> or
/// <c>Functional</c> class (the built-in <c>[Trait]</c> is the only form IDEs such as Rider read as a category).
/// </summary>
public static class TestCategory
{
    public const string Name = "Category";

    public const string Unit = nameof(Unit);
    public const string Integration = nameof(Integration);
    public const string Functional = nameof(Functional);
}
