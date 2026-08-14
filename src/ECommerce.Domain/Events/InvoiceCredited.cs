using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record InvoiceCredited(
    Guid InvoiceId,
    Guid CreditNoteId,
    decimal Amount,
    decimal RemainingTotal) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
