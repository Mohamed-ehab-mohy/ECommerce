using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Orders.Commands;

public sealed record RejectReturnRequestCommand(Guid ReturnRequestId, string Reason) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.OrdersRead;
}
