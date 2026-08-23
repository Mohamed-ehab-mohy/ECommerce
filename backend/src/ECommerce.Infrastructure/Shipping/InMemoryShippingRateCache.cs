using System.Collections.Concurrent;
using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.Infrastructure.Shipping;

public sealed class InMemoryShippingRateCache(TimeProvider timeProvider) : IShippingRateCache
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public bool TryGet(string key, out CarrierQuoteResult quote)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAtUtc > utcNow)
        {
            quote = entry.Quote;
            return true;
        }

        _entries.TryRemove(key, out _);
        quote = default!;
        return false;
    }

    public void Set(string key, CarrierQuoteResult quote)
    {
        _entries[key] = new Entry(
            quote,
            timeProvider.GetUtcNow().UtcDateTime.Add(DefaultTtl));
    }

    private sealed record Entry(CarrierQuoteResult Quote, DateTime ExpiresAtUtc);
}
