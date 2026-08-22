
namespace ECommerce.UseCases.Catalog.Queries;

public sealed record AutocompleteProductsQuery(string Query, int Limit = 10) : IRequest<IReadOnlyList<AutocompleteResult>>;

public sealed record AutocompleteResult(Guid ProductId, string Name, string Sku, decimal ListAmount);
