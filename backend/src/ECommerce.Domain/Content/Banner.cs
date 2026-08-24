using ECommerce.Domain.Common;

namespace ECommerce.Domain.Content;

public sealed class Banner : BaseEntity<Guid>
{
    private Banner() { }

    public string Title { get; private set; } = string.Empty;
    public string ImageUrl { get; private set; } = string.Empty;
    public string? TargetUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public static Banner Create(Guid? tenantId, string title, string imageUrl, string? targetUrl, int displayOrder, bool isActive)
    {
        return new Banner
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            ImageUrl = imageUrl,
            TargetUrl = targetUrl,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string imageUrl, string? targetUrl, int displayOrder, bool isActive)
    {
        Title = title;
        ImageUrl = imageUrl;
        TargetUrl = targetUrl;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
