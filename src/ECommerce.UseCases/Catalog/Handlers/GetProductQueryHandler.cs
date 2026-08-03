using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Pricing;
using MediatR;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class GetProductQueryHandler(
    IProductRepository products,
    ILocaleCatalog locales,
    ICurrencyCatalog currencies)
    : IRequestHandler<GetProductQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await products.GetActiveByIdAsync(request.ProductId, cancellationToken);

        return product is null
            ? Result<ProductResponse>.Failure(ProductErrors.ProductNotFound)
            : Result<ProductResponse>.Success(ProductResponseFactory.From(product, locales, currencies, request.Locale, request.Currency));
    }
}
