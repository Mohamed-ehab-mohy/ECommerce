using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Checkout.Ports;

namespace ECommerce.Infrastructure.Orders;

public sealed class CheckoutRepository(ECommerceDbContext dbContext) : ICheckoutRepository
{
    public Task<Checkout?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Checkout>().SingleOrDefaultAsync(checkout => checkout.Id == id, cancellationToken);

    public Task<Checkout?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        dbContext.Set<Checkout>()
            .SingleOrDefaultAsync(checkout => checkout.PaymentId == paymentId, cancellationToken);

    public void Add(Checkout checkout) => dbContext.Set<Checkout>().Add(checkout);
}
