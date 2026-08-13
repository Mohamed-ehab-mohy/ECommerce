using WishlistAggregate = ECommerce.Domain.Wishlist.Wishlist;

namespace ECommerce.UseCases.Wishlist.Ports;

public interface IWishlistRepository
{
    Task<WishlistAggregate?> ByOwnerKeyAsync(string ownerKey, CancellationToken cancellationToken);

    Task SaveAsync(WishlistAggregate wishlist, CancellationToken cancellationToken);
}
