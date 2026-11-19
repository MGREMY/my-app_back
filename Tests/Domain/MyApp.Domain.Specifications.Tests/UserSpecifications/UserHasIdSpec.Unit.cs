using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.UserSpecifications;

namespace MyApp.Domain.Specifications.Tests.UserSpecifications;

public sealed partial class UserHasIdSpecTests
{
    [UnitTest]
    public sealed class Unit
    {
        [Fact]
        public void Criteria_SameId_ReturnsTrue()
        {
            var id = Guid.NewGuid();

            var result = new UserHasIdSpec(id).Criteria.Compile()(UserTestData.Create(id, "a"));

            result.ShouldBeTrue();
        }

        [Fact]
        public void Criteria_OtherId_ReturnsFalse()
        {
            var result = new UserHasIdSpec(Guid.NewGuid()).Criteria.Compile()(UserTestData.Create(Guid.NewGuid(), "a"));

            result.ShouldBeFalse();
        }

        [Fact]
        public void Spec_Query_ReturnsOnlyUserWithId()
        {
            var id = Guid.NewGuid();
            var users = new[] { UserTestData.Create(Guid.NewGuid(), "a"), UserTestData.Create(id, "b"), UserTestData.Create(Guid.NewGuid(), "c") };

            var result = users.AsQueryable().Spec(new UserHasIdSpec(id)).ToArray();

            result.ShouldHaveSingleItem().Id.ShouldBe(id);
        }
    }
}
