using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Wishlist.Ports;
using ECommerce.UseCases.Wishlist.Queries;
using ECommerce.UseCases.Wishlist.Responses;
using WishlistAggregate = ECommerce.Domain.Wishlist.Wishlist;

namespace ECommerce.UseCases.Wishlist.Handlers;

public sealed class GetWishlistQueryHandler(
    IWishlistRepository wishlists,
    TimeProvider timeProvider,
    IValidator<GetWishlistQuery> validator) : IRequestHandler<GetWishlistQuery, Result<WishlistResponse>>
{
    public async Task<Result<WishlistResponse>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<WishlistResponse>();
        }

        var wishlist = await wishlists.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);

        if (wishlist is null)
        {
            var empty = WishlistAggregate.Create(request.OwnerKey, timeProvider.GetUtcNow().UtcDateTime);
            return Result<WishlistResponse>.Success(WishlistResponseFactory.From(empty));
        }

        return Result<WishlistResponse>.Success(WishlistResponseFactory.From(wishlist));
    }
}
