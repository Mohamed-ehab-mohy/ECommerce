using ECommerce.Domain.Partners;
using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Partners;

public sealed record CreatePartnerAccountCommand(
    string Name,
    string Email,
    int RateLimitPerMinute) : IRequest<Result<PartnerAccountDto>>;

public sealed class CreatePartnerAccountHandler : IRequestHandler<CreatePartnerAccountCommand, Result<PartnerAccountDto>>
{
    private readonly IPartnerRepository _repo;

    public CreatePartnerAccountHandler(IPartnerRepository repo) => _repo = repo;

    public async Task<Result<PartnerAccountDto>> Handle(
        CreatePartnerAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = PartnerAccount.Create(
            request.Name,
            request.Email,
            request.RateLimitPerMinute,
            DateTime.UtcNow);

        await _repo.CreateAccountAsync(account, cancellationToken);

        return Result<PartnerAccountDto>.Success(new PartnerAccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Email = account.Email,
            RateLimitPerMinute = account.RateLimitPerMinute,
            IsActive = account.IsActive,
            CreatedAt = account.CreatedAt
        });
    }
}
