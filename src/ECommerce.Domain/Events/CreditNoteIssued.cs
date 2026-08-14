using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record CreditNoteIssued(
    Guid CreditNoteId,
    string CreditNoteNumber,
    Guid InvoiceId,
    Guid? RefundId,
    decimal Amount,
    string Currency) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
