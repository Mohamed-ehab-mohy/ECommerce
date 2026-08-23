namespace ECommerce.UseCases.Cart.Responses;

public sealed record CartPriceChangeWarning(
    Guid ProductId,
    string Sku,
    string Name,
    decimal CartUnitPrice,
    decimal CurrentUnitPrice,
    decimal Delta);

public sealed record CartPriceChangesResponse(IReadOnlyList<CartPriceChangeWarning> Warnings)
{
    public static CartPriceChangesResponse Empty { get; } = new([]);
}
