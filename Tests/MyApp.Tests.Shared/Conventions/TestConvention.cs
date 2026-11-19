using System.Reflection;
using MyApp.Tests.Shared.Categories;
using Shouldly;
using Xunit;
using Xunit.v3;

namespace MyApp.Tests.Shared.Conventions;

/// <summary>
/// Checks the test layout conventions of a test assembly:
/// <list type="bullet">
/// <item>a nested class named <c>Unit</c>, <c>Integration</c> or <c>Functional</c> carries the matching category attribute;</item>
/// <item>a class carrying a category attribute is the nested class of that name inside a <c>&lt;Class&gt;Tests</c> class;</item>
/// <item>every class declaring tests carries a category attribute;</item>
/// <item>no raw <c>[Trait("Category", ...)]</c> is used.</item>
/// </list>
/// </summary>
public static class TestConvention
{
    private static readonly string[] CategoryNames = [TestCategory.Unit, TestCategory.Integration, TestCategory.Functional];

    extension(Assembly assembly)
    {
        public void ShouldFollowTestConventions()
        {
            var violations = new List<string>();

            if (HasRawCategoryTrait(assembly.GetCustomAttributesData()))
                violations.Add($"Assembly {assembly.GetName().Name}: raw [Trait(\"{TestCategory.Name}\", ...)], use a category attribute");

            foreach (var type in assembly.GetTypes().Where(x => x.IsClass))
            {
                var category = type.GetCustomAttribute<CategoryAttribute>(inherit: false)?.Category;

                if (type.IsNested && CategoryNames.Contains(type.Name) && category != type.Name)
                    violations.Add($"{type.FullName}: nested class {type.Name} must carry [{type.Name}Test]");

                if (category is not null && !(type.IsNested && type.Name == category && type.DeclaringType!.Name.EndsWith("Tests")))
                    violations.Add($"{type.FullName}: [{category}Test] is only allowed on a nested class {category} of a <Class>Tests class");

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

                if (category is null && methods.Any(x => x.GetCustomAttributes().Any(a => a is IFactAttribute)))
                    violations.Add($"{type.FullName}: declares tests without category, nest them in a Unit, Integration or Functional class");

                if (HasRawCategoryTrait(type.GetCustomAttributesData()) || methods.Any(x => HasRawCategoryTrait(x.GetCustomAttributesData())))
                    violations.Add($"{type.FullName}: raw [Trait(\"{TestCategory.Name}\", ...)], use a category attribute");
            }

            string.Join(Environment.NewLine, violations).ShouldBeEmpty();
        }
    }

    private static bool HasRawCategoryTrait(IEnumerable<CustomAttributeData> attributes)
    {
        return attributes.Any(x =>
            x.AttributeType == typeof(TraitAttribute)
            && x.ConstructorArguments is [{ Value: TestCategory.Name }, ..]);
    }
}
