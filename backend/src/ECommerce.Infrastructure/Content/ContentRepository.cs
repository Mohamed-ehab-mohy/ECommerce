using ECommerce.Domain.Content;
using ECommerce.UseCases.Content.Ports;

namespace ECommerce.Infrastructure.Content;

internal sealed class ContentRepository : IContentRepository
{
    private readonly List<Banner> _banners = new(); // In-memory for now to satisfy the tests

    public Task AddBannerAsync(Banner banner, CancellationToken cancellationToken = default)
    {
        _banners.Add(banner);
        return Task.CompletedTask;
    }

    public Task<Banner?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var banner = _banners.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(banner);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
