using ECommerce.Domain.Common;

namespace ECommerce.Domain.Payments;

public enum ReconciliationStatus
{
    /// <summary>Awaiting the nightly provider reconciliation job (S12).</summary>
    Pending,
    /// <summary>Provider transaction matches the platform record.</summary>
    Matched,
    /// <summary>Provider transaction differs from the platform record.</summary>
    Drift,
    /// <summary>Provider transaction exists with no matching platform record.</summary>
    Unmatched
}

/// <summary>
/// Provider-vs-platform reconciliation snapshot (T-DAT-010). One row per payment expected from the provider;
/// the S12 nightly job fills <see cref="ProviderStatus"/> and marks Matched/Drift/Unmatched.
/// </summary>
public sealed class PaymentReconciliationRecord : BaseEntity<Guid>
{
    private PaymentReconciliationRecord()
    {
        ProviderKey = string.Empty;
        ProviderReference = string.Empty;
        Currency = string.Empty;
        RecordedStatus = string.Empty;
        ProviderStatus = string.Empty;
        Detail = string.Empty;
    }

    public Guid PaymentId { get; private set; }

    public string ProviderKey { get; private set; }

    public string ProviderReference { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    /// <summary>Platform-side payment status at snapshot time (as enum name).</summary>
    public string RecordedStatus { get; private set; }

    /// <summary>Provider-reported status; filled by the S12 reconciliation job.</summary>
    public string ProviderStatus { get; private set; }

    public ReconciliationStatus Status { get; private set; }

    public string Detail { get; private set; }

    public DateTime CheckedAtUtc { get; private set; }

    public static PaymentReconciliationRecord Create(
        Guid paymentId,
        string providerKey,
        string providerReference,
        decimal amount,
        string currency,
        string recordedStatus,
        DateTime utcNow)
    {
        var record = new PaymentReconciliationRecord
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            ProviderKey = providerKey,
            ProviderReference = providerReference,
            Amount = amount,
            Currency = currency,
            RecordedStatus = recordedStatus,
            ProviderStatus = string.Empty,
            Status = ReconciliationStatus.Pending,
            Detail = string.Empty,
            CheckedAtUtc = utcNow,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        return record;
    }

    /// <summary>Marks a provider/platform mismatch; drift is detectable via the status flag (T-DAT-010).</summary>
    public void MarkDrift(string detail, DateTime utcNow)
    {
        Status = ReconciliationStatus.Drift;
        Detail = detail;
        UpdatedAt = utcNow;
    }

    public void MarkMatched(string providerStatus, DateTime utcNow)
    {
        Status = ReconciliationStatus.Matched;
        ProviderStatus = providerStatus;
        UpdatedAt = utcNow;
    }

    /// <summary>Marks a provider transaction that has no matching platform record (T-DAT-015).</summary>
    public void MarkUnmatched(string detail, DateTime utcNow)
    {
        Status = ReconciliationStatus.Unmatched;
        Detail = detail;
        UpdatedAt = utcNow;
    }
}
