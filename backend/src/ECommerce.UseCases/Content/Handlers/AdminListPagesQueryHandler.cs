using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Queries;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class AdminListPagesQueryHandler(
    IContentRepository content,
    IValidator<AdminListPagesQuery> validator) : IRequestHandler<AdminListPagesQuery, Result<PagedPagesResponse>>
{
    public async Task<Result<PagedPagesResponse>> Handle(AdminListPagesQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedPagesResponse>();
        }

        var items = await content.ListPagesAsync(request.Page, request.PageSize, cancellationToken);
        var total = await content.CountPagesAsync(cancellationToken);

        return Result<PagedPagesResponse>.Success(new PagedPagesResponse(
            items.Select(GetPageBySlugQueryHandler.ToResponse).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}
