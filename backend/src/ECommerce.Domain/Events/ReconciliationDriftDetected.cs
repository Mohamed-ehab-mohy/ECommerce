using ECommerce.Domain.Abstractions;
using ECommerce.Domain.Payments;

namespace ECommerce.Domain.Events;

/// <summary>
/// Raised when a reconciliation record is flagged Drift or Unmatched; consumed by the real-time
/// layer to push <c>ReconciliationDrift</c> to the admin group (US-N-003, FR-12).
/// </summary>
public sealed record ReconciliationDriftDetected(
    Guid RecordId,
    Guid PaymentId,
    string ProviderReference,
    decimal Amount,
    string Currency,
    ReconciliationStatus Status,
    string Detail) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
