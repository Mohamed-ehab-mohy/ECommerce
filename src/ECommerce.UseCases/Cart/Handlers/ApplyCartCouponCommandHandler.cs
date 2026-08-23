using ECommerce.Domain.Cart;
using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;
using ECommerce.UseCases.Promotions.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Cart.Handlers;

public sealed class ApplyCartCouponCommandHandler(
    ICartRepository carts,
    ICouponRepository coupons,
    ICurrencyCatalog currencies,
    TimeProvider timeProvider,
    IValidator<ApplyCartCouponCommand> validator,
    ILogger<ApplyCartCouponCommandHandler> logger) : IRequestHandler<ApplyCartCouponCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(ApplyCartCouponCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CartResponse>();
        }

        if (!request.OwnerKey.StartsWith("user:", StringComparison.Ordinal))
        {
            return CouponErrors.CustomerRequired;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var cart = await carts.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);
        if (cart is null)
        {
            return CartErrors.CartNotFound;
        }

        var coupon = await coupons.GetByCodeAsync(request.Code, cancellationToken);
        if (coupon is null)
        {
            return CouponErrors.CouponNotFound;
        }

        if (!coupon.IsActiveAt(utcNow))
        {
            return CouponErrors.Inactive;
        }

        if (coupon.UsedCount >= coupon.TotalUses)
        {
            return CouponErrors.Exhausted;
        }

        cart.ApplyCoupon(coupon.Code, utcNow);

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
