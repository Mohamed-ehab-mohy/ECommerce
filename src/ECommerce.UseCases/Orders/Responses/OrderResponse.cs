using ECommerce.Domain.Orders;

namespace ECommerce.UseCases.Orders.Responses;

public sealed record OrderLineResponse(
    Guid ProductId,
    string Sku,
    string Name,
    decimal UnitPrice,
    int Quantity,
    string? ImageUrl);

public sealed record OrderTimelineResponse(
    OrderStatus? FromStatus,
    OrderStatus ToStatus,
    string ActorType,
    Guid? ActorId,
    string? TraceId,
    DateTime OccurredAt);

public sealed record OrderResponse(
    Guid OrderId,
    string OrderNumber,
    Guid CheckoutId,
    Guid CartId,
    Guid? CustomerId,
    string CustomerEmail,
    string Currency,
    decimal Subtotal,
    decimal ItemDiscount,
    decimal CartDiscount,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string Status,
    DateTime? PlacedAt,
    IReadOnlyList<OrderLineResponse> Lines,
    IReadOnlyList<OrderTimelineResponse> Timeline)
{
    public static OrderResponse From(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.CheckoutId,
            order.CartId,
            order.CustomerId,
            order.CustomerEmail,
            order.Currency,
            order.Subtotal,
            order.ItemDiscount,
            order.CartDiscount,
            order.ShippingTotal,
            order.TaxTotal,
            order.GrandTotal,
            order.Status.ToString(),
            order.PlacedAt,
            order.Items
                .Select(item => new OrderLineResponse(
                    item.ProductId,
                    item.Sku,
                    item.Name,
                    item.UnitPrice,
                    item.Quantity,
                    item.ImageUrl))
                .ToList(),
            order.StatusLogs
                .OrderBy(entry => entry.OccurredAt)
                .Select(entry => new OrderTimelineResponse(
                    entry.FromStatus,
                    entry.ToStatus,
                    entry.ActorType,
                    entry.ActorId,
                    entry.TraceId,
                    entry.OccurredAt))
                .ToList());
}
