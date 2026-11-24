using MyApp.Domain.Specifications.UserSpecifications;

namespace MyApp.Domain.Specifications.Tests.UserSpecifications;

public sealed partial class UserHasAuthIdSpecTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        [Fact]
        public void Criteria_SameAuthId_ReturnsTrue()
        {
            var result = new UserHasAuthIdSpec("auth-1").Criteria.Compile()(UserTestData.Create(Guid.NewGuid(), "auth-1"));

            result.ShouldBeTrue();
        }

        [Theory]
        [InlineData("auth-2")]
        [InlineData("AUTH-1")]
        public void Criteria_OtherAuthId_ReturnsFalse(string authId)
        {
            var result = new UserHasAuthIdSpec("auth-1").Criteria.Compile()(UserTestData.Create(Guid.NewGuid(), authId));

            result.ShouldBeFalse();
        }
    }
}
