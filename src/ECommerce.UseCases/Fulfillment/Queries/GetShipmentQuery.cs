using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Responses;
using MediatR;

namespace ECommerce.UseCases.Fulfillment.Queries;

public sealed record GetShipmentQuery(
    Guid ShipmentId) : IRequest<Result<ShipmentResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentRead;
}
