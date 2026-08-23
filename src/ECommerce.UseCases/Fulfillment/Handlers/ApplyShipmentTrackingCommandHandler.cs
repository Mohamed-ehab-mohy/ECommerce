using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Responses;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class ApplyShipmentTrackingCommandHandler(
    IShipmentRepository shipments,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<ApplyShipmentTrackingCommand> validator) : IRequestHandler<ApplyShipmentTrackingCommand, Result<ShipmentResponse>>
{
    public async Task<Result<ShipmentResponse>> Handle(ApplyShipmentTrackingCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ShipmentResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return ShipmentErrors.ShipmentNotFound;
        }

        if (!Enum.TryParse<ShipmentStatus>(request.Status, ignoreCase: true, out var status))
        {
            return ShipmentErrors.InvalidTransition;
        }

        var result = shipment.ApplyTrackingUpdate(status, utcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        if (status == ShipmentStatus.Delivered
            && !await shipments.HasUndeliveredShipmentsAsync(shipment.OrderId, shipment.Id, cancellationToken))
        {
            var order = await orders.GetByIdAsync(shipment.OrderId, cancellationToken);
            if (order is null)
            {
                return OrderErrors.OrderNotFound;
            }

            var delivery = order.Deliver("system", null, null, utcNow);
            if (delivery.IsFailure)
            {
                return delivery.Error;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ShipmentResponse.From(shipment);
    }
}
