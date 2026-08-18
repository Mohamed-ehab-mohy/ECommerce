using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Orders;

public sealed class OrderRepository(ECommerceDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        CompiledQueries.GetOrderById(dbContext, id);

    public Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken) =>
        CompiledQueries.GetOrderByNumber(dbContext, orderNumber);

    public Task<Order?> GetByNumberWithDetailsAsync(string orderNumber, CancellationToken cancellationToken) =>
        CompiledQueries.GetOrderByNumberWithDetails(dbContext, orderNumber);

    public async Task<IReadOnlyList<OrderBackorderItem>> ListOpenBackorderItemsBySkuAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<OrderBackorderItem>()
            .Where(item => item.Sku == sku && item.Status == BackorderStatus.Open)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<OrderHistoryPage> ListByCustomerAsync(
        Guid customerId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Order>()
            .Where(order => order.CustomerId == customerId)
            .AsNoTracking();

        if (Cursor.TryDecode(cursor, out var at, out var id))
        {
            query = query.Where(order =>
                order.PlacedAt != null
                && (order.PlacedAt < at
                    || (order.PlacedAt == at && order.Id.CompareTo(id) < 0)));
        }

        var items = await query
            .Include(order => order.Items)
            .OrderByDescending(order => order.PlacedAt)
            .ThenByDescending(order => order.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNext = items.Count > pageSize;
        var page = items.Take(pageSize).ToList();

        string? nextCursor = null;
        if (hasNext && page.Count > 0)
        {
            var last = page[^1];
            nextCursor = last.PlacedAt is { } placedAt
                ? Cursor.Encode(placedAt, last.Id)
                : null;
        }

        return new OrderHistoryPage(page, nextCursor, hasNext);
    }

    public async Task<IReadOnlyList<Order>> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(order => order.CustomerEmail == email)
            .OrderByDescending(order => order.PlacedAt)
            .ToListAsync(cancellationToken);

    public void Add(Order order) => dbContext.Set<Order>().Add(order);
}
