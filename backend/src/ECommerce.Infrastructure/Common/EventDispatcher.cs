using ECommerce.Domain.Abstractions;
using ECommerce.UseCases.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Common;

public sealed class EventDispatcher(IServiceProvider serviceProvider) : IEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(domainEvent.GetType());

        foreach (var handler in serviceProvider.GetServices(handlerType))
        {
            if (handler is null)
            {
                continue;
            }

            dynamic dynamicEvent = domainEvent;
            await ((dynamic)handler).HandleAsync(dynamicEvent, cancellationToken);
        }
    }
}
