using ECommerce.Domain.Integrations;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Integrations.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Integrations;

public sealed class WebhookDeadLetterRepository(
    ECommerceDbContext dbContext,
    IWebhookDeliveryRepository deliveryRepository) : IWebhookDeadLetterRepository
{
    public async Task<WebhookDeadLetterEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Set<WebhookDeadLetterEntry>()
            .Where(entry => entry.Id == id && !entry.IsDeleted)
            .Select(entry => MapToDto(entry))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WebhookDeadLetterEntryDto>> ListAsync(
        int limit,
        int offset,
        string? eventType,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<WebhookDeadLetterEntry>()
            .Where(entry => !entry.IsDeleted);

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(entry => entry.EventType == eventType);
        }

        return await query
            .OrderByDescending(entry => entry.LastFailedAtUtc)
            .Skip(offset)
            .Take(limit)
            .Select(entry => MapToDto(entry))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(string? eventType, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<WebhookDeadLetterEntry>()
            .Where(entry => !entry.IsDeleted);

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(entry => entry.EventType == eventType);
        }

        return await query.CountAsync(cancellationToken);
    }

    public void Add(WebhookDeadLetterEntryDto entry)
    {
        var entity = WebhookDeadLetterEntry.Create(
            entry.DeliveryId,
            entry.EndpointId,
            entry.EventType,
            entry.EventId,
            entry.PayloadJson,
            entry.EndpointUrl,
            entry.EndpointName,
            entry.TotalAttempts,
            entry.LastStatusCode,
            entry.ErrorReason,
            entry.FirstFailedAtUtc);
        dbContext.Set<WebhookDeadLetterEntry>().Add(entity);
    }

    public async Task<bool> ExistsForDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        await dbContext.Set<WebhookDeadLetterEntry>()
            .AnyAsync(entry => entry.DeliveryId == deliveryId && !entry.IsDeleted, cancellationToken);

    public async Task MarkReplayedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Set<WebhookDeadLetterEntry>()
            .SingleOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        if (entry is not null)
        {
            entry.MarkReplayed(utcNow);
        }
    }

    public async Task<bool> MarkDeliveryReplayedAsync(Guid entryId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Set<WebhookDeadLetterEntry>()
            .SingleOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        var delivery = await deliveryRepository.GetByIdAsync(entry.DeliveryId, cancellationToken);
        if (delivery is null)
        {
            return false;
        }

        delivery.ResetForReplay(utcNow);
        entry.MarkReplayed(utcNow);
        return true;
    }

    private static WebhookDeadLetterEntryDto MapToDto(WebhookDeadLetterEntry entry) =>
        new(
            entry.Id,
            entry.DeliveryId,
            entry.EndpointId,
            entry.EventType,
            entry.EventId,
            entry.PayloadJson,
            entry.EndpointUrl,
            entry.EndpointName,
            entry.TotalAttempts,
            entry.LastStatusCode,
            entry.ErrorReason,
            entry.FirstFailedAtUtc,
            entry.LastFailedAtUtc,
            entry.IsReplayed,
            entry.ReplayedAtUtc);
}
