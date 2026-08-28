using ECommerce.UseCases.Checkout.Ports;

namespace ECommerce.Infrastructure.Orders;

/// <summary>
/// Resolves tax rates from a configurable static ruleset (fallback rules).
/// Configuration key: <c>Tax:Rates:&lt;countryCode&gt;</c> holds the percentage (e.g. 14 = 14%).
/// Any country not present falls back to the built-in default rate.
/// </summary>
public sealed class StaticTaxRateProvider : ITaxRateProvider
{
    /// <summary>Default rate (0..1) applied to countries without an explicit rule.</summary>
    public const decimal DefaultRate = 0.18m;

    private static readonly IReadOnlyDictionary<string, decimal> BuiltInRates =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["EG"] = 0.14m,
            ["SA"] = 0.15m,
            ["AE"] = 0.05m,
            ["US"] = 0.0825m,
            ["UK"] = 0.20m,
            ["DE"] = 0.19m,
            ["FR"] = 0.20m,
            ["IT"] = 0.22m,
            ["ES"] = 0.21m,
            ["NL"] = 0.21m,
            ["IN"] = 0.18m,
        };

    private readonly IReadOnlyDictionary<string, decimal> _configuredRates;
    private readonly decimal _defaultRate;

    public StaticTaxRateProvider(
        decimal defaultRate = DefaultRate,
        IReadOnlyDictionary<string, decimal>? configuredRates = null)
    {
        _defaultRate = defaultRate;
        _configuredRates = configuredRates ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }

    public Task<decimal> GetRateAsync(
        string country,
        string currency,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country, nameof(country));

        var rate = _configuredRates.TryGetValue(country, out var configured)
            ? configured
            : BuiltInRates.TryGetValue(country, out var builtIn)
                ? builtIn
                : _defaultRate;

        return Task.FromResult(Clamp(rate));
    }

    private static decimal Clamp(decimal rate) => Math.Clamp(rate, 0m, 1m);
}
