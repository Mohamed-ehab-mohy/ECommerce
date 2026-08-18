using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using MediatR;

namespace ECommerce.UseCases.Orders.Commands;

public sealed record ApproveReturnRequestCommand(Guid ReturnRequestId, string? Notes) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.OrdersRead;
}
