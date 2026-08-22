using ECommerce.Domain.Partners;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class PartnerApiKeyConfiguration : IEntityTypeConfiguration<PartnerApiKey>
{
    public void Configure(EntityTypeBuilder<PartnerApiKey> builder)
    {
        builder.ToTable("partner_api_keys");

        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(k => k.PartnerId)
            .IsRequired()
            .HasColumnName("partner_id");

        builder.Property(k => k.KeyHash)
            .HasMaxLength(512)
            .IsRequired()
            .HasColumnName("key_hash");

        builder.Property(k => k.Name)
            .HasMaxLength(200)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(k => k.Scopes)
            .HasColumnType("jsonb")
            .HasColumnName("scopes")
            .HasConversion(new JsonValueConverter<IReadOnlyCollection<string>>())
            .IsRequired();

        builder.Property(k => k.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.Property(k => k.ExpiresAt).HasColumnName("expires_at");
        builder.Property(k => k.LastUsedAt).HasColumnName("last_used_at");

        builder.Property(k => k.CreatedAt).HasColumnName("created_at");
        builder.Property(k => k.UpdatedAt).HasColumnName("updated_at");
        builder.Property(k => k.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(k => k.DomainEvents);

        builder.HasIndex(k => k.KeyHash).IsUnique().HasDatabaseName("ux_partner_api_keys_key_hash");
        builder.HasIndex(k => k.PartnerId).HasDatabaseName("ix_partner_api_keys_partner_id");
        builder.HasIndex(k => k.IsActive).HasDatabaseName("ix_partner_api_keys_is_active");
    }
}
