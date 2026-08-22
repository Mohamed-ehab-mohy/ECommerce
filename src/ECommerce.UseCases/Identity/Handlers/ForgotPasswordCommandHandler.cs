using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<ForgotPasswordCommand> validator) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var email = request.Email.Trim().ToLowerInvariant();

        var customer = await users.GetByEmailAsync(email, cancellationToken);
        if (customer is not null)
        {
            var resetToken = VerificationTokens.Create();
            customer.IssuePasswordResetToken(
                VerificationTokens.Hash(resetToken),
                resetToken,
                utcNow.AddMinutes(30),
                utcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
