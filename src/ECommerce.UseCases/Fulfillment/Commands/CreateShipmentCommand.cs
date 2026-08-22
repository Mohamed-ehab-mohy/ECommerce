using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UseCases.Fulfillment.Commands;

public sealed record CreateShipmentCommand(
    Guid TaskId,
    string CarrierKey,
    string DestinationCountry,
    string DestinationPostalCode,
    int WeightGrams,
    string Currency) : IRequest<Result<ShipmentResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentWrite;
}
