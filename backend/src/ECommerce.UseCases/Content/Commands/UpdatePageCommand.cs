using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Content.Commands;

public sealed record UpdatePageCommand(
    Guid PageId,
    string Title,
    string Slug,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    bool IsPublished) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.ContentPageWrite;
}
