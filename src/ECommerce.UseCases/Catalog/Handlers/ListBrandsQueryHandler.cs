using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class ListBrandsQueryHandler(
    IBrandRepository brands,
    IValidator<ListBrandsQuery> validator) : IRequestHandler<ListBrandsQuery, Result<PagedBrandsResponse>>
{
    public async Task<Result<PagedBrandsResponse>> Handle(ListBrandsQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedBrandsResponse>();
        }

        var items = await brands.ListAsync(request.Page, request.PageSize, cancellationToken);
        var total = await brands.CountAsync(cancellationToken);

        return Result<PagedBrandsResponse>.Success(new PagedBrandsResponse(
            items.Select(brand => new BrandResponse(brand.Id, brand.Name, brand.Description, brand.Website)).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}
