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

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == id));

    public Task<Customer?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.VerificationTokenHash == tokenHash));

    public Task<Customer?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.PasswordResetTokenHash == tokenHash));

    public void Add(Customer customer) => Customers.Add(customer);
}

internal sealed class FakeAddressRepository : IAddressRepository
{
    public List<CustomerAddress> Addresses { get; } = [];

    public Task<IReadOnlyList<CustomerAddress>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CustomerAddress>>(
            Addresses.Where(address => address.CustomerId == customerId).ToList());

    public Task<CustomerAddress?> GetByIdAndCustomerIdAsync(Guid id, Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(Addresses.FirstOrDefault(address => address.Id == id && address.CustomerId == customerId));

    public void Add(CustomerAddress address) => Addresses.Add(address);

    public void Remove(CustomerAddress address) => Addresses.Remove(address);
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public int RevokeFamilyCalls { get; private set; }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    public Task<int> RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken)
    {
        RevokeFamilyCalls++;
        var count = 0;

        foreach (var token in Tokens.Where(token => token.FamilyId == familyId && !token.IsRevoked))
        {
            token.Revoke(null, utcNow);
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<int> RevokeAllByUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var token in Tokens.Where(token => token.UserId == userId && !token.IsRevoked))
        {
            token.Revoke(null, utcNow);
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<int> TryRevokeAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var token = Tokens.FirstOrDefault(candidate => candidate.Id == id && !candidate.IsRevoked);

        if (token is null)
        {
            return Task.FromResult(0);
        }

        token.Revoke(null, utcNow);
        return Task.FromResult(1);
    }

    public void Add(RefreshToken token) => Tokens.Add(token);
}

internal sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddMinutes(15);

    public int IssueCount { get; private set; }

    public IssuedAccessToken Issue(AccessTokenClaims claims)
    {
        IssueCount++;
        return new IssuedAccessToken($"access:{claims.UserId}:{claims.TokenId}", ExpiresAtUtc);
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        Task.FromResult<ITransaction>(new FakeTransaction());
}

internal sealed class FakeTransaction : ITransaction
{
    public int CommitCount { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        CommitCount++;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
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
