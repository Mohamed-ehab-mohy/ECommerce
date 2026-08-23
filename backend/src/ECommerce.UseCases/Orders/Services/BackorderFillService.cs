using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Orders.Services;

public sealed class BackorderFillService(
    IOrderRepository orders,
    IStockAllocator stockAllocator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task FillForSkuAsync(string sku, CancellationToken cancellationToken)
    {
        var openItems = await orders.ListOpenBackorderItemsBySkuAsync(sku, cancellationToken);
        if (openItems.Count == 0)
        {
            return;
        }

        var remaining = openItems.Sum(item => item.Quantity - item.FilledQuantity);
        if (remaining <= 0)
        {
            return;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var allocation = await stockAllocator.AllocateAsync(
            [new AllocationRequestItem(sku, remaining)],
            "BACKORDER",
            $"BACKORDER:{sku}",
            utcNow,
            cancellationToken);

        var filledTotal = allocation.Allocated.Sum(line => line.Quantity);
        if (filledTotal <= 0)
        {
            return;
        }

        var remainingToFill = filledTotal;
        foreach (var orderId in openItems.Select(item => item.OrderId).Distinct())
        {
            if (remainingToFill <= 0)
            {
                break;
            }

            var order = await orders.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                continue;
            }

            var orderRemaining = order.BackorderItems
                .Where(item => item.Sku == sku && !item.IsFilled)
                .Sum(item => item.Quantity - item.FilledQuantity);

            var fill = Math.Min(remainingToFill, orderRemaining);
            if (fill <= 0)
            {
                continue;
            }

            order.FillBackorderItems(sku, fill, utcNow);
            remainingToFill -= fill;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
