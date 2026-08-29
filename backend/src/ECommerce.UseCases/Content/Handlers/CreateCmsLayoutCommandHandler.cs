using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class CreateCmsLayoutCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    ITenantService tenantService,
    TimeProvider timeProvider,
    IValidator<CreateCmsLayoutCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreateCmsLayoutCommand, Result<CmsLayoutResponse>>
{
    public async Task<Result<CmsLayoutResponse>> Handle(CreateCmsLayoutCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CmsLayoutResponse>();
        }

        var slug = request.Slug.Trim();
        if (await content.GetLayoutBySlugAsync(slug, cancellationToken) is not null)
        {
            return Result<CmsLayoutResponse>.Failure(ContentErrors.LayoutSlugAlreadyExists);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var layout = CmsLayout.Create(
            tenantService.GetCurrentTenantId(),
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

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.CmsLayoutCreated,
            "CmsLayout",
            layout.Id.ToString(),
            After: new { layout.Name, layout.Slug, layout.IsActive }), cancellationToken);

        content.AddLayout(layout);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CmsLayoutResponse>.Success(GetCmsLayoutBySlugQueryHandler.ToResponse(layout));
    }
}
