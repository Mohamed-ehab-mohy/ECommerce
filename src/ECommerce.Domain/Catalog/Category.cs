using ECommerce.Domain.Common;

namespace ECommerce.Domain.Catalog;

public sealed class Category : BaseEntity<Guid>
{
    private Category()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public string Name { get; private set; }

    public string Slug { get; private set; }

    public Guid? ParentId { get; private set; }

    public int SortOrder { get; private set; }

    public int Level { get; private set; }

    public Category? Parent { get; private set; }

    public static Category Create(
        string name,
        string slug,
        Guid? parentId,
        int sortOrder,
        int level,
        DateTime utcNow)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            ParentId = parentId,
            SortOrder = sortOrder,
            Level = level,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void UpdateDetails(string name, string slug, int sortOrder, DateTime utcNow)
    {
        Name = name;
        Slug = slug;
        SortOrder = sortOrder;
        UpdatedAt = utcNow;
    }

    public void ChangeParent(Guid? parentId, int newLevel, DateTime utcNow)
    {
        ParentId = parentId;
        Level = newLevel;
        UpdatedAt = utcNow;
    }
}
