using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Responses;
using MediatR;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record CompleteRefundCommand(Guid PaymentId, string? ProviderReference = null)
    : IRequest<Result<PaymentResponse>>, IRequirePermission
{
    public string Permission => Permissions.PaymentsRefundApprove;
}
