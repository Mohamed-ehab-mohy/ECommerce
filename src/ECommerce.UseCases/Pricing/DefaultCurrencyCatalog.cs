namespace ECommerce.UseCases.Pricing;

public sealed class DefaultCurrencyCatalog : ICurrencyCatalog
{
    private static readonly IReadOnlyDictionary<string, decimal> UnitsPerBase =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 1m,
            ["EUR"] = 0.92m,
            ["GBP"] = 0.79m,
            ["AED"] = 3.6725m,
            ["EGP"] = 48.5m
        };

    public IReadOnlyList<string> SupportedCurrencies { get; } = ["USD", "EUR", "GBP", "AED", "EGP"];

    public bool IsSupported(string? currency) =>
        !string.IsNullOrWhiteSpace(currency) && UnitsPerBase.ContainsKey(currency.Trim());

    public decimal GetRate(string sourceCurrency, string targetCurrency)
    {
        var source = Normalize(sourceCurrency);
        var target = Normalize(targetCurrency);

        return RateFor(target, targetCurrency, nameof(targetCurrency)) /
               RateFor(source, sourceCurrency, nameof(sourceCurrency));
    }

    private static decimal RateFor(string normalized, string original, string parameterName) =>
        UnitsPerBase.TryGetValue(normalized, out var rate)
            ? rate
            : throw new ArgumentException($"No FX rate is configured for currency '{original}'.", parameterName);

    private static string Normalize(string currency) => currency.Trim().ToUpperInvariant();
}
