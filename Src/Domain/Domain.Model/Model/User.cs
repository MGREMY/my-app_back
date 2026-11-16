using Domain.Model.Model.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Model.Model;

[EntityTypeConfiguration<UserEntityConfiguration, User>]
public class User : BaseEntity, ICreatedAt, ISoftDeletable
{
    public required string AuthId { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public bool HasBeenModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
}

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .ToTable("user")
            .AddBaseEntity()
            .AddCreatedAt()
            .AddSoftDeletion();

        builder
            .Property(x => x.AuthId)
            .HasColumnName("auth_id")
            .HasMaxLength(200)
            .ValueGeneratedNever()
            .IsRequired();

        builder
            .Property(x => x.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder
            .HasIndex(x => x.AuthId)
            .IsUnique();
    }
}