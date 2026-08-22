using ECommerce.Domain.Cart;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Cart.Queries;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UseCases.Cart.Handlers;

public sealed class GetCartQueryHandler(
    ICartRepository carts,
    ICurrencyCatalog currencies,
    TimeProvider timeProvider,
    IValidator<GetCartQuery> validator,
    ILogger<GetCartQueryHandler> logger) : IRequestHandler<GetCartQuery, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CartResponse>();
        }

        var cart = await carts.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);

        if (cart is null)
        {
            var empty = CartAggregate.Create(request.OwnerKey, request.Currency, timeProvider.GetUtcNow().UtcDateTime.AddDays(30), timeProvider.GetUtcNow().UtcDateTime);
            logger.LogInformation("Creating empty cart for owner key {OwnerKey} in {Currency}", request.OwnerKey, request.Currency);
            return Result<CartResponse>.Success(CartResponseFactory.From(empty, currencies));
        }

        return Result<CartResponse>.Success(CartResponseFactory.From(cart, currencies));
    }
}
