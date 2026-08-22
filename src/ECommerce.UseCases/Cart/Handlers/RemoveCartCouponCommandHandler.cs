using ECommerce.Domain.Cart;
using ECommerce.Domain.Pricing;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Cart.Handlers;

public sealed class RemoveCartCouponCommandHandler(
    ICartRepository carts,
    ICurrencyCatalog currencies,
    TimeProvider timeProvider,
    IValidator<RemoveCartCouponCommand> validator,
    ILogger<RemoveCartCouponCommandHandler> logger) : IRequestHandler<RemoveCartCouponCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(RemoveCartCouponCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CartResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var cart = await carts.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);
        if (cart is null)
        {
            return CartErrors.CartNotFound;
        }

        if (cart.AppliedCouponCode is null)
        {
            return CouponErrors.NotApplied;
        }

        cart.RemoveCoupon(utcNow);

        try
        {
            await carts.SaveAsync(cart, cancellationToken);
        }
        catch (CartConcurrencyException exception)
        {
            logger.LogWarning(exception, "Concurrent cart mutation rejected for owner key {OwnerKey}", request.OwnerKey);
            return CartErrors.ConcurrencyConflict;
        }

        return Result<CartResponse>.Success(CartResponseFactory.From(cart, currencies));
    }
}
