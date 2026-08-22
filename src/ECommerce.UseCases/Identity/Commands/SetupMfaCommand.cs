using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record SetupMfaCommand(Guid CustomerId) : IRequest<Result<MfaSetupResponse>>;

public sealed record MfaSetupResponse(string SecretKey, string TotpUri, string QrCodeUrl);
