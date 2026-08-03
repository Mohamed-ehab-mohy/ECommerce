using System.Text.Json;
using ECommerce.Domain.Cart;

namespace ECommerce.Infrastructure.Carts;

internal static class CartCacheCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(Cart cart)
    {
        var dto = new CartCacheDto(
            cart.Id,
            cart.OwnerKey,
            cart.Currency,
            cart.Version,
            cart.ExpiresAt,
            cart.CreatedAt,
            cart.UpdatedAt,
            cart.Items
                .Select(item => new CartCacheItemDto(
                    item.ProductId,
                    item.Sku,
                    item.Name,
                    item.UnitPrice,
                    item.Quantity,
                    item.ImageUrl,
                    item.UpdatedAt))
                .ToList());

        return JsonSerializer.Serialize(dto, Options);
    }

    public static Cart Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<CartCacheDto>(json, Options)
            ?? throw new InvalidOperationException("Failed to deserialize cached cart.");

        var items = dto.Items
            .Select(item => CartItem.Rehydrate(
                item.ProductId,
                item.Sku,
                item.Name,
                item.UnitPrice,
                item.Quantity,
                item.ImageUrl,
                item.UpdatedAt))
            .ToList();

        return Cart.Rehydrate(
            dto.Id,
            dto.OwnerKey,
            dto.Currency,
            dto.Version,
            dto.ExpiresAt,
            dto.CreatedAt,
            dto.UpdatedAt,
            items);
    }

    private sealed record CartCacheItemDto(
        Guid ProductId,
        string Sku,
        string Name,
        decimal UnitPrice,
        int Quantity,
        string? ImageUrl,
        DateTime UpdatedAt);

    private sealed record CartCacheDto(
        Guid Id,
        string OwnerKey,
        string Currency,
        long Version,
        DateTime ExpiresAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<CartCacheItemDto> Items);
}
