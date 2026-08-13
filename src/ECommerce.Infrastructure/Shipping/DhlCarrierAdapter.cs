using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.Infrastructure.Shipping;

public sealed class DhlCarrierAdapter(TimeProvider timeProvider) : ICarrierAdapter
{
    public const string Key = "dhl";

    private const string OriginCountry = "AE";
    private const decimal BaseRate = 20m;
    private const decimal Per100Grams = 1.0m;
    private static readonly TimeSpan LeadTime = TimeSpan.FromDays(2);

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
        var trackingNumber = $"DHL{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";

        return Task.FromResult(new CarrierShipmentResult(
            Key,
            trackingNumber,
            $"https://sandbox.dhl.example.com/labels/{trackingNumber}.pdf"));
    }
}
