using ECommerce.Domain.Orders;

namespace ECommerce.UseCases.Checkout.Ports;

public interface IShippingRateProvider
{
    Task<IReadOnlyList<ShippingMethod>> ListAsync(
        string country,
        string currency,
        CancellationToken cancellationToken);

    Task<ShippingMethod?> GetRateAsync(
        string methodId,
        string country,
        string currency,
        CancellationToken cancellationToken);
}
