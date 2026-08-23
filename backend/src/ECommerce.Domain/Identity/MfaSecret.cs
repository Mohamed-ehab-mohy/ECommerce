using ECommerce.Domain.Common;

namespace ECommerce.Domain.Identity;

public sealed class MfaSecret : BaseEntity<Guid>
{
    private MfaSecret() { }

    public Guid CustomerId { get; private set; }
    public string SecretKey { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public DateTime? EnabledAt { get; private set; }
    public int FailedAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    public static MfaSecret Create(Guid customerId, string secretKey, DateTime utcNow)
    {
        return new MfaSecret
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            SecretKey = secretKey,
            IsEnabled = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Enable(DateTime utcNow)
    {
        IsEnabled = true;
        EnabledAt = utcNow;
        FailedAttempts = 0;
        UpdatedAt = utcNow;
    }

    public void Disable(DateTime utcNow)
    {
        IsEnabled = false;
        UpdatedAt = utcNow;
    }

    public void RecordFailedAttempt(DateTime utcNow)
    {
        FailedAttempts++;
        UpdatedAt = utcNow;
    }

    public void ResetFailedAttempts(DateTime utcNow)
    {
        FailedAttempts = 0;
        UpdatedAt = utcNow;
    }
}
