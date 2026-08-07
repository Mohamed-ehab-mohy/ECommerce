using ECommerce.Domain.Cart;
using ECommerce.UseCases.Inventory.Ports;

namespace ECommerce.UseCases.Checkout.Services;

public sealed record AvailabilityIssue(string Sku, int Requested, int Available);

public sealed class StockAvailabilityVerifier(IStockRepository stock)
{
    public async Task<IReadOnlyList<AvailabilityIssue>> VerifyAsync(
        IReadOnlyCollection<CartItem> items,
        CancellationToken cancellationToken)
    {
        var issues = new List<AvailabilityIssue>();

        foreach (var group in items.GroupBy(item => item.Sku))
        {
            var requested = group.Sum(item => item.Quantity);
            var stockItems = await stock.ListBySkuAsync(group.Key, cancellationToken);
            var available = stockItems.Sum(stockItem => stockItem.Available);

            if (available < requested)
            {
                issues.Add(new AvailabilityIssue(group.Key, requested, available));
            }
        }

        return issues;
    }
}
