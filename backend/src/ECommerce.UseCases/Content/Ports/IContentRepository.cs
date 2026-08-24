using ECommerce.Domain.Content;

namespace ECommerce.UseCases.Content.Ports;

public interface IContentRepository
{
    Task AddBannerAsync(Banner banner, CancellationToken cancellationToken = default);
    Task<Banner?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
