using System.Text.Json;
using ECommerce.Domain.Abstractions;
using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.Infrastructure.Outbox;

public sealed class DomainEventsInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            PersistDomainEvents(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void PersistDomainEvents(DbContext context)
    {
        var entries = context.ChangeTracker
            .Entries<BaseEntity<Guid>>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            var domainEvents = entry.Entity.DomainEvents.ToList();
            if (domainEvents.Count == 0)
            {
                continue;
            }

            entry.Entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                context.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    AggregateId = entry.Entity.Id,
                    EventType = domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    OccurredOn = domainEvent.OccurredOn
                });
            }
        }
    }
}
