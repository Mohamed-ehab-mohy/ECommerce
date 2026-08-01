using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class AddAddressCommandHandler(
    IUserRepository users,
    IAddressRepository addresses,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<AddAddressCommand> validator) : IRequestHandler<AddAddressCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddAddressCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<Guid>();
        }

        if (await users.GetByIdAsync(request.CustomerId, cancellationToken) is null)
        {
            return Result<Guid>.Failure(CustomerErrors.CustomerNotFound);
        }

        var address = CustomerAddress.Create(
            request.CustomerId,
            request.Label,
            request.Street.Trim(),
            request.City.Trim(),
            request.Region,
            request.Country.Trim().ToUpperInvariant(),
            request.PostalCode,
            timeProvider.GetUtcNow().UtcDateTime);

        addresses.Add(address);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(address.Id);
    }
}
