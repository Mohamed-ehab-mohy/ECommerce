using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Responses;

namespace ECommerce.UseCases.Payments.Commands;

/// <summary>
/// Executes an approved refund idempotently through the originating provider (key = refund id, QAS-04).
/// </summary>
public sealed record ExecuteRefundCommand(Guid RefundId)
    : IRequest<Result<RefundResponse>>, IRequirePermission
{
    public string Permission => Permissions.PaymentsRefundApprove;
}
