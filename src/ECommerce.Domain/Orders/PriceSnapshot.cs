namespace ECommerce.Domain.Orders;

public sealed record PriceSnapshotItem(
    Guid ProductId,
    string Sku,
    string Name,
    decimal ListPrice,
    decimal UnitPrice,
    int Quantity,
    string? ImageUrl);

public sealed record TotalsSnapshot(
    decimal Subtotal,
    decimal ItemDiscount,
    decimal CartDiscount,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal);

public sealed record PriceSnapshot(
    IReadOnlyList<PriceSnapshotItem> Lines,
    TotalsSnapshot Totals)
{
    public static readonly PriceSnapshot Empty = new([], new TotalsSnapshot(0m, 0m, 0m, 0m, 0m, 0m));

    public bool IsEmpty => Lines.Count == 0;
}
