namespace ECommerce.Infrastructure.Messaging;

public sealed class InboxMessage
{
    public long Id { get; set; }

    public string ConsumerQueue { get; set; } = string.Empty;

    public Guid MessageId { get; set; }

    public DateTime ProcessedAt { get; set; }
}
