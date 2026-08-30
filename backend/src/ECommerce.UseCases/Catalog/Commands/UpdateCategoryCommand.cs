
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string? Name,
    string? Slug,
    Guid? ParentId,
    int? SortOrder) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.CatalogCategoryWrite;
}
