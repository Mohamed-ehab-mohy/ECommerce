using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record CreateProductCommand(
    string Sku,
    string Slug,
    string Name,
    string? Description,
    string Currency,
    decimal ListAmount,
    decimal? OfferAmount,
    Guid? CategoryId,
    Guid? BrandId,
    bool IsFeatured,
    string? Status,
    string Locale) : IRequest<Result<Guid>>, IRequirePermission
{
    public string Permission => Permissions.CatalogProductWrite;
}
