using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Responses;
using MediatR;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record ApproveRefundCommand(Guid RefundId, Guid? ApprovedBy)
    : IRequest<Result<RefundResponse>>, IRequirePermission
{
    public string Permission => Permissions.PaymentsRefundApprove;
}
