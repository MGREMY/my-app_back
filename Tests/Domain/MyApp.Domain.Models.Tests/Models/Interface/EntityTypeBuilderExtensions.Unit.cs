using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyApp.Domain.Models.Tests.TestModels;

namespace MyApp.Domain.Models.Tests.Models.Interface;

public sealed partial class EntityTypeBuilderExtensionsTests
{
    /// <summary>Reads the model built by the Npgsql provider; no database connection is opened.</summary>
    [UnitTest]
    public sealed class Unit : IDisposable
    {
        private readonly EntitiesTestDbContext _context = EntitiesTestDbContext.Create("Host=localhost;Database=unused");

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public void AddBaseEntity_Entity_MapsIdAsPrimaryKeyColumn()
        {
            var entityType = EntityType<TrackedEntity>();

            var id = entityType.FindProperty(nameof(TrackedEntity.Id))!;
            entityType.FindPrimaryKey()!.Properties.ShouldBe([id]);
            id.GetColumnName().ShouldBe("id");
            id.IsNullable.ShouldBeFalse();
        }

        [Fact]
        public void AddBaseEntity_AddedEntity_GeneratesDistinctIdsClientSide()
        {
            var first = new TrackedEntity { Name = "a" };
            var second = new TrackedEntity { Name = "b" };

            _context.AddRange(first, second);

            first.Id.ShouldNotBe(Guid.Empty);
            second.Id.ShouldNotBe(Guid.Empty);
            second.Id.ShouldNotBe(first.Id);
        }

        [Fact]
        public void AddCreatedAt_Entity_MapsColumns()
        {
            var entityType = EntityType<TrackedEntity>();

            var createdAt = entityType.FindProperty(nameof(TrackedEntity.CreatedAtUtc))!;
            var updatedAt = entityType.FindProperty(nameof(TrackedEntity.UpdatedAtUtc))!;
            var hasBeenModified = entityType.FindProperty(nameof(TrackedEntity.HasBeenModified))!;

            createdAt.GetColumnName().ShouldBe("created_at_utc");
            createdAt.IsNullable.ShouldBeFalse();
            createdAt.ValueGenerated.ShouldBe(ValueGenerated.Never);
            updatedAt.GetColumnName().ShouldBe("updated_at_utc");
            updatedAt.IsNullable.ShouldBeTrue();
            updatedAt.ValueGenerated.ShouldBe(ValueGenerated.Never);
            hasBeenModified.GetColumnName().ShouldBe("has_been_modified");
            hasBeenModified.IsNullable.ShouldBeFalse();
            hasBeenModified.GetDefaultValue().ShouldBe(false);
        }

        [Fact]
        public void AddSoftDeletion_Entity_MapsColumnsAndIndex()
        {
            var entityType = EntityType<TrackedEntity>();

            var isDeleted = entityType.FindProperty(nameof(TrackedEntity.IsDeleted))!;
            var deletedAt = entityType.FindProperty(nameof(TrackedEntity.DeletedAtUtc))!;

            isDeleted.GetColumnName().ShouldBe("is_deleted");
            isDeleted.IsNullable.ShouldBeFalse();
            isDeleted.GetDefaultValue().ShouldBe(false);
            deletedAt.GetColumnName().ShouldBe("deleted_at_utc");
            deletedAt.IsNullable.ShouldBeTrue();
            entityType.GetIndexes().ShouldContain(x => x.Properties.SequenceEqual(new[] { isDeleted }));
        }

        [Fact]
        public void AddSoftDeletion_Default_AddsNamedQueryFilter()
        {
            var filters = EntityType<TrackedEntity>().GetDeclaredQueryFilters();

            filters.ShouldHaveSingleItem().Key.ShouldBe(ModelConstant.SoftDeletionFilter);
        }

        [Fact]
        public void AddSoftDeletion_WithoutGlobalQueryFilter_AddsNoQueryFilter()
        {
            var entityType = EntityType<UnfilteredEntity>();

            entityType.GetDeclaredQueryFilters().ShouldBeEmpty();
            entityType.FindProperty(nameof(UnfilteredEntity.IsDeleted))!.GetColumnName().ShouldBe("is_deleted");
        }

        private IEntityType EntityType<T>()
        {
            return _context.Model.FindEntityType(typeof(T)).ShouldNotBeNull();
        }
    }
}
