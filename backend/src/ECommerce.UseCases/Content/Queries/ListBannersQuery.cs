using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record ListBannersQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedBannersResponse>>;
