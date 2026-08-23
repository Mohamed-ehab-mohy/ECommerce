namespace ECommerce.UseCases.Fulfillment.Shipping;

public sealed class CarrierOptions
{
    public const string SectionName = "Carriers";

    public CarrierConfig Aramex { get; set; } = new();
    public CarrierConfig Dhl { get; set; } = new();
}

public sealed class CarrierConfig
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
}
