using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class DeactivatePageCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter) : IRequestHandler<DeactivatePageCommand, Result>
{
    public async Task<Result> Handle(DeactivatePageCommand request, CancellationToken cancellationToken)
    {
        var page = await content.GetPageByIdAsync(request.PageId, cancellationToken);
        if (page is null)
        {
            return Result.Failure(ContentErrors.PageNotFound);
        }

        var before = new { page.IsPublished };

        page.Delete();

        var after = new { page.IsPublished };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.PageDeactivated,
            "Page",
            page.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
