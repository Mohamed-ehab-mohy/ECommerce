namespace ECommerce.Domain.Pricing;

/// <summary>
/// Result of a tax calculation: the effective tax <see cref="Rate"/> (0..1) and the computed
/// <see cref="Amount"/> on the taxable base. Stored at order level (FR-09-003).
/// </summary>
public sealed record TaxCalculation(decimal Rate, decimal Amount)
{
    public static readonly TaxCalculation Zero = new(0m, 0m);
}
