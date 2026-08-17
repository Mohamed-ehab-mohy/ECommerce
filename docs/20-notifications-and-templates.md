# 20 — Notifications & Templates

## 1. Overview

The notification system delivers transactional messages (email, SMS) to customers and operations staff. It uses a provider-per-channel architecture, in-memory template rendering with locale fallback, Hangfire-based async dispatch, and per-user channel/kind preferences. Notifications are gated by feature flags.

## 2. Domain Entities

| Entity | Namespace | Purpose |
|--------|-----------|---------|
| `NotificationPreference` | `Notifications` | Per-customer, per-channel, per-kind toggle (`Enabled` flag). |
| `NotificationChannel` (enum) | `Notifications` | `Email` or `Sms`. |
| `NotificationKind` (enum) | `Notifications` | `OrderConfirmation`, `OrderStatusUpdate`, `LowStockAlert`, `WebhookSuspended`. |

### Key Ports (UseCases)

| Port | Purpose |
|------|---------|
| `INotificationProvider` | Channel-specific sender (`Channel`, `Key`, `SendAsync`). |
| `INotificationQueue` | Enqueues `NotificationEnvelope` for async delivery. |
| `INotificationTemplateStore` | Renders template key + locale + placeholders into subject/body. |

## 3. Key Operations

| Operation | Flow | Key Files |
|-----------|------|-----------|
| **Send notification** | `NotificationDispatcher` → `INotificationQueue.EnqueueAsync()` → Hangfire → `NotificationSender.SendAsync()` → matched `INotificationProvider` | `NotificationQueue.cs`, `NotificationSender.cs`, `SendNotificationJob.cs` |
| **Template rendering** | `INotificationTemplateStore.RenderAsync(key, locale, placeholders)` with locale fallback chain `["en", "ar"]` | `InMemoryNotificationTemplateStore.cs` |
| **Order placed notification** | `OrderPlaced` event → `NotificationOrderNotifier.NotifyPlacedAsync()` → dispatches `order.confirmation` template | `NotificationOrderNotifier.cs` |
| **Order shipped notification** | `OrderShipped` event → dispatches `order.shipped` template with carrier/tracking | `NotificationOrderNotifier.cs` |
| **Order cancelled notification** | `OrderCancelled` event → dispatches `order.cancelled` template with reason | `NotificationOrderNotifier.cs` |
| **Low stock alert** | `LowStockAlertRaised` event → `LowStockAlertNotificationHandler` → sends to ops email | `LowStockAlertNotificationHandler.cs` |
| **Update preferences** | User PUT → `UpdateNotificationPreferenceCommand` → `NotificationPreference.SetEnabled()` | `NotificationPreferencesController.cs` |

### Template Fallback Chain

Templates are keyed as `{templateKey}.{locale}` (e.g., `order.confirmation.en`, `order.confirmation.ar`). The store resolves in order: exact locale match → `en` → `ar`.

### Feature Flag Gating

Each order notification type is gated by a feature flag:
- `notifications.order-confirmation.enabled`
- `notifications.order-cancelled.enabled`
- `notifications.order-shipped.enabled`

If the flag is disabled, the notification is skipped with a log message.

## 4. API Endpoints

| Method | Route | Controller | Description |
|--------|-------|------------|-------------|
| `GET` | `/api/v1/me/notifications/preferences` | `NotificationPreferencesController` | List all notification preferences for the caller |
| `PUT` | `/api/v1/me/notifications/preferences/{channel}/{kind}` | `NotificationPreferencesController` | Toggle a notification preference on/off |

## 5. Integration Points

- **Domain events**: `OrderPlaced`, `OrderCancelled`, `OrderShipped`, `LowStockAlertRaised` → notification dispatch.
- **Hangfire**: `SendNotificationJob` (`[AutomaticRetry(Attempts = 5)]`) is enqueued by `NotificationQueue` for async delivery.
- **Feature flags**: `IFeatureFlagService` gates notification types per configurable flag.
- **Email channel**: `SmtpEmailProvider` uses MailKit; falls back to structured log when SMTP host is empty.
- **SMS channel**: `StubSmsProvider` logs outbound SMS (stub for future provider integration).
- **PII masking**: `PiiMasker.MaskEmail()` / `PiiMasker.MaskPhone()` protect PII in logs.

## 6. File References

| File | Path |
|------|------|
| `NotificationPreference.cs` | `src/ECommerce.Domain/Notifications/NotificationPreference.cs` |
| `NotificationKind.cs` | `src/ECommerce.Domain/Notifications/NotificationKind.cs` |
| `NotificationChannel.cs` | `src/ECommerce.Domain/Notifications/NotificationChannel.cs` |
| `NotificationSender.cs` | `src/ECommerce.Infrastructure/Notifications/NotificationSender.cs` |
| `NotificationQueue.cs` | `src/ECommerce.Infrastructure/Notifications/NotificationQueue.cs` |
| `NotificationOrderNotifier.cs` | `src/ECommerce.Infrastructure/Notifications/NotificationOrderNotifier.cs` |
| `InMemoryNotificationTemplateStore.cs` | `src/ECommerce.Infrastructure/Notifications/InMemoryNotificationTemplateStore.cs` |
| `SmtpEmailProvider.cs` | `src/ECommerce.Infrastructure/Notifications/SmtpEmailProvider.cs` |
| `StubSmsProvider.cs` | `src/ECommerce.Infrastructure/Notifications/StubSmsProvider.cs` |
| `LowStockAlertNotificationHandler.cs` | `src/ECommerce.Infrastructure/Notifications/LowStockAlertNotificationHandler.cs` |
| `SendNotificationJob.cs` | `src/ECommerce.Infrastructure/Jobs/SendNotificationJob.cs` |
| `NotificationPreferencesController.cs` | `src/ECommerce.API/Controllers/NotificationPreferencesController.cs` |
| `NotificationRequests.cs` | `src/ECommerce.API/Controllers/NotificationRequests.cs` |
