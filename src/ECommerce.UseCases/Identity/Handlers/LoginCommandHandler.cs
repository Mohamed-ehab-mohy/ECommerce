using ECommerce.Domain.Audit;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Audit;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    AuthSettings settings,
    TimeProvider timeProvider,
    TokenPairFactory tokenPairFactory,
    IValidator<LoginCommand> validator,
    ILoginAttemptThrottler loginAttemptThrottler,
    IAuditLogWriter auditLogWriter) : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<LoginResult>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var email = request.Email.Trim().ToLowerInvariant();
        var clientIp = request.IpAddress.Trim();

        if (loginAttemptThrottler.IsBlocked(clientIp, utcNow, out var retryAfterSeconds))
        {
            return Result<LoginResult>.Failure(AuthErrors.TooManyAttempts(retryAfterSeconds));
        }

        var customer = await users.GetByEmailAsync(email, cancellationToken);
        if (customer is null)
        {
            return RejectWithRecordedFailure(clientIp, utcNow, AuthErrors.InvalidCredentials);
        }

        if (customer.IsLockedOut(utcNow))
        {
            return Result<LoginResult>.Failure(AuthErrors.AccountLocked(
                (int)customer.RemainingLockout(utcNow).TotalSeconds));
        }

        if (settings.RequireVerifiedEmail && !customer.EmailVerified)
        {
            return RejectWithRecordedFailure(clientIp, utcNow, CustomerErrors.EmailNotVerified);
        }

        if (!passwordHasher.Verify(request.Password, customer.PasswordHash))
        {
            loginAttemptThrottler.RecordFailure(clientIp, utcNow);
            customer.RecordFailedLogin(
                settings.MaxFailedLoginAttempts,
                TimeSpan.FromMinutes(settings.LockoutDurationMinutes),
                utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return loginAttemptThrottler.IsBlocked(clientIp, utcNow, out retryAfterSeconds)
                ? Result<LoginResult>.Failure(AuthErrors.TooManyAttempts(retryAfterSeconds))
                : customer.IsLockedOut(utcNow)
                    ? Result<LoginResult>.Failure(AuthErrors.AccountLocked(
                        (int)customer.RemainingLockout(utcNow).TotalSeconds))
                    : Result<LoginResult>.Failure(AuthErrors.InvalidCredentials);
        }

        loginAttemptThrottler.RecordSuccess(clientIp, utcNow);
        customer.RecordSuccessfulLogin(utcNow);

        var roles = await users.GetRolesAsync(customer.Id, cancellationToken);
        var permissions = await users.GetPermissionsAsync(customer.Id, cancellationToken);
        var pair = tokenPairFactory.Issue(customer, roles, permissions, request.DeviceId, Guid.NewGuid());
        refreshTokens.Add(pair.RefreshToken);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.Login,
            "Customer",
            customer.Id.ToString(),
            After: new { userId = customer.Id },
            ActorId: customer.Id,
            ActorType: AuditActorType.User), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResult>.Success(pair.Result);
    }

    private Result<LoginResult> RejectWithRecordedFailure(string clientIp, DateTime utcNow, Error failure)
    {
        loginAttemptThrottler.RecordFailure(clientIp, utcNow);

        return loginAttemptThrottler.IsBlocked(clientIp, utcNow, out var retryAfterSeconds)
            ? Result<LoginResult>.Failure(AuthErrors.TooManyAttempts(retryAfterSeconds))
            : Result<LoginResult>.Failure(failure);
    }
}
