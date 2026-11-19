using MyApp.Tests.Shared.Conventions;

namespace MyApp.Host.Api.Tests.Conventions;

public sealed partial class TestConventionTests
{
    [UnitTest]
    public sealed class Unit
    {
        [Fact]
        public void TestAssembly_FollowsTestConventions()
        {
            typeof(Unit).Assembly.ShouldFollowTestConventions();
        }
    }
}
