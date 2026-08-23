using ECommerce.Domain.Events;
using ECommerce.Domain.Notifications;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Notifications.Ports;
using ECommerce.UseCases.Notifications.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Notifications;

public sealed class LowStockAlertOptions
{
    public const string SectionName = "Notifications:LowStock";

    public string OpsEmail { get; init; } = "ops@ecommerce.dev";
}

public sealed class LowStockAlertNotificationHandler(
    NotificationDispatcher dispatcher,
    IOptions<LowStockAlertOptions> options,
    ILogger<LowStockAlertNotificationHandler> logger) : IEventHandler<LowStockAlertRaised>
{
    public async Task HandleAsync(LowStockAlertRaised domainEvent, CancellationToken cancellationToken)
    {
        await dispatcher.DispatchAsync(new NotificationRequest(
            CustomerId: null,
            Channel: NotificationChannel.Email,
            Kind: NotificationKind.LowStockAlert,
            TemplateKey: "low.stock.alert",
            Locale: "en",
            Recipient: options.Value.OpsEmail,
            ReferenceId: $"{domainEvent.StockItemId:N}-{domainEvent.WarehouseId:N}",
            Placeholders: new Dictionary<string, string>
            {
                ["Sku"] = domainEvent.Sku,
                ["WarehouseId"] = domainEvent.WarehouseId.ToString("N"),
                ["Available"] = domainEvent.Available.ToString(),
                ["Threshold"] = domainEvent.Threshold.ToString()
            },
            Transactional: false), cancellationToken);

        logger.LogInformation(
            "Low-stock alert dispatched for {Sku} (warehouse {WarehouseId}).",
            domainEvent.Sku,
            domainEvent.WarehouseId);
    }
}
