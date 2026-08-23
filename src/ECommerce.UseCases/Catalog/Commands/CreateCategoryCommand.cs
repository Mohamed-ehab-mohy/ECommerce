
namespace ECommerce.UseCases.Catalog.Commands;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    Guid? ParentId,
    int SortOrder) : IRequest<Result<Guid>>;
