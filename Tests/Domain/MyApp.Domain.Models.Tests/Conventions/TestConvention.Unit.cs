using MyApp.Tests.Shared.Conventions;

namespace MyApp.Domain.Models.Tests.Conventions;

public sealed partial class TestConventionTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        [Fact]
        public void TestAssembly_FollowsTestConventions()
        {
            typeof(Unit).Assembly.ShouldFollowTestConventions();
        }
    }
}
