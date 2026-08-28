namespace ECommerce.Domain.Payments;

public sealed record PaymentIntentRequest(
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string MethodType,
    Guid? CustomerId);

public sealed record PaymentIntentResult(
    bool IsSuccess,
    string ClientToken,
    string ProviderToken,
    string? ProviderReference,
    string? ErrorCode);

public sealed record PaymentAuthorizationRequest(
    decimal Amount,
    string Currency,
    string ProviderToken,
    string IdempotencyKey);

public sealed record PaymentAuthorizationResult(
    bool IsSuccess,
    string ProviderReference,
    string? DeclineCode);

public sealed record PaymentRefundRequest(
    decimal Amount,
    string Currency,
    string ProviderReference,
    string IdempotencyKey);

public sealed record PaymentRefundResult(
    bool IsSuccess,
    string? ProviderReference,
    string? ErrorCode);

public sealed record ProviderTransaction(
    string ProviderReference,
    string EventType,
    decimal Amount,
    string Currency,
    string Status,
    DateTime OccurredAt);

public interface IPaymentProvider
{
    string Key { get; }

    Task<PaymentIntentResult> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken cancellationToken);

    Task<PaymentAuthorizationResult> AuthorizeAsync(
        PaymentAuthorizationRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a refund through the provider. The provider must treat <paramref name="request.IdempotencyKey"/>
    /// (the refund id) as idempotent: replaying the same key returns the original result (FRS-G-008).
    /// </summary>
    Task<PaymentRefundResult> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the provider-side transactions (authorize/capture/refund) within the given window
    /// for nightly reconciliation.
    /// </summary>
    Task<IReadOnlyList<ProviderTransaction>> ListTransactionsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
