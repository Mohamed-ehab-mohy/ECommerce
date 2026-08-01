using ECommerce.Domain.Abstractions;

namespace ECommerce.UseCases.Common;

public interface IEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
