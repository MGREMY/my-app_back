using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.UserSpecifications;

namespace MyApp.Domain.Specifications.Tests.UserSpecifications;

public sealed partial class UserHasAuthIdSpecTests
{
    [UnitTest]
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

        [Fact]
        public void Spec_ComposedWithUserHasIdSpec_ReturnsUserMatchingBoth()
        {
            var id = Guid.NewGuid();
            var users = new[] { UserTestData.Create(id, "a"), UserTestData.Create(Guid.NewGuid(), "b") };

            var result = users.AsQueryable().Spec(new UserHasAuthIdSpec("a").And(new UserHasIdSpec(id))).ToArray();

            result.ShouldHaveSingleItem().AuthId.ShouldBe("a");
        }
    }
}
