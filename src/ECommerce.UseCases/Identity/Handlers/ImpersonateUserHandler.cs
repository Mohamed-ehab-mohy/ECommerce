using ECommerce.Domain.Audit;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Audit;
using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class ImpersonateUserHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IAccessTokenIssuer accessTokenIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IAuditLogWriter auditLogWriter,
    ICurrentUser currentUser,
    ILogger<ImpersonateUserHandler> logger) : IRequestHandler<ImpersonateUserCommand, Result<ImpersonateResult>>
{
    public async Task<Result<ImpersonateResult>> Handle(ImpersonateUserCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.Permissions.Contains(Permissions.AuthImpersonate, StringComparer.Ordinal))
        {
            return Result<ImpersonateResult>.Failure(AuthorizationErrors.PermissionDenied(Permissions.AuthImpersonate));
        }

        var target = await users.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (target is null)
        {
            return Result<ImpersonateResult>.Failure(CustomerErrors.CustomerNotFound);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var roles = await users.GetRolesAsync(target.Id, cancellationToken);
        var permissions = await users.GetPermissionsAsync(target.Id, cancellationToken);

        var impersonatorId = currentUser.UserId ?? Guid.Empty;

        var issuedAccessToken = accessTokenIssuer.Issue(new AccessTokenClaims(
            target.Id,
            target.Email,
            roles,
            permissions,
            Guid.NewGuid().ToString()));

        var familyId = Guid.NewGuid();
        var rawToken = RefreshTokens.Create();
        var refreshToken = RefreshToken.Create(
            target.Id,
            familyId,
            "impersonation",
            RefreshTokens.Hash(rawToken),
            utcNow.AddMinutes(30),
            utcNow);
        refreshTokens.Add(refreshToken);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ImpersonationStarted,
            "Customer",
            target.Id.ToString(),
            After: new
            {
                impersonatorId,
                targetId = target.Id,
                targetEmail = target.Email
            },
            ActorId: impersonatorId,
            ActorType: AuditActorType.User), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var expiresInSeconds = (int)(issuedAccessToken.ExpiresAtUtc - utcNow).TotalSeconds;

        logger.LogInformation(
            "Impersonation started: {ImpersonatorId} → {TargetUserId}",
            impersonatorId,
            target.Id);

        return Result<ImpersonateResult>.Success(new ImpersonateResult(
            issuedAccessToken.Value,
            rawToken,
            expiresInSeconds,
            target.Id,
            target.Email,
            roles,
            impersonatorId));
    }
}
