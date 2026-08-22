using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UseCases.Fulfillment.Commands;

public sealed record AssignFulfillmentTaskCommand(
    Guid TaskId,
    Guid AssigneeId) : IRequest<Result<FulfillmentTaskResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentWrite;
}

public sealed record StartPickingFulfillmentTaskCommand(
    Guid TaskId) : IRequest<Result<FulfillmentTaskResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentWrite;
}

public sealed record MarkFulfillmentTaskPackedCommand(
    Guid TaskId) : IRequest<Result<FulfillmentTaskResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentWrite;
}
