using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UseCases.Fulfillment.Commands;

public sealed record ApplyShipmentTrackingCommand(
    Guid ShipmentId,
    string Status) : IRequest<Result<ShipmentResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentWrite;
}
