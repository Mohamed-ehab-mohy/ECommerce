namespace ECommerce.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    public Guid OrderId { get; internal set; }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public decimal ListPrice { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public string? ImageUrl { get; private set; }

    internal static OrderItem FromSnapshot(PriceSnapshotItem line) =>
        new()
        {
            ProductId = line.ProductId,
            Sku = line.Sku,
            Name = line.Name,
            ListPrice = line.ListPrice,
            UnitPrice = line.UnitPrice,
            Quantity = line.Quantity,
            ImageUrl = line.ImageUrl
        };
}
