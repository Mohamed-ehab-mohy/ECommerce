using ECommerce.Domain.Events;
using ECommerce.Domain.Integrations;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Integrations.Services;

/// <summary>
/// Bridges domain events to signed webhook deliveries (US-M-004). Maps each event to the catalog
/// payload (docs/08 §8.2) and hands it to the delivery service.
/// </summary>
public sealed class WebhookEventDispatcher(
    WebhookDeliveryService deliveryService,
    IOrderRepository orders,
    IProductRepository products,
    IWarehouseRepository warehouses) : IEventHandler<OrderPlaced>,
        IEventHandler<PaymentCaptured>,
        IEventHandler<OrderShipped>,
        IEventHandler<OrderCancelled>,
        IEventHandler<RefundCompleted>,
        IEventHandler<ProductUpdated>,
        IEventHandler<LowStockAlertRaised>
{
    public async Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken)
    {
        object payload;
        var order = await orders.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is not null)
        {
            payload = new
            {
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                customerId = order.CustomerId,
                currency = order.Currency,
                totals = new
                {
                    subtotal = order.Subtotal,
                    discount = order.ItemDiscount + order.CartDiscount,
                    shipping = order.ShippingTotal,
                    tax = order.TaxTotal,
                    grandTotal = order.GrandTotal
                },
                lines = order.Items.Select(item => new
                {
                    productId = item.ProductId,
                    sku = item.Sku,
                    name = item.Name,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice
                }).ToList()
            };
        }
        else
        {
            payload = new
            {
                orderId = domainEvent.OrderId,
                orderNumber = domainEvent.OrderNumber,
                customerId = (Guid?)null,
                currency = domainEvent.Currency,
                totals = new { grandTotal = domainEvent.Total },
                lines = Array.Empty<object>()
            };
        }

        await deliveryService.DispatchAsync(domainEvent.OccurredOn, WebhookEventTypes.OrderPlaced, payload, cancellationToken);
    }

    public async Task HandleAsync(PaymentCaptured domainEvent, CancellationToken cancellationToken)
    {
        var order = domainEvent.OrderId is { } orderId
            ? await orders.GetByIdAsync(orderId, cancellationToken)
            : null;

        await deliveryService.DispatchAsync(domainEvent.OccurredOn, WebhookEventTypes.OrderPaid, new
        {
            orderNumber = order?.OrderNumber,
            paymentId = domainEvent.PaymentId,
            amount = domainEvent.Amount,
            currency = domainEvent.Currency
        }, cancellationToken);
    }

    public Task HandleAsync(OrderShipped domainEvent, CancellationToken cancellationToken) =>
        deliveryService.DispatchAsync(domainEvent.OccurredOn, WebhookEventTypes.OrderShipped, new
        {
            orderNumber = domainEvent.OrderNumber,
            trackingNumbers = domainEvent.TrackingNumbers
        }, cancellationToken);

    public Task HandleAsync(OrderCancelled domainEvent, CancellationToken cancellationToken) =>
        deliveryService.DispatchAsync(domainEvent.OccurredOn, WebhookEventTypes.OrderCancelled, new
        {
            orderNumber = domainEvent.OrderNumber,
            reason = domainEvent.Reason
        }, cancellationToken);

    public async Task HandleAsync(RefundCompleted domainEvent, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(domainEvent.OrderId, cancellationToken);

        await deliveryService.DispatchAsync(domainEvent.OccurredOn, WebhookEventTypes.RefundCompleted, new
        {
            refundId = domainEvent.RefundId,
            orderNumber = order?.OrderNumber,
            amount = domainEvent.Amount,
            currency = domainEvent.Currency
        }, cancellationToken);
    }

    public async Task HandleAsync(ProductUpdated domainEvent, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(domainEvent.ProductId, cancellationToken);

        await deliveryService.DispatchAsync(domainEvent.OccurredOn, WebhookEventTypes.ProductUpdated, new
        {
            productId = domainEvent.ProductId,
            sku = domainEvent.Sku,
            status = product?.Status.ToString()
        }, cancellationToken);
    }

    public async Task HandleAsync(LowStockAlertRaised domainEvent, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(domainEvent.WarehouseId, cancellationToken);

        await deliveryService.DispatchAsync(domainEvent.OccurredOn, WebhookEventTypes.StockLow, new
        {
            sku = domainEvent.Sku,
            warehouseCode = warehouse?.Code,
            onHand = domainEvent.Available,
            threshold = domainEvent.Threshold
        }, cancellationToken);
    }
}
