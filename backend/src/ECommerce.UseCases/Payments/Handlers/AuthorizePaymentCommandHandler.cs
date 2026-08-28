using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Options;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Responses;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ECommerce.UseCases.Payments.Handlers;

public sealed class AuthorizePaymentCommandHandler(
    IPaymentRepository payments,
    ICheckoutRepository checkouts,
    IPaymentProviderFactory providerFactory,
    IPaymentProviderHealth health,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<PaymentRetryOptions> retryOptions,
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

        if (payment.Status is not (PaymentStatus.Created or PaymentStatus.Failed or PaymentStatus.RetryPending))
        {
            return PaymentErrors.CaptureConflict;
        }

        if (payment.Status is PaymentStatus.Failed or PaymentStatus.RetryPending)
        {
            var canRetry = payment.CanRetry(utcNow);
            if (canRetry.IsFailure)
            {
                return canRetry.Error;
            }
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
            if (provider is not null)
            {
                health.RecordFailure(provider.Key);
            }

            return PaymentErrors.ProviderUnavailable;
        }

        if (!authorization.IsSuccess)
        {
            payment.MarkFailed(utcNow, authorization.DeclineCode);
            payment.RecordAttempt("authorize", payment.Amount, "declined", JsonSerializer.Serialize(authorization), null, utcNow);

            if (authorization.DeclineCode == "provider_unavailable")
            {
                health.RecordFailure(provider.Key);
            }
            else
            {
                // Bounded retry: schedule a retry window unless the attempt budget is exhausted
                // a provider_unavailable signal is a transport failure, not a decline.
                var retry = payment.PlanRetry(
                    retryOptions.Value.Cooldown,
                    retryOptions.Value.MaxAttempts,
                    utcNow);
                if (retry.IsFailure && retry.Error == PaymentErrors.RetryExhausted)
                {
                    payment.RecordAttempt("retry", payment.Amount, "exhausted", null, null, utcNow);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return authorization.DeclineCode == "provider_unavailable"
                ? PaymentErrors.ProviderUnavailable
                : PaymentErrors.PaymentDeclined;
        }

        health.RecordSuccess(provider.Key);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var markAuthorizedResult = payment.MarkAuthorized(authorization.ProviderReference, utcNow);
        if (markAuthorizedResult.IsFailure)
        {
            return markAuthorizedResult.Error;
        }

        payment.RecordAttempt("authorize", payment.Amount, "authorized", JsonSerializer.Serialize(authorization), null, utcNow);

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
