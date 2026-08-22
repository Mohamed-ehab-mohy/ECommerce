using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class GetProductQueryHandler(
    IProductRepository products,
    ILocaleCatalog locales,
    ICurrencyCatalog currencies,
    IValidator<GetProductQuery> validator)
    : IRequestHandler<GetProductQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ProductResponse>();
        }

        var product = await products.GetActiveByIdAsync(request.ProductId, cancellationToken);

        return product is null
            ? Result<ProductResponse>.Failure(ProductErrors.ProductNotFound)
            : Result<ProductResponse>.Success(ProductResponseFactory.From(product, locales, currencies, request.Locale, request.Currency));
    }
}
