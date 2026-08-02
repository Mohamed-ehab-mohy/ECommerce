using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using MediatR;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record DeactivateProductCommand(Guid ProductId) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.CatalogProductDelete;
}
