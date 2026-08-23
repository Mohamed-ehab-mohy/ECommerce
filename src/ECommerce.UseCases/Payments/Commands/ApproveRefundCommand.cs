using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Responses;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record ApproveRefundCommand(Guid RefundId, Guid? ApprovedBy)
    : IRequest<Result<RefundResponse>>, IRequirePermission
{
    public string Permission => Permissions.PaymentsRefundApprove;
}
