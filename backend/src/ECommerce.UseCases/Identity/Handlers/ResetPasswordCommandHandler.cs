using ECommerce.Domain.Identity;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class ResetPasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IPasswordBreachChecker breachChecker,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<ResetPasswordCommand> validator) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var customer = await users.GetByResetTokenHashAsync(
            VerificationTokens.Hash(request.Token),
            cancellationToken);
        if (customer is null)
        {
            return Result.Failure(CustomerErrors.PasswordResetTokenInvalid);
        }

        if (await breachChecker.IsBreachedAsync(request.NewPassword, cancellationToken))
        {
            return Result.Failure(CustomerErrors.BreachedPassword);
        }

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);
        var reVerifyToken = VerificationTokens.Create();

        var result = customer.ResetPassword(
            newPasswordHash,
            VerificationTokens.Hash(request.Token),
            reVerifyToken,
            VerificationTokens.Hash(reVerifyToken),
            utcNow.AddHours(24),
            utcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await refreshTokens.RevokeAllByUserAsync(customer.Id, utcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}
