using ECommerce.Domain.Fulfillment;

namespace ECommerce.UseCases.Fulfillment.Ports;

public interface IFulfillmentTaskRepository
{
    Task<FulfillmentTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<FulfillmentTask?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<bool> HasUnshippedTasksAsync(Guid orderId, Guid excludedTaskId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FulfillmentTask>> ListAsync(
        Guid? warehouseId,
        FulfillmentTaskStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountAsync(Guid? warehouseId, FulfillmentTaskStatus? status, CancellationToken cancellationToken);

    Task<IReadOnlyList<FulfillmentTask>> ListOpenByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken);

    void Add(FulfillmentTask task);
}
