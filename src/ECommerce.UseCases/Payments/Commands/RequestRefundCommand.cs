using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Responses;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record RefundItemRequest(Guid ProductId, int Quantity);

public sealed record RequestRefundCommand(
    string OrderNumber,
    decimal Amount,
    string Reason,
    IReadOnlyCollection<RefundItemRequest>? Items,
    bool Restock,
    string IdempotencyKey)
    : IRequest<Result<RefundResponse>>, IRequirePermission
{
    public string Permission => Permissions.PaymentsRefundApprove;
}
