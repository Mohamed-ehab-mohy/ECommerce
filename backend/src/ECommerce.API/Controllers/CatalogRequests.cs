namespace ECommerce.API.Controllers;

public sealed record CreateCategoryRequest(
    string Name,
    string Slug,
    Guid? ParentId,
    int SortOrder = 0);

public sealed record UpdateCategoryRequest(
    string? Name,
    string? Slug,
    Guid? ParentId,
    int? SortOrder);

public sealed record CreateBrandRequest(
    string Name,
    string? Description,
    string? Website);

public sealed record UpdateBrandRequest(
    string? Name,
    string? Description,
    string? Website);
