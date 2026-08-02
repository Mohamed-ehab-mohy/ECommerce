using ECommerce.Domain.Catalog;

namespace ECommerce.UseCases.Catalog.Responses;

public sealed record CategoryNodeResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    int SortOrder,
    int Level,
    IReadOnlyList<CategoryNodeResponse> Children);
