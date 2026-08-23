namespace ECommerce.Domain.Payments;

public sealed class PaymentAttempt
{
    private PaymentAttempt()
    {
        Action = string.Empty;
        Status = string.Empty;
    }

    public long Id { get; private set; }

    public Guid PaymentId { get; internal set; }

    public int AttemptNo { get; private set; }

    public string Action { get; private set; }

    public decimal Amount { get; private set; }

    public string? ProviderResponse { get; private set; }

    public string Status { get; private set; }

    public string? TraceId { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public static PaymentAttempt Create(
        Guid paymentId,
        int attemptNo,
        string action,
        decimal amount,
        string status,
        string? providerResponse,
        string? traceId,
        DateTime utcNow)
    {
        return new PaymentAttempt
        {
            PaymentId = paymentId,
            AttemptNo = attemptNo,
            Action = action,
            Amount = amount,
            Status = status,
            ProviderResponse = string.IsNullOrWhiteSpace(providerResponse) ? null : providerResponse,
            TraceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId,
            OccurredAt = utcNow
        };
    }
}
