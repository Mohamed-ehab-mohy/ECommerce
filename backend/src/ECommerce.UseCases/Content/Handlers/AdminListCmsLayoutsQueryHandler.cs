using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Queries;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class AdminListCmsLayoutsQueryHandler(
    IContentRepository content,
    IValidator<AdminListCmsLayoutsQuery> validator) : IRequestHandler<AdminListCmsLayoutsQuery, Result<PagedCmsLayoutsResponse>>
{
    public async Task<Result<PagedCmsLayoutsResponse>> Handle(AdminListCmsLayoutsQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedCmsLayoutsResponse>();
        }

        var items = await content.ListLayoutsAsync(request.Page, request.PageSize, cancellationToken);
        var total = await content.CountLayoutsAsync(cancellationToken);

        return Result<PagedCmsLayoutsResponse>.Success(new PagedCmsLayoutsResponse(
            items.Select(GetCmsLayoutBySlugQueryHandler.ToResponse).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}
