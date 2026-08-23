using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Responses;
using ECommerce.UseCases.Fulfillment.Shipping;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class CreateShipmentCommandHandler(
    IFulfillmentTaskRepository tasks,
    IOrderRepository orders,
    IShipmentRepository shipments,
    IEnumerable<ICarrierAdapter> carriers,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateShipmentCommand> validator) : IRequestHandler<CreateShipmentCommand, Result<ShipmentResponse>>
{
    public async Task<Result<ShipmentResponse>> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ShipmentResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return FulfillmentErrors.TaskNotFound;
        }

        if (task.Status != FulfillmentTaskStatus.Packed)
        {
            return FulfillmentErrors.NotPacked;
        }

        var order = await orders.GetByIdAsync(task.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.OrderNotFound;
        }

        var carrier = carriers.FirstOrDefault(adapter => string.Equals(
            adapter.CarrierKey,
            request.CarrierKey.Trim().ToLowerInvariant(),
            StringComparison.Ordinal));
        if (carrier is null)
        {
            return ShipmentErrors.UnknownCarrier;
        }

        CarrierShipmentResult carrierResult;
        try
        {
            carrierResult = await carrier.CreateShipmentAsync(
                new CarrierShipmentRequest(
                    ShippingConstants.OriginCountry,
                    request.DestinationCountry,
                    request.DestinationPostalCode,
                    request.WeightGrams,
                    request.Currency,
                    task.Items
                        .Select(item => new CarrierItem(item.Sku, item.Quantity))
                        .ToList()),
                cancellationToken);
        }
        catch
        {
            return FulfillmentErrors.CarrierUnavailable;
        }

        var shipment = Shipment.Create(
            order.Id,
            task.Id,
            carrier.CarrierKey,
            carrierResult.TrackingNumber,
            carrierResult.LabelUrl,
            utcNow);

        var shipped = task.MarkShipped(utcNow);
        if (shipped.IsFailure)
        {
            return shipped.Error;
        }

        var allTasksShipped = !await tasks.HasUnshippedTasksAsync(order.Id, task.Id, cancellationToken);
        if (allTasksShipped)
        {
            var orderShip = order.Ship(
                carrier.CarrierKey,
                [shipment.TrackingNumber],
                "user",
                null,
                null,
                utcNow);
            if (orderShip.IsFailure)
            {
                return orderShip.Error;
            }
        }

        shipments.Add(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ShipmentResponse.From(shipment);
    }
}
