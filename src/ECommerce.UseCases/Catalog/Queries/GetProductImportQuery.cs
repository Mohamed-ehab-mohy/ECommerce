using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Queries;

/// <summary>Returns the status and per-row error report of an import (US-B-007).</summary>
public sealed record GetProductImportQuery(Guid ImportId)
    : IRequest<Result<ProductImportStatusResponse>>, IRequirePermission
{
    public string Permission => Permissions.CatalogProductWrite;
}
