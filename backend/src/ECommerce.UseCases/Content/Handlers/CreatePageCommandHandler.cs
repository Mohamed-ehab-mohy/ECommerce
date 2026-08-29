using ECommerce.Domain.Audit;
using ECommerce.Domain.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Ports;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Handlers;

public sealed class CreatePageCommandHandler(
    IContentRepository content,
    IUnitOfWork unitOfWork,
    ITenantService tenantService,
    IValidator<CreatePageCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreatePageCommand, Result<PageResponse>>
{
    public async Task<Result<PageResponse>> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PageResponse>();
        }

        var slug = request.Slug.Trim();
        if (await content.GetPageBySlugAsync(slug, cancellationToken) is not null)
        {
            return Result<PageResponse>.Failure(ContentErrors.PageSlugAlreadyExists);
        }

        var page = Page.Create(
            tenantService.GetCurrentTenantId(),
            request.Title.Trim(),
            slug,
            request.HtmlContent,
            request.MetaTitle?.Trim(),
            request.MetaDescription?.Trim(),
            request.IsPublished);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.PageCreated,
            "Page",
            page.Id.ToString(),
            After: new { page.Title, page.Slug, page.IsPublished }), cancellationToken);

        content.AddPage(page);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PageResponse>.Success(GetPageBySlugQueryHandler.ToResponse(page));
    }
}
