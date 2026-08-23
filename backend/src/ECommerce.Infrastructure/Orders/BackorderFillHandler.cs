using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Services;

namespace ECommerce.Infrastructure.Orders;

public sealed class BackorderFillHandler(BackorderFillService fillService) : IEventHandler<StockRestocked>
{
    public async Task HandleAsync(StockRestocked domainEvent, CancellationToken cancellationToken)
        => await fillService.FillForSkuAsync(domainEvent.Sku, cancellationToken);
}
