using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Responses;
using ECommerce.UseCases.Payments.Ports;
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

        var order = Order.Create(
            checkout.Id,
            checkout.CartId,
            checkout.CustomerId,
            checkout.CustomerEmail,
            checkout.Currency,
            checkout.PriceSnapshot,
            checkout.ShippingAddress,
            checkout.BillingAddress,
            checkout.ShippingMethodId,
            payment.Id,
            utcNow);

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
            return CheckoutErrors.InsufficientStock(
                allocation.Shortfalls
                    .Select(shortfall => new StockShortageLine(shortfall.Sku, shortfall.Requested, shortfall.Available))
                    .ToList());
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

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OrderResponse.From(order);
    }
}
