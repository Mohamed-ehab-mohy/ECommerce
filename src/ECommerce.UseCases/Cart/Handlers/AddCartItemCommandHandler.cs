using ECommerce.Domain.Catalog;
using ECommerce.Domain.Cart;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UseCases.Cart.Handlers;

public sealed class AddCartItemCommandHandler(
    ICartRepository carts,
    IProductRepository products,
    ICurrencyCatalog currencies,
    TimeProvider timeProvider,
    IValidator<AddCartItemCommand> validator,
    ILogger<AddCartItemCommandHandler> logger) : IRequestHandler<AddCartItemCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CartResponse>();
        }

        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.ProductNotFound;
        }

        if (product.Status != ProductStatus.Active || product.IsDeleted)
        {
            return CartErrors.ProductInactive;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var cart = await carts.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);

        if (cart is null)
        {
            cart = CartAggregate.Create(request.OwnerKey, request.Currency, utcNow.AddDays(30), utcNow);
        }

        var price = ProductResponseFactory.ResolveSnapshotPrice(product, currencies, cart.Currency);
        var name = product.Translations.FirstOrDefault()?.Name ?? string.Empty;
        var imageUrl = product.ImageUrls.FirstOrDefault();

        var addResult = cart.AddItem(
            product.Id,
            product.Sku,
            name,
            price.ListAmount,
            price.OfferAmount,
            request.Quantity,
            imageUrl,
            utcNow);

        if (addResult.IsFailure)
        {
            return addResult.Error;
        }

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
