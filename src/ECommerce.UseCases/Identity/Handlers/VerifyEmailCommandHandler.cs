using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class VerifyEmailCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<VerifyEmailCommand> validator) : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var tokenHash = VerificationTokens.Hash(request.Token);

        var customer = await users.GetByVerificationTokenHashAsync(tokenHash, cancellationToken);
        if (customer is null)
        {
            return Result.Failure(CustomerErrors.VerificationTokenInvalid);
        }

        var result = customer.VerifyEmail(tokenHash, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
