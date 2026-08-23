using ECommerce.Domain.Audit;
using ECommerce.Domain.Identity;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class UpdateProfileCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<UpdateProfileCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdateProfileCommand, Result>
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

        var before = new { customer.DisplayName, customer.Phone, customer.Locale, customer.Currency };

        customer.UpdateProfile(
            request.DisplayName?.Trim(),
            request.Phone?.Trim(),
            request.Locale?.Trim(),
            request.Currency?.Trim().ToUpperInvariant(),
            timeProvider.GetUtcNow().UtcDateTime);

        var after = new { customer.DisplayName, customer.Phone, customer.Locale, customer.Currency };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ProfileUpdated,
            "Customer",
            request.CustomerId.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
