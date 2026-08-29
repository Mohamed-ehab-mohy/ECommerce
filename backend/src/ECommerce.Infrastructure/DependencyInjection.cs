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
using ECommerce.Infrastructure.Grpc;
using ECommerce.Infrastructure.Integrations;
using ECommerce.UseCases.Grpc.Ports;
using ECommerce.Infrastructure.Inventory;
using ECommerce.Infrastructure.Invoicing;
using ECommerce.Infrastructure.Jobs;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Metrics;
using ECommerce.Infrastructure.Notifications;
using ECommerce.Infrastructure.Orders;
using ECommerce.Infrastructure.Outbox;
using ECommerce.Infrastructure.Partners;
using ECommerce.Infrastructure.Pricing;
using ECommerce.Infrastructure.Recommendations;
using ECommerce.UseCases.Partners;
using ECommerce.UseCases.Pricing;
using ECommerce.UseCases.Recommendations;
using ECommerce.Infrastructure.Payments;
using ECommerce.Infrastructure.Promotions;
using ECommerce.Infrastructure.Redis;
using ECommerce.Infrastructure.Realtime;
using ECommerce.Infrastructure.Reports;
using ECommerce.Infrastructure.Reviews;
using ECommerce.Infrastructure.Search;
using ECommerce.Infrastructure.Shipping;
using ECommerce.Infrastructure.Storage;
using ECommerce.Infrastructure.ReadModels;
using ECommerce.Infrastructure.Wishlists;
using Elastic.Clients.Elasticsearch;
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
using ECommerce.UseCases.Integrations.Ports;
using ECommerce.UseCases.Integrations.Services;
using ECommerce.UseCases.Messaging.Ports;
using ECommerce.UseCases.Notifications.Ports;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Services;
using ECommerce.UseCases.Payments.Options;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Promotions.Ports;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Reports.Ports;
using ECommerce.UseCases.Wishlist.Ports;
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
        string redisConnectionString,
        string? sqlServerConnectionString = null,
        string dataProvider = "Postgres")
    {
        if (dataProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(sqlServerConnectionString))
        {
            services.AddDbContext<ECommerceDbContext>(options => options
                .UseSqlServer(sqlServerConnectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                .AddInterceptors(new DomainEventsInterceptor(), new TenantAwareSaveChangesInterceptor(), new SoftDeleteInterceptor()));
        }
        else
        {
            var csBuilder = new NpgsqlConnectionStringBuilder(postgresConnectionString)
            {
                MinPoolSize = 5,
                MaxPoolSize = 100,
                ConnectionIdleLifetime = 300
            };

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(csBuilder.ConnectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<ECommerceDbContext>(options => options
                .UseNpgsql(dataSource, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                .AddInterceptors(new DomainEventsInterceptor(), new TenantAwareSaveChangesInterceptor(), new SoftDeleteInterceptor()));
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 10;
            options.ConnectTimeout = 10_000;
            options.SyncTimeout = 10_000;
            options.AsyncTimeout = 10_000;
            return ConnectionMultiplexer.Connect(options);
        });

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        services.AddHybridCache();
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IMfaService, TotpMfaService>();
        services.AddScoped<IMfaSecretRepository, MfaSecretRepository>();
        services.AddScoped<IProductRepository, CachedProductRepository>();
        services.AddScoped<ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductImportRepository, ProductImportRepository>();
        services.AddScoped<IProductImportJobScheduler, HangfireProductImportJobScheduler>();
        services.AddScoped<ProductSearchRepository>();
        services.AddScoped<IProductSearchRepository>(sp => sp.GetRequiredService<ProductSearchRepository>());
        services.AddScoped<IAutocompleteRepository, AutocompleteRepository>();
        services.AddScoped<IEventHandler<ProductCreated>, ProductSearchIndexSynchronizer>();
        services.AddScoped<IEventHandler<ProductUpdated>, ProductSearchIndexSynchronizer>();
        services.AddScoped<IEventHandler<ProductDeactivated>, ProductSearchIndexSynchronizer>();
        services.AddScoped<IEventHandler<ProductCreated>, ProductCacheInvalidationHandler>();
        services.AddScoped<IEventHandler<ProductUpdated>, ProductCacheInvalidationHandler>();
        services.AddScoped<IEventHandler<ProductDeactivated>, ProductCacheInvalidationHandler>();
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
        services.AddSingleton<ISocialLoginProvider, StubSocialLoginProvider>();
        services.AddScoped<IOAuthClientValidator, OAuthClientValidatorAdapter>();
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
        services.AddScoped<ECommerce.UseCases.Content.Ports.IContentRepository, ECommerce.Infrastructure.Content.ContentRepository>();
        services.AddScoped<IFulfillmentTaskRepository, FulfillmentTaskRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddSingleton<IShippingRateCache, InMemoryShippingRateCache>();
        services.AddHttpClient<AramexCarrierAdapter>(client => client.Timeout = TimeSpan.FromSeconds(15))
            .AddStandardResilienceHandler();
        services.AddHttpClient<DhlCarrierAdapter>(client => client.Timeout = TimeSpan.FromSeconds(15))
            .AddStandardResilienceHandler();
        services.AddScoped<ICarrierAdapter>(sp => sp.GetRequiredService<AramexCarrierAdapter>());
        services.AddScoped<ICarrierAdapter>(sp => sp.GetRequiredService<DhlCarrierAdapter>());
        services.AddScoped<CarrierRateSelector>();
        services.AddScoped<PickListGenerationService>();
        services.AddSingleton<OutboxMetrics>();
        services.AddSingleton<BusinessMetrics>();
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

        services.AddScoped<IRealtimeEventStore, RealtimeEventStore>();
        services.AddScoped<IRealtimeEventForwarder, RealtimeEventForwarder>();
        services.AddScoped<IEventHandler<OrderStatusChanged>, OrderRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<OrderTimelineUpdated>, OrderRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<FulfillmentTaskCreated>, WarehouseRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<FulfillmentTaskAssigned>, WarehouseRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<FulfillmentTaskPicking>, WarehouseRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<FulfillmentTaskPacked>, WarehouseRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<FulfillmentTaskShipped>, WarehouseRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<FulfillmentTaskCancelled>, WarehouseRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<FulfillmentTaskSplit>, WarehouseRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<LowStockAlertRaised>, WarehouseRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<LowStockAlertRaised>, AdminRealtimeBroadcaster>();
        services.AddScoped<IEventHandler<ReconciliationDriftDetected>, AdminRealtimeBroadcaster>();
        services.AddScoped<LiveOpsMetricsJob>();

        services.AddScoped<IReportingQueryService, ReportingQueryService>();
        services.AddScoped<IGrpcQueryService, GrpcQueryService>();
        services.AddScoped<IExportJobRepository, ExportJobRepository>();
        services.AddScoped<IExportJobScheduler, HangfireExportJobScheduler>();
        services.AddScoped<IExportFileStore>(sp =>
            new LocalExportFileStore(
                sp.GetRequiredService<ILogger<LocalExportFileStore>>(),
                sp.GetRequiredService<IConfiguration>().GetValue("Storage:BasePath", "./storage")));
        services.AddScoped<GenerateExportJob>();

        services.AddScoped<IWebhookEndpointRepository, WebhookEndpointRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();
        services.AddScoped<IWebhookDeadLetterRepository, WebhookDeadLetterRepository>();
        services.AddScoped<IWebhookSigner, HmacWebhookSigner>();
        services.AddScoped<IWebhookHttpDeliverer, HttpWebhookDeliverer>();
        services.AddScoped<IWebhookDeliveryJobScheduler, HangfireWebhookDeliveryJobScheduler>();
        services.AddScoped<DeliverWebhookJob>();
        services.AddScoped<WebhookDeliveryService>();
        services.AddOptions<WebhookOptions>().BindConfiguration(WebhookOptions.SectionName);
        services.AddScoped<IEventHandler<OrderPlaced>, WebhookEventDispatcher>();
        services.AddScoped<IEventHandler<PaymentCaptured>, WebhookEventDispatcher>();
        services.AddScoped<IEventHandler<OrderShipped>, WebhookEventDispatcher>();
        services.AddScoped<IEventHandler<OrderCancelled>, WebhookEventDispatcher>();
        services.AddScoped<IEventHandler<RefundCompleted>, WebhookEventDispatcher>();
        services.AddScoped<IEventHandler<ProductUpdated>, WebhookEventDispatcher>();
        services.AddScoped<IEventHandler<LowStockAlertRaised>, WebhookEventDispatcher>();
        services.AddHttpClient("webhooks", client => client.Timeout = TimeSpan.FromSeconds(30))
            .AddStandardResilienceHandler();

        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis");

        services.AddHttpClient<IPasswordBreachChecker, HibpPasswordBreachChecker>(client =>
            client.Timeout = TimeSpan.FromSeconds(10))
            .AddStandardResilienceHandler();

        services.AddHostedService<MigrateOnStartupHostedService>();
        services.AddScoped<PostCommitActions>();
        services.AddHostedService<OutboxBackgroundService>();

        services.AddSingleton<IDbConnectionFactory, DapperReadModelStore>();
        services.AddSingleton<IReadModelStore, DapperReadModelStore>();
        services.AddScoped<ECommerce.UseCases.Wallets.Ports.IWalletRepository, ECommerce.Infrastructure.Wallets.WalletRepository>();
        services.AddScoped<ECommerce.UseCases.Tenants.Ports.ITenantRepository, ECommerce.Infrastructure.Tenants.TenantRepository>();
        services.AddScoped<IPartnerRepository, PostgresPartnerRepository>();
        services.AddScoped<IPartnerAuthService, PostgresPartnerAuthService>();
        services.AddScoped<IRecommendationService, CollaborativeFilteringRecommendationService>();
        services.AddSingleton<ICurrencyExchangeService, CurrencyExchangeService>();
        services.AddScoped<DapperProductReadService>();
        services.AddScoped<DapperOrderReadService>();
        services.AddScoped<DapperStockReadService>();

        return services;
    }

    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PaymentProviderOptions>(configuration.GetSection(PaymentProviderOptions.SectionName));
        services.Configure<StripeWebhookOptions>(configuration.GetSection(StripeWebhookOptions.SectionName));
        services.AddOptions<PaymentRetryOptions>().BindConfiguration(PaymentRetryOptions.SectionName);
        services.AddOptions<RefundRetryOptions>().BindConfiguration(RefundRetryOptions.SectionName);
        services.AddSingleton<IPaymentProviderHealth, PaymentCircuitBreaker>();
        services.AddSingleton<IPaymentProvider, MockPaymentProvider>();

        var paymentOptions = configuration
            .GetSection(PaymentProviderOptions.SectionName)
            .Get<PaymentProviderOptions>() ?? new PaymentProviderOptions();

        if (paymentOptions.Stripe.Enabled && !string.IsNullOrWhiteSpace(paymentOptions.Stripe.SecretKey))
        {
            services.AddHttpClient("Stripe")
                    .AddStandardResilienceHandler();

            services.AddSingleton<IPaymentProvider>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                return new StripePaymentProvider(paymentOptions.Stripe.SecretKey, httpClientFactory);
            });
        }

        services.AddScoped<IPaymentProvider, ECommerce.Infrastructure.Wallets.WalletPaymentProvider>();

        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<IRefundRetryJobScheduler, HangfireRefundRetryJobScheduler>();

        return services;
    }

    public static IServiceCollection AddSearchInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var elasticOptions = configuration
            .GetSection(ElasticSearchOptions.SectionName)
            .Get<ElasticSearchOptions>() ?? new ElasticSearchOptions();

        services.Configure<ElasticSearchOptions>(configuration.GetSection(ElasticSearchOptions.SectionName));

        if (elasticOptions.Enabled)
        {
            services.AddSingleton(sp =>
            {
                var settings = new ElasticsearchClientSettings(new Uri(elasticOptions.Uri))
                    .DefaultIndex(elasticOptions.IndexName);
                return new ElasticsearchClient(settings);
            });
            services.AddScoped<ProductIndexerService>();
            services.AddScoped<IProductSearchRepository, ElasticProductSearchRepository>();
        }

        return services;
    }
}
