using MyApp.Domain.Models.Models.Interface;
using MyApp.Domain.Models.Tests.TestModels;

namespace MyApp.Domain.Models.Tests.Models.Interface;

public sealed partial class SoftDeletableExtensionsTests
{
    [UnitTest]
    public sealed class Unit
    {
        [Fact]
        public void SetSoftDeletableData_Entity_MarksDeletedWithDate()
        {
            var entity = new UnfilteredEntity { Name = "a" };
            var before = DateTimeOffset.UtcNow;

            var result = entity.SetSoftDeletableData();

            result.ShouldBeSameAs(entity);
            entity.IsDeleted.ShouldBeTrue();
            entity.DeletedAtUtc.ShouldNotBeNull().ShouldBeInRange(before, DateTimeOffset.UtcNow);
        }
    }
}
