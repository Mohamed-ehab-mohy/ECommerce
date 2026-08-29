using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class DeactivateCmsLayoutCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter) : IRequestHandler<DeactivateCmsLayoutCommand, Result>
{
    public async Task<Result> Handle(DeactivateCmsLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await content.GetLayoutByIdAsync(request.LayoutId, cancellationToken);
        if (layout is null)
        {
            return Result.Failure(ContentErrors.LayoutNotFound);
        }

        var before = new { layout.IsActive };

        layout.Deactivate();

        var after = new { layout.IsActive };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.CmsLayoutDeactivated,
            "CmsLayout",
            layout.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
