using ECommerce.Domain.Common;

namespace ECommerce.Domain.Content;

public sealed class CmsLayout : BaseEntity<Guid>
{
    private readonly List<CmsLayoutSection> _sections = [];

    private CmsLayout()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public string Name { get; private set; }

    public string Slug { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<CmsLayoutSection> Sections => _sections;

    public static CmsLayout Create(
        Guid? tenantId,
        string name,
        string slug,
        bool isActive,
        DateTime utcNow)
    {
        return new CmsLayout
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Slug = slug,
            IsActive = isActive,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Update(
        string name,
        string slug,
        bool isActive,
        DateTime utcNow)
    {
        Name = name;
        Slug = slug;
        IsActive = isActive;
        UpdatedAt = utcNow;
    }

    public void ReplaceSections(IReadOnlyList<CmsLayoutSection> sections)
    {
        _sections.Clear();

        foreach (var section in sections)
        {
            _sections.Add(section);
        }
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
