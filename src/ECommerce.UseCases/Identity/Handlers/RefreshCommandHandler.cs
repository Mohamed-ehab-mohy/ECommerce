using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class RefreshCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    TokenPairFactory tokenPairFactory,
    IValidator<RefreshCommand> validator) : IRequestHandler<RefreshCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<LoginResult>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var token = await refreshTokens.GetByTokenHashAsync(
            RefreshTokens.Hash(request.RefreshToken),
            cancellationToken);

        if (token is null || !token.CanBeUsed(utcNow))
        {
            return Result<LoginResult>.Failure(AuthErrors.RefreshTokenInvalid);
        }

        var revoked = await refreshTokens.TryRevokeAsync(token.Id, utcNow, cancellationToken);
        if (revoked == 0)
        {
            await refreshTokens.RevokeFamilyAsync(token.FamilyId, utcNow, cancellationToken);
            return Result<LoginResult>.Failure(AuthErrors.RefreshTokenReused);
        }

        var customer = await users.GetByIdAsync(token.UserId, cancellationToken);
        if (customer is null)
        {
            return Result<LoginResult>.Failure(AuthErrors.RefreshTokenInvalid);
        }

        var roles = await users.GetRolesAsync(token.UserId, cancellationToken);
        var permissions = await users.GetPermissionsAsync(token.UserId, cancellationToken);
        var pair = tokenPairFactory.Issue(customer, roles, permissions, token.DeviceId, token.FamilyId);
        token.Revoke(pair.RefreshToken.Id, utcNow);
        refreshTokens.Add(pair.RefreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResult>.Success(pair.Result);
    }
}
