using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Orders.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Orders;

public sealed class OrderRepository(ECommerceDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Order>()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public void Add(Order order) => dbContext.Set<Order>().Add(order);
}
