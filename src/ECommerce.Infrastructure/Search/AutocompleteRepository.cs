using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Catalog.Ports;

namespace ECommerce.Infrastructure.Search;

public sealed class AutocompleteRepository(ECommerceDbContext dbContext) : IAutocompleteRepository
{
    public async Task<IReadOnlyList<AutocompleteSuggestion>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var trimmed = query.Trim();
        return await dbContext.Set<ProductSearchDocument>()
            .AsNoTracking()
            .Where(d => d.Locale == "en" &&
                (d.SearchVector!.Matches(EF.Functions.WebSearchToTsQuery("simple", trimmed)) ||
                 EF.Functions.TrigramsSimilarity(d.Name, trimmed) > 0.2))
            .OrderByDescending(d => EF.Functions.TrigramsSimilarity(d.Name, trimmed))
            .Take(limit)
            .Select(d => new AutocompleteSuggestion(d.ProductId, d.Name, d.Sku, d.ListAmount))
            .ToListAsync(cancellationToken);
    }
}
