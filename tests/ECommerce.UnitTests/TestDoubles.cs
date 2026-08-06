using System.Reflection;
using ECommerce.Domain.Audit;
using ECommerce.Domain.Cart;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Identity;
using ECommerce.Domain.Inventory;
using ECommerce.Shared.Audit;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Inventory.Ports;

namespace ECommerce.UnitTests;

internal sealed class FakeCartRepository : ICartRepository
{
    public List<Cart> Carts { get; } = [];

    public bool ThrowConcurrencyOnSave { get; set; }

    public Task<Cart?> ByOwnerKeyAsync(string ownerKey, CancellationToken cancellationToken) =>
        Task.FromResult(Carts.FirstOrDefault(cart => cart.OwnerKey == ownerKey));

    public Task SaveAsync(Cart cart, CancellationToken cancellationToken)
    {
        if (ThrowConcurrencyOnSave)
        {
            throw new CartConcurrencyException("Simulated concurrent modification.");
        }

        var existing = Carts.FirstOrDefault(candidate => candidate.OwnerKey == cart.OwnerKey);
        if (existing is null)
        {
            Carts.Add(cart);
            return Task.CompletedTask;
        }

        Carts.Remove(existing);
        Carts.Add(cart);
        return Task.CompletedTask;
    }
}

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

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> Categories { get; } = [];

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Categories.FirstOrDefault(category => category.Id == id));

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken) =>
        Task.FromResult(Categories.FirstOrDefault(category => category.Slug == slug));

    public Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Category>>(Categories.Where(category => !category.IsDeleted).ToList());

    public void Add(Category category) => Categories.Add(category);
}

internal sealed class FakeBrandRepository : IBrandRepository
{
    public List<Brand> Brands { get; } = [];

    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Brands.FirstOrDefault(brand => brand.Id == id));

    public Task<Brand?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Brands.FirstOrDefault(brand => brand.Name == name));

    public Task<IReadOnlyList<Brand>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Brand>>(Brands
            .Where(brand => !brand.IsDeleted)
            .OrderBy(brand => brand.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Brands.Count(brand => !brand.IsDeleted));

    public void Add(Brand brand) => Brands.Add(brand);
}

internal sealed class FakeWarehouseRepository : IWarehouseRepository
{
    public List<Warehouse> Warehouses { get; } = [];

    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Warehouses.FirstOrDefault(warehouse => warehouse.Id == id && !warehouse.IsDeleted));

    public Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Warehouses.FirstOrDefault(warehouse => warehouse.Code == code && !warehouse.IsDeleted));

    public Task<IReadOnlyList<Warehouse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Warehouse>>(Warehouses
            .Where(warehouse => !warehouse.IsDeleted)
            .OrderBy(warehouse => warehouse.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Warehouses.Count(warehouse => !warehouse.IsDeleted));

    public void Add(Warehouse warehouse) => Warehouses.Add(warehouse);
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<Customer> Customers { get; } = [];

    public Dictionary<Guid, List<string>> RolesByUser { get; } = [];

    public Dictionary<Guid, List<string>> PermissionsByUser { get; } = [];

    public Customer? ExistingByEmail { get; set; }

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingByEmail);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == id));

    public Task<Customer?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.VerificationTokenHash == tokenHash));

    public Task<Customer?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.PasswordResetTokenHash == tokenHash));

    public Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(RolesByUser.TryGetValue(userId, out var roles) ? roles : []);

    public Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(PermissionsByUser.TryGetValue(userId, out var perms) ? perms : []);

    public Task<IReadOnlyList<Customer>> SearchAsync(string? email, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Customer>>(Filtered(email)
            .OrderBy(customer => customer.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());

    public Task<int> CountAsync(string? email, CancellationToken cancellationToken) =>
        Task.FromResult(Filtered(email).Count());

    private IEnumerable<Customer> Filtered(string? email)
    {
        var customers = Customers.Where(customer => !customer.IsDeleted);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var term = email.Trim().ToLowerInvariant();
            customers = customers.Where(customer => customer.Email.ToLower().Contains(term));
        }

        return customers;
    }

    public void Add(Customer customer) => Customers.Add(customer);

    public void AddRole(UserRole userRole) => throw new NotSupportedException(
        "FakeUserRepository.AddRole is not supported; seed RolesByUser directly.");
}

internal sealed class FakeRoleRepository : IRoleRepository
{
    public List<Role> Roles { get; } = [];

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Roles.FirstOrDefault(role => role.Id == id && !role.IsDeleted));

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Roles.FirstOrDefault(role => role.Name == name && !role.IsDeleted));

    public Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Role>>(
            Roles.Where(role => !role.IsDeleted).OrderBy(role => role.Name).ToList());

    public void Add(Role role) => Roles.Add(role);
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

internal sealed class FakeCurrentUser(
    bool isAuthenticated = true,
    IReadOnlyList<string>? roles = null,
    IReadOnlyList<string>? permissions = null,
    Guid? userId = null) : ICurrentUser
{
    public Guid? UserId { get; } = userId;

    public bool IsAuthenticated { get; } = isAuthenticated;

    public IReadOnlyList<string> Roles { get; } = roles ?? [];

    public IReadOnlyList<string> Permissions { get; } = permissions ?? [];
}

internal sealed class FakeStockRepository : IStockRepository
{
    public List<StockItem> Items { get; } = [];

    public List<StockMovement> Movements { get; } = [];

    public Task<StockItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

    public Task<StockItem?> GetBySkuAndWarehouseAsync(string sku, Guid warehouseId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(
            item => item.Sku == sku && item.WarehouseId == warehouseId && !item.IsDeleted));

    public Task<IReadOnlyList<StockItem>> ListAsync(int page, int pageSize, Guid? warehouseId, CancellationToken cancellationToken)
    {
        var query = Items.Where(item => !item.IsDeleted);

        if (warehouseId is not null)
        {
            query = query.Where(item => item.WarehouseId == warehouseId);
        }

        var items = query
            .OrderBy(item => item.Sku)
            .ThenBy(item => item.WarehouseId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<StockItem>>(items);
    }

    public Task<int> CountAsync(Guid? warehouseId, CancellationToken cancellationToken)
    {
        var query = Items.Where(item => !item.IsDeleted);

        if (warehouseId is not null)
        {
            query = query.Where(item => item.WarehouseId == warehouseId);
        }

        return Task.FromResult(query.Count());
    }

    public Task<IReadOnlyList<StockMovement>> ListMovementsAsync(Guid stockItemId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = Movements
            .Where(movement => movement.StockItemId == stockItemId)
            .OrderByDescending(movement => movement.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<StockMovement>>(items);
    }

    public Task<int> CountMovementsAsync(Guid stockItemId, CancellationToken cancellationToken) =>
        Task.FromResult(Movements.Count(movement => movement.StockItemId == stockItemId));

    public void Add(StockItem stockItem) => Items.Add(stockItem);

    public void AddMovement(StockMovement movement) => Movements.Add(movement);
}
