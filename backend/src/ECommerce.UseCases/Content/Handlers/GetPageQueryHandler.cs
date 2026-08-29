using ECommerce.Domain.Content;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Queries;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class GetPageQueryHandler(
    IContentRepository content,
    IValidator<GetPageQuery> validator) : IRequestHandler<GetPageQuery, Result<PageResponse>>
{
    public async Task<Result<PageResponse>> Handle(GetPageQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PageResponse>();
        }

        var page = await content.GetPageByIdAsync(request.PageId, cancellationToken);

        return page is null
            ? Result<PageResponse>.Failure(ContentErrors.PageNotFound)
            : Result<PageResponse>.Success(GetPageBySlugQueryHandler.ToResponse(page));
    }
}
