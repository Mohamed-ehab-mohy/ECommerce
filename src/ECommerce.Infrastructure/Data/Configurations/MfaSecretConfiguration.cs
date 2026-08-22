using ECommerce.Domain.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class MfaSecretConfiguration : IEntityTypeConfiguration<MfaSecret>
{
    public void Configure(EntityTypeBuilder<MfaSecret> builder)
    {
        builder.ToTable("mfa_secrets");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever().HasColumnName("id");
        builder.Property(m => m.CustomerId).HasColumnName("customer_id");
        builder.Property(m => m.SecretKey).HasColumnName("secret_key").HasMaxLength(200);
        builder.Property(m => m.IsEnabled).HasColumnName("is_enabled");
        builder.Property(m => m.EnabledAt).HasColumnName("enabled_at");
        builder.Property(m => m.FailedAttempts).HasColumnName("failed_attempts");
        builder.Property(m => m.LockedUntil).HasColumnName("locked_until");

        builder.HasIndex(m => m.CustomerId).IsUnique();

        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        builder.Property(m => m.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(m => m.DomainEvents);
    }
}
