using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using MediatR;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class GetProductQueryHandler(IProductRepository products)
    : IRequestHandler<GetProductQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await products.GetActiveByIdAsync(request.ProductId, cancellationToken);

        return product is null
            ? Result<ProductResponse>.Failure(ProductErrors.ProductNotFound)
            : Result<ProductResponse>.Success(ProductResponseFactory.From(product, request.Locale, request.Currency));
    }
}
