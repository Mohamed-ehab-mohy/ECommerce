using ECommerce.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(customer => customer.Email)
            .HasMaxLength(254)
            .IsRequired()
            .HasColumnName("email");

        builder.HasIndex(customer => customer.Email)
            .IsUnique();

        builder.Property(customer => customer.DisplayName)
            .HasMaxLength(100)
            .IsRequired()
            .HasColumnName("display_name");

        builder.Property(customer => customer.Locale)
            .HasMaxLength(10)
            .IsRequired()
            .HasColumnName("locale");

        builder.Property(customer => customer.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(customer => customer.PasswordHash)
            .HasMaxLength(256)
            .IsRequired()
            .HasColumnName("password_hash");

        builder.Property(customer => customer.EmailVerified)
            .HasColumnName("email_verified");

        builder.Property(customer => customer.EmailVerifiedAt)
            .HasColumnName("email_verified_at");

        builder.Property(customer => customer.VerificationTokenHash)
            .HasMaxLength(128)
            .HasColumnName("verification_token_hash");

        builder.Property(customer => customer.VerificationTokenExpiresAt)
            .HasColumnName("verification_token_expires_at");

        builder.Property(customer => customer.FailedLoginCount)
            .HasColumnName("failed_login_count");

        builder.Property(customer => customer.LockoutEndAtUtc)
            .HasColumnName("lockout_end");

        builder.Property(customer => customer.CreatedAt).HasColumnName("created_at");
        builder.Property(customer => customer.UpdatedAt).HasColumnName("updated_at");
        builder.Property(customer => customer.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(customer => customer.DomainEvents);
    }
}
