using ECommerce.Domain.Integrations;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Integrations.Ports;

namespace ECommerce.Infrastructure.Integrations;

public sealed class WebhookDeliveryRepository(ECommerceDbContext dbContext) : IWebhookDeliveryRepository
{
    public Task<WebhookDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<WebhookDelivery>()
            .SingleOrDefaultAsync(delivery => delivery.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WebhookDelivery>> ListByEndpointAsync(
        Guid endpointId,
        CancellationToken cancellationToken) =>
        await dbContext.Set<WebhookDelivery>()
            .Where(delivery => delivery.EndpointId == endpointId)
            .OrderByDescending(delivery => delivery.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(WebhookDelivery delivery) => dbContext.Set<WebhookDelivery>().Add(delivery);
}
