namespace ECommerce.Domain.Content;

public sealed class CmsLayoutSection
{
    private CmsLayoutSection()
    {
        Title = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid LayoutId { get; private set; }

    public string Title { get; private set; }

    public CmsSectionType Type { get; private set; }

    public int DisplayOrder { get; private set; }

    public string? ConfigJson { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static CmsLayoutSection Create(
        Guid layoutId,
        string title,
        CmsSectionType type,
        int displayOrder,
        string? configJson,
        bool isActive,
        DateTime utcNow)
    {
        return new CmsLayoutSection
        {
            Id = Guid.NewGuid(),
            LayoutId = layoutId,
            Title = title,
            Type = type,
            DisplayOrder = displayOrder,
            ConfigJson = configJson,
            IsActive = isActive,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAt = utcNow;
    }
}
