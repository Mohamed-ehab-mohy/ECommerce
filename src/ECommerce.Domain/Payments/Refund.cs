using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Payments;

/// <summary>
/// Refund request aggregate (FRS-I-004, UC-I-004). Created in <see cref="RefundStatus.Requested"/> and driven
/// through approval and idempotent provider execution. The refund id doubles as the provider idempotency key,
/// so replaying execution never creates a duplicate provider refund (QAS-04).
/// </summary>
public sealed class Refund : BaseEntity<Guid>
{
    private readonly List<RefundItem> _items = [];

    private Refund()
    {
        Reason = string.Empty;
        Currency = string.Empty;
        IdempotencyKey = string.Empty;
    }

    public Guid OrderId { get; private set; }

    public Guid PaymentId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public string Reason { get; private set; }

    public bool Restock { get; private set; }

    /// <summary>Client-supplied idempotency key; unique across refunds.</summary>
    public string IdempotencyKey { get; private set; }

    public RefundStatus Status { get; private set; }

    public string? ProviderReference { get; private set; }

    public string? FailureDetail { get; private set; }

    public Guid? ApprovedBy { get; private set; }

    public DateTime? ApprovedAt { get; private set; }

    public int Attempts { get; private set; }

    public IReadOnlyCollection<RefundItem> Items => _items;

    public static Refund Create(
        Guid orderId,
        Guid paymentId,
        decimal amount,
        string currency,
        string reason,
        bool restock,
        string idempotencyKey,
        IReadOnlyCollection<RefundItem> items,
        DateTime utcNow)
    {
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PaymentId = paymentId,
            Amount = amount,
            Currency = currency,
            Reason = reason,
            Restock = restock,
            IdempotencyKey = idempotencyKey,
            Status = RefundStatus.Requested,
            Attempts = 0,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        refund._items.AddRange(items);

        refund.AddDomainEvent(new RefundRequested(refund.Id, orderId, paymentId, amount, currency));

        return refund;
    }

    public Result Approve(Guid? approvedBy, DateTime utcNow)
    {
        if (Status != RefundStatus.Requested)
        {
            return RefundErrors.InvalidState;
        }

        Status = RefundStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = utcNow;
        UpdatedAt = utcNow;

        AddDomainEvent(new RefundApproved(Id, OrderId, PaymentId, Amount, Currency, approvedBy));

        return Result.Success();
    }

    public Result Reject(Guid? rejectedBy, string reason, DateTime utcNow)
    {
        if (Status != RefundStatus.Requested)
        {
            return RefundErrors.InvalidState;
        }

        Status = RefundStatus.Rejected;
        FailureDetail = reason;
        UpdatedAt = utcNow;

        AddDomainEvent(new RefundRejected(Id, OrderId, reason, rejectedBy));

        return Result.Success();
    }

    /// <summary>Marks the refund as executing (or retrying after a failure).</summary>
    public Result BeginExecution(DateTime utcNow)
    {
        if (Status is not (RefundStatus.Approved or RefundStatus.Failed))
        {
            return RefundErrors.InvalidState;
        }

        Status = RefundStatus.Executing;
        Attempts++;
        UpdatedAt = utcNow;

        AddDomainEvent(new RefundExecuting(Id, PaymentId, Amount, Currency, Attempts));

        return Result.Success();
    }

    public Result MarkCompleted(string? providerReference, DateTime utcNow)
    {
        if (Status != RefundStatus.Executing)
        {
            return RefundErrors.InvalidState;
        }

        Status = RefundStatus.Completed;
        ProviderReference = providerReference ?? ProviderReference;
        FailureDetail = null;
        UpdatedAt = utcNow;

        AddDomainEvent(new RefundCompleted(Id, OrderId, PaymentId, Amount, Currency, ProviderReference));

        return Result.Success();
    }

    public Result MarkFailed(string detail, DateTime utcNow)
    {
        if (Status != RefundStatus.Executing)
        {
            return RefundErrors.InvalidState;
        }

        Status = RefundStatus.Failed;
        FailureDetail = detail;
        UpdatedAt = utcNow;

        AddDomainEvent(new RefundFailed(Id, PaymentId, Amount, detail));

        return Result.Success();
    }
}
