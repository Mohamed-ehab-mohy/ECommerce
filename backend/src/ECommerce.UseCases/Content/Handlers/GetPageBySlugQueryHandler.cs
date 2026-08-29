using ECommerce.Domain.Content;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Queries;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class GetPageBySlugQueryHandler(
    IContentRepository content,
    IValidator<GetPageBySlugQuery> validator) : IRequestHandler<GetPageBySlugQuery, Result<PageResponse>>
{
    public async Task<Result<PageResponse>> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PageResponse>();
        }

        var page = await content.GetPublishedPageBySlugAsync(request.Slug, cancellationToken);

        return page is null
            ? Result<PageResponse>.Failure(ContentErrors.PageNotFound)
            : Result<PageResponse>.Success(ToResponse(page));
    }

    internal static PageResponse ToResponse(Page page) =>
        new(page.Id, page.Title, page.Slug, page.HtmlContent, page.MetaTitle, page.MetaDescription, page.IsPublished);
}
