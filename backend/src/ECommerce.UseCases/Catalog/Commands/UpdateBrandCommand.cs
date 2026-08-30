
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record UpdateBrandCommand(
    Guid BrandId,
    string? Name,
    string? Description,
    string? Website) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.CatalogBrandWrite;
}
