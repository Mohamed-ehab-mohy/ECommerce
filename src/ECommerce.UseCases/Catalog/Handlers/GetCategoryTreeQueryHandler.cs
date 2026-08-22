using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class GetCategoryTreeQueryHandler(ICategoryRepository categories)
    : IRequestHandler<GetCategoryTreeQuery, Result<IReadOnlyList<CategoryNodeResponse>>>
{
    public async Task<Result<IReadOnlyList<CategoryNodeResponse>>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken cancellationToken)
    {
        var all = await categories.ListAllAsync(cancellationToken);

        return Result<IReadOnlyList<CategoryNodeResponse>>.Success(BuildTree(all, null));
    }

    private static IReadOnlyList<CategoryNodeResponse> BuildTree(IReadOnlyList<Category> all, Guid? parentId)
    {
        return all
            .Where(category => category.ParentId == parentId)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryNodeResponse(
                category.Id,
                category.Name,
                category.Slug,
                category.ParentId,
                category.SortOrder,
                category.Level,
                BuildTree(all, category.Id)))
            .ToList();
    }
}
