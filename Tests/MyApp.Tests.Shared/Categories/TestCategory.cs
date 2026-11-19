namespace MyApp.Tests.Shared.Categories;

/// <summary>
/// Name and values of the trait used to categorize tests (<c>--filter-trait "Category=Unit"</c>).
/// Never use them in a raw <c>[Trait]</c>: use <see cref="UnitTestAttribute"/>, <see cref="IntegrationTestAttribute"/>
/// or <see cref="FunctionalTestAttribute"/>.
/// </summary>
public static class TestCategory
{
    public const string Name = "Category";

    public const string Unit = nameof(Unit);
    public const string Integration = nameof(Integration);
    public const string Functional = nameof(Functional);
}
