using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Payments.Handlers;

public sealed class RequestRefundCommandHandler(
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<RequestRefundCommand> validator) : IRequestHandler<RequestRefundCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(RequestRefundCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PaymentResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return PaymentErrors.PaymentNotFound;
        }

        var result = payment.RequestRefund(request.Reason, utcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PaymentResponse.From(payment);
    }
}
