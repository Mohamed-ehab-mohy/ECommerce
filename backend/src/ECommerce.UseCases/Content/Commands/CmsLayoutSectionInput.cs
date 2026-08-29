using ECommerce.Shared.Content;

namespace ECommerce.UseCases.Content.Commands;

public sealed record CmsLayoutSectionInput(
    string Title,
    CmsSectionType Type,
    int DisplayOrder,
    string? ConfigJson,
    bool IsActive);
