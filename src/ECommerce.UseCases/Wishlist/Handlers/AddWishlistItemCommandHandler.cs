using ECommerce.Domain.Catalog;
using ECommerce.Domain.Wishlist;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Wishlist.Commands;
using ECommerce.UseCases.Wishlist.Ports;
using ECommerce.UseCases.Wishlist.Responses;
using WishlistAggregate = ECommerce.Domain.Wishlist.Wishlist;

namespace ECommerce.UseCases.Wishlist.Handlers;

public sealed class AddWishlistItemCommandHandler(
    IWishlistRepository wishlists,
    IProductRepository products,
    TimeProvider timeProvider,
    IValidator<AddWishlistItemCommand> validator) : IRequestHandler<AddWishlistItemCommand, Result<WishlistResponse>>
{
    public async Task<Result<WishlistResponse>> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<WishlistResponse>();
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

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var wishlist = await wishlists.ByOwnerKeyAsync(request.OwnerKey, cancellationToken)
            ?? WishlistAggregate.Create(request.OwnerKey, utcNow);

        var result = wishlist.AddItem(request.ProductId, utcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await wishlists.SaveAsync(wishlist, cancellationToken);

        return Result<WishlistResponse>.Success(WishlistResponseFactory.From(wishlist));
    }
}
