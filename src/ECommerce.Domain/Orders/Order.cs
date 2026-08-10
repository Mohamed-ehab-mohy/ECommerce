using ECommerce.Domain.Common;
using ECommerce.Domain.Events;
using ECommerce.Shared.Primitives;

namespace ECommerce.Domain.Orders;

public sealed class Order : BaseEntity<Guid>
{
    private readonly List<OrderStatusLog> _statusLogs = [];

    private Order()
    {
        OrderNumber = string.Empty;
        CustomerEmail = string.Empty;
        Currency = string.Empty;
        ShippingMethodId = string.Empty;
        Items = [];
    }

    public Guid CheckoutId { get; private set; }

    public Guid CartId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string OrderNumber { get; private set; }

    public string CustomerEmail { get; private set; }

    public string Currency { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal ItemDiscount { get; private set; }

    public decimal CartDiscount { get; private set; }

    public decimal ShippingTotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal GrandTotal { get; private set; }

    public AddressSnapshot ShippingAddress { get; private set; } = null!;

    public AddressSnapshot BillingAddress { get; private set; } = null!;

    public string ShippingMethodId { get; private set; }

    public Guid PaymentId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime? PlacedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items { get; private set; }

    public IReadOnlyCollection<OrderStatusLog> StatusLogs => _statusLogs;

    public static Order Create(
        Guid checkoutId,
        Guid cartId,
        Guid? customerId,
        string customerEmail,
        string currency,
        string orderNumber,
        PriceSnapshot priceSnapshot,
        AddressSnapshot shippingAddress,
        AddressSnapshot billingAddress,
        string shippingMethodId,
        Guid paymentId,
        DateTime utcNow)
    {
        var items = priceSnapshot.Lines
            .Select(OrderItem.FromSnapshot)
            .ToList();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CheckoutId = checkoutId,
            CartId = cartId,
            CustomerId = customerId,
            CustomerEmail = customerEmail,
            Currency = currency,
            OrderNumber = orderNumber,
            Subtotal = priceSnapshot.Totals.Subtotal,
            ItemDiscount = priceSnapshot.Totals.ItemDiscount,
            CartDiscount = priceSnapshot.Totals.CartDiscount,
            ShippingTotal = priceSnapshot.Totals.ShippingTotal,
            TaxTotal = priceSnapshot.Totals.TaxTotal,
            GrandTotal = priceSnapshot.Totals.GrandTotal,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            ShippingMethodId = shippingMethodId,
            PaymentId = paymentId,
            Status = OrderStatus.Placed,
            PlacedAt = utcNow,
            Items = items,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        foreach (var item in items)
        {
            item.OrderId = order.Id;
        }

        order.RecordStatusChange(null, OrderStatus.Placed, "system", null, null, utcNow);

        order.AddDomainEvent(new OrderPlaced(
            order.Id,
            order.OrderNumber,
            checkoutId,
            cartId,
            customerEmail,
            order.GrandTotal,
            currency));

        return order;
    }

    public Result Cancel(
        string reason,
        string actorType,
        Guid? actorId,
        string? traceId,
        DateTime utcNow)
    {
        if (Status != OrderStatus.Placed)
        {
            return OrderErrors.CancellationNotAllowed;
        }

        RecordStatusChange(Status, OrderStatus.Cancelled, actorType, actorId, traceId, utcNow);
        Status = OrderStatus.Cancelled;
        UpdatedAt = utcNow;

        AddDomainEvent(new OrderCancelled(
            Id,
            OrderNumber,
            CustomerEmail,
            GrandTotal,
            Currency,
            reason));

        return Result.Success();
    }

    private void RecordStatusChange(
        OrderStatus? from,
        OrderStatus to,
        string actorType,
        Guid? actorId,
        string? traceId,
        DateTime utcNow)
    {
        _statusLogs.Add(OrderStatusLog.Create(Id, from, to, actorType, actorId, traceId, utcNow));
    }
}
