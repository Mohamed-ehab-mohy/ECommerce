
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Partners;

public sealed record ListPartnerApiKeysQuery(Guid PartnerId) : IRequest<Result<IReadOnlyList<PartnerApiKeyDto>>>, IRequirePermission
{
    public string Permission => Permissions.PartnersWrite;
}

public sealed class ListPartnerApiKeysHandler : IRequestHandler<ListPartnerApiKeysQuery, Result<IReadOnlyList<PartnerApiKeyDto>>>
{
    private readonly IPartnerRepository _repo;

    public ListPartnerApiKeysHandler(IPartnerRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<PartnerApiKeyDto>>> Handle(
        ListPartnerApiKeysQuery request,
        CancellationToken cancellationToken)
    {
        var keys = await _repo.ListApiKeysByPartnerAsync(request.PartnerId, cancellationToken);

        return Result<IReadOnlyList<PartnerApiKeyDto>>.Success(
            keys.Select(k => new PartnerApiKeyDto
            {
                Id = k.Id,
                PartnerId = k.PartnerId,
                Name = k.Name,
                Scopes = [.. k.Scopes],
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt
            }).ToList());
    }
}
