using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Model.Model.Interface;

public interface ICreatedAt
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public bool HasBeenModified { get; set; }
}

public static partial class EntityTypeBuilderExtensions
{
    public static EntityTypeBuilder<T> AddCreatedAt<T>(this EntityTypeBuilder<T> builder)
        where T : class, ICreatedAt
    {
        builder
            .Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .ValueGeneratedNever()
            .IsRequired();

        builder
            .Property(p => p.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .ValueGeneratedNever()
            .IsRequired(false);

        builder
            .Property(x => x.HasBeenModified)
            .HasColumnName("has_been_modified")
            .HasDefaultValue(false)
            .IsRequired();

        return builder;
    }
}

public static class CreatedAtExtensions
{
    public static T SetCreatedAtData<T>(this T entity) where T : class, ICreatedAt
    {
        entity.CreatedAtUtc = DateTimeOffset.UtcNow;

        return entity;
    }

    public static T SetUpdatedAtData<T>(this T entity) where T : class, ICreatedAt
    {
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        entity.HasBeenModified = true;

        return entity;
    }
}