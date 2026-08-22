using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record UpdateBrandCommand(
    Guid BrandId,
    string? Name,
    string? Description,
    string? Website) : IRequest<Result>;
