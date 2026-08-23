using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record ProductImportRow(
    string Sku,
    string Name,
    string Currency,
    decimal ListAmount,
    decimal? OfferAmount,
    string? Slug,
    Guid? CategoryId,
    Guid? BrandId,
    string? Description,
    bool IsFeatured,
    string? Status,
    string Locale);

/// <summary>Starts an async bulk product import batch (US-B-007, BR-1207, FR-02-007).</summary>
public sealed record StartProductImportCommand(IReadOnlyList<ProductImportRow> Rows)
    : IRequest<Result<ProductImportStartedResponse>>, IRequirePermission
{
    public string Permission => Permissions.CatalogProductWrite;
}
