using ECommerce.Domain.Events;
using ECommerce.UseCases.Messaging.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Messaging;

public sealed class LogOrderNotifier(ILogger<LogOrderNotifier> logger) : IOrderNotifier
{
    public Task NotifyPlacedAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order {OrderId} placed for customer {CustomerEmail}: total {Total} {Currency} (checkout {CheckoutId}).",
            orderPlaced.OrderId,
            orderPlaced.CustomerEmail,
            orderPlaced.Total,
            orderPlaced.Currency,
            orderPlaced.CheckoutId);

        return Task.CompletedTask;
    }
}
