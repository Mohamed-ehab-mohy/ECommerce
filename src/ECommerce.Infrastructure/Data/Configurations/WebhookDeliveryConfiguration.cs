using ECommerce.Domain.Integrations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries");

        builder.HasKey(delivery => delivery.Id);
        builder.Property(delivery => delivery.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(delivery => delivery.EndpointId)
            .IsRequired()
            .HasColumnName("endpoint_id");

        builder.Property(delivery => delivery.EventId)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("event_id");

        builder.Property(delivery => delivery.EventType)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("event_type");

        builder.Property(delivery => delivery.PayloadJson)
            .HasColumnType("text")
            .IsRequired()
            .HasColumnName("payload_json");

        builder.Property(delivery => delivery.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(delivery => delivery.Attempts)
            .IsRequired()
            .HasColumnName("attempts");

        builder.Property(delivery => delivery.NextRetryAtUtc).HasColumnName("next_retry_at_utc");
        builder.Property(delivery => delivery.LastStatusCode).HasColumnName("last_status_code");

        builder.Property(delivery => delivery.LastError)
            .HasColumnType("text")
            .HasColumnName("last_error");

        builder.Property(delivery => delivery.DeliveredAtUtc).HasColumnName("delivered_at_utc");
        builder.Property(delivery => delivery.CreatedAt).HasColumnName("created_at");
        builder.Property(delivery => delivery.UpdatedAt).HasColumnName("updated_at");
        builder.Property(delivery => delivery.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(delivery => delivery.DomainEvents);

        builder.HasIndex(delivery => delivery.EndpointId).HasDatabaseName("ix_webhook_deliveries_endpoint_id");
        builder.HasIndex(delivery => new { delivery.Status, delivery.NextRetryAtUtc }).HasDatabaseName("ix_webhook_deliveries_status_next_retry");
        builder.HasIndex(delivery => delivery.EventId).HasDatabaseName("ix_webhook_deliveries_event_id");
    }
}
