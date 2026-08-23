using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Responses;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class CreateFulfillmentTaskCommandHandler(
    IOrderRepository orders,
    IProductRepository products,
    IWarehouseRepository warehouses,
    IFulfillmentTaskRepository tasks,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateFulfillmentTaskCommand> validator) : IRequestHandler<CreateFulfillmentTaskCommand, Result<FulfillmentTaskResponse>>
{
    public async Task<Result<FulfillmentTaskResponse>> Handle(CreateFulfillmentTaskCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<FulfillmentTaskResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (await tasks.ExistsForOrderAsync(request.OrderId, cancellationToken))
        {
            return FulfillmentErrors.TaskExistsForOrder;
        }

        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.OrderNotFound;
        }

        if (order.Status != OrderStatus.AwaitingFulfillment)
        {
            return FulfillmentErrors.OrderNotReady;
        }

        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return FulfillmentErrors.WarehouseNotFound;
        }

        var task = FulfillmentTask.Create(order.Id, warehouse.Id, request.Priority, utcNow, request.Zone);

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var productEntities = await products.GetByIdsAsync(productIds, cancellationToken);
        var productById = productEntities.ToDictionary(product => product.Id);

        foreach (var line in order.Items)
        {
            if (productById.TryGetValue(line.ProductId, out var product))
            {
                task.AddItem(product.Id, product.Sku, line.Quantity, null);
            }
        }

        if (task.Items.Count == 0)
        {
            return FulfillmentErrors.InvalidState;
        }

        tasks.Add(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return FulfillmentTaskResponse.From(task);
    }
}
