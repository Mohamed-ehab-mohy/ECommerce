using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.UseCases.Fulfillment.Shipping;

public sealed class CarrierRateSelector(
    IEnumerable<ICarrierAdapter> carriers,
    IShippingRateCache cache)
{
    public async Task<RateQuoteSelection> SelectAsync(
        CarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var quotes = new List<CarrierQuoteResult>();
        var unavailable = new List<string>();
        var fromCache = false;

        foreach (var carrier in carriers.OrderBy(carrier => carrier.CarrierKey, StringComparer.Ordinal))
        {
            var key = BuildKey(carrier.CarrierKey, request);

            if (cache.TryGet(key, out var cached))
            {
                quotes.Add(cached);
                fromCache = true;
                continue;
            }

            try
            {
                var quote = await carrier.QuoteAsync(request, cancellationToken);
                cache.Set(key, quote);
                quotes.Add(quote);
            }
            catch
            {
                unavailable.Add(carrier.CarrierKey);
            }
        }

        var cheapest = quotes.Count > 0 ? quotes.OrderBy(quote => quote.Amount).First() : null;

        return new RateQuoteSelection(
            cheapest,
            unavailable.Count > 0,
            fromCache,
            unavailable);
    }

    private static string BuildKey(string carrierKey, CarrierShipmentRequest request) =>
        string.Join(':', carrierKey, request.DestinationCountry, request.DestinationPostalCode, request.WeightGrams);
}

public sealed record RateQuoteSelection(
    CarrierQuoteResult? Cheapest,
    bool IsFallback,
    bool FromCache,
    IReadOnlyList<string> UnavailableCarriers);
