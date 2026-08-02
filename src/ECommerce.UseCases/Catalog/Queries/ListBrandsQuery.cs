using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Responses;
using MediatR;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed record ListBrandsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedBrandsResponse>>;
