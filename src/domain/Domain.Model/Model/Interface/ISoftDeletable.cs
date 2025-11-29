using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Model.Model.Interface;

public interface ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}

public static partial class EntityTypeBuilderExtensions
{
    public static EntityTypeBuilder<T> AddSoftDeletion<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, bool>>? filter = null)
        where T : class, ISoftDeletable
    {
        filter ??= p => !p.IsDeleted;

        builder.HasQueryFilter(filter);

        builder
            .Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder
            .Property(x => x.DeletedAtUtc)
            .HasColumnName("deleted_at_utc")
            .IsRequired(false);

        builder.HasIndex(p => p.IsDeleted);

        return builder;
    }
}

public static class SoftDeletableExtensions
{
    public static T SetSoftDeletableData<T>(this T entity) where T : class, ISoftDeletable
    {
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.IsDeleted = true;

        return entity;
    }
}