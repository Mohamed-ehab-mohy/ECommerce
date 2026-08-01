using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
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
    IValidator<LoginCommand> validator) : IRequestHandler<LoginCommand, Result<LoginResult>>
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

        var customer = await users.GetByEmailAsync(email, cancellationToken);
        if (customer is null)
        {
            return Result<LoginResult>.Failure(AuthErrors.InvalidCredentials);
        }

        if (customer.IsLockedOut(utcNow))
        {
            return Result<LoginResult>.Failure(AuthErrors.AccountLocked(
                (int)customer.RemainingLockout(utcNow).TotalSeconds));
        }

        if (settings.RequireVerifiedEmail && !customer.EmailVerified)
        {
            return Result<LoginResult>.Failure(CustomerErrors.EmailNotVerified);
        }

        if (!passwordHasher.Verify(request.Password, customer.PasswordHash))
        {
            customer.RecordFailedLogin(
                settings.MaxFailedLoginAttempts,
                TimeSpan.FromMinutes(settings.LockoutDurationMinutes),
                utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return customer.IsLockedOut(utcNow)
                ? Result<LoginResult>.Failure(AuthErrors.AccountLocked(
                    (int)customer.RemainingLockout(utcNow).TotalSeconds))
                : Result<LoginResult>.Failure(AuthErrors.InvalidCredentials);
        }

        customer.RecordSuccessfulLogin(utcNow);

        var pair = tokenPairFactory.Issue(customer, request.DeviceId, Guid.NewGuid());
        refreshTokens.Add(pair.RefreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResult>.Success(pair.Result);
    }
}
