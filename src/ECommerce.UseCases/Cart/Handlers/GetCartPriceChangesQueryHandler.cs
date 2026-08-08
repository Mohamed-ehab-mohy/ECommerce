using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Cart.Queries;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Pricing;
using MediatR;

namespace ECommerce.UseCases.Cart.Handlers;

public sealed class GetCartPriceChangesQueryHandler(
    ICartRepository carts,
    IProductRepository products,
    ICurrencyCatalog currencies) : IRequestHandler<GetCartPriceChangesQuery, Result<CartPriceChangesResponse>>
{
    public async Task<Result<CartPriceChangesResponse>> Handle(
        GetCartPriceChangesQuery request,
        CancellationToken cancellationToken)
    {
        var cart = await carts.ByOwnerKeyAsync(request.OwnerKey, cancellationToken);
        if (cart is null)
        {
            return Result<CartPriceChangesResponse>.Success(CartPriceChangesResponse.Empty);
        }

        var productIds = cart.Items.Select(item => item.ProductId).ToList();
        var current = await products.GetByIdsAsync(productIds, cancellationToken);
        var currentBySku = current.ToDictionary(product => product.Sku);

        var warnings = new List<CartPriceChangeWarning>();

        foreach (var item in cart.Items)
        {
            if (!currentBySku.TryGetValue(item.Sku, out var product))
            {
                continue;
            }

            var price = ProductResponseFactory.ResolveSnapshotPrice(product, currencies, cart.Currency);
            var currentUnitPrice = price.OfferAmount ?? price.ListAmount;
            var cartUnitPrice = item.UnitPrice;

            if (currentUnitPrice == cartUnitPrice)
            {
                continue;
            }

            warnings.Add(new CartPriceChangeWarning(
                item.ProductId,
                item.Sku,
                item.Name,
                cartUnitPrice,
                currentUnitPrice,
                currentUnitPrice - cartUnitPrice));
        }

        return Result<CartPriceChangesResponse>.Success(new CartPriceChangesResponse(warnings));
    }
}
