using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Domain.Models.Tests.TestInfrastructure;
using MyApp.Domain.Models.Tests.TestModels;
using MyApp.Tests.Shared.Containers;
using MyApp.Tests.Shared.Database;
using Npgsql;

namespace MyApp.Domain.Models.Tests.Models.Interface;

public sealed partial class EntityTypeBuilderExtensionsTests
{
    [Trait(TestCategory.Name, TestCategory.Integration)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Integration(ContainersFixture containers) : IAsyncLifetime
    {
        private EntitiesTestDbContext _context = null!;

        public async ValueTask InitializeAsync()
        {
            await containers.EnsureStartedAsync();

            _context = EntitiesTestDbContext.Create(containers.DatabaseConnectionString);

            await DatabaseSchema.CreateAsync(_context, TestContext.Current.CancellationToken);
            await containers.ResetAsync(TestContext.Current.CancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _context.DisposeAsync();
        }

        [Fact]
        public async Task AddSoftDeletion_Default_FiltersDeletedRows()
        {
            await SeedTrackedAsync();

            var names = await _context.TrackedEntities.Select(x => x.Name).ToListAsync(TestContext.Current.CancellationToken);

            names.ShouldBe(["active"]);
        }

        [Fact]
        public async Task AddSoftDeletion_IgnoringNamedFilter_ReturnsDeletedRows()
        {
            await SeedTrackedAsync();

            var names = await _context.TrackedEntities
                .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync(TestContext.Current.CancellationToken);

            names.ShouldBe(["active", "deleted"]);
        }

        [Fact]
        public async Task SaveChanges_ModifiedAndDeletedEntity_PersistsTrackingColumns()
        {
            var entity = new TrackedEntity { Name = "a" }.SetCreatedAtData();

            _context.TrackedEntities.Add(entity);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            entity.SetUpdatedAtData().SetSoftDeletableData();
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            _context.ChangeTracker.Clear();

            var stored = await _context.TrackedEntities
                .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
                .SingleAsync(TestContext.Current.CancellationToken);

            stored.Id.ShouldBe(entity.Id);
            stored.CreatedAtUtc.ShouldBe(entity.CreatedAtUtc, TimeSpan.FromMilliseconds(1));
            stored.UpdatedAtUtc.ShouldNotBeNull();
            stored.HasBeenModified.ShouldBeTrue();
            stored.IsDeleted.ShouldBeTrue();
            stored.DeletedAtUtc.ShouldNotBeNull();
        }

        [Fact]
        public async Task AddCreatedAtAndSoftDeletion_RowInsertedWithoutFlags_UsesFalseDefaults()
        {
            await using (var connection = new NpgsqlConnection(containers.DatabaseConnectionString))
            {
                await connection.OpenAsync(TestContext.Current.CancellationToken);
                await using var command = new NpgsqlCommand(
                    "INSERT INTO tracked_entity (id, name, created_at_utc) VALUES (gen_random_uuid(), 'raw', now())", connection);
                await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }

            var stored = await _context.TrackedEntities.SingleAsync(TestContext.Current.CancellationToken);

            stored.HasBeenModified.ShouldBeFalse();
            stored.IsDeleted.ShouldBeFalse();
        }

        private async Task SeedTrackedAsync()
        {
            _context.TrackedEntities.AddRange(
                new TrackedEntity { Name = "active" }.SetCreatedAtData(),
                new TrackedEntity { Name = "deleted" }.SetCreatedAtData().SetSoftDeletableData());
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            _context.ChangeTracker.Clear();
        }
    }
}
