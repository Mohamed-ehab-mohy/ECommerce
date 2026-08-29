using ECommerce.Domain.Content;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Queries;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class GetBannerQueryHandler(
    IContentRepository content,
    IValidator<GetBannerQuery> validator) : IRequestHandler<GetBannerQuery, Result<BannerResponse>>
{
    public async Task<Result<BannerResponse>> Handle(GetBannerQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<BannerResponse>();
        }

        var banner = await content.GetBannerByIdAsync(request.BannerId, cancellationToken);

        return banner is null
            ? Result<BannerResponse>.Failure(ContentErrors.BannerNotFound)
            : Result<BannerResponse>.Success(ToResponse(banner));
    }

    internal static BannerResponse ToResponse(Banner banner) =>
        new(banner.Id, banner.Title, banner.ImageUrl, banner.TargetUrl, banner.DisplayOrder, banner.IsActive);
}
