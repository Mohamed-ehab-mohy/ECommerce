using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Payments.Handlers;

public sealed class AuthorizePaymentCommandHandler(
    IPaymentRepository payments,
    ICheckoutRepository checkouts,
    IPaymentProviderFactory providerFactory,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<AuthorizePaymentCommand> validator) : IRequestHandler<AuthorizePaymentCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(AuthorizePaymentCommand request, CancellationToken cancellationToken)
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

        if (payment.Status == PaymentStatus.Authorized)
        {
            await MarkCheckoutAuthorizedIfCreatedAsync(payment, utcNow, cancellationToken);
            return PaymentResponse.From(payment);
        }

        if (payment.Status is not (PaymentStatus.Created or PaymentStatus.Failed))
        {
            return PaymentErrors.CaptureConflict;
        }

        IPaymentProvider provider;
        try
        {
            provider = await providerFactory.GetAsync(payment.ProviderKey, cancellationToken);
        }
        catch (Exception)
        {
            return PaymentErrors.ProviderUnavailable;
        }

        PaymentAuthorizationResult authorization;
        try
        {
            authorization = await provider.AuthorizeAsync(
                new PaymentAuthorizationRequest(
                    payment.Amount,
                    payment.Currency,
                    payment.ProviderToken,
                    payment.Id.ToString("N")),
                cancellationToken);
        }
        catch (Exception)
        {
            return PaymentErrors.ProviderUnavailable;
        }

        if (!authorization.IsSuccess)
        {
            payment.MarkFailed(utcNow);
            payment.RecordAttempt("authorize", payment.Amount, "declined", authorization.DeclineCode, null, utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return authorization.DeclineCode == "provider_unavailable"
                ? PaymentErrors.ProviderUnavailable
                : PaymentErrors.PaymentDeclined;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var markAuthorizedResult = payment.MarkAuthorized(authorization.ProviderReference, utcNow);
        if (markAuthorizedResult.IsFailure)
        {
            return markAuthorizedResult.Error;
        }

        payment.RecordAttempt("authorize", payment.Amount, "authorized", authorization.ProviderReference, null, utcNow);

        var checkout = await checkouts.GetByPaymentIdAsync(payment.Id, cancellationToken);
        if (checkout is not null && checkout.Status == CheckoutStatus.Created)
        {
            var markCheckoutResult = checkout.MarkPaymentAuthorized(utcNow);
            if (markCheckoutResult.IsFailure)
            {
                return markCheckoutResult.Error;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return PaymentResponse.From(payment);
    }

    private async Task MarkCheckoutAuthorizedIfCreatedAsync(
        Payment payment,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var checkout = await checkouts.GetByPaymentIdAsync(payment.Id, cancellationToken);
        if (checkout is not null && checkout.Status == CheckoutStatus.Created)
        {
            var result = checkout.MarkPaymentAuthorized(utcNow);
            if (result.IsSuccess)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
