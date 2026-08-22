using ECommerce.Domain.Catalog;
using ECommerce.Domain.Cart;
using ECommerce.Domain.Orders;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Checkout.Services;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class ReorderOrderCommandHandler(
    IOrderRepository orders,
    IProductRepository products,
    ICartRepository carts,
    StockAvailabilityVerifier availability,
    ICurrencyCatalog currencies,
    TimeProvider timeProvider,
    IValidator<ReorderOrderCommand> validator,
    ILogger<ReorderOrderCommandHandler> logger) : IRequestHandler<ReorderOrderCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(ReorderOrderCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CartResponse>();
        }

        if (!OrderNumber.TryParse(request.OrderNumber, out var orderNumber) || orderNumber is null)
        {
            return OrderErrors.OrderNotFound;
        }

        var order = await orders.GetByNumberWithDetailsAsync(orderNumber.Value, cancellationToken);
        if (order is null)
        {
            return OrderErrors.OrderNotFound;
        }

        if (request.RequesterCustomerId is not { } customerId || order.CustomerId != customerId)
        {
            return OrderErrors.NotYourOrder;
        }

        var productIds = order.Items.Select(item => item.ProductId).ToList();
        var productsById = (await products.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(product => product.Id);

        foreach (var line in order.Items)
        {
            if (!productsById.TryGetValue(line.ProductId, out var product)
                || product.Status != ProductStatus.Active
                || product.IsDeleted)
            {
                return CartErrors.ProductInactive;
            }
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var ownerKey = $"user:{customerId}";
        var cart = await carts.ByOwnerKeyAsync(ownerKey, cancellationToken)
            ?? CartAggregate.Create(ownerKey, order.Currency, utcNow.AddDays(30), utcNow);

        foreach (var line in order.Items)
        {
            var product = productsById[line.ProductId];
            var price = ProductResponseFactory.ResolveSnapshotPrice(product, currencies, cart.Currency);
            var name = product.Translations.FirstOrDefault()?.Name ?? string.Empty;
            var imageUrl = product.ImageUrls.FirstOrDefault();

            var addResult = cart.AddItem(
                product.Id,
                product.Sku,
                name,
                price.ListAmount,
                price.OfferAmount,
                line.Quantity,
                imageUrl,
                utcNow);

            if (addResult.IsFailure)
            {
                return addResult.Error;
            }
        }

        var issues = await availability.VerifyAsync(cart.Items, cancellationToken);
        if (issues.Count > 0)
        {
            return CheckoutErrors.InsufficientStock(
                issues
                    .Select(issue => new StockShortageLine(issue.Sku, issue.Requested, issue.Available))
                    .ToList());
        }

        try
        {
            await carts.SaveAsync(cart, cancellationToken);
        }
        catch (CartConcurrencyException exception)
        {
            logger.LogWarning(exception, "Concurrent cart mutation rejected for owner key {OwnerKey}", ownerKey);
            return CartErrors.ConcurrencyConflict;
        }

        return Result<CartResponse>.Success(CartResponseFactory.From(cart, currencies));
    }
}
