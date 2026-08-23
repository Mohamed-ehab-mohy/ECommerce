using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Responses;

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
