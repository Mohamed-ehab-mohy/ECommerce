using ECommerce.UseCases.Catalog.Responses;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed record GetProductQuery(Guid ProductId, string? Locale = null, string? Currency = null)
    : IRequest<Result<ProductResponse>>;
