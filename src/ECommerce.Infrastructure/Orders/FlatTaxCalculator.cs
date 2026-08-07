using ECommerce.UseCases.Checkout.Ports;

namespace ECommerce.Infrastructure.Orders;

public sealed class FlatTaxCalculator : ITaxCalculator
{
    public Task<decimal> ComputeAsync(
        decimal taxableSubtotal,
        string country,
        string currency,
        CancellationToken cancellationToken) =>
        Task.FromResult(0m);
}
