using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Commands;

public sealed record CreatePageCommand(
    Guid? TenantId,
    string Title,
    string Slug,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    bool IsPublished) : IRequest<Result<PageResponse>>, IRequirePermission
{
    public string Permission => Permissions.ContentPageWrite;
}
