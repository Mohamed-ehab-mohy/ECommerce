using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class UpdatePageCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    IValidator<UpdatePageCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdatePageCommand, Result>
{
    public async Task<Result> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var page = await content.GetPageByIdAsync(request.PageId, cancellationToken);
        if (page is null)
        {
            return Result.Failure(ContentErrors.PageNotFound);
        }

        var slug = request.Slug.Trim();
        var existingBySlug = await content.GetPageBySlugAsync(slug, cancellationToken);
        if (existingBySlug is not null && existingBySlug.Id != page.Id)
        {
            return Result.Failure(ContentErrors.PageSlugAlreadyExists);
        }

        var before = new { page.Title, page.Slug, page.IsPublished };

        page.Update(
            request.Title.Trim(),
            slug,
            request.HtmlContent,
            request.MetaTitle?.Trim(),
            request.MetaDescription?.Trim(),
            request.IsPublished);

        var after = new { page.Title, page.Slug, page.IsPublished };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.PageUpdated,
            "Page",
            page.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
