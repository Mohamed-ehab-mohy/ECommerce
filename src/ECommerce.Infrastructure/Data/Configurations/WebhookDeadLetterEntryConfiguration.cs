using ECommerce.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class WebhookDeadLetterEntryConfiguration : IEntityTypeConfiguration<WebhookDeadLetterEntry>
{
    public void Configure(EntityTypeBuilder<WebhookDeadLetterEntry> builder)
    {
        builder.ToTable("webhook_dead_letter");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(entry => entry.DeliveryId).IsRequired().HasColumnName("delivery_id");
        builder.Property(entry => entry.EndpointId).IsRequired().HasColumnName("endpoint_id");

        builder.Property(entry => entry.EventType)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("event_type");

        builder.Property(entry => entry.EventId)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("event_id");

        builder.Property(entry => entry.PayloadJson)
            .HasColumnType("text")
            .IsRequired()
            .HasColumnName("payload_json");

        builder.Property(entry => entry.EndpointUrl)
            .HasMaxLength(2000)
            .IsRequired()
            .HasColumnName("endpoint_url");

        builder.Property(entry => entry.EndpointName)
            .HasMaxLength(100)
            .IsRequired()
            .HasColumnName("endpoint_name");

        builder.Property(entry => entry.TotalAttempts).IsRequired().HasColumnName("total_attempts");
        builder.Property(entry => entry.LastStatusCode).HasColumnName("last_status_code");

        builder.Property(entry => entry.ErrorReason)
            .HasColumnType("text")
            .IsRequired()
            .HasColumnName("error_reason");

        builder.Property(entry => entry.FirstFailedAtUtc).HasColumnName("first_failed_at_utc");
        builder.Property(entry => entry.LastFailedAtUtc).HasColumnName("last_failed_at_utc");
        builder.Property(entry => entry.ReplayedAtUtc).HasColumnName("replayed_at_utc");

        builder.Property(entry => entry.CreatedAt).HasColumnName("created_at");
        builder.Property(entry => entry.UpdatedAt).HasColumnName("updated_at");
        builder.Property(entry => entry.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(entry => entry.DomainEvents);

        builder.HasIndex(entry => entry.EndpointId).HasDatabaseName("ix_webhook_dlq_endpoint_id");
        builder.HasIndex(entry => entry.EventType).HasDatabaseName("ix_webhook_dlq_event_type");
        builder.HasIndex(entry => entry.LastFailedAtUtc).HasDatabaseName("ix_webhook_dlq_last_failed");
        builder.HasIndex(entry => new { entry.IsReplayed, entry.LastFailedAtUtc }).HasDatabaseName("ix_webhook_dlq_replayed_failed");
    }
}
