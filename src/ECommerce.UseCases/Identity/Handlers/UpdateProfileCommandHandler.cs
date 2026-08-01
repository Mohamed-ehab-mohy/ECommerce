using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class UpdateProfileCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<UpdateProfileCommand> validator) : IRequestHandler<UpdateProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var customer = await users.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Failure(CustomerErrors.CustomerNotFound);
        }

        customer.UpdateProfile(
            request.DisplayName?.Trim(),
            request.Phone?.Trim(),
            request.Locale?.Trim(),
            request.Currency?.Trim().ToUpperInvariant(),
            timeProvider.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
