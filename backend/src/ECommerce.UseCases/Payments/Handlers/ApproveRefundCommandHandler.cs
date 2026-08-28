using ECommerce.Domain.Payments;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Responses;

namespace ECommerce.UseCases.Payments.Handlers;

/// <summary>Approves a requested refund per policy.</summary>
public sealed class ApproveRefundCommandHandler(
    IRefundRepository refunds,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<ApproveRefundCommand> validator) : IRequestHandler<ApproveRefundCommand, Result<RefundResponse>>
{
    public async Task<Result<RefundResponse>> Handle(ApproveRefundCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<RefundResponse>();
        }

        var refund = await refunds.GetByIdAsync(request.RefundId, cancellationToken);
        if (refund is null)
        {
            return RefundErrors.RefundNotFound;
        }

        var approve = refund.Approve(request.ApprovedBy, timeProvider.GetUtcNow().UtcDateTime);
        if (approve.IsFailure)
        {
            return approve.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RefundResponse.From(refund, 0m);
    }
}
