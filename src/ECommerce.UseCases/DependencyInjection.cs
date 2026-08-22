using ECommerce.Domain.Events;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Cart.Services;
using ECommerce.UseCases.Catalog.Services;
using ECommerce.UseCases.Checkout.Services;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Events;
using ECommerce.UseCases.Invoicing.Services;
using ECommerce.UseCases.Notifications.Services;
using ECommerce.UseCases.Payments.Services;
using ECommerce.UseCases.Pricing;
using ECommerce.UseCases.Promotions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILocaleCatalog, DefaultLocaleCatalog>();
        services.AddSingleton<ICurrencyCatalog, DefaultCurrencyCatalog>();
        services.AddScoped<IEventHandler<CustomerRegistered>, CustomerRegisteredEmailHandler>();
        services.AddScoped<IEventHandler<PasswordResetRequested>, PasswordResetRequestedEmailHandler>();
        services.AddScoped<IEventHandler<PasswordReset>, PasswordResetNotificationHandler>();
        services.AddScoped<TokenPairFactory>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<CheckoutTotalsCalculator>();
        services.AddScoped<StockAvailabilityVerifier>();
        services.AddScoped<PaymentIntentService>();
        services.AddScoped<CartMergeService>();
        services.AddScoped<NotificationDispatcher>();
        services.AddScoped<PromotionScheduleEnforcer>();
        services.AddScoped<ReconciliationService>();
        services.AddScoped<ProductImportService>();
        services.AddScoped<InvoiceIssuanceService>();
        services.AddScoped<InvoicePdfGenerationService>();

        return services;
    }
}
