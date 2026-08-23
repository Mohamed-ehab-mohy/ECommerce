using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Wallets.Ports;

namespace ECommerce.UseCases.Wallets.EventHandlers;

public sealed class OrderCompletedLoyaltyHandler(
    IOrderRepository orders,
    IWalletRepository wallets,
    IUnitOfWork unitOfWork) : IEventHandler<OrderPlaced>
{
    private const int DollarsPerPoint = 1;

    public async Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order is null || order.CustomerId is null)
        {
            return;
        }

        var wallet = await wallets.GetByCustomerIdAsync(order.CustomerId.Value, cancellationToken);
        if (wallet is null)
        {
            wallet = ECommerce.Domain.Wallets.Wallet.Create(order.CustomerId.Value, order.Currency);
            await wallets.AddAsync(wallet, cancellationToken);
        }

        // Calculate points based on the order total. E.g. $10.50 => 10 points
        var pointsToEarn = (int)Math.Floor(order.GrandTotal / DollarsPerPoint);
        if (pointsToEarn > 0)
        {
            wallet.AddPoints(pointsToEarn, $"Order_{order.Id}");
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
