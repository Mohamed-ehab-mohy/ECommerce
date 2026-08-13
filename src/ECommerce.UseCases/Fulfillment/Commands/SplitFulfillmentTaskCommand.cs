using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Responses;
using MediatR;

namespace ECommerce.UseCases.Fulfillment.Commands;

public sealed record SplitFulfillmentTaskCommand(
    Guid TaskId,
    Guid WarehouseId,
    IReadOnlyList<Guid> ItemIds,
    int Priority,
    string? Zone) : IRequest<Result<FulfillmentTaskResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentWrite;
}
