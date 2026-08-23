using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Fulfillment;

public sealed class FulfillmentTask : BaseEntity<Guid>
{
    private readonly List<FulfillmentTaskItem> _items = [];

    private FulfillmentTask()
    {
    }

    public Guid OrderId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? ParentTaskId { get; private set; }

    public string? Zone { get; private set; }

    public int Priority { get; private set; }

    public FulfillmentTaskStatus Status { get; private set; }

    public Guid? AssignedTo { get; private set; }

    public DateTime? AssignedAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? PackedAt { get; private set; }

    public DateTime? ShippedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public long Version { get; private set; }

    public IReadOnlyCollection<FulfillmentTaskItem> Items => _items;

    public static FulfillmentTask Create(
        Guid orderId,
        Guid warehouseId,
        int priority,
        DateTime utcNow,
        string? zone = null)
    {
        var task = new FulfillmentTask
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            WarehouseId = warehouseId,
            Zone = string.IsNullOrWhiteSpace(zone) ? null : zone.Trim(),
            Priority = priority,
            Status = FulfillmentTaskStatus.Queued,
            Version = 1,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        task.AddDomainEvent(new FulfillmentTaskCreated(task.Id, orderId, warehouseId, task.Zone, priority));

        return task;
    }

    public void AddItem(Guid productId, string sku, int quantity, string? binLocation)
    {
        _items.Add(FulfillmentTaskItem.Create(Id, productId, sku, quantity, binLocation));
        UpdatedAt = DateTime.UtcNow;
    }

    public Result Assign(Guid pickerId, DateTime utcNow)
    {
        if (Status != FulfillmentTaskStatus.Queued)
        {
            return FulfillmentErrors.NotQueued;
        }

        AssignedTo = pickerId;
        AssignedAt = utcNow;
        Status = FulfillmentTaskStatus.Assigned;
        Version++;
        UpdatedAt = utcNow;

        AddDomainEvent(new FulfillmentTaskAssigned(Id, OrderId, WarehouseId, pickerId));

        return Result.Success();
    }

    public Result<FulfillmentTask> Split(
        Guid warehouseId,
        IReadOnlyCollection<Guid> itemIds,
        int priority,
        string? zone,
        DateTime utcNow)
    {
        if (Status != FulfillmentTaskStatus.Queued)
        {
            return FulfillmentErrors.NotQueued;
        }

        var moving = _items.Where(item => itemIds.Contains(item.Id)).ToList();
        if (moving.Count == 0)
        {
            return FulfillmentErrors.InvalidSplit;
        }

        if (moving.Count == _items.Count)
        {
            return FulfillmentErrors.InvalidSplit;
        }

        var part = new FulfillmentTask
        {
            Id = Guid.NewGuid(),
            OrderId = OrderId,
            WarehouseId = warehouseId,
            ParentTaskId = Id,
            Zone = string.IsNullOrWhiteSpace(zone) ? null : zone.Trim(),
            Priority = priority,
            Status = FulfillmentTaskStatus.Queued,
            Version = 1,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        foreach (var item in moving)
        {
            _items.Remove(item);
            item.MoveTo(part.Id);
            part._items.Add(item);
        }

        Version++;
        UpdatedAt = utcNow;

        AddDomainEvent(new FulfillmentTaskSplit(
            Id,
            part.Id,
            OrderId,
            WarehouseId,
            moving.Select(item => item.Sku).ToList()));

        part.AddDomainEvent(new FulfillmentTaskCreated(
            part.Id,
            part.OrderId,
            part.WarehouseId,
            part.Zone,
            part.Priority));

        return Result<FulfillmentTask>.Success(part);
    }

    public Result StartPicking(DateTime utcNow)
    {
        if (Status != FulfillmentTaskStatus.Assigned)
        {
            return FulfillmentErrors.NotAssigned;
        }

        StartedAt = utcNow;
        Status = FulfillmentTaskStatus.Picking;
        Version++;
        UpdatedAt = utcNow;

        AddDomainEvent(new FulfillmentTaskPicking(Id, OrderId, WarehouseId));

        return Result.Success();
    }

    public Result MarkPacked(DateTime utcNow)
    {
        if (Status != FulfillmentTaskStatus.Picking)
        {
            return FulfillmentErrors.NotPicking;
        }

        PackedAt = utcNow;
        Status = FulfillmentTaskStatus.Packed;
        Version++;
        UpdatedAt = utcNow;

        AddDomainEvent(new FulfillmentTaskPacked(Id, OrderId, WarehouseId));

        return Result.Success();
    }

    public Result MarkShipped(DateTime utcNow)
    {
        if (Status != FulfillmentTaskStatus.Packed)
        {
            return FulfillmentErrors.NotPacked;
        }

        ShippedAt = utcNow;
        Status = FulfillmentTaskStatus.Shipped;
        Version++;
        UpdatedAt = utcNow;

        AddDomainEvent(new FulfillmentTaskShipped(Id, OrderId, WarehouseId));

        return Result.Success();
    }

    public Result Cancel(string reason, DateTime utcNow)
    {
        if (Status is FulfillmentTaskStatus.Shipped or FulfillmentTaskStatus.Cancelled)
        {
            return FulfillmentErrors.InvalidState;
        }

        CancellationReason = reason;
        CancelledAt = utcNow;
        Status = FulfillmentTaskStatus.Cancelled;
        Version++;
        UpdatedAt = utcNow;

        AddDomainEvent(new FulfillmentTaskCancelled(Id, OrderId, WarehouseId, reason));

        return Result.Success();
    }
}
