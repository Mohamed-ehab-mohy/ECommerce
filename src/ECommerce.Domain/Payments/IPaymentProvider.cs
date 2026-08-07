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

public interface IPaymentProvider
{
    string Key { get; }

    Task<PaymentIntentResult> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken cancellationToken);

    Task<PaymentAuthorizationResult> AuthorizeAsync(
        PaymentAuthorizationRequest request,
        CancellationToken cancellationToken);
}
