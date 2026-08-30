
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record CreateBrandCommand(
    string Name,
    string? Description,
    string? Website) : IRequest<Result<Guid>>, IRequirePermission
{
    public string Permission => Permissions.CatalogBrandWrite;
}
