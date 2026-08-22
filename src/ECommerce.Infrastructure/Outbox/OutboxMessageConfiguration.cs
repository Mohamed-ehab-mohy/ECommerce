using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_events");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(message => message.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        builder.Property(message => message.EventType)
            .HasMaxLength(256)
            .HasColumnName("event_type")
            .IsRequired();

        builder.Property(message => message.Content)
            .HasColumnType("jsonb")
            .HasColumnName("content")
            .IsRequired();

        builder.Property(message => message.OccurredOn)
            .HasColumnName("occurred_on")
            .IsRequired();

        builder.Property(message => message.ProcessedOn)
            .HasColumnName("processed_on");

        builder.Property(message => message.Attempts)
            .HasColumnName("attempts");

        builder.Property(message => message.Error)
            .HasColumnName("error");

        builder.HasIndex(message => new { message.ProcessedOn, message.OccurredOn });
    }
}
