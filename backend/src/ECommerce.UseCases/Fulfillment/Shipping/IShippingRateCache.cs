using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.UseCases.Fulfillment.Shipping;

public interface IShippingRateCache
{
    bool TryGet(string key, out CarrierQuoteResult quote);

    void Set(string key, CarrierQuoteResult quote);
}
