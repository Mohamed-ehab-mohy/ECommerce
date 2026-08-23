namespace ECommerce.UseCases.Fulfillment.Shipping;

public sealed record CarrierItem(string Sku, int Quantity);

public sealed record CarrierShipmentRequest(
    string OriginCountry,
    string DestinationCountry,
    string DestinationPostalCode,
    int WeightGrams,
    string Currency,
    IReadOnlyList<CarrierItem> Items);

public sealed record CarrierQuoteResult(
    string CarrierKey,
    decimal Amount,
    string Currency,
    DateTime EstimatedDeliveryUtc);

public sealed record CarrierShipmentResult(
    string CarrierKey,
    string TrackingNumber,
    string LabelUrl);

public interface ICarrierAdapter
{
    string CarrierKey { get; }

    Task<CarrierQuoteResult> QuoteAsync(CarrierShipmentRequest request, CancellationToken cancellationToken);

    Task<CarrierShipmentResult> CreateShipmentAsync(CarrierShipmentRequest request, CancellationToken cancellationToken);
}
