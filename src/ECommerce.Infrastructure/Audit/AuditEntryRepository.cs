using ECommerce.Domain.Audit;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Audit.Ports;

namespace ECommerce.Infrastructure.Audit;

public sealed class AuditEntryRepository(ECommerceDbContext dbContext) : IAuditEntryRepository
{
    public Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        dbContext.AuditEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<string?> GetLatestHashAsync(CancellationToken cancellationToken) =>
        dbContext.AuditEntries
            .OrderByDescending(entry => entry.Id)
            .Select(entry => entry.Hash)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditEntry>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken) =>
        await ApplyFilters(dbContext.AuditEntries.AsNoTracking(), query)
            .OrderByDescending(entry => entry.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(AuditLogQuery query, CancellationToken cancellationToken) =>
        ApplyFilters(dbContext.AuditEntries.AsNoTracking(), query)
            .CountAsync(cancellationToken);

    private static IQueryable<AuditEntry> ApplyFilters(IQueryable<AuditEntry> source, AuditLogQuery query)
    {
        if (query.ActorId is { } actorId)
        {
            source = source.Where(entry => entry.ActorId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            source = source.Where(entry => entry.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            source = source.Where(entry => entry.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            source = source.Where(entry => entry.EntityId == query.EntityId);
        }

        if (query.From is { } from)
        {
            source = source.Where(entry => entry.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            source = source.Where(entry => entry.OccurredAt <= to);
        }

        return source;
    }
}
