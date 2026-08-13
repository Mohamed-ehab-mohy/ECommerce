using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.Infrastructure.Shipping;

public sealed class AramexCarrierAdapter(TimeProvider timeProvider) : ICarrierAdapter
{
    public const string Key = "aramex";

    private const string OriginCountry = "AE";
    private const decimal BaseRate = 15m;
    private const decimal Per100Grams = 1.2m;
    private static readonly TimeSpan LeadTime = TimeSpan.FromDays(4);

    public string CarrierKey => Key;

    public Task<CarrierQuoteResult> QuoteAsync(
        CarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var units = (int)Math.Ceiling(request.WeightGrams / 100.0);
        var amount = BaseRate + units * Per100Grams;

        return Task.FromResult(new CarrierQuoteResult(
            Key,
            amount,
            request.Currency,
            timeProvider.GetUtcNow().UtcDateTime.Add(LeadTime)));
    }

    public Task<CarrierShipmentResult> CreateShipmentAsync(
        CarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var trackingNumber = $"ARX{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";

        return Task.FromResult(new CarrierShipmentResult(
            Key,
            trackingNumber,
            $"https://sandbox.aramex.example.com/labels/{trackingNumber}.pdf"));
    }
}
