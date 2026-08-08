using ECommerce.Domain.Common;

namespace ECommerce.Domain.Flags;

public sealed class FeatureFlag : BaseEntity<Guid>
{
    private FeatureFlag()
    {
        Description = string.Empty;
    }

    public string Key { get; private set; } = string.Empty;

    public string Description { get; private set; }

    public bool Enabled { get; private set; }

    public static FeatureFlag Create(string key, string description, bool enabled, DateTime utcNow)
    {
        return new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key.Trim(),
            Description = description.Trim(),
            Enabled = enabled,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public static FeatureFlag Rehydrate(string key, string description, bool enabled)
    {
        return new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key,
            Description = description,
            Enabled = enabled,
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue
        };
    }

    public void SetEnabled(bool enabled, DateTime utcNow)
    {
        Enabled = enabled;
        UpdatedAt = utcNow;
    }
}
