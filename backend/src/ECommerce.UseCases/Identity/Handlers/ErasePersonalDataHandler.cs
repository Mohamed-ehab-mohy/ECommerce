using ECommerce.Domain.Audit;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Audit;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class ErasePersonalDataHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IAddressRepository addresses,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IAuditLogWriter auditLogWriter) : IRequestHandler<ErasePersonalDataCommand, Result>
{
    public async Task<Result> Handle(ErasePersonalDataCommand request, CancellationToken cancellationToken)
    {
        var customer = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (customer is null)
        {
            return Result.Failure(CustomerErrors.CustomerNotFound);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        customer.Anonymize(utcNow);

        await refreshTokens.RevokeAllByUserAsync(customer.Id, utcNow, cancellationToken);

        var customerAddresses = await addresses.GetByCustomerIdAsync(customer.Id, cancellationToken);
        foreach (var address in customerAddresses)
        {
            addresses.Remove(address);
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.AccountErased,
            "Customer",
            customer.Id.ToString(),
            After: new { customerId = customer.Id },
            ActorId: customer.Id,
            ActorType: AuditActorType.User), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
