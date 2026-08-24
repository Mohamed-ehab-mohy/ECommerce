using ECommerce.Domain.Common;

namespace ECommerce.Domain.Content;

public sealed class Page : BaseEntity<Guid>
{
    private Page() { }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string HtmlContent { get; private set; } = string.Empty;
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public bool IsPublished { get; private set; }

    public static Page Create(Guid? tenantId, string title, string slug, string htmlContent, string? metaTitle, string? metaDescription, bool isPublished)
    {
        return new Page
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Slug = slug,
            HtmlContent = htmlContent,
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            IsPublished = isPublished,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string slug, string htmlContent, string? metaTitle, string? metaDescription, bool isPublished)
    {
        Title = title;
        Slug = slug;
        HtmlContent = htmlContent;
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        IsPublished = isPublished;
        UpdatedAt = DateTime.UtcNow;
    }
}
