using ECommerce.Domain.Events;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.Infrastructure.Catalog;

public sealed class ProductCacheInvalidationHandler(IProductRepository repository)
    : IEventHandler<ProductCreated>,
      IEventHandler<ProductUpdated>,
      IEventHandler<ProductDeactivated>
{
    public async Task HandleAsync(ProductCreated domainEvent, CancellationToken cancellationToken)
    {
        if (repository is CachedProductRepository cached)
        {
            await cached.InvalidateListCacheAsync();
        }
    }

    public async Task HandleAsync(ProductUpdated domainEvent, CancellationToken cancellationToken)
    {
        if (repository is CachedProductRepository cached)
        {
            await cached.InvalidateProductAsync(domainEvent.ProductId);
            await cached.InvalidateListCacheAsync();
        }
    }

    public async Task HandleAsync(ProductDeactivated domainEvent, CancellationToken cancellationToken)
    {
        if (repository is CachedProductRepository cached)
        {
            await cached.InvalidateProductAsync(domainEvent.ProductId);
            await cached.InvalidateListCacheAsync();
        }
    }
}
