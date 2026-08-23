using ECommerce.UseCases.Catalog.Responses;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed record GetCategoryTreeQuery : IRequest<Result<IReadOnlyList<CategoryNodeResponse>>>;
