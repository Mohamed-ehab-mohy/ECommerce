using ECommerce.Domain.Orders;
using ECommerce.UseCases.Checkout.Ports;

namespace ECommerce.Infrastructure.Orders;

public sealed class ShippingRateStubProvider : IShippingRateProvider
{
    public Task<IReadOnlyList<ShippingMethod>> ListAsync(
        string country,
        string currency,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ShippingMethod> methods =
        [
            new ShippingMethod("economy", "Economy", 4.90m, currency, "5-8 business days"),
            new ShippingMethod("standard", "Standard", 9.90m, currency, "3-5 business days"),
            new ShippingMethod("express", "Express", 24.90m, currency, "1-2 business days")
        ];

        return Task.FromResult(methods);
    }

    public Task<ShippingMethod?> GetRateAsync(
        string methodId,
        string country,
        string currency,
        CancellationToken cancellationToken)
    {
        ShippingMethod? match = methodId switch
        {
            "economy" => new ShippingMethod("economy", "Economy", 4.90m, currency, "5-8 business days"),
            "standard" => new ShippingMethod("standard", "Standard", 9.90m, currency, "3-5 business days"),
            "express" => new ShippingMethod("express", "Express", 24.90m, currency, "1-2 business days"),
            _ => null
        };

        return Task.FromResult(match);
    }
}
