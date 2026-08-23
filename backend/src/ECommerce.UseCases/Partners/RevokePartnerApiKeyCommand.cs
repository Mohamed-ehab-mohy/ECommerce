
namespace ECommerce.UseCases.Partners;

public sealed record RevokePartnerApiKeyCommand(Guid ApiKeyId) : IRequest<Result>;

public sealed class RevokePartnerApiKeyHandler : IRequestHandler<RevokePartnerApiKeyCommand, Result>
{
    private readonly IPartnerRepository _repo;

    public RevokePartnerApiKeyHandler(IPartnerRepository repo) => _repo = repo;

    public async Task<Result> Handle(RevokePartnerApiKeyCommand request, CancellationToken cancellationToken)
    {
        var key = await _repo.GetApiKeyByIdAsync(request.ApiKeyId, cancellationToken);
        if (key is null)
            return Result.Failure(new Error("PartnerApiKey.NotFound", "API key not found", ErrorType.NotFound));

        key.Revoke(DateTime.UtcNow);
        await _repo.UpdateApiKeyAsync(key, cancellationToken);

        return Result.Success();
    }
}
