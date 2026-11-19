using MyApp.Domain.Models.Models.Interface;
using MyApp.Domain.Models.Tests.TestModels;

namespace MyApp.Domain.Models.Tests.Models.Interface;

public sealed partial class CreatedAtExtensionsTests
{
    [UnitTest]
    public sealed class Unit
    {
        [Fact]
        public void SetCreatedAtData_Entity_SetsCreationDateOnly()
        {
            var entity = new TrackedEntity { Name = "a" };
            var before = DateTimeOffset.UtcNow;

            var result = entity.SetCreatedAtData();

            result.ShouldBeSameAs(entity);
            entity.CreatedAtUtc.ShouldBeInRange(before, DateTimeOffset.UtcNow);
            entity.CreatedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
            entity.UpdatedAtUtc.ShouldBeNull();
            entity.HasBeenModified.ShouldBeFalse();
        }

        [Fact]
        public void SetUpdatedAtData_Entity_SetsUpdateDateAndModifiedFlag()
        {
            var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var entity = new TrackedEntity { Name = "a", CreatedAtUtc = createdAt };
            var before = DateTimeOffset.UtcNow;

            var result = entity.SetUpdatedAtData();

            result.ShouldBeSameAs(entity);
            entity.UpdatedAtUtc.ShouldNotBeNull().ShouldBeInRange(before, DateTimeOffset.UtcNow);
            entity.HasBeenModified.ShouldBeTrue();
            entity.CreatedAtUtc.ShouldBe(createdAt);
        }
    }
}
