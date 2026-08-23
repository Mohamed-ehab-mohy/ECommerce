using ECommerce.Domain.Wishlist;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Wishlist.Commands;
using ECommerce.UseCases.Wishlist.Ports;
using ECommerce.UseCases.Wishlist.Responses;

namespace ECommerce.UseCases.Wishlist.Handlers;

public sealed class RemoveWishlistItemCommandHandler(
    IWishlistRepository wishlists,
    TimeProvider timeProvider,
    IValidator<RemoveWishlistItemCommand> validator) : IRequestHandler<RemoveWishlistItemCommand, Result<WishlistResponse>>
{
    public async Task<Result<WishlistResponse>> Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<WishlistResponse>();
        }

        var wishlist = await wishlists.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);
        if (wishlist is null)
        {
            return WishlistErrors.WishlistNotFound;
        }

        var result = wishlist.RemoveItem(request.ProductId, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await wishlists.SaveAsync(wishlist, cancellationToken);

        return Result<WishlistResponse>.Success(WishlistResponseFactory.From(wishlist));
    }
}
