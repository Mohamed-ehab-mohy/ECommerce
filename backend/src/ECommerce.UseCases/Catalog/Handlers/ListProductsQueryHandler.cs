using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class ListProductsQueryHandler(
    IProductRepository products,
    ILocaleCatalog locales,
    ICurrencyCatalog currencies,
    IValidator<ListProductsQuery> validator) : IRequestHandler<ListProductsQuery, Result<PagedProductsResponse>>
{
    public async Task<Result<PagedProductsResponse>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedProductsResponse>();
        }

        var items = await products.ListActiveAsync(request.Page, request.PageSize, cancellationToken);
        var total = await products.CountActiveAsync(cancellationToken);

        return Result<PagedProductsResponse>.Success(new PagedProductsResponse(
            items.Select(item => ProductResponseFactory.From(item, locales, currencies, request.Locale, request.Currency)).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}
