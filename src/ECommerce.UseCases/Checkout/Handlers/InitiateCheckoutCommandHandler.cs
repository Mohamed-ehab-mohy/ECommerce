using ECommerce.Domain.Cart;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Pricing;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Checkout.Commands;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Checkout.Responses;
using ECommerce.UseCases.Checkout.Services;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Services;
using ECommerce.UseCases.Promotions.Ports;
using CartAggregate = ECommerce.Domain.Cart.Cart;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.UseCases.Checkout.Handlers;

public sealed class InitiateCheckoutCommandHandler(
    ICartRepository carts,
    ICheckoutRepository checkouts,
    IPaymentRepository payments,
    IProductRepository products,
    IPromotionRepository promotions,
    ICouponRepository coupons,
    PaymentIntentService paymentIntents,
    CheckoutTotalsCalculator totalsCalculator,
    StockAvailabilityVerifier availabilityVerifier,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<InitiateCheckoutCommand> validator) : IRequestHandler<InitiateCheckoutCommand, Result<CheckoutResponse>>
{
    private const string DefaultCustomerSegment = "General";

    public async Task<Result<CheckoutResponse>> Handle(InitiateCheckoutCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CheckoutResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var shippingAddress = ToSnapshot(request.ShippingAddress);
        var billingAddress = request.BillingAddress is null ? null : ToSnapshot(request.BillingAddress);

        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null)
        {
            return CartErrors.CartNotFound;
        }

        if (cart.Items.Count == 0)
        {
            return CheckoutErrors.CartEmpty;
        }

        var lines = cart.Items
            .Select(item => new PriceSnapshotItem(
                item.ProductId,
                item.Sku,
                item.Name,
                item.ListPrice,
                item.UnitPrice,
                item.Quantity,
                item.ImageUrl))
            .ToList();

        var productAttributes = await LoadProductAttributesAsync(lines, cancellationToken);

        var activePromotions = await promotions.GetActiveForScopeAsync(utcNow, cancellationToken);

        var (coupon, couponError) = await ResolveCouponAsync(cart, request.CustomerId, utcNow, cancellationToken);
        if (couponError is not null)
        {
            return couponError;
        }

        var totalsResult = await totalsCalculator.ComputePromotionAwareAsync(
            lines,
            productAttributes,
            activePromotions,
            coupon,
            DefaultCustomerSegment,
            utcNow,
            request.ShippingMethodId,
            shippingAddress.Country,
            request.Currency,
            cancellationToken);
        if (totalsResult.IsFailure)
        {
            return totalsResult.Error;
        }

        var issues = await availabilityVerifier.VerifyAsync(cart.Items, cancellationToken);
        if (issues.Count > 0)
        {
            return CheckoutErrors.InsufficientStock(
                issues.Select(issue => new StockShortageLine(issue.Sku, issue.Requested, issue.Available)).ToList());
        }

        var paymentResult = await paymentIntents.CreateIntentAsync(
            request.CustomerId,
            request.ProviderKey,
            request.MethodType,
            request.Currency,
            request.Country,
            totalsResult.Value.Totals.GrandTotal,
            cancellationToken);
        if (paymentResult.IsFailure)
        {
            return paymentResult.Error;
        }

        var checkout = CheckoutAggregate.Create(
            cart.Id,
            request.CustomerId,
            request.CustomerEmail,
            request.Currency,
            new PriceSnapshot(lines, new TotalsSnapshot(
                totalsResult.Value.Totals.Subtotal,
                totalsResult.Value.Totals.ItemDiscount,
                totalsResult.Value.Totals.CartDiscount,
                totalsResult.Value.Totals.ShippingTotal,
                totalsResult.Value.Totals.TaxTotal,
                totalsResult.Value.Totals.GrandTotal,
                totalsResult.Value.Totals.TaxRate)),
            shippingAddress,
            billingAddress ?? shippingAddress,
            request.ShippingMethodId,
            paymentResult.Value.Payment.Id,
            utcNow.AddMinutes(30),
            utcNow,
            totalsResult.Value.AppliedCouponId,
            totalsResult.Value.AppliedPromotionIds);

        payments.Add(paymentResult.Value.Payment);
        checkouts.Add(checkout);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CheckoutResponseFactory.From(checkout, paymentResult.Value.Payment);
    }

    private async Task<IReadOnlyDictionary<Guid, ProductLineAttributes>> LoadProductAttributesAsync(
        IReadOnlyCollection<PriceSnapshotItem> lines,
        CancellationToken cancellationToken)
    {
        var productIds = lines.Select(line => line.ProductId).Distinct().ToList();
        var productEntities = await products.GetByIdsAsync(productIds, cancellationToken);

        return productEntities.ToDictionary(
            product => product.Id,
            product => new ProductLineAttributes(
                product.CategoryId is { } categoryId ? [categoryId] : [],
                product.BrandId is { } brandId ? [brandId] : []));
    }

    private async Task<(Coupon? Coupon, Error? Error)> ResolveCouponAsync(
        CartAggregate cart,
        Guid? customerId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (cart.AppliedCouponCode is null)
        {
            return (null, null);
        }

        var coupon = await coupons.GetByCodeAsync(cart.AppliedCouponCode, cancellationToken);
        if (coupon is null)
        {
            return (null, CouponErrors.CouponNotFound);
        }

        if (!coupon.IsActiveAt(utcNow))
        {
            return (null, CouponErrors.Inactive);
        }

        if (coupon.UsedCount >= coupon.TotalUses)
        {
            return (null, CouponErrors.Exhausted);
        }

        if (customerId is not null
            && coupon.PerCustomerLimit is { } limit
            && await coupons.GetRedemptionCountAsync(coupon.Id, customerId.Value, cancellationToken) >= limit)
        {
            return (null, CouponErrors.AlreadyUsed);
        }

        return (coupon, null);
    }

    private static AddressSnapshot ToSnapshot(AddressInput address) =>
        new(
            address.FullName,
            address.Phone,
            address.Street,
            address.City,
            address.Region,
            address.Country,
            address.PostalCode);
}
