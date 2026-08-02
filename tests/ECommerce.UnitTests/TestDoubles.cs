using System.Reflection;
using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Audit;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UnitTests;

internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Products { get; } = [];

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Products.FirstOrDefault(product => product.Id == id));

    public Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Products.FirstOrDefault(
            product => product.Id == id && product.Status == ProductStatus.Active && !product.IsDeleted));

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        Task.FromResult(Products.Any(product => product.Sku == sku));

    public Task<bool> SlugExistsAsync(string slug, Guid excludeProductId, CancellationToken cancellationToken) =>
        Task.FromResult(Products.Any(product => product.Slug == slug && product.Id != excludeProductId));

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        Task.FromResult(Products.Any(product => product.Slug == slug));

    public Task<IReadOnlyList<Product>> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>(Products
            .Where(product => product.Status == ProductStatus.Active && !product.IsDeleted)
            .OrderBy(product => product.Slug)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Products.Count(
            product => product.Status == ProductStatus.Active && !product.IsDeleted));

    public void Add(Product product) => Products.Add(product);
}

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

internal sealed class FakeAuditEntryRepository : IAuditEntryRepository
{
    private static readonly PropertyInfo IdProperty = typeof(AuditEntry).GetProperty(nameof(AuditEntry.Id))!;

    private long _nextId = 1;

    public List<AuditEntry> Entries { get; } = [];

    public Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        IdProperty.SetValue(entry, _nextId++);
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<string?> GetLatestHashAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Entries.Count == 0 ? null : Entries[^1].Hash);

    public Task<IReadOnlyList<AuditEntry>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        var items = Entries
            .Where(entry => Matches(entry, query))
            .OrderByDescending(entry => entry.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditEntry>>(items);
    }

    public Task<int> CountAsync(AuditLogQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.Count(entry => Matches(entry, query)));

    private static bool Matches(AuditEntry entry, AuditLogQuery query) =>
        (query.ActorId is not { } actorId || entry.ActorId == actorId) &&
        (string.IsNullOrWhiteSpace(query.Action) || string.Equals(entry.Action, query.Action, StringComparison.Ordinal)) &&
        (string.IsNullOrWhiteSpace(query.EntityType) || string.Equals(entry.EntityType, query.EntityType, StringComparison.Ordinal)) &&
        (string.IsNullOrWhiteSpace(query.EntityId) || string.Equals(entry.EntityId, query.EntityId, StringComparison.Ordinal)) &&
        (query.From is not { } from || entry.OccurredAt >= from) &&
        (query.To is not { } to || entry.OccurredAt <= to);
}

internal sealed class FakeAuditContextProvider(Guid? actorId = null, string? ip = "203.0.113.1") : IAuditContextProvider
{
    public Guid? ActorId { get; set; } = actorId;

    public AuditContext Get() => new(ActorId, AuditActorType.User, ip, "test-agent", "trace-1");
}
