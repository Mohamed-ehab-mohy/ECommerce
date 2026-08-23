using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Caching.Hybrid;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class GetProductQueryHandler(
    IProductRepository products,
    ILocaleCatalog locales,
    ICurrencyCatalog currencies,
    IValidator<GetProductQuery> validator,
    HybridCache hybridCache)
    : IRequestHandler<GetProductQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ProductResponse>();
        }

        var response = await hybridCache.GetOrCreateAsync(
            $"product_resp:{request.ProductId}:{request.Locale}:{request.Currency}",
            async cancel =>
            {
                var p = await products.GetActiveByIdAsync(request.ProductId, cancel);
                return p is null ? null : ProductResponseFactory.From(p, locales, currencies, request.Locale, request.Currency);
            },
            cancellationToken: cancellationToken);

        return response is null
            ? Result<ProductResponse>.Failure(ProductErrors.ProductNotFound)
            : Result<ProductResponse>.Success(response);
    }
}
