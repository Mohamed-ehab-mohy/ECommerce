using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Grpc.Ports;

namespace ECommerce.Infrastructure.Grpc;

public sealed class GrpcQueryService(ECommerceDbContext dbContext) : IGrpcQueryService
{
    public async Task<OrderStatusDto?> GetOrderStatusAsync(string orderNumber, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(o => o.StatusLogs)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);

        return new OrderStatusDto(
            order.OrderNumber,
            order.Status.ToString(),
            payment?.Status.ToString() ?? "Unknown",
            order.CustomerEmail,
            order.PlacedAt,
            order.StatusLogs
                .OrderBy(l => l.OccurredAt)
                .Select(l => new OrderTimelineEntryDto(l.ToStatus.ToString(), l.ActorType, l.OccurredAt))
                .ToList());
    }

    public async Task<ProductSummaryDto?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(p => p.Translations)
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var defaultTranslation = product.Translations.FirstOrDefault();
        var defaultPrice = product.Prices.FirstOrDefault();

        return new ProductSummaryDto(
            product.Id,
            product.Sku,
            defaultTranslation?.Name ?? string.Empty,
            product.Slug,
            defaultPrice?.ListAmount ?? 0m,
            product.Status.ToString() == "Active");
    }
}
