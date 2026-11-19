using Microsoft.EntityFrameworkCore;

namespace MyApp.Services.Generators.Tests.TestModels;

/// <summary>Context of the test model. Has no migration: its schema is created from the model.</summary>
public sealed class PaginationTestDbContext(DbContextOptions<PaginationTestDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<Continent> Continents { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Tag> Tags { get; set; }

    public static PaginationTestDbContext Create(string connectionString)
    {
        return new PaginationTestDbContext(new DbContextOptionsBuilder<PaginationTestDbContext>()
            .UseNpgsql(connectionString)
            .Options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(builder =>
        {
            builder.HasOne(x => x.Mentor).WithMany().HasForeignKey(x => x.MentorId);
            builder.HasMany(x => x.Posts).WithOne(x => x.Author).HasForeignKey(x => x.AuthorId);
        });
    }
}
