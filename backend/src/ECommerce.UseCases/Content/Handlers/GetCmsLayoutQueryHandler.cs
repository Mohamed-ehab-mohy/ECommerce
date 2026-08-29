using ECommerce.Domain.Content;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Queries;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class GetCmsLayoutQueryHandler(
    IContentRepository content,
    IValidator<GetCmsLayoutQuery> validator) : IRequestHandler<GetCmsLayoutQuery, Result<CmsLayoutResponse>>
{
    public async Task<Result<CmsLayoutResponse>> Handle(GetCmsLayoutQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CmsLayoutResponse>();
        }

        var layout = await content.GetLayoutByIdAsync(request.LayoutId, cancellationToken);

        return layout is null
            ? Result<CmsLayoutResponse>.Failure(ContentErrors.LayoutNotFound)
            : Result<CmsLayoutResponse>.Success(GetCmsLayoutBySlugQueryHandler.ToResponse(layout));
    }
}
