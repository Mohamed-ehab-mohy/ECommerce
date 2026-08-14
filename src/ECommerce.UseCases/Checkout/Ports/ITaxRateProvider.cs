namespace ECommerce.UseCases.Checkout.Ports;

/// <summary>
/// Resolves the effective tax rate (0..1) for a destination country/currency. US-I-003:
/// integration provider with local fallback rules (FR-09-003).
/// </summary>
public interface ITaxRateProvider
{
    Task<decimal> GetRateAsync(
        string country,
        string currency,
        CancellationToken cancellationToken);
}
