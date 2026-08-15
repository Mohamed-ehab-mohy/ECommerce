using ECommerce.Domain.Events;
using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Audit;
using ECommerce.Infrastructure.Carts;
using ECommerce.Infrastructure.Catalog;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Flags;
using ECommerce.Infrastructure.Fulfillment;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Inventory;
using ECommerce.Infrastructure.Invoicing;
using ECommerce.Infrastructure.Jobs;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Notifications;
using ECommerce.Infrastructure.Orders;
using ECommerce.Infrastructure.Outbox;
using ECommerce.Infrastructure.Payments;
using ECommerce.Infrastructure.Promotions;
using ECommerce.Infrastructure.Redis;
using ECommerce.Infrastructure.Reviews;
using ECommerce.Infrastructure.Search;
using ECommerce.Infrastructure.Shipping;
using ECommerce.Infrastructure.Storage;
using ECommerce.Infrastructure.Wishlists;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Flags.Ports;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Services;
using ECommerce.UseCases.Fulfillment.Shipping;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Invoicing.Ports;
using ECommerce.UseCases.Messaging.Ports;
using ECommerce.UseCases.Notifications.Ports;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Services;
using ECommerce.UseCases.Payments.Options;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Promotions.Ports;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Wishlist.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string postgresConnectionString,
        string redisConnectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ECommerceDbContext>(options => options
            .UseNpgsql(dataSource)
            .AddInterceptors(new DomainEventsInterceptor()));

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductImportRepository, ProductImportRepository>();
        services.AddScoped<IProductImportJobScheduler, HangfireProductImportJobScheduler>();
        services.AddScoped<IProductSearchRepository, ProductSearchRepository>();
        services.AddScoped<IEventHandler<ProductCreated>, ProductSearchIndexSynchronizer>();
        services.AddScoped<IEventHandler<ProductUpdated>, ProductSearchIndexSynchronizer>();
        services.AddScoped<IEventHandler<ProductDeactivated>, ProductSearchIndexSynchronizer>();
        services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
        services.AddScoped<IReviewVoteRepository, ReviewVoteRepository>();
        services.AddScoped<IVerifiedPurchaseChecker, VerifiedPurchaseChecker>();
        services.AddScoped<IEventHandler<ReviewPublished>, ReviewRatingSynchronizer>();
        services.AddScoped<IEventHandler<ReviewRemoved>, ReviewRatingSynchronizer>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockAllocator, StockAllocator>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICheckoutRepository, CheckoutRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddScoped<IIdempotencyKeyRepository, IdempotencyKeyRepository>();
        services.AddScoped<IShippingRateProvider, ShippingRateStubProvider>();
        services.AddScoped<ITaxRateProvider, StaticTaxRateProvider>();
        services.AddScoped<ITaxCalculator, TaxCalculator>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailSender, LogEmailSender>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddSingleton<ILoginAttemptThrottler, InMemoryLoginAttemptThrottler>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();
        services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        services.AddScoped<IFeatureFlagService, CachedFeatureFlagService>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddSingleton<INotificationTemplateStore, InMemoryNotificationTemplateStore>();
        services.AddScoped<NotificationSender>();
        services.AddScoped<INotificationQueue, NotificationQueue>();
        services.AddScoped<INotificationProvider, SmtpEmailProvider>();
        services.AddScoped<INotificationProvider, StubSmsProvider>();
        services.AddScoped<IOrderNotifier, NotificationOrderNotifier>();
        services.AddScoped<IEventHandler<LowStockAlertRaised>, LowStockAlertNotificationHandler>();
        services.AddScoped<BackorderFillService>();
        services.AddScoped<IEventHandler<StockRestocked>, BackorderFillHandler>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IFulfillmentTaskRepository, FulfillmentTaskRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddSingleton<IShippingRateCache, InMemoryShippingRateCache>();
        services.AddScoped<ICarrierAdapter, AramexCarrierAdapter>();
        services.AddScoped<ICarrierAdapter, DhlCarrierAdapter>();
        services.AddScoped<CarrierRateSelector>();
        services.AddScoped<PickListGenerationService>();
        services.AddSingleton<OutboxMetrics>();
        services.AddScoped<OutboxPublisher>();

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
        services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
        services.AddScoped<ICreditNoteNumberGenerator, CreditNoteNumberGenerator>();
        services.AddScoped<IInvoiceDocumentStore>(sp =>
            new LocalFileDocumentStore(
                sp.GetRequiredService<ILogger<LocalFileDocumentStore>>(),
                sp.GetRequiredService<IConfiguration>().GetValue("Storage:BasePath", "./storage")));
        services.AddSingleton<IInvoicePdfRenderer, QuestPdfInvoiceRenderer>();
        services.AddScoped<IInvoicePdfJobScheduler, HangfireInvoicePdfJobScheduler>();
        services.AddScoped<IEventHandler<PaymentCaptured>, InvoiceOnPaymentCapturedHandler>();
        services.AddScoped<IEventHandler<PaymentRefunded>, CreditNoteOnPaymentRefundedHandler>();

        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis");

        services.AddHttpClient<IPasswordBreachChecker, HibpPasswordBreachChecker>(client =>
            client.Timeout = TimeSpan.FromSeconds(10));

        services.AddHostedService<MigrateOnStartupHostedService>();
        services.AddHostedService<OutboxBackgroundService>();

        return services;
    }

    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PaymentProviderOptions>(configuration.GetSection(PaymentProviderOptions.SectionName));
        services.AddOptions<PaymentRetryOptions>().BindConfiguration(PaymentRetryOptions.SectionName);
        services.AddOptions<RefundRetryOptions>().BindConfiguration(RefundRetryOptions.SectionName);
        services.AddSingleton<IPaymentProviderHealth, PaymentCircuitBreaker>();
        services.AddSingleton<IPaymentProvider, MockPaymentProvider>();

        var paymentOptions = configuration
            .GetSection(PaymentProviderOptions.SectionName)
            .Get<PaymentProviderOptions>() ?? new PaymentProviderOptions();

        if (paymentOptions.Stripe.Enabled && !string.IsNullOrWhiteSpace(paymentOptions.Stripe.SecretKey))
        {
            services.AddSingleton<IPaymentProvider>(_ => new StripePaymentProvider(paymentOptions.Stripe.SecretKey));
        }

        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IRefundRetryJobScheduler, HangfireRefundRetryJobScheduler>();

        return services;
    }
}
