using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class PasswordTokenHandler(
    IOAuthClientValidator clientValidator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    ILoginAttemptThrottler loginAttemptThrottler,
    IUnitOfWork unitOfWork,
    AuthSettings settings,
    TimeProvider timeProvider) : IRequestHandler<PasswordTokenCommand, Result<OAuthTokenResult>>
{
    public async Task<Result<OAuthTokenResult>> Handle(PasswordTokenCommand request, CancellationToken cancellationToken)
    {
        var validation = await clientValidator.ValidateAsync(request.ClientId, request.ClientSecret, cancellationToken);
        if (validation is null || !validation.IsValid)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidClient);
        }

        if (!validation.AllowedGrantTypes.Contains("password"))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.UnauthorizedGrantType);
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidGrant);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var clientIp = string.IsNullOrWhiteSpace(request.IpAddress) ? "unknown" : request.IpAddress.Trim();
        var email = request.Username.Trim().ToLowerInvariant();

        if (loginAttemptThrottler.IsBlocked(clientIp, utcNow, out var retryAfterSeconds))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.TooManyAttempts(retryAfterSeconds));
        }

        var customer = await users.GetByEmailAsync(email, cancellationToken);
        if (customer is null || !passwordHasher.Verify(request.Password, customer.PasswordHash))
        {
            loginAttemptThrottler.RecordFailure(clientIp, utcNow);

            if (customer is not null)
            {
                customer.RecordFailedLogin(
                    settings.MaxFailedLoginAttempts,
                    TimeSpan.FromMinutes(settings.LockoutDurationMinutes),
                    utcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return loginAttemptThrottler.IsBlocked(clientIp, utcNow, out retryAfterSeconds)
                ? Result<OAuthTokenResult>.Failure(OAuthErrors.TooManyAttempts(retryAfterSeconds))
                : customer is not null && customer.IsLockedOut(utcNow)
                    ? Result<OAuthTokenResult>.Failure(OAuthErrors.AccountLocked(
                        (int)customer.RemainingLockout(utcNow).TotalSeconds))
                    : Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidCredentials);
        }

        if (customer.IsLockedOut(utcNow))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.AccountLocked(
                (int)customer.RemainingLockout(utcNow).TotalSeconds));
        }

        if (!customer.EmailVerified)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidGrant);
        }

        loginAttemptThrottler.RecordSuccess(clientIp, utcNow);

        var requestedScopes = ParseScopes(request.Scope);
        var allowedScopes = requestedScopes
            .Where(s => validation.AllowedScopes.Contains(s))
            .ToList();

        if (allowedScopes.Count == 0)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidScope);
        }

        var roles = await users.GetRolesAsync(customer.Id, cancellationToken);
        var permissions = await users.GetPermissionsAsync(customer.Id, cancellationToken);

        var claims = new AccessTokenClaims(
            customer.Id,
            customer.Email,
            roles,
            permissions,
            Guid.NewGuid().ToString(),
            customer.TenantId);

        var issued = accessTokenIssuer.Issue(claims);
        var expiresInSeconds = (int)(issued.ExpiresAtUtc - timeProvider.GetUtcNow().UtcDateTime).TotalSeconds;

        return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
            issued.Value,
            "Bearer",
            expiresInSeconds,
            string.Join(' ', allowedScopes)));
    }

    private static List<string> ParseScopes(string? scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
