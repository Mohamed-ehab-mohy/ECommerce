namespace ECommerce.Domain.Payments;

/// <summary>A line on a refund request identifying the returned product (used for atomic restock).</summary>
public sealed class RefundItem
{
    private RefundItem()
    {
    }

    public Guid RefundId { get; internal set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public static RefundItem Create(Guid refundId, Guid productId, int quantity) =>
        new()
        {
            RefundId = refundId,
            ProductId = productId,
            Quantity = quantity
        };
}
