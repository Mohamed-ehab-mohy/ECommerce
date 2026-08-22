using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string? Slug,
    string? Name,
    string? Description,
    string? Currency,
    decimal? ListAmount,
    decimal? OfferAmount,
    Guid? CategoryId,
    Guid? BrandId,
    bool? IsFeatured,
    string? Status,
    string? Locale) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.CatalogProductWrite;
}
