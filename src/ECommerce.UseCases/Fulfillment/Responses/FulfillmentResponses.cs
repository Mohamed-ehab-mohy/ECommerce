using ECommerce.Domain.Fulfillment;

namespace ECommerce.UseCases.Fulfillment.Responses;

public sealed record FulfillmentTaskItemResponse(
    Guid Id,
    Guid ProductId,
    string Sku,
    int Quantity,
    string? BinLocation);

public sealed record FulfillmentTaskResponse(
    Guid TaskId,
    Guid OrderId,
    Guid WarehouseId,
    Guid? ParentTaskId,
    string? Zone,
    int Priority,
    string Status,
    Guid? AssignedTo,
    DateTime? AssignedAt,
    DateTime? StartedAt,
    DateTime? PackedAt,
    DateTime? ShippedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<FulfillmentTaskItemResponse> Items)
{
    public static FulfillmentTaskResponse From(FulfillmentTask task) =>
        new(
            task.Id,
            task.OrderId,
            task.WarehouseId,
            task.ParentTaskId,
            task.Zone,
            task.Priority,
            task.Status.ToString(),
            task.AssignedTo,
            task.AssignedAt,
            task.StartedAt,
            task.PackedAt,
            task.ShippedAt,
            task.CancelledAt,
            task.CancellationReason,
            task.CreatedAt,
            task.UpdatedAt,
            task.Items
                .Select(item => new FulfillmentTaskItemResponse(
                    item.Id,
                    item.ProductId,
                    item.Sku,
                    item.Quantity,
                    item.BinLocation))
                .ToList());
}

public sealed record PagedFulfillmentTasksResponse(
    IReadOnlyList<FulfillmentTaskResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
