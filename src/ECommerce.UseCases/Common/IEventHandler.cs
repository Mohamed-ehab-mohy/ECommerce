using ECommerce.Domain.Abstractions;

namespace ECommerce.UseCases.Common;

public interface IEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
