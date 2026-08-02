using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Responses;
using MediatR;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed record GetCategoryTreeQuery : IRequest<Result<IReadOnlyList<CategoryNodeResponse>>>;
