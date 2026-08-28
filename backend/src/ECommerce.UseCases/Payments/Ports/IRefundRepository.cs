using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Ports;

public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Refund?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>All non-rejected refunds for a payment, for computing the remaining refundable amount.</summary>
    Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Refund>> ListFailedAsync(CancellationToken cancellationToken);

    void Add(Refund refund);
}
