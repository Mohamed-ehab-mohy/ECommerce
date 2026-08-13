using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Domain.Pricing;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Responses;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Promotions.Ports;
using FluentValidation;
using MediatR;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class PlaceOrderCommandHandler(
    ICheckoutRepository checkouts,
    IPaymentRepository payments,
    IOrderRepository orders,
    IIdempotencyKeyRepository idempotencyKeys,
    IStockAllocator stockAllocator,
    IOrderNumberGenerator orderNumberGenerator,
    ICouponRepository coupons,
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<PlaceOrderCommand> validator) : IRequestHandler<PlaceOrderCommand, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<OrderResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var existingIdempotency = await idempotencyKeys.GetByKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingIdempotency is not null)
        {
            if (existingIdempotency.CheckoutId != request.CheckoutId)
            {
                return OrderErrors.IdempotencyKeyReuse;
            }

            var replayedOrder = await orders.GetByIdAsync(existingIdempotency.OrderId, cancellationToken);
            return replayedOrder is null ? OrderErrors.OrderNotFound : OrderResponse.From(replayedOrder);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var checkout = await checkouts.GetByIdAsync(request.CheckoutId, cancellationToken);
        if (checkout is null)
        {
            return CheckoutErrors.CheckoutNotFound;
        }

        if (checkout.IsExpired(utcNow))
        {
            return CheckoutErrors.CheckoutExpired;
        }

        if (checkout.Status != CheckoutStatus.PaymentAuthorized)
        {
            var concurrentKey = await idempotencyKeys.GetByKeyAsync(request.IdempotencyKey, cancellationToken);
            if (concurrentKey is not null)
            {
                if (concurrentKey.CheckoutId != request.CheckoutId)
                {
                    return OrderErrors.IdempotencyKeyReuse;
                }

                var replayedOrder = await orders.GetByIdAsync(concurrentKey.OrderId, cancellationToken);
                return replayedOrder is null ? OrderErrors.OrderNotFound : OrderResponse.From(replayedOrder);
            }

            return CheckoutErrors.InvalidState;
        }

        if (checkout.PaymentId is null)
        {
            return CheckoutErrors.InvalidState;
        }

        var payment = await payments.GetByIdAsync(checkout.PaymentId.Value, cancellationToken);
        if (payment is null)
        {
            return PaymentErrors.PaymentNotFound;
        }

        if (payment.Status != PaymentStatus.Authorized)
        {
            return PaymentErrors.PaymentNotAuthorized;
        }

        var orderNumber = await orderNumberGenerator.GenerateAsync(utcNow, cancellationToken);

        var order = Order.Create(
            checkout.Id,
            checkout.CartId,
            checkout.CustomerId,
            checkout.CustomerEmail,
            checkout.Currency,
            orderNumber,
            checkout.PriceSnapshot,
            checkout.ShippingAddress,
            checkout.BillingAddress,
            checkout.ShippingMethodId,
            payment.Id,
            utcNow,
            checkout.AppliedCouponId,
            checkout.AppliedPromotionIds);

        orders.Add(order);

        var conflictingKey = await idempotencyKeys.AddIfAbsentAsync(
            IdempotencyKey.Create(request.IdempotencyKey, checkout.Id, order.Id, utcNow),
            cancellationToken);
        if (conflictingKey is not null)
        {
            var conflictingOrder = await orders.GetByIdAsync(conflictingKey.OrderId, cancellationToken);
            return conflictingOrder is null ? OrderErrors.OrderNotFound : OrderResponse.From(conflictingOrder);
        }

        var allocation = await stockAllocator.AllocateAsync(
            order.Items
                .Select(item => new AllocationRequestItem(item.Sku, item.Quantity))
                .ToList(),
            "ORDER",
            order.Id.ToString("N"),
            utcNow,
            cancellationToken);

        if (allocation.HasShortfalls)
        {
            var shortfallBySku = allocation.Shortfalls.ToDictionary(shortfall => shortfall.Sku);
            var catalog = await products.GetBySkusAsync(shortfallBySku.Keys, cancellationToken);
            var catalogBySku = catalog.ToDictionary(product => product.Sku);

            var notBackorderable = shortfallBySku.Keys
                .Where(sku => !catalogBySku.TryGetValue(sku, out var product) || !product.Backorderable)
                .ToList();

            if (notBackorderable.Count > 0)
            {
                return CheckoutErrors.InsufficientStock(
                    notBackorderable
                        .Select(sku =>
                        {
                            var shortfall = shortfallBySku[sku];
                            return new StockShortageLine(sku, shortfall.Requested, shortfall.Available);
                        })
                        .ToList());
            }

            var backorderLines = shortfallBySku.Values
                .Select(shortfall => (
                    ProductId: order.Items.First(item => item.Sku == shortfall.Sku).ProductId,
                    shortfall.Sku,
                    Quantity: shortfall.Requested - shortfall.Available))
                .ToList();

            var backorderResult = order.MarkBackordered(backorderLines, utcNow);
            if (backorderResult.IsFailure)
            {
                return backorderResult.Error;
            }
        }

        var attachResult = payment.AttachOrder(order.Id, utcNow);
        if (attachResult.IsFailure)
        {
            return attachResult.Error;
        }

        var markPlacedResult = checkout.MarkPlaced(utcNow);
        if (markPlacedResult.IsFailure)
        {
            return markPlacedResult.Error;
        }

        // QAS-02: atomic coupon redemption inside the same transaction. Concurrent place-order attempts
        // race on `UPDATE coupons SET used_count = used_count + 1 WHERE used_count < total_uses`.
        if (checkout.AppliedCouponId is { } couponId && checkout.CustomerId is { } customerId)
        {
            var redemption = await coupons.TryRedeemAsync(
                couponId,
                order.Id,
                customerId,
                utcNow,
                cancellationToken);

            if (redemption == CouponRedemptionResult.Exhausted)
            {
                return CouponErrors.Exhausted;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OrderResponse.From(order);
    }
}
