using ECommerce.Domain.Orders;

namespace ECommerce.UseCases.Orders.Ports;

public sealed record OrderHistoryPage(
    IReadOnlyList<Order> Items,
    string? NextCursor,
    bool HasNext);

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken);

    Task<Order?> GetByNumberWithDetailsAsync(string orderNumber, CancellationToken cancellationToken);

    Task<OrderHistoryPage> ListByCustomerAsync(
        Guid customerId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderBackorderItem>> ListOpenBackorderItemsBySkuAsync(string sku, CancellationToken cancellationToken);

    void Add(Order order);
}
