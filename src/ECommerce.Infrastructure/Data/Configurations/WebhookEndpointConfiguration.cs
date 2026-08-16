using ECommerce.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<WebhookEndpoint> builder)
    {
        builder.ToTable("webhook_endpoints");

        builder.HasKey(endpoint => endpoint.Id);
        builder.Property(endpoint => endpoint.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(endpoint => endpoint.Name)
            .HasMaxLength(100)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(endpoint => endpoint.Url)
            .HasMaxLength(2000)
            .IsRequired()
            .HasColumnName("url");

        builder.Property(endpoint => endpoint.Secret)
            .HasMaxLength(512)
            .IsRequired()
            .HasColumnName("secret");

        builder.Property(endpoint => endpoint.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.Property(endpoint => endpoint.SuspendedUntilUtc).HasColumnName("suspended_until_utc");
        builder.Property(endpoint => endpoint.SecretRotatedAtUtc).HasColumnName("secret_rotated_at_utc");

        builder.Property(endpoint => endpoint.EventTypes)
            .HasColumnType("jsonb")
            .HasColumnName("event_types")
            .HasConversion(new JsonValueConverter<IReadOnlyCollection<string>>())
            .IsRequired();

        builder.Property(endpoint => endpoint.CreatedAt).HasColumnName("created_at");
        builder.Property(endpoint => endpoint.UpdatedAt).HasColumnName("updated_at");
        builder.Property(endpoint => endpoint.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(endpoint => endpoint.DomainEvents);

        builder.HasIndex(endpoint => endpoint.IsActive).HasDatabaseName("ix_webhook_endpoints_is_active");
    }
}
