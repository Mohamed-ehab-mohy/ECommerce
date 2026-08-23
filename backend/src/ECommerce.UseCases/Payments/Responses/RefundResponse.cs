using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Responses;

public sealed record RefundItemResponse(Guid ProductId, int Quantity);

public sealed record RefundResponse(
    Guid RefundId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Reason,
    bool Restock,
    string Status,
    string? ProviderReference,
    decimal RefundableAmount,
    string IdempotencyKey)
{
    public static RefundResponse From(Refund refund, decimal refundableAmount) =>
        new(
            refund.Id,
            refund.OrderId,
            refund.PaymentId,
            refund.Amount,
            refund.Currency,
            refund.Reason,
            refund.Restock,
            refund.Status.ToString().ToLowerInvariant(),
            refund.ProviderReference,
            refundableAmount,
            refund.IdempotencyKey);
}
