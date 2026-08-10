using System.Globalization;
using ECommerce.Domain.Events;
using ECommerce.Domain.Notifications;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Flags.Ports;
using ECommerce.UseCases.Messaging.Ports;
using ECommerce.UseCases.Notifications.Ports;
using ECommerce.UseCases.Notifications.Services;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Notifications;

public sealed class NotificationOrderNotifier(
    NotificationDispatcher dispatcher,
    IFeatureFlagService flags,
    ILogger<NotificationOrderNotifier> logger) : IOrderNotifier
{
    public const string OrderConfirmationFlag = "notifications.order-confirmation.enabled";

    public async Task NotifyPlacedAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken)
    {
        if (!await flags.IsEnabledAsync(OrderConfirmationFlag, cancellationToken))
        {
            logger.LogInformation(
                "Order confirmation notifications disabled by flag for order {OrderId}.",
                orderPlaced.OrderId);
            return;
        }

        await dispatcher.DispatchAsync(new NotificationRequest(
            CustomerId: null,
            Channel: NotificationChannel.Email,
            Kind: NotificationKind.OrderConfirmation,
            TemplateKey: "order.confirmation",
            Locale: "en",
            Recipient: orderPlaced.CustomerEmail,
            ReferenceId: orderPlaced.OrderId.ToString("N"),
            Placeholders: new Dictionary<string, string>
            {
                ["OrderNumber"] = orderPlaced.OrderNumber,
                ["Total"] = orderPlaced.Total.ToString("0.00", CultureInfo.InvariantCulture),
                ["Currency"] = orderPlaced.Currency
            },
            Transactional: true), cancellationToken);
    }
}
