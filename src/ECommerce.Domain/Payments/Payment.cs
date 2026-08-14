using ECommerce.Domain.Common;
using ECommerce.Domain.Events;
using ECommerce.Shared.Primitives;

namespace ECommerce.Domain.Payments;

public sealed class Payment : BaseEntity<Guid>
{
    private readonly List<PaymentAttempt> _attempts = [];
    private readonly List<PaymentLedgerEntry> _ledger = [];

    private Payment()
    {
        ProviderKey = string.Empty;
        ProviderToken = string.Empty;
        ClientToken = string.Empty;
        Currency = string.Empty;
    }

    public Guid? OrderId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string ProviderKey { get; private set; }

    public string ProviderToken { get; private set; }

    public string ClientToken { get; private set; }

    public string? ProviderReference { get; private set; }

    public string Currency { get; private set; }

    public decimal Amount { get; private set; }

    public decimal? FxRate { get; private set; }

    public decimal AuthorizedAmount { get; private set; }

    public PaymentStatus Status { get; private set; }

    public DateTime? AuthorizedAt { get; private set; }

    public DateTime? CapturedAt { get; private set; }

    public DateTime? VoidedAt { get; private set; }

    public int Attempt { get; private set; }

    /// <summary>Earliest time a retry of a declined payment is allowed (cooldown, US-G-004).</summary>
    public DateTime? RetryAfterUtc { get; private set; }

    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts;

    /// <summary>Append-only payment ledger (US-G-007); entries are never updated or deleted.</summary>
    public IReadOnlyCollection<PaymentLedgerEntry> Ledger => _ledger;

    public static Payment Create(
        Guid? customerId,
        string providerKey,
        string providerToken,
        string clientToken,
        string? providerReference,
        string currency,
        decimal amount,
        decimal? fxRate,
        DateTime utcNow)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ProviderKey = providerKey,
            ProviderToken = providerToken,
            ClientToken = clientToken,
            ProviderReference = providerReference,
            Currency = currency,
            Amount = amount,
            FxRate = fxRate,
            Status = PaymentStatus.Created,
            Attempt = 0,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        payment.RecordLedger("intent_created", "created", amount, providerReference, null, utcNow);

        return payment;
    }

    public Result AttachOrder(Guid orderId, DateTime utcNow)
    {
        if (OrderId is not null && OrderId != orderId)
        {
            return PaymentErrors.CaptureConflict;
        }

        OrderId = orderId;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result MarkAuthorized(string providerReference, DateTime utcNow)
    {
        if (Status is not (PaymentStatus.Created or PaymentStatus.Failed or PaymentStatus.RetryPending))
        {
            return PaymentErrors.CaptureConflict;
        }

        Status = PaymentStatus.Authorized;
        AuthorizedAmount = Amount;
        ProviderReference = providerReference;
        AuthorizedAt = utcNow;
        RetryAfterUtc = null;
        UpdatedAt = utcNow;
        RecordLedger("authorized", "authorized", Amount, providerReference, null, utcNow);
        return Result.Success();
    }

    public Result MarkFailed(DateTime utcNow, string? declineCode = null)
    {
        if (Status is PaymentStatus.Created or PaymentStatus.Failed or PaymentStatus.RetryPending)
        {
            Status = PaymentStatus.Failed;
            UpdatedAt = utcNow;
            RecordLedger("failed", "failed", Amount, null, declineCode, utcNow);
            return Result.Success();
        }

        return PaymentErrors.CaptureConflict;
    }

    /// <summary>Checks whether a declined payment may be retried now (bounded + cooldown, US-G-004).</summary>
    public Result CanRetry(DateTime utcNow) =>
        Status is not (PaymentStatus.Failed or PaymentStatus.RetryPending)
            ? PaymentErrors.CaptureConflict
            : _attempts.Count > 0 && _attempts[^1].Status == "exhausted"
                ? PaymentErrors.RetryExhausted
                : RetryAfterUtc is not null && utcNow < RetryAfterUtc.Value
                    ? PaymentErrors.RetryInCooldown
                    : Result.Success();

    /// <summary>
    /// Schedules a bounded retry after a decline: transitions Failed → RetryPending with a cooldown
    /// window, refusing further retries once the attempt budget is exhausted.
    /// </summary>
    public Result PlanRetry(TimeSpan cooldown, int maxAttempts, DateTime utcNow)
    {
        if (Status != PaymentStatus.Failed)
        {
            return PaymentErrors.CaptureConflict;
        }

        if (Attempt >= maxAttempts)
        {
            return PaymentErrors.RetryExhausted;
        }

        Status = PaymentStatus.RetryPending;
        RetryAfterUtc = utcNow + cooldown;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Capture(decimal amount, DateTime utcNow)
    {
        if (Status != PaymentStatus.Authorized)
        {
            return PaymentErrors.CaptureConflict;
        }

        if (amount <= 0m || amount > AuthorizedAmount)
        {
            return PaymentErrors.CaptureExceedsAuthorization;
        }

        Status = PaymentStatus.Captured;
        CapturedAt = utcNow;
        UpdatedAt = utcNow;
        RecordLedger("captured", "captured", amount, ProviderReference, null, utcNow);

        AddDomainEvent(new PaymentCaptured(Id, OrderId, amount, Currency));

        return Result.Success();
    }

    public Result Void(DateTime utcNow)
    {
        if (Status != PaymentStatus.Authorized)
        {
            return PaymentErrors.CaptureConflict;
        }

        Status = PaymentStatus.Cancelled;
        VoidedAt = utcNow;
        UpdatedAt = utcNow;
        RecordLedger("voided", "voided", Amount, ProviderReference, null, utcNow);
        return Result.Success();
    }

    public Result RequestRefund(string reason, DateTime utcNow)
    {
        if (Status != PaymentStatus.Captured)
        {
            return PaymentErrors.RefundNotAllowed;
        }

        Status = PaymentStatus.Refunding;
        UpdatedAt = utcNow;
        RecordAttempt("refund_requested", Amount, "pending", null, null, utcNow);
        RecordLedger("refund_requested", "pending", Amount, ProviderReference, null, utcNow);
        return Result.Success();
    }

    /// <summary>Completes a refund for a payment in <see cref="PaymentStatus.Refunding"/> (US-I-002).</summary>
    public Result MarkRefunded(DateTime utcNow, string? providerReference = null)
    {
        if (Status != PaymentStatus.Refunding)
        {
            return PaymentErrors.CaptureConflict;
        }

        Status = PaymentStatus.Refunded;
        ProviderReference = providerReference ?? ProviderReference;
        UpdatedAt = utcNow;
        RecordAttempt("refund_completed", Amount, "refunded", providerReference, null, utcNow);
        RecordLedger("refunded", "refunded", Amount, ProviderReference, null, utcNow);

        AddDomainEvent(new PaymentRefunded(Id, OrderId, Amount, Currency, ProviderReference));

        return Result.Success();
    }

    public void RecordAttempt(
        string action,
        decimal amount,
        string status,
        string? providerResponse,
        string? traceId,
        DateTime utcNow)
    {
        Attempt++;
        _attempts.Add(PaymentAttempt.Create(Id, Attempt, action, amount, status, providerResponse, traceId, utcNow));
        UpdatedAt = utcNow;
    }

    /// <summary>Appends an immutable ledger entry for a payment transition (US-G-007).</summary>
    private void RecordLedger(
        string eventType,
        string status,
        decimal amount,
        string? providerReference,
        string? detail,
        DateTime utcNow) =>
        _ledger.Add(PaymentLedgerEntry.Create(
            Id,
            _ledger.Count + 1,
            eventType,
            status,
            amount,
            providerReference,
            detail,
            utcNow));
}
