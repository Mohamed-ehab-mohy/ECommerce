namespace ECommerce.UseCases.Orders.Responses;

public sealed record OrderLineResponse(
    Guid ProductId,
    string Sku,
    string Name,
    decimal UnitPrice,
    int Quantity,
    string? ImageUrl);

public sealed record OrderResponse(
    Guid OrderId,
    Guid CheckoutId,
    Guid CartId,
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
    IReadOnlyList<OrderLineResponse> Lines)
{
    public static OrderResponse From(ECommerce.Domain.Orders.Order order) =>
        new(
            order.Id,
            order.CheckoutId,
            order.CartId,
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
                .ToList());
}
