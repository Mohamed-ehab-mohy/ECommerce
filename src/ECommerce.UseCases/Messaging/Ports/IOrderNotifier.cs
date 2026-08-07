using ECommerce.Domain.Events;

namespace ECommerce.UseCases.Messaging.Ports;

public interface IOrderNotifier
{
    Task NotifyPlacedAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken);
}
