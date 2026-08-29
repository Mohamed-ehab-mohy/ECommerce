using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Content.Commands;

public sealed record UpdateCmsLayoutCommand(
    Guid LayoutId,
    string Name,
    string Slug,
    bool IsActive,
    IReadOnlyList<CmsLayoutSectionInput> Sections) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.ContentLayoutWrite;
}
