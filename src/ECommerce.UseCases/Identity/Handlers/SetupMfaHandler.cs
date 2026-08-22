using ECommerce.Domain.Identity;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class SetupMfaHandler(
    IUserRepository users,
    IMfaSecretRepository mfaSecrets,
    IMfaService mfaService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<SetupMfaCommand, Result<MfaSetupResponse>>
{
    public async Task<Result<MfaSetupResponse>> Handle(SetupMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.CustomerId, cancellationToken);
        if (user is null)
        {
            return Result<MfaSetupResponse>.Failure(new Error("User.NotFound", "User not found", ErrorType.NotFound));
        }

        var secretKey = mfaService.GenerateSecretKey();
        var issuer = "ECommerce";
        var totpUri = mfaService.GetTotpUri(secretKey, user.Email, issuer);
        var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(totpUri)}";

        var mfaSecret = MfaSecret.Create(request.CustomerId, secretKey, timeProvider.GetUtcNow().UtcDateTime);
        mfaSecrets.Add(mfaSecret);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<MfaSetupResponse>.Success(new MfaSetupResponse(secretKey, totpUri, qrCodeUrl));
    }
}
