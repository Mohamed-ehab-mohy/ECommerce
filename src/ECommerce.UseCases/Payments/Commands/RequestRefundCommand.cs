using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Responses;
using MediatR;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record RequestRefundCommand(Guid PaymentId, string Reason)
    : IRequest<Result<PaymentResponse>>, IRequirePermission
{
    public string Permission => Permissions.PaymentsRefundApprove;
}
