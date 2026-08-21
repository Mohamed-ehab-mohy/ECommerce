using ECommerce.Domain.Common;

namespace ECommerce.Domain.Partners;

public sealed class PartnerAccount : BaseEntity<Guid>
{
    private PartnerAccount()
    {
        Name = string.Empty;
        Email = string.Empty;
    }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public int RateLimitPerMinute { get; private set; } = 60;

    public bool IsActive { get; private set; }

    public static PartnerAccount Create(string name, string email, int rateLimitPerMinute, DateTime utcNow)
    {
        return new PartnerAccount
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            RateLimitPerMinute = rateLimitPerMinute,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void UpdateDetails(string name, string email, int rateLimitPerMinute, DateTime utcNow)
    {
        Name = name;
        Email = email;
        RateLimitPerMinute = rateLimitPerMinute;
        UpdatedAt = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAt = utcNow;
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAt = utcNow;
    }
}
