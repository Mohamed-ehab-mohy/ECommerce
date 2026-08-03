namespace ECommerce.Domain.Pricing;

public readonly record struct Money
{
    private const int StoragePrecision = 4;

    private const int DisplayPrecision = 2;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public decimal DisplayAmount => decimal.Round(Amount, DisplayPrecision, MidpointRounding.AwayFromZero);

    public static Money From(decimal amount, string currency) =>
        string.IsNullOrWhiteSpace(currency)
            ? throw new ArgumentException("A currency code is required.", nameof(currency))
            : new Money(
                decimal.Round(amount, StoragePrecision, MidpointRounding.AwayFromZero),
                currency.Trim().ToUpperInvariant());

    public Money ConvertTo(string targetCurrency, decimal rate) =>
        string.IsNullOrWhiteSpace(targetCurrency)
            ? throw new ArgumentException("A target currency code is required.", nameof(targetCurrency))
            : From(Amount * rate, targetCurrency);

    public override string ToString() => $"{DisplayAmount:F2} {Currency}";
}
