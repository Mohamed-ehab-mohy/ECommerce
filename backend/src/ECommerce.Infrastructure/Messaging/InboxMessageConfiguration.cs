using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Messaging;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedOnAdd().HasColumnName("id");

        builder.Property(message => message.ConsumerQueue)
            .HasMaxLength(128)
            .IsRequired()
            .HasColumnName("consumer_queue");

        builder.Property(message => message.MessageId)
            .IsRequired()
            .HasColumnName("message_id");

        builder.Property(message => message.ProcessedAt)
            .IsRequired()
            .HasColumnName("processed_at");

        builder.HasIndex(message => new { message.ConsumerQueue, message.MessageId })
            .IsUnique()
            .HasDatabaseName("ux_inbox_messages_queue_message");
    }
}
