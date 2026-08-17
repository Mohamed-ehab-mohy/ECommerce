namespace ECommerce.Infrastructure.ReadModels;

public sealed class QueryProviderOptions
{
    public const string SectionName = "QueryProvider";
    public string Provider { get; set; } = "Ef";
}
