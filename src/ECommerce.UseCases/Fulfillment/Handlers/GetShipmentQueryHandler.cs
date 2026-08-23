using ECommerce.Domain.Fulfillment;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Queries;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class GetShipmentQueryHandler(
    IShipmentRepository shipments) : IRequestHandler<GetShipmentQuery, Result<ShipmentResponse>>
{
    public async Task<Result<ShipmentResponse>> Handle(GetShipmentQuery request, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken);

        return shipment is null
            ? Result<ShipmentResponse>.Failure(ShipmentErrors.ShipmentNotFound)
            : Result<ShipmentResponse>.Success(ShipmentResponse.From(shipment));
    }
}
