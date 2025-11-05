using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Model.Model.Interface;

namespace MyApp.Domain.Model.Model;

[EntityTypeConfiguration<UserEntityConfiguration, User>]
public class User : IBaseEntity<string>, ICreatedAt, ISoftDeletable
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool HasBeenModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .ToTable("user")
            .AddCreatedAt()
            .AddSoftDeletion();

        builder
            .HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasMaxLength(200)
            .ValueGeneratedNever()
            .IsRequired();
    }
}