using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record GetPageBySlugQuery(string Slug) : IRequest<Result<PageResponse>>;
