using ECommerce.Domain.Orders;

namespace ECommerce.UseCases.Orders.Ports;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Order order);
}
