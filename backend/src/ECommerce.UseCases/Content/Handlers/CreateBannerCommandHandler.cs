using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class CreateBannerCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    ITenantService tenantService,
    IValidator<CreateBannerCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreateBannerCommand, Result<BannerResponse>>
{
    public async Task<Result<BannerResponse>> Handle(CreateBannerCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<BannerResponse>();
        }

        var banner = Banner.Create(
            tenantService.GetCurrentTenantId(),
            request.Title.Trim(),
            request.ImageUrl.Trim(),
            request.TargetUrl?.Trim(),
            request.DisplayOrder,
            request.IsActive);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.BannerCreated,
            "Banner",
            banner.Id.ToString(),
            After: new { banner.Title, banner.ImageUrl, banner.TargetUrl, banner.DisplayOrder, banner.IsActive }), cancellationToken);

        content.AddBanner(banner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BannerResponse>.Success(GetBannerQueryHandler.ToResponse(banner));
    }
}
