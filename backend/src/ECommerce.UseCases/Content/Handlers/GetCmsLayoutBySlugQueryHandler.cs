using ECommerce.Domain.Content;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Queries;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class GetCmsLayoutBySlugQueryHandler(
    IContentRepository content,
    IValidator<GetCmsLayoutBySlugQuery> validator) : IRequestHandler<GetCmsLayoutBySlugQuery, Result<CmsLayoutResponse>>
{
    public async Task<Result<CmsLayoutResponse>> Handle(GetCmsLayoutBySlugQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CmsLayoutResponse>();
        }

        var layout = await content.GetActiveLayoutBySlugAsync(request.Slug, cancellationToken);

        return layout is null
            ? Result<CmsLayoutResponse>.Failure(ContentErrors.LayoutNotFound)
            : Result<CmsLayoutResponse>.Success(ToResponse(layout));
    }

    internal static CmsLayoutResponse ToResponse(CmsLayout layout) =>
        new(
            layout.Id,
            layout.Name,
            layout.Slug,
            layout.IsActive,
            layout.Sections
                .OrderBy(section => section.DisplayOrder)
                .Select(ToSectionResponse)
                .ToList());

    internal static CmsLayoutSectionResponse ToSectionResponse(CmsLayoutSection section) =>
        new(section.Id, section.Title, section.Type, section.DisplayOrder, section.ConfigJson, section.IsActive);
}
