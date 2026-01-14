using Domain.Model.Model.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Model.Model;

[EntityTypeConfiguration<UserEntityConfiguration, User>]
public class User : BaseEntity, ICreatedAt, ISoftDeletable
{
    public string AuthId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

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