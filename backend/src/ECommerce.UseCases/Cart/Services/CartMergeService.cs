using ECommerce.Domain.Cart;
using ECommerce.UseCases.Cart.Ports;
using Microsoft.Extensions.Logging;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UseCases.Cart.Services;

public sealed class CartMergeService(
    ICartRepository carts,
    TimeProvider timeProvider,
    ILogger<CartMergeService> logger)
{
    public async Task MergeGuestCartAsync(string guestCartKey, Guid userId, CancellationToken cancellationToken)
    {
        var guestKey = $"anon:{guestCartKey}";
        var userKey = $"user:{userId}";

        var guest = await carts.ByOwnerKeyAsync(guestKey, cancellationToken);
        if (guest is null)
        {
            return;
        }

        var user = await carts.ByOwnerKeyAsync(userKey, cancellationToken);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (user is null)
        {
            user = CartAggregate.Create(userKey, guest.Currency, utcNow.AddDays(30), utcNow);
        }

        user.MergeFrom(guest, utcNow);

        await carts.SaveAsync(user, cancellationToken);
        await carts.SaveAsync(guest, cancellationToken);

        logger.LogInformation(
            "Guest cart {GuestKey} merged into {UserKey} ({CartItemCount} items).",
            guestKey,
            userKey,
            user.Items.Count);
    }
}
