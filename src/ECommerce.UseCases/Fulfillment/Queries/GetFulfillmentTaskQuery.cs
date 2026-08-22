using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UseCases.Fulfillment.Queries;

public sealed record GetFulfillmentTaskQuery(
    Guid TaskId) : IRequest<Result<FulfillmentTaskResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentRead;
}
