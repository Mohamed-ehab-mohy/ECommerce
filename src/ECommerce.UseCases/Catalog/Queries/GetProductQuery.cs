using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Responses;
using MediatR;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed record GetProductQuery(Guid ProductId, string? Locale = null, string? Currency = null)
    : IRequest<Result<ProductResponse>>;
