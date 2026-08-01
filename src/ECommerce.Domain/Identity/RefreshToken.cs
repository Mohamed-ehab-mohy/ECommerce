using ECommerce.Domain.Common;

namespace ECommerce.Domain.Identity;

public sealed class RefreshToken : BaseEntity<Guid>
{
    private RefreshToken()
    {
        DeviceId = string.Empty;
        TokenHash = string.Empty;
    }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string DeviceId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? ReplacedById { get; private set; }

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc < utcNow;

    public bool CanBeUsed(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Create(
        Guid userId,
        Guid familyId,
        string deviceId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime utcNow)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId,
            DeviceId = deviceId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Revoke(Guid? replacedById, DateTime utcNow)
    {
        RevokedAtUtc = utcNow;
        ReplacedById = replacedById;
        UpdatedAt = utcNow;
    }
}
