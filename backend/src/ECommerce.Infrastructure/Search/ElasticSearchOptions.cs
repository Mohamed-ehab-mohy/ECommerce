namespace ECommerce.Infrastructure.Search;

public sealed class ElasticSearchOptions
{
    public const string SectionName = "ElasticSearch";
    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "products";
    public bool Enabled { get; set; }
}
