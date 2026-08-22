using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Realtime;

public sealed class RealtimeEventStore(ECommerceDbContext dbContext) : IRealtimeEventStore
{
    public async Task<long> AppendAsync(RealtimeEvent realtimeEvent, CancellationToken cancellationToken)
    {
        dbContext.RealtimeEvents.Add(realtimeEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return realtimeEvent.Id;
    }

    public async Task<IReadOnlyList<RealtimeEvent>> GetAfterAsync(
        string groupKey,
        long lastEventId,
        int take,
        CancellationToken cancellationToken) =>
        await dbContext.RealtimeEvents
            .Where(realtimeEvent => realtimeEvent.GroupKey == groupKey && realtimeEvent.Id > lastEventId)
            .OrderBy(realtimeEvent => realtimeEvent.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
}
