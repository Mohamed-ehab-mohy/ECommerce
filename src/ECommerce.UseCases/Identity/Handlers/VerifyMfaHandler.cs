using ECommerce.Domain.Identity;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class VerifyMfaHandler(
    IMfaSecretRepository mfaSecrets,
    IMfaService mfaService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<VerifyMfaCommand, Result>
{
    public async Task<Result> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        var mfa = await mfaSecrets.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (mfa is null)
        {
            return Result.Failure(new Error("Mfa.NotSetup", "MFA not configured", ErrorType.NotFound));
        }

        if (!mfa.IsEnabled)
        {
            return Result.Failure(new Error("Mfa.NotEnabled", "MFA not enabled yet", ErrorType.Validation));
        }

        if (mfa.LockedUntil.HasValue && mfa.LockedUntil.Value > timeProvider.GetUtcNow().UtcDateTime)
        {
            return Result.Failure(new Error("Mfa.Locked", "Too many failed attempts", ErrorType.Locked));
        }

        if (!mfaService.VerifyTotp(mfa.SecretKey, request.Code))
        {
            mfa.RecordFailedAttempt(timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(new Error("Mfa.InvalidCode", "Invalid verification code", ErrorType.Unauthorized));
        }

        if (!mfa.IsEnabled)
        {
            mfa.Enable(timeProvider.GetUtcNow().UtcDateTime);
        }

        mfa.ResetFailedAttempts(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
