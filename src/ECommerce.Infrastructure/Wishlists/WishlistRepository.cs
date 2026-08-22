using ECommerce.Domain.Wishlist;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Wishlist.Ports;
using WishlistAggregate = ECommerce.Domain.Wishlist.Wishlist;

namespace ECommerce.Infrastructure.Wishlists;

public sealed class WishlistRepository(ECommerceDbContext dbContext) : IWishlistRepository
{
    public Task<WishlistAggregate?> ByOwnerKeyAsync(string ownerKey, CancellationToken cancellationToken) =>
        dbContext.Set<WishlistAggregate>()
            .Include(wishlist => wishlist.Items)
            .SingleOrDefaultAsync(wishlist => wishlist.OwnerKey == ownerKey, cancellationToken);

    public async Task SaveAsync(WishlistAggregate wishlist, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<WishlistAggregate>()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == wishlist.Id, cancellationToken);

        if (existing is null)
        {
            dbContext.Set<WishlistAggregate>().Add(wishlist);
        }
        else
        {
            await dbContext.Set<WishlistItem>()
                .Where(item => item.WishlistId == wishlist.Id)
                .ExecuteDeleteAsync(cancellationToken);

            var entry = dbContext.Set<WishlistAggregate>().Attach(wishlist);
            entry.State = EntityState.Modified;

            foreach (var item in wishlist.Items)
            {
                dbContext.Entry(item).State = EntityState.Added;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
