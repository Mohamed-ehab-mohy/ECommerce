using ECommerce.Domain.Partners;

namespace ECommerce.UseCases.Partners;

public sealed record CreatePartnerApiKeyCommand(
    Guid PartnerId,
    CreatePartnerApiKeyRequest Request) : IRequest<Result<PartnerApiKeyDto>>;

public sealed class CreatePartnerApiKeyHandler : IRequestHandler<CreatePartnerApiKeyCommand, Result<PartnerApiKeyDto>>
{
    private readonly IPartnerRepository _repo;

    public CreatePartnerApiKeyHandler(IPartnerRepository repo) => _repo = repo;

    public async Task<Result<PartnerApiKeyDto>> Handle(
        CreatePartnerApiKeyCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _repo.GetByIdAsync(request.PartnerId, cancellationToken);
        if (account is null || !account.IsActive)
            return Result<PartnerApiKeyDto>.Failure(new Error("Partner.NotFound", "Partner account not found or inactive", ErrorType.NotFound));

        var rawKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var keyHash = ApiKeyHasher.Hash(rawKey);
        var expiresAt = request.Request.ExpiresInDays is { } days
            ? DateTime.UtcNow.AddDays(days)
            : (DateTime?)null;

        var apiKey = PartnerApiKey.Create(
            request.PartnerId,
            keyHash,
            request.Request.Name,
            request.Request.Scopes,
            expiresAt,
            DateTime.UtcNow);

        await _repo.CreateApiKeyAsync(apiKey, cancellationToken);

        return Result<PartnerApiKeyDto>.Success(new PartnerApiKeyDto
        {
            Id = apiKey.Id,
            PartnerId = apiKey.PartnerId,
            Name = apiKey.Name,
            Scopes = [.. apiKey.Scopes],
            IsActive = apiKey.IsActive,
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt
        });
    }
}

public static class ApiKeyHasher
{
    public static string Hash(string key)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
