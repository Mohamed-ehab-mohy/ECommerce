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

    public const string OrderCancellationFlag = "notifications.order-cancelled.enabled";

    public const string OrderShippedFlag = "notifications.order-shipped.enabled";

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

    public async Task NotifyCancelledAsync(OrderCancelled orderCancelled, CancellationToken cancellationToken)
    {
        if (!await flags.IsEnabledAsync(OrderCancellationFlag, cancellationToken))
        {
            logger.LogInformation(
                "Order cancellation notifications disabled by flag for order {OrderId}.",
                orderCancelled.OrderId);
            return;
        }

        await dispatcher.DispatchAsync(new NotificationRequest(
            CustomerId: null,
            Channel: NotificationChannel.Email,
            Kind: NotificationKind.OrderStatusUpdate,
            TemplateKey: "order.cancelled",
            Locale: "en",
            Recipient: orderCancelled.CustomerEmail,
            ReferenceId: orderCancelled.OrderId.ToString("N"),
            Placeholders: new Dictionary<string, string>
            {
                ["OrderNumber"] = orderCancelled.OrderNumber,
                ["Total"] = orderCancelled.Total.ToString("0.00", CultureInfo.InvariantCulture),
                ["Currency"] = orderCancelled.Currency,
                ["Reason"] = orderCancelled.Reason
            },
            Transactional: true), cancellationToken);
    }

    public async Task NotifyShippedAsync(OrderShipped orderShipped, CancellationToken cancellationToken)
    {
        if (!await flags.IsEnabledAsync(OrderShippedFlag, cancellationToken))
        {
            logger.LogInformation(
                "Order shipped notifications disabled by flag for order {OrderId}.",
                orderShipped.OrderId);
            return;
        }

        var tracking = orderShipped.TrackingNumbers.Count > 0
            ? string.Join(", ", orderShipped.TrackingNumbers)
            : "—";

        await dispatcher.DispatchAsync(new NotificationRequest(
            CustomerId: null,
            Channel: NotificationChannel.Email,
            Kind: NotificationKind.OrderStatusUpdate,
            TemplateKey: "order.shipped",
            Locale: "en",
            Recipient: orderShipped.CustomerEmail,
            ReferenceId: orderShipped.OrderId.ToString("N"),
            Placeholders: new Dictionary<string, string>
            {
                ["OrderNumber"] = orderShipped.OrderNumber,
                ["Carrier"] = orderShipped.CarrierKey,
                ["TrackingNumbers"] = tracking
            },
            Transactional: true), cancellationToken);
    }
}
