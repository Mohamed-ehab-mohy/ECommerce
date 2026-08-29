using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class DeactivateBannerCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter) : IRequestHandler<DeactivateBannerCommand, Result>
{
    public async Task<Result> Handle(DeactivateBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = await content.GetBannerByIdAsync(request.BannerId, cancellationToken);
        if (banner is null)
        {
            return Result.Failure(ContentErrors.BannerNotFound);
        }

        var before = new { banner.IsActive };

        banner.Deactivate();

        var after = new { banner.IsActive };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.BannerDeactivated,
            "Banner",
            banner.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
