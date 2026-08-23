namespace ECommerce.Domain.Orders;

public sealed class OrderBackorderItem
{
    private OrderBackorderItem()
    {
        Sku = string.Empty;
    }

    private OrderBackorderItem(
        Guid orderId,
        Guid productId,
        string sku,
        int quantity,
        DateTime utcNow)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        Sku = sku;
        Quantity = quantity;
        FilledQuantity = 0;
        Status = BackorderStatus.Open;
        CreatedAt = utcNow;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; }

    public int Quantity { get; private set; }

    public int FilledQuantity { get; private set; }

    public BackorderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? FilledAt { get; private set; }

    public bool IsFilled => FilledQuantity >= Quantity;

    public static OrderBackorderItem Create(
        Guid orderId,
        Guid productId,
        string sku,
        int quantity,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        return new OrderBackorderItem(orderId, productId, sku, quantity, utcNow);
    }

    public void Fill(int quantity, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        FilledQuantity = Math.Min(Quantity, FilledQuantity + quantity);
        if (IsFilled)
        {
            Status = BackorderStatus.Filled;
            FilledAt = utcNow;
        }
    }
}
