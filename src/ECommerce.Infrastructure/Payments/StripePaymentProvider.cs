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

    public async Task<PaymentRefundResult> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = new RefundService(_client);
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.ProviderReference,
                Amount = ToMinorUnits(request.Amount, request.Currency)
            };

            var refund = await service.CreateAsync(
                options,
                new RequestOptions { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);

            return new PaymentRefundResult(true, refund.Id, null);
        }
        catch (StripeException)
        {
            return new PaymentRefundResult(false, null, "provider_unavailable");
        }
    }

    public async Task<IReadOnlyList<ProviderTransaction>> ListTransactionsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = new PaymentIntentService(_client);
            var transactions = new List<ProviderTransaction>();

            var listOptions = new PaymentIntentListOptions
            {
                Created = new DateRangeOptions { GreaterThanOrEqual = fromUtc, LessThanOrEqual = toUtc },
                Limit = 100
            };

            var page = await service.ListAsync(listOptions, requestOptions: null, cancellationToken);
            while (page is not null)
            {
                foreach (var intent in page.Data)
                {
                    transactions.Add(new ProviderTransaction(
                        intent.Id,
                        intent.Status is "succeeded" or "requires_capture" ? "captured" : "authorized",
                        FromMinorUnits(intent.Amount, intent.Currency),
                        intent.Currency,
                        intent.Status,
                        intent.Created));
                }

                if (!page.HasMore)
                {
                    break;
                }

                listOptions.StartingAfter = page.Data.LastOrDefault()?.Id;
                page = await service.ListAsync(listOptions, requestOptions: null, cancellationToken);
            }

            return transactions;
        }
        catch (StripeException)
        {
            return [];
        }
    }

    private static long ToMinorUnits(decimal amount, string currency)
    {
        var factor = ZeroDecimalCurrencies.Contains(currency.ToUpperInvariant()) ? 1 : 100;
        return decimal.ToInt64(decimal.Round(amount * factor, 0, MidpointRounding.AwayFromZero));
    }

    private static decimal FromMinorUnits(long amount, string currency)
    {
        var factor = ZeroDecimalCurrencies.Contains(currency.ToUpperInvariant()) ? 1 : 100;
        return decimal.Round(amount / (decimal)factor, 4, MidpointRounding.AwayFromZero);
    }

    private static readonly HashSet<string> ZeroDecimalCurrencies =
    [
        "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG",
        "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    ];
}
