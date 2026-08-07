using ECommerce.Domain.Payments;
using ECommerce.UseCases.Payments.Ports;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Payments;

public sealed class PaymentProviderFactory(IOptions<PaymentProviderOptions> options) : IPaymentProviderFactory
{
    private readonly IPaymentProvider _mock = new MockPaymentProvider();

    public Task<IPaymentProvider> RouteAsync(
        string currency,
        string country,
        CancellationToken cancellationToken) =>
        GetAsync(options.Value.DefaultProvider, cancellationToken);

    public Task<IPaymentProvider> GetAsync(string providerKey, CancellationToken cancellationToken)
    {
        var key = providerKey.ToLowerInvariant();

        return Task.FromResult<IPaymentProvider>(
            key == "stripe"
            && options.Value.Stripe.Enabled
            && !string.IsNullOrWhiteSpace(options.Value.Stripe.SecretKey)
                ? new StripePaymentProvider(options.Value.Stripe.SecretKey)
                : _mock);
    }
}
