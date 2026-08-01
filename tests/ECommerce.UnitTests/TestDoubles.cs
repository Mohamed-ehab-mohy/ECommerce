using ECommerce.Domain.Identity;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UnitTests;

internal sealed class FakeUserRepository : IUserRepository
{
    public List<Customer> Customers { get; } = [];

    public Customer? ExistingByEmail { get; set; }

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingByEmail);

    public Task<Customer?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.VerificationTokenHash == tokenHash));

    public void Add(Customer customer) => Customers.Add(customer);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hash:{password}";

    public bool Verify(string password, string hash) => hash == $"hash:{password}";
}

internal sealed class FakeBreachChecker(bool breached) : IPasswordBreachChecker
{
    public Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken) =>
        Task.FromResult(breached);
}

internal sealed class CapturingEmailSender : IEmailSender
{
    public List<EmailMessage> Messages { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
