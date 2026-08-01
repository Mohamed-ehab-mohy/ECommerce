namespace ECommerce.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public Guid AggregateId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime OccurredOn { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public int Attempts { get; set; }

    public string? Error { get; set; }
}
