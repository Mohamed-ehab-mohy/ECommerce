using System.Collections.Concurrent;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Pricing;

public sealed class CurrencyExchangeService : ICurrencyExchangeService
{
    private readonly ConcurrentDictionary<string, decimal> _rates = new();
    private readonly ConcurrentDictionary<string, GeoPricingAdjustment> _geoAdjustments = new();
    private DateTime _lastUpdated = DateTime.MinValue;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly ILogger<CurrencyExchangeService> _logger;

    public CurrencyExchangeService(ILogger<CurrencyExchangeService> logger)
    {
        _logger = logger;
        InitializeStaticRates();
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        await EnsureRatesLoadedAsync(cancellationToken);

        var fromKey = fromCurrency.ToUpperInvariant();
        var toKey = toCurrency.ToUpperInvariant();

        if (_rates.TryGetValue($"{fromKey}:{toKey}", out var directRate))
        {
            return Math.Round(amount * directRate, 2);
        }

        if (_rates.TryGetValue($"{toKey}:{fromKey}", out var inverseRate) && inverseRate != 0)
        {
            return Math.Round(amount / inverseRate, 2);
        }

        if (_rates.TryGetValue($"USD:{toKey}", out var toUsd) &&
            _rates.TryGetValue($"USD:{fromKey}", out var fromUsd) &&
            fromUsd != 0)
        {
            var usdAmount = amount / fromUsd;
            return Math.Round(usdAmount * toUsd, 2);
        }

        _logger.LogWarning("No exchange rate found for {From} to {To}. Returning original amount.", fromCurrency, toCurrency);
        return amount;
    }

    public async Task<IReadOnlyList<CurrencyRate>> GetAllRatesAsync(string baseCurrency, CancellationToken cancellationToken = default)
    {
        await EnsureRatesLoadedAsync(cancellationToken);
        var baseKey = baseCurrency.ToUpperInvariant();
        var now = DateTime.UtcNow;

        var rates = new List<CurrencyRate>();
        foreach (var kvp in _rates)
        {
            var parts = kvp.Key.Split(':');
            if (parts.Length == 2 && parts[0] == baseKey)
            {
                rates.Add(new CurrencyRate(parts[0], parts[1], kvp.Value, now));
            }
        }

        return rates;
    }

    public Task<IReadOnlyList<string>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        var currencies = new HashSet<string>();
        foreach (var kvp in _rates)
        {
            var parts = kvp.Key.Split(':');
            if (parts.Length == 2)
            {
                currencies.Add(parts[0]);
                currencies.Add(parts[1]);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(currencies.OrderBy(c => c).ToList());
    }

    public GeoPricingAdjustment GetGeoAdjustment(string countryCode)
    {
        return _geoAdjustments.TryGetValue(countryCode.ToUpperInvariant(), out var adjustment)
            ? adjustment
            : new GeoPricingAdjustment(countryCode, 0, 0, "USD");
    }

    private async Task EnsureRatesLoadedAsync(CancellationToken cancellationToken)
    {
        if (_lastUpdated.AddHours(1) > DateTime.UtcNow && _rates.Count > 0)
        {
            return;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_lastUpdated.AddHours(1) > DateTime.UtcNow && _rates.Count > 0)
            {
                return;
            }

            InitializeStaticRates();
            _lastUpdated = DateTime.UtcNow;
            _logger.LogInformation("Currency exchange rates refreshed. {Count} rates loaded.", _rates.Count);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void InitializeStaticRates()
    {
        _rates.Clear();

        var baseRates = new Dictionary<string, decimal>
        {
            ["EUR"] = 0.92m,
            ["GBP"] = 0.79m,
            ["JPY"] = 149.50m,
            ["CAD"] = 1.36m,
            ["AUD"] = 1.53m,
            ["CHF"] = 0.88m,
            ["CNY"] = 7.24m,
            ["INR"] = 83.12m,
            ["BRL"] = 4.97m,
            ["KRW"] = 1328.50m,
            ["MXN"] = 17.15m,
            ["SAR"] = 3.75m,
            ["AED"] = 3.67m,
            ["EGP"] = 48.50m,
            ["TRY"] = 30.25m,
            ["SEK"] = 10.42m,
            ["NOK"] = 10.55m,
            ["DKK"] = 6.87m,
            ["PLN"] = 4.03m,
            ["ZAR"] = 18.65m,
            ["THB"] = 35.20m,
            ["SGD"] = 1.34m,
            ["HKD"] = 7.82m,
            ["NZD"] = 1.65m,
        };

        foreach (var (toCurrency, rate) in baseRates)
        {
            _rates[$"USD:{toCurrency}"] = rate;
            if (rate != 0)
            {
                _rates[$"{toCurrency}:USD"] = Math.Round(1 / rate, 6);
            }
        }

        foreach (var (fromCurrency, fromRate) in baseRates)
        {
            foreach (var (toCurrency, toRate) in baseRates)
            {
                if (fromCurrency != toCurrency && fromRate != 0)
                {
                    _rates[$"{fromCurrency}:{toCurrency}"] = Math.Round(toRate / fromRate, 6);
                }
            }
        }

        _geoAdjustments["US"] = new GeoPricingAdjustment("US", 0, 8.25m, "USD");
        _geoAdjustments["GB"] = new GeoPricingAdjustment("GB", 5, 20, "GBP");
        _geoAdjustments["DE"] = new GeoPricingAdjustment("DE", 5, 19, "EUR");
        _geoAdjustments["JP"] = new GeoPricingAdjustment("JP", 10, 10, "JPY");
        _geoAdjustments["AE"] = new GeoPricingAdjustment("AE", 15, 5, "AED");
        _geoAdjustments["SA"] = new GeoPricingAdjustment("SA", 15, 15, "SAR");
        _geoAdjustments["EG"] = new GeoPricingAdjustment("EG", 20, 14, "EGP");
        _geoAdjustments["IN"] = new GeoPricingAdjustment("IN", 10, 18, "INR");
        _geoAdjustments["BR"] = new GeoPricingAdjustment("BR", 15, 17, "BRL");
        _geoAdjustments["AU"] = new GeoPricingAdjustment("AU", 10, 10, "AUD");
    }
}
