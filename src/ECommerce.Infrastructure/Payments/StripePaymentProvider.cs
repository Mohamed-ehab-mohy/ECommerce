using ECommerce.Domain.Payments;
using Stripe;

namespace ECommerce.Infrastructure.Payments;

public sealed class StripePaymentProvider(string secretKey) : IPaymentProvider
{
    private readonly IStripeClient _client = new StripeClient(apiKey: secretKey);

    public string Key => "stripe";

    public async Task<PaymentIntentResult> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = new PaymentIntentService(_client);
            var options = new PaymentIntentCreateOptions
            {
                Amount = ToMinorUnits(request.Amount, request.Currency),
                Currency = request.Currency.ToLowerInvariant(),
                PaymentMethodTypes = [request.MethodType],
                Metadata = new Dictionary<string, string>
                {
                    ["idempotencyKey"] = request.IdempotencyKey
                }
            };

            var intent = await service.CreateAsync(options, requestOptions: null, cancellationToken);

            return new PaymentIntentResult(
                true,
                intent.ClientSecret,
                intent.Id,
                intent.Id,
                null);
        }
        catch (StripeException)
        {
            return new PaymentIntentResult(false, string.Empty, string.Empty, null, "provider_unavailable");
        }
    }

    public async Task<PaymentAuthorizationResult> AuthorizeAsync(
        PaymentAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = new PaymentIntentService(_client);
            var intent = await service.GetAsync(
                id: request.ProviderToken,
                options: null,
                requestOptions: null,
                cancellationToken: cancellationToken);

            return intent.Status is "succeeded" or "requires_capture"
                ? new PaymentAuthorizationResult(true, intent.Id, null)
                : new PaymentAuthorizationResult(false, intent.Id, "not_authorized");
        }
        catch (StripeException)
        {
            return new PaymentAuthorizationResult(false, string.Empty, "provider_unavailable");
        }
    }

    private static long ToMinorUnits(decimal amount, string currency)
    {
        var factor = ZeroDecimalCurrencies.Contains(currency.ToUpperInvariant()) ? 1 : 100;
        return decimal.ToInt64(decimal.Round(amount * factor, 0, MidpointRounding.AwayFromZero));
    }

    private static readonly HashSet<string> ZeroDecimalCurrencies =
    [
        "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG",
        "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    ];
}
