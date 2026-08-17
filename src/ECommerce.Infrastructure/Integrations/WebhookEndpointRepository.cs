using ECommerce.Domain.Integrations;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Integrations.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Integrations;

public sealed class WebhookEndpointRepository(ECommerceDbContext dbContext) : IWebhookEndpointRepository
{
    public Task<WebhookEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<WebhookEndpoint>()
            .SingleOrDefaultAsync(endpoint => endpoint.Id == id && !endpoint.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<WebhookEndpoint>> GetActiveByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken) =>
        await dbContext.Set<WebhookEndpoint>()
            .Where(endpoint => endpoint.IsActive && !endpoint.IsDeleted && EF.Functions.JsonExists(endpoint.EventTypes, eventType))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WebhookEndpoint>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<WebhookEndpoint>()
            .Where(endpoint => !endpoint.IsDeleted)
            .ToListAsync(cancellationToken);

    public void Add(WebhookEndpoint endpoint) => dbContext.Set<WebhookEndpoint>().Add(endpoint);
}
