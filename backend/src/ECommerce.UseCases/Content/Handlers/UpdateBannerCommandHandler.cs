using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class UpdateBannerCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    IValidator<UpdateBannerCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdateBannerCommand, Result>
{
    public async Task<Result> Handle(UpdateBannerCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var banner = await content.GetBannerByIdAsync(request.BannerId, cancellationToken);
        if (banner is null)
        {
            return Result.Failure(ContentErrors.BannerNotFound);
        }

        var before = new { banner.Title, banner.ImageUrl, banner.TargetUrl, banner.DisplayOrder, banner.IsActive };

        banner.Update(
            request.Title.Trim(),
            request.ImageUrl.Trim(),
            request.TargetUrl?.Trim(),
            request.DisplayOrder,
            request.IsActive);

        var after = new { banner.Title, banner.ImageUrl, banner.TargetUrl, banner.DisplayOrder, banner.IsActive };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.BannerUpdated,
            "Banner",
            banner.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
