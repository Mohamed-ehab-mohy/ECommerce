using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record InvoiceIssued(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid OrderId,
    decimal Total,
    string Currency) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
