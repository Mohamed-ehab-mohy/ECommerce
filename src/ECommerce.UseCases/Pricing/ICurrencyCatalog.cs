namespace ECommerce.UseCases.Pricing;

public interface ICurrencyCatalog
{
    IReadOnlyList<string> SupportedCurrencies { get; }

    bool IsSupported(string? currency);

    decimal GetRate(string sourceCurrency, string targetCurrency);
}
