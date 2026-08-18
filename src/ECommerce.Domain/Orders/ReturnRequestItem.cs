namespace ECommerce.Domain.Orders;

public sealed class ReturnRequestItem
{
    private ReturnRequestItem() { }

    public Guid ReturnRequestId { get; internal set; }
    public Guid OrderItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? Reason { get; private set; }

    public static ReturnRequestItem Create(Guid orderItemId, Guid productId, string sku,
        int quantity, decimal unitPrice, string? reason) =>
        new()
        {
            OrderItemId = orderItemId, ProductId = productId, Sku = sku,
            Quantity = quantity, UnitPrice = unitPrice, Reason = reason
        };
}
