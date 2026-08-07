using ECommerce.Infrastructure.Audit;
using ECommerce.Infrastructure.Carts;
using ECommerce.Infrastructure.Catalog;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Inventory;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Orders;
using ECommerce.Infrastructure.Outbox;
using ECommerce.Infrastructure.Payments;
using ECommerce.Infrastructure.Redis;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Messaging.Ports;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Payments.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockAllocator, StockAllocator>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICheckoutRepository, CheckoutRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IIdempotencyKeyRepository, IdempotencyKeyRepository>();
        services.AddScoped<IShippingRateProvider, ShippingRateStubProvider>();
        services.AddScoped<ITaxCalculator, FlatTaxCalculator>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailSender, LogEmailSender>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddSingleton<ILoginAttemptThrottler, InMemoryLoginAttemptThrottler>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();
        services.AddScoped<IOrderNotifier, LogOrderNotifier>();
        services.AddSingleton<OutboxMetrics>();
        services.AddScoped<OutboxPublisher>();

        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis");

        services.AddHttpClient<IPasswordBreachChecker, HibpPasswordBreachChecker>(client =>
            client.Timeout = TimeSpan.FromSeconds(10));

        services.AddHostedService<OutboxBackgroundService>();

        return services;
    }

    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PaymentProviderOptions>(configuration.GetSection(PaymentProviderOptions.SectionName));
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        return services;
    }
}
