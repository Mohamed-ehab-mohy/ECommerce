using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class DeleteAddressCommandHandler(
    IAddressRepository addresses,
    IUnitOfWork unitOfWork,
    IValidator<DeleteAddressCommand> validator) : IRequestHandler<DeleteAddressCommand, Result>
{
    public async Task<Result> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var address = await addresses.GetByIdAndCustomerIdAsync(request.AddressId, request.CustomerId, cancellationToken);
        if (address is null)
        {
            return Result.Failure(AddressErrors.AddressNotFound);
        }

        addresses.Remove(address);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
