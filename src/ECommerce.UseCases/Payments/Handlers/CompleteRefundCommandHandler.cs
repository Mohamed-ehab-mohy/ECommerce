using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Payments.Handlers;

public sealed class CompleteRefundCommandHandler(
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CompleteRefundCommand> validator) : IRequestHandler<CompleteRefundCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(CompleteRefundCommand request, CancellationToken cancellationToken)
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

        var result = payment.MarkRefunded(utcNow, request.ProviderReference);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PaymentResponse.From(payment);
    }
}
