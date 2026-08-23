using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.UseCases.Fulfillment.Shipping;

public sealed record RateQuoteResponse(
    string CarrierKey,
    decimal Amount,
    string Currency,
    DateTime EstimatedDeliveryUtc,
    bool IsFallback,
    bool FromCache,
    IReadOnlyList<string> UnavailableCarriers);
