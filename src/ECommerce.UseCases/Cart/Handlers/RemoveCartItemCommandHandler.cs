using ECommerce.Domain.Cart;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Cart.Handlers;

public sealed class RemoveCartItemCommandHandler(
    ICartRepository carts,
    ICurrencyCatalog currencies,
    TimeProvider timeProvider,
    IValidator<RemoveCartItemCommand> validator,
    ILogger<RemoveCartItemCommandHandler> logger) : IRequestHandler<RemoveCartItemCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CartResponse>();
        }

        var cart = await carts.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);
        if (cart is null)
        {
            return CartErrors.CartNotFound;
        }

        var result = cart.RemoveItem(request.ProductId, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result.Error;
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
