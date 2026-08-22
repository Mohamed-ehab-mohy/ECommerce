using ECommerce.Domain.Audit;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class DeleteAddressCommandHandler(
    IAddressRepository addresses,
    IUnitOfWork unitOfWork,
    IValidator<DeleteAddressCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<DeleteAddressCommand, Result>
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

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.AddressRemoved,
            "CustomerAddress",
            address.Id.ToString(),
            Before: new
            {
                address.Label,
                address.Street,
                address.City,
                address.Region,
                address.Country,
                address.PostalCode
            }), cancellationToken);

        addresses.Remove(address);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
