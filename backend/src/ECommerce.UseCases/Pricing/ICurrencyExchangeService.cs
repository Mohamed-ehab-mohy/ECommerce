namespace ECommerce.UseCases.Pricing;

public interface ICurrencyExchangeService
{
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CurrencyRate>> GetAllRatesAsync(string baseCurrency, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default);
}

public sealed record CurrencyRate(string FromCurrency, string ToCurrency, decimal Rate, DateTime UpdatedAt);

public sealed record GeoPricingAdjustment(
    string CountryCode,
    decimal MarkupPercent,
    decimal TaxRatePercent,
    string Currency);
