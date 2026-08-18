using ECommerce.Domain.Audit;
using ECommerce.Domain.Cart;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Checkout;
using ECommerce.Domain.Flags;
using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Identity;
using ECommerce.Domain.Integrations;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Invoicing;
using ECommerce.Domain.Notifications;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Domain.Pricing;
using ECommerce.Domain.Reporting;
using ECommerce.Domain.Reviews;
using ECommerce.Domain.Wishlist;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Outbox;
using ECommerce.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public sealed class ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductImport> ProductImports => Set<ProductImport>();

    public DbSet<ProductImportError> ProductImportErrors => Set<ProductImportError>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();

    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CategoryHierarchy> CategoryHierarchy => Set<CategoryHierarchy>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();

    public DbSet<PaymentLedgerEntry> PaymentLedgerEntries => Set<PaymentLedgerEntry>();

    public DbSet<PaymentReconciliationRecord> PaymentReconciliationRecords => Set<PaymentReconciliationRecord>();

    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<RefundItem> RefundItems => Set<RefundItem>();

    public DbSet<Checkout> Checkouts => Set<Checkout>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderBackorderItem> OrderBackorderItems => Set<OrderBackorderItem>();

    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();

    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    public DbSet<FulfillmentTask> FulfillmentTasks => Set<FulfillmentTask>();

    public DbSet<FulfillmentTaskItem> FulfillmentTaskItems => Set<FulfillmentTaskItem>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<TrackingUpdate> TrackingUpdates => Set<TrackingUpdate>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();

    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();

    public DbSet<RealtimeEvent> RealtimeEvents => Set<RealtimeEvent>();

    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();

    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();

    public DbSet<ReturnRequestItem> ReturnRequestItems => Set<ReturnRequestItem>();

    public DbSet<CheckoutSagaState> CheckoutSagaStates => Set<CheckoutSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceDbContext).Assembly);
    }
}
