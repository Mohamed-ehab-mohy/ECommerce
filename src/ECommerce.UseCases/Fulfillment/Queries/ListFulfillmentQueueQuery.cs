using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Responses;
using MediatR;

namespace ECommerce.UseCases.Fulfillment.Queries;

public sealed record ListFulfillmentQueueQuery(
    Guid? WarehouseId,
    string? Status,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedFulfillmentTasksResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentRead;
}
