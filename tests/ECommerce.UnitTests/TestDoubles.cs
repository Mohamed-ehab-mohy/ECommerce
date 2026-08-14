using System.Reflection;
using ECommerce.Domain.Abstractions;
using ECommerce.Domain.Audit;
using ECommerce.Domain.Cart;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Events;
using ECommerce.Domain.Flags;
using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Identity;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Invoicing;
using ECommerce.Domain.Notifications;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Domain.Pricing;
using ECommerce.Domain.Wishlist;
using ECommerce.Shared.Audit;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Flags.Ports;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Shipping;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Invoicing.Ports;
using ECommerce.UseCases.Messaging.Ports;
using ECommerce.UseCases.Notifications.Ports;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Promotions.Ports;
using ECommerce.UseCases.Wishlist.Ports;
using System.Diagnostics.Metrics;

namespace ECommerce.UnitTests;

internal sealed class FakeCartRepository : ICartRepository
{
    public List<Cart> Carts { get; } = [];

    public bool ThrowConcurrencyOnSave { get; set; }

    public Task<Cart?> ByOwnerKeyAsync(string ownerKey, CancellationToken cancellationToken) =>
        Task.FromResult(Carts.FirstOrDefault(cart => cart.OwnerKey == ownerKey));

    public Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Carts.FirstOrDefault(cart => cart.Id == id));

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

internal sealed class FakeWishlistRepository : IWishlistRepository
{
    public List<Wishlist> Wishlists { get; } = [];

    public Task<Wishlist?> ByOwnerKeyAsync(string ownerKey, CancellationToken cancellationToken) =>
        Task.FromResult(Wishlists.FirstOrDefault(wishlist => wishlist.OwnerKey == ownerKey));

    public Task SaveAsync(Wishlist wishlist, CancellationToken cancellationToken)
    {
        var existing = Wishlists.FirstOrDefault(candidate => candidate.OwnerKey == wishlist.OwnerKey);
        if (existing is null)
        {
            Wishlists.Add(wishlist);
            return Task.CompletedTask;
        }

        Wishlists.Remove(existing);
        Wishlists.Add(wishlist);
        return Task.CompletedTask;
    }
}

internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Products { get; } = [];

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Products.FirstOrDefault(product => product.Id == id));

    public Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>(
            Products.Where(product => ids.Contains(product.Id)).ToList());

    public Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Products.FirstOrDefault(
            product => product.Id == id && product.Status == ProductStatus.Active && !product.IsDeleted));

    public Task<IReadOnlyList<Product>> GetBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>(
            Products.Where(product => skus.Contains(product.Sku)).ToList());

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

internal sealed class FakeCheckoutRepository : ICheckoutRepository
{
    public List<Checkout> Checkouts { get; } = [];

    public Task<Checkout?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Checkouts.FirstOrDefault(checkout => checkout.Id == id));

    public Task<Checkout?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        Task.FromResult(Checkouts.FirstOrDefault(checkout => checkout.PaymentId == paymentId));

    public void Add(Checkout checkout) => Checkouts.Add(checkout);
}

internal sealed class FakePaymentRepository : IPaymentRepository
{
    public List<Payment> Payments { get; } = [];

    public List<PaymentReconciliationRecord> ReconciliationRecords { get; } = [];

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Payments.FirstOrDefault(payment => payment.Id == id));

    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Payments.FirstOrDefault(payment => payment.OrderId == orderId));

    public Task<IReadOnlyList<Payment>> GetUnreconciledAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Payment>>(Payments
            .Where(payment => payment.ProviderReference != null)
            .Where(payment => !ReconciliationRecords.Any(record => record.PaymentId == payment.Id))
            .ToList());

    public Task<IReadOnlyList<PaymentReconciliationRecord>> GetReconciliationRecordsAsync(
        ReconciliationStatus? status,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PaymentReconciliationRecord>>(ReconciliationRecords
            .Where(record => status == null || record.Status == status)
            .ToList());

    public void Add(Payment payment) => Payments.Add(payment);

    public void AddReconciliationRecord(PaymentReconciliationRecord record) =>
        ReconciliationRecords.Add(record);
}

internal sealed class FakePaymentProvider(
    string key = "mock",
    PaymentIntentResult? intent = null,
    PaymentAuthorizationResult? authorization = null) : IPaymentProvider
{
    public PaymentIntentResult? IntentResult { get; set; } = intent;

    public PaymentAuthorizationResult? AuthorizationResult { get; set; } = authorization;

    public int AuthorizeCallCount { get; private set; }

    public PaymentIntentRequest? LastIntentRequest { get; private set; }

    public PaymentAuthorizationRequest? LastAuthorizationRequest { get; private set; }

    public string Key => key;

    public Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken)
    {
        LastIntentRequest = request;
        return Task.FromResult(IntentResult ?? new PaymentIntentResult(true, "tok_mock_1", "pi_mock_1", "pi_mock_1", null));
    }

    public Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken cancellationToken)
    {
        AuthorizeCallCount++;
        LastAuthorizationRequest = request;
        return Task.FromResult(AuthorizationResult ?? new PaymentAuthorizationResult(true, "pi_mock_1_auth", null));
    }
}

internal sealed class FakePaymentProviderFactory(
    string providerKey = "mock",
    FakePaymentProvider? provider = null) : IPaymentProviderFactory
{
    public FakePaymentProvider Provider { get; } = provider ?? new FakePaymentProvider(providerKey);

    public string? MissingKey { get; set; }

    public Task<IPaymentProvider> RouteAsync(string currency, string country, CancellationToken cancellationToken) =>
        Task.FromResult<IPaymentProvider>(Provider);

    public Task<IPaymentProvider> GetAsync(string providerKey, CancellationToken cancellationToken) =>
        Task.FromResult<IPaymentProvider>(MissingKey == providerKey ? null! : Provider);
}

internal sealed class FakePaymentProviderHealth : IPaymentProviderHealth
{
    private readonly HashSet<string> _unavailable = [];

    public int FailureCount { get; private set; }

    public int SuccessCount { get; private set; }

    public void SetUnavailable(string providerKey) => _unavailable.Add(providerKey);

    public bool IsAvailable(string providerKey) => !_unavailable.Contains(providerKey);

    public void RecordSuccess(string providerKey) => SuccessCount++;

    public void RecordFailure(string providerKey) => FailureCount++;
}

internal sealed class FakeShippingRateProvider(IReadOnlyList<ShippingMethod>? methods = null) : IShippingRateProvider
{
    public IReadOnlyList<ShippingMethod> Methods { get; } =
        methods ?? new List<ShippingMethod>
        {
            new("standard", "Standard", 9.90m, "USD", "3-5 business days")
        };

    public Task<IReadOnlyList<ShippingMethod>> ListAsync(string country, string currency, CancellationToken cancellationToken) =>
        Task.FromResult(Methods);

    public Task<ShippingMethod?> GetRateAsync(string methodId, string country, string currency, CancellationToken cancellationToken) =>
        Task.FromResult(Methods.FirstOrDefault(method => method.Id == methodId));
}

internal sealed class FakeTaxCalculator(decimal tax = 0m, decimal rate = 0m) : ITaxCalculator
{
    public Task<TaxCalculation> ComputeAsync(decimal taxableSubtotal, string country, string currency, CancellationToken cancellationToken) =>
        Task.FromResult(new TaxCalculation(rate, tax));
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public string Tag { get; set; } = "fake";

    public int SaveCount { get; private set; }

    public FakeTransaction? LastTransaction { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        LastTransaction = new FakeTransaction();
        return Task.FromResult<ITransaction>(LastTransaction);
    }
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

internal sealed class FakeFeatureFlagRepository : IFeatureFlagRepository
{
    public List<FeatureFlag> Flags { get; } = [];

    public Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Flags.FirstOrDefault(flag => flag.Key == key));

    public Task<IReadOnlyList<FeatureFlag>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FeatureFlag>>(Flags.ToList());

    public void Add(FeatureFlag flag) => Flags.Add(flag);
}

internal sealed class FakeNotificationPreferenceRepository : INotificationPreferenceRepository
{
    public List<NotificationPreference> Preferences { get; } = [];

    public Task<bool> IsEnabledAsync(
        Guid customerId,
        NotificationChannel channel,
        NotificationKind kind,
        CancellationToken cancellationToken) =>
        Task.FromResult(Preferences.Any(preference =>
            preference.CustomerId == customerId &&
            preference.Channel == channel &&
            preference.Kind == kind &&
            preference.Enabled));

    public Task<NotificationPreference?> GetAsync(
        Guid customerId,
        NotificationChannel channel,
        NotificationKind kind,
        CancellationToken cancellationToken) =>
        Task.FromResult(Preferences.FirstOrDefault(preference =>
            preference.CustomerId == customerId &&
            preference.Channel == channel &&
            preference.Kind == kind));

    public Task<IReadOnlyList<NotificationPreference>> ListByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<NotificationPreference>>(
            Preferences.Where(preference => preference.CustomerId == customerId).ToList());

    public void Add(NotificationPreference preference) => Preferences.Add(preference);
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

    public Task<IReadOnlyList<StockItem>> ListBySkuAsync(string sku, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StockItem>>(
            Items.Where(item => item.Sku == sku && !item.IsDeleted)
                .OrderBy(item => item.WarehouseId)
                .ToList());

    public Task<IReadOnlyList<StockItem>> LockForTransferAsync(
        string sku,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StockItem>>(
            Items.Where(item => item.Sku == sku
                && (item.WarehouseId == fromWarehouseId || item.WarehouseId == toWarehouseId)
                && !item.IsDeleted)
                .ToList());

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

internal sealed class FakeStockAllocator(
    IReadOnlyList<StockAllocationLine>? lines = null,
    IReadOnlyList<StockShortfall>? shortfalls = null) : IStockAllocator
{
    public int AllocateCount { get; private set; }

    public int ReleaseCount { get; private set; }

    public List<AllocationRequestItem> LastItems { get; private set; } = [];

    public string? LastReason { get; private set; }

    public string? LastReference { get; private set; }

    public Task<StockAllocationResult> AllocateAsync(
        IReadOnlyCollection<AllocationRequestItem> items,
        string reason,
        string reference,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        AllocateCount++;
        LastItems = items.ToList();
        LastReason = reason;
        LastReference = reference;
        return Task.FromResult(new StockAllocationResult(lines ?? [], shortfalls ?? []));
    }

    public Task<StockReleaseResult> ReleaseAsync(
        IReadOnlyCollection<AllocationRequestItem> items,
        string reason,
        string reference,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        ReleaseCount++;
        LastItems = items.ToList();
        LastReason = reason;
        LastReference = reference;
        return Task.FromResult(new StockReleaseResult([]));
    }
}

internal sealed class FakeOrderRepository : IOrderRepository
{
    public List<Order> Orders { get; } = [];

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Orders.FirstOrDefault(order => order.Id == id));

    public Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Orders.FirstOrDefault(order => order.OrderNumber == orderNumber));

    public Task<Order?> GetByNumberWithDetailsAsync(string orderNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Orders.FirstOrDefault(order => order.OrderNumber == orderNumber));

    public Task<OrderHistoryPage> ListByCustomerAsync(
        Guid customerId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = Orders
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.PlacedAt)
            .ToList();

        var items = query.Take(pageSize).ToList();
        var hasNext = query.Count > pageSize;

        return Task.FromResult(new OrderHistoryPage(items, null, hasNext));
    }

    public Task<IReadOnlyList<Order>> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Order>>(
            Orders.Where(order => order.CustomerEmail == email).ToList());

    public Task<IReadOnlyList<OrderBackorderItem>> ListOpenBackorderItemsBySkuAsync(
        string sku,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OrderBackorderItem>>(
            Orders
                .SelectMany(order => order.BackorderItems)
                .Where(item => item.Sku == sku && item.Status == BackorderStatus.Open)
                .OrderBy(item => item.CreatedAt)
                .ToList());

    public void Add(Order order) => Orders.Add(order);
}

internal sealed class FakeOrderNumberGenerator : IOrderNumberGenerator
{
    private int _sequence;

    public Task<string> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult(OrderNumber.Create(utcNow, Interlocked.Increment(ref _sequence)).Value);
}

internal sealed class FakeIdempotencyKeyRepository : IIdempotencyKeyRepository
{
    public List<IdempotencyKey> Keys { get; } = [];

    public Task<IdempotencyKey?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Keys.FirstOrDefault(idempotencyKey => idempotencyKey.Key == key));

    public Task<IdempotencyKey?> AddIfAbsentAsync(
        IdempotencyKey idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existing = Keys.FirstOrDefault(candidate => candidate.Key == idempotencyKey.Key);
        if (existing is not null)
        {
            return Task.FromResult<IdempotencyKey?>(existing);
        }

        Keys.Add(idempotencyKey);
        return Task.FromResult<IdempotencyKey?>(null);
    }
}

internal sealed class FakeEventDispatcher : IEventDispatcher
{
    public List<IDomainEvent> Dispatched { get; } = [];

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        Dispatched.Add(domainEvent);
        return Task.CompletedTask;
    }
}

internal sealed class FakeInboxMessageRepository : IInboxMessageRepository
{
    private readonly HashSet<(string ConsumerQueue, Guid MessageId)> _processed = [];

    public int ConsumeCalls { get; private set; }

    public Task<bool> TryConsumeAsync(
        string consumerQueue,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        ConsumeCalls++;
        return Task.FromResult(_processed.Add((consumerQueue, messageId)));
    }
}

internal sealed class CapturingOrderNotifier : IOrderNotifier
{
    public List<OrderPlaced> Notified { get; } = [];

    public List<OrderCancelled> Cancelled { get; } = [];

    public List<OrderShipped> Shipped { get; } = [];

    public Task NotifyPlacedAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken)
    {
        Notified.Add(orderPlaced);
        return Task.CompletedTask;
    }

    public Task NotifyCancelledAsync(OrderCancelled orderCancelled, CancellationToken cancellationToken)
    {
        Cancelled.Add(orderCancelled);
        return Task.CompletedTask;
    }

    public Task NotifyShippedAsync(OrderShipped orderShipped, CancellationToken cancellationToken)
    {
        Shipped.Add(orderShipped);
        return Task.CompletedTask;
    }
}

internal sealed class FakeMeterFactory : IMeterFactory
{
    public Meter Create(MeterOptions options) => new(options);

    public void Dispose() { }
}

internal sealed class FakeAuditLogWriter : IAuditLogWriter
{
    public List<AuditOperation> Operations { get; } = [];

    public Task WriteAsync(AuditOperation operation, CancellationToken cancellationToken)
    {
        Operations.Add(operation);
        return Task.CompletedTask;
    }
}

internal sealed class FakePromotionRepository : IPromotionRepository
{
    public List<Promotion> Promotions { get; } = [];

    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Promotions.FirstOrDefault(promotion => promotion.Id == id));

    public Task<IReadOnlyList<Promotion>> GetActiveForScopeAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Promotion>>(Promotions
            .Where(promotion => promotion.State == PromotionState.Active)
            .ToList());

    public Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Promotion>>(Promotions.ToList());

    public Task<IReadOnlyList<Promotion>> GetDueForActivationAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Promotion>>(Promotions
            .Where(promotion => promotion.State == PromotionState.Draft)
            .Where(promotion => promotion.StartsAt != null)
            .Where(promotion => promotion.StartsAt <= utcNow)
            .Where(promotion => promotion.EndsAt == null || promotion.EndsAt >= utcNow)
            .ToList());

    public Task<IReadOnlyList<Promotion>> GetDueForPauseAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Promotion>>(Promotions
            .Where(promotion => promotion.State == PromotionState.Active)
            .Where(promotion => promotion.EndsAt != null)
            .Where(promotion => promotion.EndsAt < utcNow)
            .ToList());

    public void Add(Promotion promotion) => Promotions.Add(promotion);
}

internal sealed class FakeCouponRepository : ICouponRepository
{
    public List<Coupon> Coupons { get; } = [];

    public List<CouponUsage> Usages { get; } = [];

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Coupons.FirstOrDefault(coupon => coupon.Code == code.Trim().ToUpperInvariant()));

    public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Coupons.FirstOrDefault(coupon => coupon.Id == id));

    public Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Coupon>>(Coupons.ToList());

    public Task<int> GetRedemptionCountAsync(Guid couponId, Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(Usages.Count(usage => usage.CouponId == couponId && usage.CustomerId == customerId));

    public Task<CouponRedemptionResult> TryRedeemAsync(
        Guid couponId,
        Guid orderId,
        Guid customerId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var coupon = Coupons.First(coupon => coupon.Id == couponId);
        if (Usages.Any(usage => usage.CouponId == couponId && usage.OrderId == orderId))
        {
            return Task.FromResult(CouponRedemptionResult.AlreadyApplied);
        }

        if (coupon.UsedCount >= coupon.TotalUses
            || (coupon.PerCustomerLimit is { } limit
                && Usages.Count(usage => usage.CouponId == couponId && usage.CustomerId == customerId) >= limit))
        {
            return Task.FromResult(CouponRedemptionResult.Exhausted);
        }

        Usages.Add(new CouponUsage(Guid.NewGuid(), couponId, orderId, customerId, utcNow));
        return Task.FromResult(CouponRedemptionResult.Redeemed);
    }

    public void Add(Coupon coupon) => Coupons.Add(coupon);
}

internal sealed class FakeFulfillmentTaskRepository : IFulfillmentTaskRepository
{
    private static readonly FulfillmentTaskStatus[] OpenStatuses =
    [
        FulfillmentTaskStatus.Queued,
        FulfillmentTaskStatus.Assigned,
        FulfillmentTaskStatus.Picking
    ];

    public List<FulfillmentTask> Tasks { get; } = [];

    public Task<FulfillmentTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Tasks.FirstOrDefault(task => task.Id == id));

    public Task<FulfillmentTask?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Tasks.FirstOrDefault(task => task.OrderId == orderId));

    public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Tasks.Any(task => task.OrderId == orderId));

    public Task<bool> HasUnshippedTasksAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Tasks.Any(task =>
            task.OrderId == orderId
            && task.Status != FulfillmentTaskStatus.Shipped
            && task.Status != FulfillmentTaskStatus.Cancelled));

    public Task<IReadOnlyList<FulfillmentTask>> ListAsync(
        Guid? warehouseId,
        FulfillmentTaskStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FulfillmentTask>>(Tasks
            .Where(task => warehouseId == null || task.WarehouseId == warehouseId)
            .Where(task => status == null || task.Status == status)
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());

    public Task<int> CountAsync(
        Guid? warehouseId,
        FulfillmentTaskStatus? status,
        CancellationToken cancellationToken) =>
        Task.FromResult(Tasks.Count(task =>
            (warehouseId == null || task.WarehouseId == warehouseId) &&
            (status == null || task.Status == status)));

    public Task<IReadOnlyList<FulfillmentTask>> ListOpenByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FulfillmentTask>>(Tasks
            .Where(task => task.WarehouseId == warehouseId && OpenStatuses.Contains(task.Status))
            .OrderBy(task => task.Zone)
            .ThenBy(task => task.CreatedAt)
            .ToList());

    public void Add(FulfillmentTask task) => Tasks.Add(task);
}

internal sealed class FakeShipmentRepository : IShipmentRepository
{
    public List<Shipment> Shipments { get; } = [];

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Shipments.FirstOrDefault(shipment => shipment.Id == id));

    public Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Shipments.FirstOrDefault(shipment => shipment.TrackingNumber == trackingNumber));

    public Task<bool> HasUndeliveredShipmentsAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Shipments.Any(shipment =>
            shipment.OrderId == orderId && shipment.Status != ShipmentStatus.Delivered));

    public void Add(Shipment shipment) => Shipments.Add(shipment);
}

internal sealed class FakeCarrierAdapter(
    string key,
    CarrierQuoteResult? quote = null,
    CarrierShipmentResult? shipment = null) : ICarrierAdapter
{
    public string CarrierKey => key;

    public bool ThrowOnQuote { get; set; }

    public bool ThrowOnCreate { get; set; }

    public int QuoteCallCount { get; private set; }

    public int CreateCallCount { get; private set; }

    public Task<CarrierQuoteResult> QuoteAsync(CarrierShipmentRequest request, CancellationToken cancellationToken)
    {
        QuoteCallCount++;
        return ThrowOnQuote
            ? throw new InvalidOperationException("Carrier unavailable.")
            : Task.FromResult(quote ?? new CarrierQuoteResult(
                key,
                10m,
                request.Currency,
                DateTime.UtcNow.AddDays(3)));
    }

    public Task<CarrierShipmentResult> CreateShipmentAsync(CarrierShipmentRequest request, CancellationToken cancellationToken)
    {
        CreateCallCount++;
        return ThrowOnCreate
            ? throw new InvalidOperationException("Carrier unavailable.")
            : Task.FromResult(shipment ?? new CarrierShipmentResult(
                key,
                $"TRK-{key}-{CreateCallCount}",
                $"https://example.com/labels/{key}-{CreateCallCount}.pdf"));
    }
}

internal sealed class FakeShippingRateCache : IShippingRateCache
{
    public Dictionary<string, CarrierQuoteResult> Quotes { get; } = [];

    public bool TryGet(string key, out CarrierQuoteResult quote) =>
        Quotes.TryGetValue(key, out quote!);

    public void Set(string key, CarrierQuoteResult quote) => Quotes[key] = quote;
}

internal sealed class FakeProductSearchRepository : IProductSearchRepository
{
    public List<Product> Products { get; } = [];

    public int TotalCount { get; set; } = 0;

    public ProductSearchFacets Facets { get; set; } = new([], [], [], []);

    public List<ProductSearchCriteria> ReceivedCriteria { get; } = [];

    public Task<ProductSearchPage> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ReceivedCriteria.Add(criteria);

        var items = Products
            .OrderBy(product => product.Slug)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        return Task.FromResult(new ProductSearchPage(
            items,
            TotalCount == 0 ? items.Count : TotalCount,
            Facets));
    }
}

internal sealed class FakeInvoiceRepository : IInvoiceRepository
{
    public List<Invoice> Invoices { get; } = [];

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Invoices.FirstOrDefault(invoice => invoice.Id == id));

    public Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Invoices.FirstOrDefault(invoice => invoice.OrderId == orderId));

    public Task<InvoiceListPage> ListAsync(
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = Invoices
            .Where(invoice => status == null || invoice.Status == status)
            .OrderByDescending(invoice => invoice.IssuedAt)
            .ToList();

        return Task.FromResult(new InvoiceListPage(
            query.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            query.Count,
            page,
            pageSize));
    }

    public void Add(Invoice invoice) => Invoices.Add(invoice);
}

internal sealed class FakeCreditNoteRepository : ICreditNoteRepository
{
    public List<CreditNote> CreditNotes { get; } = [];

    public Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(CreditNotes.FirstOrDefault(creditNote => creditNote.Id == id));

    public Task<CreditNote?> GetByRefundIdAsync(Guid refundId, CancellationToken cancellationToken) =>
        Task.FromResult(CreditNotes.FirstOrDefault(creditNote => creditNote.RefundId == refundId));

    public Task<CreditNoteListPage> ListByInvoiceAsync(
        Guid invoiceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = CreditNotes
            .Where(creditNote => creditNote.InvoiceId == invoiceId)
            .OrderByDescending(creditNote => creditNote.IssuedAt)
            .ToList();

        return Task.FromResult(new CreditNoteListPage(
            query.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            query.Count,
            page,
            pageSize));
    }

    public void Add(CreditNote creditNote) => CreditNotes.Add(creditNote);
}

internal sealed class FakeInvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private int _sequence;

    public Task<InvoiceNumber> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult(InvoiceNumber.Create(utcNow, Interlocked.Increment(ref _sequence)));
}

internal sealed class FakeCreditNoteNumberGenerator : ICreditNoteNumberGenerator
{
    private int _sequence;

    public Task<CreditNoteNumber> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult(CreditNoteNumber.Create(utcNow, Interlocked.Increment(ref _sequence)));
}

internal sealed class FakeInvoicePdfJobScheduler : IInvoicePdfJobScheduler
{
    public List<Guid> Enqueued { get; } = [];

    public void Enqueue(Guid invoiceId) => Enqueued.Add(invoiceId);
}

internal sealed class FakeInvoiceDocumentStore : IInvoiceDocumentStore
{
    public Dictionary<string, byte[]> Documents { get; } = [];

    public Task<string> PutAsync(string key, byte[] content, CancellationToken cancellationToken)
    {
        Documents[key] = content;
        return Task.FromResult($"https://cdn.example.test/{key}");
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Documents.TryGetValue(key, out var content) ? content : null);
}

internal sealed class FakeInvoicePdfRenderer : IInvoicePdfRenderer
{
    public int RenderCount { get; private set; }

    public InvoiceDocument? LastDocument { get; private set; }

    public byte[] Render(InvoiceDocument document)
    {
        RenderCount++;
        LastDocument = document;
        return [0x25, 0x50, 0x44, 0x46];
    }
}
