using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Ports;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Payments with a provider reference that have no reconciliation snapshot yet (T-DAT-010).</summary>
    Task<IReadOnlyList<Payment>> GetUnreconciledAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentReconciliationRecord>> GetReconciliationRecordsAsync(
        ReconciliationStatus? status,
        CancellationToken cancellationToken);

    Task<Payment?> GetByProviderTokenAsync(string providerToken, CancellationToken cancellationToken);

    void Add(Payment payment);

    void AddReconciliationRecord(PaymentReconciliationRecord record);
}
