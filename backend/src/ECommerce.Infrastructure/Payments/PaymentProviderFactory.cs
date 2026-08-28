using ECommerce.Domain.Payments;
using ECommerce.UseCases.Payments.Ports;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Payments;

/// <summary>
/// Routes payment traffic to the primary PSP, failing over to the configured backup when the primary's
/// circuit is open (health-based failover). Throws <see cref="PaymentProvidersUnavailableException"/>
/// when every candidate is unavailable.
/// </summary>
public sealed class PaymentProviderFactory(
    IEnumerable<IPaymentProvider> providers,
    IPaymentProviderHealth health,
    IOptions<PaymentProviderOptions> options) : IPaymentProviderFactory
{
    public Task<IPaymentProvider> RouteAsync(
        string currency,
        string country,
        CancellationToken cancellationToken) =>
        GetAsync(options.Value.DefaultProvider, cancellationToken);

    public Task<IPaymentProvider> GetAsync(string providerKey, CancellationToken cancellationToken)
    {
        var primary = providerKey.Trim().ToLowerInvariant();

        foreach (var key in ResolveCandidates(primary))
        {
            var provider = providers.FirstOrDefault(candidate =>
                candidate.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (provider is null)
            {
                continue;
            }

            if (health.IsAvailable(provider.Key))
            {
                return Task.FromResult(provider);
            }
        }

        throw new PaymentProvidersUnavailableException(primary);
    }

    private IEnumerable<string> ResolveCandidates(string primary)
    {
        var candidates = new List<string> { primary };

        var failover = options.Value.FailoverProvider?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(failover) && failover != primary)
        {
            candidates.Add(failover);
        }

        return candidates;
    }
}

public sealed class PaymentProvidersUnavailableException(string providerKey)
    : Exception($"All payment providers unavailable for primary '{providerKey}'.")
{
    public string ProviderKey { get; } = providerKey;
}
