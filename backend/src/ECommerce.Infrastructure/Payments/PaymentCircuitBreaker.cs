using ECommerce.UseCases.Payments.Ports;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ECommerce.Infrastructure.Payments;

/// <summary>
/// Per-provider circuit breaker (US-G-003). Closed → trips open after <see cref="CircuitBreakerOptions.FailureThreshold"/>
/// consecutive failures, stays open for <see cref="CircuitBreakerOptions.Cooldown"/>, then half-open trial; a success
/// closes the circuit, a failure reopens it.
/// </summary>
public sealed class PaymentCircuitBreaker(
    IOptions<PaymentProviderOptions> options,
    TimeProvider timeProvider) : IPaymentProviderHealth
{
    private sealed record CircuitState(int Failures, DateTime? OpenedAt);

    private readonly ConcurrentDictionary<string, CircuitState> _states = new();

    public bool IsAvailable(string providerKey)
    {
        var state = _states.GetOrAdd(providerKey, _ => new CircuitState(0, null));
        return state.OpenedAt is null
            || timeProvider.GetUtcNow().UtcDateTime - state.OpenedAt.Value >= options.Value.CircuitBreaker.Cooldown;
    }

    public void RecordSuccess(string providerKey)
    {
        _states[providerKey] = new CircuitState(0, null);
    }

    public void RecordFailure(string providerKey)
    {
        _states.AddOrUpdate(
            providerKey,
            _ => new CircuitState(1, null),
            (_, state) =>
            {
                var failures = state.Failures + 1;
                var openedAt = state.OpenedAt;
                if (openedAt is null)
                {
                    openedAt = failures >= options.Value.CircuitBreaker.FailureThreshold
                        ? timeProvider.GetUtcNow().UtcDateTime
                        : null;
                }
                else
                {
                    // Already open (cooldown or a half-open trial failed) → restart the cooldown.
                    openedAt = timeProvider.GetUtcNow().UtcDateTime;
                }

                return new CircuitState(failures, openedAt);
            });
    }
}
