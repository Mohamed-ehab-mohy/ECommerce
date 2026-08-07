using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Payments.Ports;

namespace ECommerce.UseCases.Payments.Services;

public sealed record PaymentInitiationResult(Payment Payment, string ClientToken);

public sealed class PaymentIntentService(
    IPaymentProviderFactory providerFactory,
    TimeProvider timeProvider)
{
    public async Task<Result<PaymentInitiationResult>> CreateIntentAsync(
        Guid? customerId,
        string providerKey,
        string methodType,
        string currency,
        string country,
        decimal amount,
        CancellationToken cancellationToken)
    {
        IPaymentProvider provider;
        try
        {
            provider = await providerFactory.GetAsync(providerKey, cancellationToken);
        }
        catch (Exception)
        {
            return PaymentErrors.ProviderUnavailable;
        }

        PaymentIntentResult intent;
        try
        {
            intent = await provider.CreateIntentAsync(
                new PaymentIntentRequest(
                    amount,
                    currency,
                    Guid.NewGuid().ToString("N"),
                    methodType,
                    customerId),
                cancellationToken);
        }
        catch (Exception)
        {
            return PaymentErrors.ProviderUnavailable;
        }

        if (!intent.IsSuccess)
        {
            return PaymentErrors.PaymentDeclined;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var payment = Payment.Create(
            customerId,
            provider.Key,
            intent.ProviderToken,
            intent.ClientToken,
            intent.ProviderReference,
            currency,
            amount,
            null,
            utcNow);

        return new PaymentInitiationResult(payment, intent.ClientToken);
    }
}
