namespace ECommerce.UseCases.Partners;

public sealed class PartnerAuthResult
{
    public bool IsAuthenticated { get; init; }
    public bool IsExpired { get; init; }
    public bool IsRateLimited { get; init; }
    public int RateLimitRemaining { get; init; }
    public int RateLimitPerMinute { get; init; }
    public Guid PartnerId { get; init; }
    public Guid ApiKeyId { get; init; }
    public string PartnerName { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = [];
}

public interface IPartnerAuthService
{
    Task<PartnerAuthResult> AuthenticateAsync(string keyHash, CancellationToken cancellationToken);
    Task RecordUsageAsync(Guid apiKeyId, CancellationToken cancellationToken);
}
