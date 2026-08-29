using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class UpdateCmsLayoutCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<UpdateCmsLayoutCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdateCmsLayoutCommand, Result>
{
    public async Task<Result> Handle(UpdateCmsLayoutCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var layout = await content.GetLayoutByIdAsync(request.LayoutId, cancellationToken);
        if (layout is null)
        {
            return Result.Failure(ContentErrors.LayoutNotFound);
        }

        var slug = request.Slug.Trim();
        var existingBySlug = await content.GetLayoutBySlugAsync(slug, cancellationToken);
        if (existingBySlug is not null && existingBySlug.Id != layout.Id)
        {
            return Result.Failure(ContentErrors.LayoutSlugAlreadyExists);
        }

        var before = new { layout.Name, layout.Slug, layout.IsActive };

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        layout.Update(
            request.Name.Trim(),
            slug,
            request.IsActive,
            utcNow);

        var sections = request.Sections
            .Select(section => CmsLayoutSection.Create(
                layout.Id,
                section.Title.Trim(),
                section.Type,
                section.DisplayOrder,
                section.ConfigJson,
                section.IsActive,
                utcNow))
            .ToList();
        layout.ReplaceSections(sections);

        var after = new { layout.Name, layout.Slug, layout.IsActive };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.CmsLayoutUpdated,
            "CmsLayout",
            layout.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
