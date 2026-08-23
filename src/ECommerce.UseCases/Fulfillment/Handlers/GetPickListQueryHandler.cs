using ECommerce.Domain.Inventory;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Queries;
using ECommerce.UseCases.Fulfillment.Responses;
using ECommerce.UseCases.Fulfillment.Services;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class GetPickListQueryHandler(
    IWarehouseRepository warehouses,
    IFulfillmentTaskRepository tasks,
    IOrderRepository orders,
    PickListGenerationService pickListService) : IRequestHandler<GetPickListQuery, Result<IReadOnlyList<PickListResponse>>>
{
    public async Task<Result<IReadOnlyList<PickListResponse>>> Handle(GetPickListQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return WarehouseErrors.WarehouseNotFound;
        }

        var openTasks = await tasks.ListOpenByWarehouseAsync(warehouse.Id, cancellationToken);

        var orderNumberByOrderId = new Dictionary<Guid, string>();
        foreach (var orderId in openTasks.Select(task => task.OrderId).Distinct())
        {
            var order = await orders.GetByIdAsync(orderId, cancellationToken);
            if (order is not null)
            {
                orderNumberByOrderId[orderId] = order.OrderNumber;
            }
        }

        return Result<IReadOnlyList<PickListResponse>>.Success(pickListService.Generate(warehouse.Code, openTasks, orderNumberByOrderId));
    }
}
