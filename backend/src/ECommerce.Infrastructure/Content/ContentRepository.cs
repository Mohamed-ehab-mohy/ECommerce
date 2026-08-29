using ECommerce.Domain.Content;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Content.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Content;

public sealed class ContentRepository(ECommerceDbContext dbContext) : IContentRepository
{
    public Task<Banner?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Banner>().SingleOrDefaultAsync(banner => banner.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Banner>> ListBannersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<Banner>()
            .Where(banner => banner.IsActive)
            .OrderBy(banner => banner.DisplayOrder)
            .ThenBy(banner => banner.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountBannersAsync(CancellationToken cancellationToken) =>
        dbContext.Set<Banner>().CountAsync(banner => banner.IsActive, cancellationToken);

    public void AddBanner(Banner banner) => dbContext.Set<Banner>().Add(banner);

    public Task<Page?> GetPageByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Page>().SingleOrDefaultAsync(page => page.Id == id, cancellationToken);

    public Task<Page?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Page>().SingleOrDefaultAsync(page => page.Slug == slug, cancellationToken);

    public Task<Page?> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Page>().SingleOrDefaultAsync(page => page.Slug == slug && page.IsPublished, cancellationToken);

    public async Task<IReadOnlyList<Page>> ListPagesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<Page>()
            .Where(page => page.IsPublished)
            .OrderBy(page => page.Slug)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountPagesAsync(CancellationToken cancellationToken) =>
        dbContext.Set<Page>().CountAsync(page => page.IsPublished, cancellationToken);

    public void AddPage(Page page) => dbContext.Set<Page>().Add(page);

    public Task<CmsLayout?> GetLayoutByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<CmsLayout>()
            .Include(layout => layout.Sections)
            .SingleOrDefaultAsync(layout => layout.Id == id, cancellationToken);

    public Task<CmsLayout?> GetLayoutBySlugAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<CmsLayout>()
            .Include(layout => layout.Sections)
            .SingleOrDefaultAsync(layout => layout.Slug == slug, cancellationToken);

    public Task<CmsLayout?> GetActiveLayoutBySlugAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<CmsLayout>()
            .Include(layout => layout.Sections)
            .SingleOrDefaultAsync(layout => layout.Slug == slug && layout.IsActive, cancellationToken);

    public async Task<IReadOnlyList<CmsLayout>> ListLayoutsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<CmsLayout>()
            .Where(layout => layout.IsActive)
            .Include(layout => layout.Sections)
            .OrderBy(layout => layout.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountLayoutsAsync(CancellationToken cancellationToken) =>
        dbContext.Set<CmsLayout>().CountAsync(layout => layout.IsActive, cancellationToken);

    public void AddLayout(CmsLayout layout) => dbContext.Set<CmsLayout>().Add(layout);
}
