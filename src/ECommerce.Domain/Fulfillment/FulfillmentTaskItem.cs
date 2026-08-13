namespace ECommerce.Domain.Fulfillment;

public sealed class FulfillmentTaskItem
{
    private FulfillmentTaskItem()
    {
        Sku = string.Empty;
    }

    private FulfillmentTaskItem(
        Guid taskId,
        Guid productId,
        string sku,
        int quantity,
        string? binLocation)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        ProductId = productId;
        Sku = sku;
        Quantity = quantity;
        BinLocation = binLocation;
    }

    public Guid Id { get; private set; }

    public Guid TaskId { get; private set; }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; }

    public int Quantity { get; private set; }

    public string? BinLocation { get; private set; }

    public static FulfillmentTaskItem Create(
        Guid taskId,
        Guid productId,
        string sku,
        int quantity,
        string? binLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        return new FulfillmentTaskItem(taskId, productId, sku, quantity, string.IsNullOrWhiteSpace(binLocation) ? null : binLocation.Trim());
    }

    public void MoveTo(Guid taskId)
    {
        TaskId = taskId;
    }
}
