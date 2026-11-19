using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyApp.Domain.Models.Tests.TestModels;

namespace MyApp.Domain.Models.Tests.Models.Interface;

public sealed partial class EntityTypeBuilderExtensionsTests
{
    /// <summary>Reads the model built by the Npgsql provider; no database connection is opened.</summary>
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit : IDisposable
    {
        private readonly EntitiesTestDbContext _context = EntitiesTestDbContext.Create("Host=localhost;Database=unused");

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public void AddBaseEntity_AddedEntity_GeneratesUuidV7()
        {
            var entity = new TrackedEntity { Name = "a" };
            _context.Add(entity);

            // Sequential key (AGENTS 6.1): a UUIDv7, ordered by its millisecond timestamp (random within a millisecond).
            entity.Id.Version.ShouldBe(7);
        }

        private IEntityType EntityType<T>()
        {
            return _context.Model.FindEntityType(typeof(T)).ShouldNotBeNull();
        }
    }
}
