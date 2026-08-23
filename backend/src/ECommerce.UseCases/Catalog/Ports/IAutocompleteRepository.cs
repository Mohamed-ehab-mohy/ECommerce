namespace ECommerce.UseCases.Catalog.Ports;

public interface IAutocompleteRepository
{
    Task<IReadOnlyList<AutocompleteSuggestion>> SearchAsync(string query, int limit, CancellationToken cancellationToken);
}

public sealed record AutocompleteSuggestion(Guid ProductId, string Name, string Sku, decimal ListAmount);
