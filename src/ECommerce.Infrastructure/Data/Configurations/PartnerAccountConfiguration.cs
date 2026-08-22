using ECommerce.Domain.Partners;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class PartnerAccountConfiguration : IEntityTypeConfiguration<PartnerAccount>
{
    public void Configure(EntityTypeBuilder<PartnerAccount> builder)
    {
        builder.ToTable("partner_accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(a => a.Name)
            .HasMaxLength(200)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(a => a.Email)
            .HasMaxLength(320)
            .IsRequired()
            .HasColumnName("email");

        builder.Property(a => a.RateLimitPerMinute)
            .IsRequired()
            .HasColumnName("rate_limit_per_minute");

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(a => a.DomainEvents);

        builder.HasIndex(a => a.Email).IsUnique().HasDatabaseName("ux_partner_accounts_email");
        builder.HasIndex(a => a.IsActive).HasDatabaseName("ix_partner_accounts_is_active");
    }
}
