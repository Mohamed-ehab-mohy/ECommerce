namespace ECommerce.Domain.Payments;

/// <summary>
/// Append-only payment ledger entry (US-G-007). Records intent/authorize/capture/void events exactly once;
/// rows are never updated or deleted, giving an audit trail that is reconciliation-ready (T-DAT-010).
/// </summary>
public sealed class PaymentLedgerEntry
{
    private PaymentLedgerEntry()
    {
        EventType = string.Empty;
        Status = string.Empty;
    }

    public long Id { get; private set; }

    public Guid PaymentId { get; internal set; }

    /// <summary>Monotonic per-payment event number, mirroring the payment's attempt counter.</summary>
    public int Sequence { get; private set; }

    /// <summary>Ledger event: intent_created, authorize_requested, authorized, failed, captured, voided, refund_requested.</summary>
    public string EventType { get; private set; }

    public string Status { get; private set; }

    public decimal Amount { get; private set; }

    public string? ProviderReference { get; private set; }

    /// <summary>Optional machine-readable detail (e.g. decline code).</summary>
    public string? Detail { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public static PaymentLedgerEntry Create(
        Guid paymentId,
        int sequence,
        string eventType,
        string status,
        decimal amount,
        string? providerReference,
        string? detail,
        DateTime utcNow) =>
        new()
        {
            PaymentId = paymentId,
            Sequence = sequence,
            EventType = eventType,
            Status = status,
            Amount = amount,
            ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference,
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail,
            OccurredAt = utcNow
        };
}
