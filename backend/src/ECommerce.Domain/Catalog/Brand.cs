using ECommerce.Domain.Common;

namespace ECommerce.Domain.Catalog;

public sealed class Brand : BaseEntity<Guid>
{
    private Brand()
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public string? Website { get; private set; }

    public static Brand Create(string name, string? description, string? website, DateTime utcNow)
    {
        return new Brand
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Website = website,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void UpdateDetails(string name, string? description, string? website, DateTime utcNow)
    {
        Name = name;
        Description = description;
        Website = website;
        UpdatedAt = utcNow;
    }
}
