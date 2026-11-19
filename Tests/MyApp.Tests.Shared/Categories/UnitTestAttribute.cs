namespace MyApp.Tests.Shared.Categories;

/// <summary>Marks the nested <c>Unit</c> class of a <c>&lt;Class&gt;Tests</c> class.</summary>
public sealed class UnitTestAttribute() : CategoryAttribute(TestCategory.Unit);
