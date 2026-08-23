using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Checkout.Ports;

namespace ECommerce.Infrastructure.Orders;

/// <summary>
/// Computes tax for a taxable base using an <see cref="ITaxRateProvider"/> and returns both the
/// effective rate and the rounded amount (US-I-003, FR-09-003).
/// </summary>
public sealed class TaxCalculator : ITaxCalculator
{
    private readonly ITaxRateProvider _rateProvider;

    public TaxCalculator(ITaxRateProvider rateProvider)
    {
        _rateProvider = rateProvider;
    }

    public async Task<TaxCalculation> ComputeAsync(
        decimal taxableSubtotal,
        string country,
        string currency,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country, nameof(country));

        var rate = await _rateProvider.GetRateAsync(country, currency, cancellationToken);
        var taxable = Math.Max(taxableSubtotal, 0m);
        var amount = Math.Round(taxable * rate, 2, MidpointRounding.AwayFromZero);

        return new TaxCalculation(rate, amount);
    }
}
