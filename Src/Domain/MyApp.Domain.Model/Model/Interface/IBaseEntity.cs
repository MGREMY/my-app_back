using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.ValueGeneration;

namespace MyApp.Domain.Model.Model.Interface;

public interface IBaseEntity<TKey>
{
    public TKey Id { get; set; }
}

public abstract class BaseEntity : IBaseEntity<Guid>
{
    public Guid Id { get; set; } = Guid.Empty;
}

public static partial class EntityTypeBuilderExtensions
{
    public static EntityTypeBuilder<T> AddBaseEntity<T>(this EntityTypeBuilder<T> builder)
        where T : BaseEntity
    {
        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasValueGenerator<NpgsqlSequentialGuidValueGenerator>()
            .IsRequired();

        return builder;
    }
}