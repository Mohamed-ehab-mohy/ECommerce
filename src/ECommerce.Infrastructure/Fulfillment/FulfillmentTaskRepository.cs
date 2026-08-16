using ECommerce.Domain.Fulfillment;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Fulfillment.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Fulfillment;

public sealed class FulfillmentTaskRepository(ECommerceDbContext dbContext) : IFulfillmentTaskRepository
{
    private static readonly FulfillmentTaskStatus[] OpenStatuses =
    [
        FulfillmentTaskStatus.Queued,
        FulfillmentTaskStatus.Assigned,
        FulfillmentTaskStatus.Picking
    ];

    public Task<FulfillmentTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<FulfillmentTask>()
            .Include(task => task.Items)
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

    public Task<FulfillmentTask?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Set<FulfillmentTask>()
            .Include(task => task.Items)
            .SingleOrDefaultAsync(task => task.OrderId == orderId, cancellationToken);

    public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Set<FulfillmentTask>()
            .AnyAsync(task => task.OrderId == orderId, cancellationToken);

    public Task<bool> HasUnshippedTasksAsync(Guid orderId, Guid excludedTaskId, CancellationToken cancellationToken) =>
        dbContext.Set<FulfillmentTask>()
            .AnyAsync(
                task => task.OrderId == orderId
                    && task.Id != excludedTaskId
                    && task.Status != FulfillmentTaskStatus.Shipped
                    && task.Status != FulfillmentTaskStatus.Cancelled,
                cancellationToken);

    public async Task<IReadOnlyList<FulfillmentTask>> ListAsync(
        Guid? warehouseId,
        FulfillmentTaskStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<FulfillmentTask>().AsNoTracking();

        if (warehouseId is not null)
        {
            query = query.Where(task => task.WarehouseId == warehouseId);
        }

        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        var items = await query
            .Include(task => task.Items)
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountAsync(
        Guid? warehouseId,
        FulfillmentTaskStatus? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<FulfillmentTask>().AsNoTracking();

        if (warehouseId is not null)
        {
            query = query.Where(task => task.WarehouseId == warehouseId);
        }

        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FulfillmentTask>> ListOpenByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<FulfillmentTask>()
            .AsNoTracking()
            .Include(task => task.Items)
            .Where(task => task.WarehouseId == warehouseId && OpenStatuses.Contains(task.Status))
            .OrderBy(task => task.Zone)
            .ThenBy(task => task.CreatedAt)
            .ToListAsync(cancellationToken);

        return items;
    }

    public void Add(FulfillmentTask task) => dbContext.Set<FulfillmentTask>().Add(task);
}
