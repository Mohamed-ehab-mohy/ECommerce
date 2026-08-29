using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Commands;

public sealed record CreateCmsLayoutCommand(
    Guid? TenantId,
    string Name,
    string Slug,
    bool IsActive,
    IReadOnlyList<CmsLayoutSectionInput> Sections) : IRequest<Result<CmsLayoutResponse>>, IRequirePermission
{
    public string Permission => Permissions.ContentLayoutWrite;
}
