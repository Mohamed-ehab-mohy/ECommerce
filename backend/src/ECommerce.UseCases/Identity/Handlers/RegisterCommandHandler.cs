using ECommerce.Domain.Identity;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class RegisterCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher passwordHasher,
    IPasswordBreachChecker breachChecker,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<RegisterCommand> validator) : IRequestHandler<RegisterCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<Guid>();
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(CustomerErrors.EmailAlreadyExists);
        }

        if (await breachChecker.IsBreachedAsync(request.Password, cancellationToken))
        {
            return Result<Guid>.Failure(CustomerErrors.BreachedPassword);
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var verificationToken = VerificationTokens.Create();
        var verificationTokenHash = VerificationTokens.Hash(verificationToken);
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddHours(24);

        var customer = Customer.Register(
            email,
            request.DisplayName.Trim(),
            request.Locale.Trim(),
            request.Currency.Trim().ToUpperInvariant(),
            passwordHash,
            verificationTokenHash,
            expiresAt,
            verificationToken);

        users.Add(customer);

        var customerRole = await roles.GetByNameAsync(IdentityRoles.Customer, cancellationToken);
        if (customerRole is not null)
        {
            users.AddRole(UserRole.Create(customer.Id, customerRole.Id));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(customer.Id);
    }
}
