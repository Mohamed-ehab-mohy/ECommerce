using ECommerce.Domain.Common;
using ECommerce.Shared.Primitives;

namespace ECommerce.Domain.Payments;

public sealed class Payment : BaseEntity<Guid>
{
    private readonly List<PaymentAttempt> _attempts = [];

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

    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts;

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
        return new Payment
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
        if (Status is not (PaymentStatus.Created or PaymentStatus.Failed))
        {
            return PaymentErrors.CaptureConflict;
        }

        Status = PaymentStatus.Authorized;
        AuthorizedAmount = Amount;
        ProviderReference = providerReference;
        AuthorizedAt = utcNow;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result MarkFailed(DateTime utcNow)
    {
        if (Status is PaymentStatus.Created or PaymentStatus.Failed)
        {
            Status = PaymentStatus.Failed;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        return PaymentErrors.CaptureConflict;
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
}
