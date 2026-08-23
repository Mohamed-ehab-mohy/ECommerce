using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record DeactivateProductCommand(Guid ProductId) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.CatalogProductDelete;
}
