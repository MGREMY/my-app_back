using System.Reflection;
using MyApp.Tests.Shared.Categories;
using Shouldly;
using Xunit;
using Xunit.v3;

namespace MyApp.Tests.Shared.Conventions;

/// <summary>
/// Checks the test layout conventions of a test assembly:
/// <list type="bullet">
/// <item>a nested class named <c>Unit</c>, <c>Integration</c> or <c>Functional</c> carries
/// <c>[Trait(TestCategory.Name, TestCategory.&lt;same name&gt;)]</c>, exactly once;</item>
/// <item>a category trait is only allowed on that nested class inside a <c>&lt;Class&gt;Tests</c> class (never on an
/// assembly, a method or another class);</item>
/// <item>every class declaring tests carries a category trait.</item>
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

            if (CategoryTraits(assembly.GetCustomAttributesData()).Count > 0)
                violations.Add($"Assembly {assembly.GetName().Name}: category trait not allowed on an assembly");

            foreach (var type in assembly.GetTypes().Where(x => x.IsClass))
            {
                var categories = CategoryTraits(type.GetCustomAttributesData());
                var isCategoryClass = type.IsNested && CategoryNames.Contains(type.Name);

                if (isCategoryClass && !(categories is [var category] && category == type.Name))
                    violations.Add($"{type.FullName}: nested class {type.Name} must carry [Trait(TestCategory.Name, TestCategory.{type.Name})] once");

                if (categories.Count > 0 && !(isCategoryClass && type.DeclaringType!.Name.EndsWith("Tests")))
                    violations.Add($"{type.FullName}: category trait only allowed on a nested Unit, Integration or Functional class of a <Class>Tests class");

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

                if (categories.Count == 0 && methods.Any(x => x.GetCustomAttributes().Any(a => a is IFactAttribute)))
                    violations.Add($"{type.FullName}: declares tests without category, nest them in a Unit, Integration or Functional class");

                if (methods.Any(x => CategoryTraits(x.GetCustomAttributesData()).Count > 0))
                    violations.Add($"{type.FullName}: category trait not allowed on a method, put it on the nested class");
            }

            string.Join(Environment.NewLine, violations).ShouldBeEmpty();
        }
    }

    private static List<string?> CategoryTraits(IEnumerable<CustomAttributeData> attributes)
    {
        return attributes
            .Where(x => x.AttributeType == typeof(TraitAttribute) && x.ConstructorArguments is [{ Value: TestCategory.Name }, _])
            .Select(x => x.ConstructorArguments[1].Value as string)
            .ToList();
    }
}
