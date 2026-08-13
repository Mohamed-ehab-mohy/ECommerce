using ECommerce.Domain.Common;
using ECommerce.Domain.Events;
using ECommerce.Shared.Primitives;

namespace ECommerce.Domain.Orders;

public sealed class Order : BaseEntity<Guid>
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusLog> _statusLogs = [];
    private readonly List<OrderBackorderItem> _backorderItems = [];

    private Order()
    {
        OrderNumber = string.Empty;
        CustomerEmail = string.Empty;
        Currency = string.Empty;
        ShippingMethodId = string.Empty;
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

    public Guid? CouponId { get; private set; }

    public IReadOnlyList<Guid> AppliedPromotionIds { get; private set; } = [];

    public AddressSnapshot ShippingAddress { get; private set; } = null!;

    public AddressSnapshot BillingAddress { get; private set; } = null!;

    public string ShippingMethodId { get; private set; }

    public Guid PaymentId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime? PlacedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    public IReadOnlyCollection<OrderStatusLog> StatusLogs => _statusLogs;

    public IReadOnlyCollection<OrderBackorderItem> BackorderItems => _backorderItems;

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
        DateTime utcNow,
        Guid? couponId = null,
        IReadOnlyList<Guid>? appliedPromotionIds = null)
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
            CouponId = couponId,
            AppliedPromotionIds = appliedPromotionIds ?? [],
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            ShippingMethodId = shippingMethodId,
            PaymentId = paymentId,
            Status = OrderStatus.Placed,
            PlacedAt = utcNow,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        foreach (var item in items)
        {
            item.OrderId = order.Id;
            order._items.Add(item);
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

    public Result MarkBackordered(
        IReadOnlyList<(Guid ProductId, string Sku, int Quantity)> lines,
        DateTime utcNow)
    {
        foreach (var (productId, _, _) in lines)
        {
            if (_backorderItems.Any(item => item.ProductId == productId && !item.IsFilled))
            {
                return OrderErrors.BackorderAlreadyOpen;
            }
        }

        if (Status != OrderStatus.Placed)
        {
            return OrderErrors.InvalidState;
        }

        foreach (var (productId, sku, quantity) in lines)
        {
            _backorderItems.Add(OrderBackorderItem.Create(Id, productId, sku, quantity, utcNow));
        }

        RecordStatusChange(Status, OrderStatus.Backordered, "system", null, null, utcNow);
        Status = OrderStatus.Backordered;
        UpdatedAt = utcNow;

        AddDomainEvent(new OrderBackordered(
            Id,
            OrderNumber,
            CustomerEmail,
            lines.Select(line => new BackorderLine(line.ProductId, line.Sku, line.Quantity)).ToList()));

        return Result.Success();
    }

    public int FillBackorderItems(string sku, int quantity, DateTime utcNow)
    {
        var filled = 0;
        var remaining = quantity;

        foreach (var item in _backorderItems
                     .Where(item => item.Sku == sku && !item.IsFilled)
                     .OrderBy(item => item.CreatedAt))
        {
            var amount = Math.Min(remaining, item.Quantity - item.FilledQuantity);
            item.Fill(amount, utcNow);
            filled += amount;
            remaining -= amount;

            if (remaining <= 0)
            {
                break;
            }
        }

        if (filled > 0)
        {
            UpdatedAt = utcNow;

            AddDomainEvent(new BackorderFilled(
                Id,
                OrderNumber,
                CustomerEmail,
                _backorderItems.First(item => item.Sku == sku).ProductId,
                sku,
                filled));

            if (_backorderItems.All(item => item.IsFilled) && Status == OrderStatus.Backordered)
            {
                RecordStatusChange(Status, OrderStatus.AwaitingFulfillment, "system", null, null, utcNow);
                Status = OrderStatus.AwaitingFulfillment;
            }
        }

        return filled;
    }

    public Result StartFulfillment(
        string actorType,
        Guid? actorId,
        string? traceId,
        DateTime utcNow)
    {
        if (Status != OrderStatus.AwaitingFulfillment)
        {
            return OrderErrors.InvalidState;
        }

        RecordStatusChange(Status, OrderStatus.Picking, actorType, actorId, traceId, utcNow);
        Status = OrderStatus.Picking;
        UpdatedAt = utcNow;

        return Result.Success();
    }

    public Result MarkPacked(
        string actorType,
        Guid? actorId,
        string? traceId,
        DateTime utcNow)
    {
        if (Status != OrderStatus.Picking)
        {
            return OrderErrors.InvalidState;
        }

        RecordStatusChange(Status, OrderStatus.Packed, actorType, actorId, traceId, utcNow);
        Status = OrderStatus.Packed;
        UpdatedAt = utcNow;

        return Result.Success();
    }

    public Result Ship(
        string carrierKey,
        IReadOnlyList<string> trackingNumbers,
        string actorType,
        Guid? actorId,
        string? traceId,
        DateTime utcNow)
    {
        if (Status != OrderStatus.Packed)
        {
            return OrderErrors.InvalidState;
        }

        RecordStatusChange(Status, OrderStatus.Shipped, actorType, actorId, traceId, utcNow);
        Status = OrderStatus.Shipped;
        UpdatedAt = utcNow;

        AddDomainEvent(new OrderShipped(Id, OrderNumber, CustomerEmail, carrierKey, trackingNumbers));

        return Result.Success();
    }

    public Result UpdateShippingAddress(
        AddressSnapshot newAddress,
        string actorType,
        Guid? actorId,
        string? traceId,
        DateTime utcNow)
    {
        if (Status is OrderStatus.Shipped
            or OrderStatus.Delivered
            or OrderStatus.Completed
            or OrderStatus.Cancelled)
        {
            return OrderErrors.AddressCorrectionNotAllowed;
        }

        if (ShippingAddress == newAddress)
        {
            return Result.Success();
        }

        var previous = ShippingAddress;
        ShippingAddress = newAddress;
        UpdatedAt = utcNow;

        AddDomainEvent(new OrderShippingAddressUpdated(
            Id,
            OrderNumber,
            CustomerEmail,
            previous,
            newAddress));

        return Result.Success();
    }

    public Result Deliver(
        string actorType,
        Guid? actorId,
        string? traceId,
        DateTime utcNow)
    {
        if (Status != OrderStatus.Shipped)
        {
            return OrderErrors.InvalidState;
        }

        RecordStatusChange(Status, OrderStatus.Delivered, actorType, actorId, traceId, utcNow);
        Status = OrderStatus.Delivered;
        UpdatedAt = utcNow;

        AddDomainEvent(new OrderDelivered(Id, OrderNumber, CustomerEmail));

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
