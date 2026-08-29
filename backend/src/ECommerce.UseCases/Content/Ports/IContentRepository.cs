using ECommerce.Domain.Content;

namespace ECommerce.UseCases.Content.Ports;

public interface IContentRepository
{
    Task<Banner?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Banner>> ListBannersAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountBannersAsync(CancellationToken cancellationToken);

    void AddBanner(Banner banner);

    Task<Page?> GetPageByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Page?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<Page?> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<Page>> ListPagesAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountPagesAsync(CancellationToken cancellationToken);

    void AddPage(Page page);

    Task<CmsLayout?> GetLayoutByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CmsLayout?> GetLayoutBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<CmsLayout?> GetActiveLayoutBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<CmsLayout>> ListLayoutsAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountLayoutsAsync(CancellationToken cancellationToken);

    void AddLayout(CmsLayout layout);
}
