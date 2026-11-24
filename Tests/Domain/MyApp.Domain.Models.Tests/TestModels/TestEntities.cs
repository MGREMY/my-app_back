using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Models.Models.Interface;

namespace MyApp.Domain.Models.Tests.TestModels;

/// <summary>Entity using every shared builder, soft deletion filter included.</summary>
[EntityTypeConfiguration<TrackedEntityConfiguration, TrackedEntity>]
public class TrackedEntity : BaseEntity, ICreatedAt, ISoftDeletable
{
    public required string Name { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public bool HasBeenModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
}

internal sealed class TrackedEntityConfiguration : IEntityTypeConfiguration<TrackedEntity>
{
    public void Configure(EntityTypeBuilder<TrackedEntity> builder)
    {
        builder
            .ToTable("tracked_entity")
            .AddBaseEntity()
            .AddCreatedAt()
            .AddSoftDeletion();

        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
    }
}

/// <summary>Soft deletable entity configured without the global query filter.</summary>
[EntityTypeConfiguration<UnfilteredEntityConfiguration, UnfilteredEntity>]
public class UnfilteredEntity : BaseEntity, ISoftDeletable
{
    public required string Name { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
}

internal sealed class UnfilteredEntityConfiguration : IEntityTypeConfiguration<UnfilteredEntity>
{
    public void Configure(EntityTypeBuilder<UnfilteredEntity> builder)
    {
        builder
            .ToTable("unfiltered_entity")
            .AddBaseEntity()
            .AddSoftDeletion(withGlobalQueryFilter: false);

        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
    }
}
