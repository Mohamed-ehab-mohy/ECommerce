using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string? Name,
    string? Slug,
    Guid? ParentId,
    int? SortOrder) : IRequest<Result>;
