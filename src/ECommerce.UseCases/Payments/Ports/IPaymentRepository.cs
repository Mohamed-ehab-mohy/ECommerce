using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Ports;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    void Add(Payment payment);
}
