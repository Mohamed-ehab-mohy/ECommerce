namespace ECommerce.UseCases.Partners;

public sealed class PartnerApiKey
{
    public Guid Id { get; init; }
    public Guid PartnerId { get; init; }
    public string KeyHash { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = [];
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
}

public sealed class PartnerAccount
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int RateLimitPerMinute { get; init; } = 60;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
