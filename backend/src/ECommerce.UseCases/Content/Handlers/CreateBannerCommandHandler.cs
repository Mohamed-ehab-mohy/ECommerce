using ECommerce.Domain.Content;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

internal sealed class CreateBannerCommandHandler(IContentRepository contentRepository, ITenantService tenantService)
    : IRequestHandler<CreateBannerCommand, Result<BannerResponse>>
{
    public async Task<Result<BannerResponse>> Handle(CreateBannerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantService.GetCurrentTenantId();

        var banner = Banner.Create(
            tenantId,
            request.Title,
            request.ImageUrl,
            request.TargetUrl,
            request.DisplayOrder,
            request.IsActive);

        await contentRepository.AddBannerAsync(banner, cancellationToken);
        await contentRepository.SaveChangesAsync(cancellationToken);

        var response = new BannerResponse(
            banner.Id,
            banner.Title,
            banner.ImageUrl,
            banner.TargetUrl,
            banner.DisplayOrder,
            banner.IsActive);

        return Result<BannerResponse>.Success(response);
    }
}
