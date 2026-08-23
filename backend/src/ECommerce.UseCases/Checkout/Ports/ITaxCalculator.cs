using ECommerce.Domain.Pricing;

namespace ECommerce.UseCases.Checkout.Ports;

public interface ITaxCalculator
{
    Task<TaxCalculation> ComputeAsync(
        decimal taxableSubtotal,
        string country,
        string currency,
        CancellationToken cancellationToken);
}
