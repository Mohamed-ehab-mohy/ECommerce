namespace ECommerce.Infrastructure;

public sealed class DataProviderOptions
{
    public const string SectionName = "DataProvider";
    public string Provider { get; set; } = "Postgres";
}
