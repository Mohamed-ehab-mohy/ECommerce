using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Reviews.Ports;

namespace ECommerce.Infrastructure.Reviews;

public sealed class VerifiedPurchaseChecker(ECommerceDbContext dbContext) : IVerifiedPurchaseChecker
{
    public Task<bool> HasPurchasedAsync(Guid customerId, Guid productId, CancellationToken cancellationToken) =>
        dbContext.Set<Order>()
            .Where(order => order.CustomerId == customerId && order.Status != OrderStatus.Cancelled)
            .SelectMany(order => order.Items)
            .AnyAsync(item => item.ProductId == productId, cancellationToken);
}
