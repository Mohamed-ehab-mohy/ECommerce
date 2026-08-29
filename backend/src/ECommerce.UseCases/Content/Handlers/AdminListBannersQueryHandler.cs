using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Queries;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class AdminListBannersQueryHandler(
    IContentRepository content,
    IValidator<AdminListBannersQuery> validator) : IRequestHandler<AdminListBannersQuery, Result<PagedBannersResponse>>
{
    public async Task<Result<PagedBannersResponse>> Handle(AdminListBannersQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedBannersResponse>();
        }

        var items = await content.ListBannersAsync(request.Page, request.PageSize, cancellationToken);
        var total = await content.CountBannersAsync(cancellationToken);

        return Result<PagedBannersResponse>.Success(new PagedBannersResponse(
            items.Select(GetBannerQueryHandler.ToResponse).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}
