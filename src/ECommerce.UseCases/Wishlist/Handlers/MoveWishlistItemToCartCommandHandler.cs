using ECommerce.Domain.Catalog;
using ECommerce.Domain.Cart;
using ECommerce.Domain.Wishlist;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Pricing;
using ECommerce.UseCases.Wishlist.Commands;
using ECommerce.UseCases.Wishlist.Ports;
using CartAggregate = ECommerce.Domain.Cart.Cart;
using WishlistAggregate = ECommerce.Domain.Wishlist.Wishlist;

namespace ECommerce.UseCases.Wishlist.Handlers;

public sealed class MoveWishlistItemToCartCommandHandler(
    IWishlistRepository wishlists,
    ICartRepository carts,
    IProductRepository products,
    IStockRepository stock,
    ICurrencyCatalog currencies,
    TimeProvider timeProvider,
    IValidator<MoveWishlistItemToCartCommand> validator) : IRequestHandler<MoveWishlistItemToCartCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(MoveWishlistItemToCartCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CartResponse>();
        }

        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.ProductNotFound;
        }

        if (product.Status != ProductStatus.Active || product.IsDeleted)
        {
            return WishlistErrors.ProductInactive;
        }

        var wishlist = await wishlists.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);
        if (wishlist is null || !wishlist.Items.Any(item => item.ProductId == request.ProductId))
        {
            return WishlistErrors.ItemNotFound;
        }

        var stockItems = await stock.ListBySkuAsync(product.Sku, cancellationToken);
        if (stockItems.Sum(item => item.Available) < 1)
        {
            return WishlistErrors.ProductOutOfStock;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var cart = await carts.ByOwnerKeyAsync(request.OwnerKey, cancellationToken)
            ?? CartAggregate.Create(request.OwnerKey, request.Currency, utcNow.AddDays(30), utcNow);

        var price = ProductResponseFactory.ResolveSnapshotPrice(product, currencies, cart.Currency);
        var name = product.Translations.FirstOrDefault()?.Name ?? string.Empty;
        var imageUrl = product.ImageUrls.FirstOrDefault();

        var addResult = cart.AddItem(
            product.Id,
            product.Sku,
            name,
            price.ListAmount,
            price.OfferAmount,
            1,
            imageUrl,
            utcNow);

        if (addResult.IsFailure)
        {
            return addResult.Error;
        }

        var removeResult = wishlist.RemoveItem(request.ProductId, utcNow);
        if (removeResult.IsFailure)
        {
            return removeResult.Error;
        }

        await carts.SaveAsync(cart, cancellationToken);
        await wishlists.SaveAsync(wishlist, cancellationToken);

        return Result<CartResponse>.Success(CartResponseFactory.From(cart, currencies));
    }
}
