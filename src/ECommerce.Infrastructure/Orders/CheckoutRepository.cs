using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Checkout.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Orders;

public sealed class CheckoutRepository(ECommerceDbContext dbContext) : ICheckoutRepository
{
    public Task<Checkout?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Checkout>().SingleOrDefaultAsync(checkout => checkout.Id == id, cancellationToken);

    public void Add(Checkout checkout) => dbContext.Set<Checkout>().Add(checkout);
}
