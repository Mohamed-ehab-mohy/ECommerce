using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record GetCmsLayoutBySlugQuery(string Slug) : IRequest<Result<CmsLayoutResponse>>;
