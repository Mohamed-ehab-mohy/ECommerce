using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Responses;

public sealed record PaymentResponse(
    Guid PaymentId,
    string Currency,
    decimal Amount,
    PaymentStatus Status,
    string ProviderKey,
    string ProviderReference,
    string ClientToken,
    DateTime? AuthorizedAt)
{
    public static PaymentResponse From(Payment payment) =>
        new(
            payment.Id,
            payment.Currency,
            payment.Amount,
            payment.Status,
            payment.ProviderKey,
            payment.ProviderReference ?? string.Empty,
            payment.ClientToken,
            payment.AuthorizedAt);
}
