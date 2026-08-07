namespace ECommerce.UseCases.Checkout.Ports;

public interface ITaxCalculator
{
    Task<decimal> ComputeAsync(
        decimal taxableSubtotal,
        string country,
        string currency,
        CancellationToken cancellationToken);
}
