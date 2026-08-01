using ECommerce.Domain.Identity;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Identity;

public sealed class UserRepository(ECommerceDbContext dbContext) : IUserRepository
{
    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Set<Customer>().SingleOrDefaultAsync(customer => customer.Email == email, cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Customer>().SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<Customer?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.Set<Customer>().SingleOrDefaultAsync(customer => customer.VerificationTokenHash == tokenHash, cancellationToken);

    public Task<Customer?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.Set<Customer>().SingleOrDefaultAsync(customer => customer.PasswordResetTokenHash == tokenHash, cancellationToken);

    public void Add(Customer customer) => dbContext.Set<Customer>().Add(customer);
}
