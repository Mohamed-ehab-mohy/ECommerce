using ECommerce.Domain.Common;

namespace ECommerce.Domain.Partners;

public sealed class PartnerApiKey : BaseEntity<Guid>
{
    private readonly List<string> _scopes = [];

    private PartnerApiKey()
    {
        KeyHash = string.Empty;
        Name = string.Empty;
    }

    public Guid PartnerId { get; private set; }

    public string KeyHash { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyCollection<string> Scopes => _scopes;

    public bool IsActive { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public DateTime? LastUsedAt { get; private set; }

    public static PartnerApiKey Create(
        Guid partnerId,
        string keyHash,
        string name,
        IReadOnlyCollection<string> scopes,
        DateTime? expiresAt,
        DateTime utcNow)
    {
        var apiKey = new PartnerApiKey
        {
            Id = Guid.NewGuid(),
            PartnerId = partnerId,
            KeyHash = keyHash,
            Name = name,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        apiKey._scopes.AddRange(scopes.Distinct(StringComparer.OrdinalIgnoreCase));

        return apiKey;
    }

    public void RecordUsage(DateTime utcNow)
    {
        LastUsedAt = utcNow;
    }

    public void Revoke(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAt = utcNow;
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAt = utcNow;
    }

    public void Rotate(string newKeyHash, DateTime utcNow)
    {
        KeyHash = newKeyHash;
        UpdatedAt = utcNow;
    }
}
