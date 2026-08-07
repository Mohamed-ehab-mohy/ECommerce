using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Payments.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Payments;

public sealed class PaymentRepository(ECommerceDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Payment>()
            .Include(payment => payment.Attempts)
            .SingleOrDefaultAsync(payment => payment.Id == id, cancellationToken);

    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Set<Payment>()
            .Include(payment => payment.Attempts)
            .SingleOrDefaultAsync(payment => payment.OrderId == orderId, cancellationToken);

    public void Add(Payment payment) => dbContext.Set<Payment>().Add(payment);
}
