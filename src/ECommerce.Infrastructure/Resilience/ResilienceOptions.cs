namespace ECommerce.Infrastructure.Resilience;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";
    public int PspRetryCount { get; set; } = 3;
    public int CarrierRetryCount { get; set; } = 2;
    public int PspTimeoutSeconds { get; set; } = 30;
    public int CarrierTimeoutSeconds { get; set; } = 60;
}
