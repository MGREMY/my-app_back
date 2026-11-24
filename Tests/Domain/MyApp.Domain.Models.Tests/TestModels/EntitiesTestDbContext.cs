using Microsoft.EntityFrameworkCore;

namespace MyApp.Domain.Models.Tests.TestModels;

/// <summary>Context of the entity builder tests. Has no migration: its schema is created from the model.</summary>
/// <param name="options">Options built by <see cref="Create"/>.</param>
public sealed class EntitiesTestDbContext(DbContextOptions<EntitiesTestDbContext> options) : DbContext(options)
{
    public DbSet<TrackedEntity> TrackedEntities { get; set; }
    public DbSet<UnfilteredEntity> UnfilteredEntities { get; set; }

    public static EntitiesTestDbContext Create(string connectionString)
    {
        return new EntitiesTestDbContext(new DbContextOptionsBuilder<EntitiesTestDbContext>()
            .UseNpgsql(connectionString)
            .Options);
    }
}
