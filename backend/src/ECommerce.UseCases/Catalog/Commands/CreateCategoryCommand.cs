using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    Guid? ParentId,
    int SortOrder) : IRequest<Result<Guid>>, IRequirePermission
{
    public string Permission => Permissions.CatalogCategoryWrite;
}
