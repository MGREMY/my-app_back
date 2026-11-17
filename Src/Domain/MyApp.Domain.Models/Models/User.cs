using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Models.Models.Interface;

namespace MyApp.Domain.Models.Models;

[EntityTypeConfiguration<UserEntityConfiguration, User>]
public class User : BaseEntity, ICreatedAt
{
    public required string AuthId { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public bool HasBeenModified { get; set; }
}

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .ToTable("user")
            .AddBaseEntity()
            .AddCreatedAt();

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