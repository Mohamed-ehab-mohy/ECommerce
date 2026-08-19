using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;

namespace ECommerce.Infrastructure.Catalog;

public sealed class ProductCacheInvalidationHandler(CachedProductRepository repository)
    : IEventHandler<ProductCreated>,
      IEventHandler<ProductUpdated>,
      IEventHandler<ProductDeactivated>
{
    public async Task HandleAsync(ProductCreated domainEvent, CancellationToken cancellationToken)
    {
        await repository.InvalidateListCacheAsync();
    }

    public async Task HandleAsync(ProductUpdated domainEvent, CancellationToken cancellationToken)
    {
        await repository.InvalidateProductAsync(domainEvent.ProductId);
        await repository.InvalidateListCacheAsync();
    }

    public async Task HandleAsync(ProductDeactivated domainEvent, CancellationToken cancellationToken)
    {
        await repository.InvalidateProductAsync(domainEvent.ProductId);
        await repository.InvalidateListCacheAsync();
    }
}
