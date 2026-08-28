using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Ports;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Locks the payment row for the duration of the current transaction
    /// (SELECT ... FOR UPDATE), so concurrent capture attempts wait and then
    /// observe the latest committed status instead of racing on the same
    /// authorized payment.
    /// </summary>
    Task<Payment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Payments with a provider reference that have no reconciliation snapshot yet.</summary>
    Task<IReadOnlyList<Payment>> GetUnreconciledAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentReconciliationRecord>> GetReconciliationRecordsAsync(
        ReconciliationStatus? status,
        CancellationToken cancellationToken);

    Task<Payment?> GetByProviderTokenAsync(string providerToken, CancellationToken cancellationToken);

    void Add(Payment payment);

    void AddReconciliationRecord(PaymentReconciliationRecord record);
}
