using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class AutocompleteProductsHandler(IAutocompleteRepository repository)
    : IRequestHandler<AutocompleteProductsQuery, IReadOnlyList<AutocompleteResult>>
{
    public async Task<IReadOnlyList<AutocompleteResult>> Handle(
        AutocompleteProductsQuery request,
        CancellationToken cancellationToken)
    {
        var suggestions = await repository.SearchAsync(request.Query, request.Limit, cancellationToken);
        return suggestions.Select(s => new AutocompleteResult(s.ProductId, s.Name, s.Sku, s.ListAmount)).ToList();
    }
}
