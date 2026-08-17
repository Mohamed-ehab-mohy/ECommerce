# 24 — Background Jobs Design

## 1. Overview

The platform uses Hangfire (PostgreSQL-backed) for scheduled and fire-and-forget background jobs, and a custom `OutboxBackgroundService` for reliable domain event dispatch. Jobs handle notifications, reconciliation, export generation, refund retries, webhook delivery, import processing, invoice PDF generation, and real-time metrics push.

## 2. Domain Entities

| Entity | Namespace | Purpose |
|--------|-----------|---------|
| `OutboxMessage` | `Infrastructure.Outbox` | Persistence entity for outbox events: `Id`, `AggregateId`, `EventType`, `Content`, `OccurredOn`, `ProcessedOn`, `Attempts`, `Error`. |
| `OutboxBackgroundService` | `Infrastructure.Outbox` | `BackgroundService` that polls unprocessed outbox messages and dispatches them. |

### Job Catalog

| Job | Schedule / Trigger | Retries | Purpose |
|-----|-------------------|---------|---------|
| `SendNotificationJob` | Fire-and-forget (Hangfire) | 5 | Delivers `NotificationEnvelope` via `NotificationSender` |
| `NightlyReconciliationJob` | Cron `0 2 * * *` | 3 | Provider vs platform payment reconciliation |
| `GenerateExportJob` | Fire-and-forget (Hangfire) | 2 | Async CSV export generation |
| `RetryFailedRefundsJob` | Scheduled on failure | 0 (manual) | Retries a failed refund through the provider |
| `ProcessProductImportJob` | Fire-and-forget | configured | Processes bulk product CSV imports |
| `GenerateInvoicePdfJob` | Fire-and-forget | configured | Renders invoice PDF via QuestPDF |
| `DeliverWebhookJob` | Fire-and-forget (manual retry) | 0 (manual) | Single webhook delivery attempt with retry/suspend policy |
| `ExpiredCartPurgeJob` | Scheduled | configured | Cleans up expired carts |
| `PromotionScheduleEnforcerJob` | Scheduled | configured | Activates/pauses promotions based on schedule windows |
| `LiveOpsMetricsJob` | Cron `*/30 * * * * *` (30s) | configured | Pushes live order metrics to admin SignalR hub |

## 3. Key Operations

### Outbox Polling (`OutboxBackgroundService`)

1. Polls every `Outbox:PollingIntervalSeconds` (default 5s).
2. Fetches up to `Outbox:BatchSize` (default 20) unprocessed messages using `FOR UPDATE SKIP LOCKED` (concurrency-safe).
3. For each message, deserializes `IDomainEvent` from `EventType`/`Content` and calls `OutboxPublisher.PublishAsync()`.
4. On success: marks `ProcessedOn` set. On failure: increments `Attempts`, records `Error`.
5. After 5 attempts (`MaxAttempts`), the message is dead-lettered and `OutboxMetrics.RecordDeadLetter()` fires.
6. Commits the transaction and runs post-commit actions.

### Outbox Publisher (`OutboxPublisher`)

1. Records lag metric: `DateTime.UtcNow - message.OccurredOn`.
2. Dispatches via `IEventDispatcher` for in-process domain event handlers.
3. If `IPublishEndpoint` (MassTransit) is configured, publishes to the message bus with the outbox message ID as `MessageId`.
4. Records published metric.

### Hangfire Job Scheduling

- `NotificationQueue` enqueues `SendNotificationJob` via `IBackgroundJobClient.Enqueue()`.
- `HangfireExportJobScheduler` enqueues `GenerateExportJob`.
- `HangfireInvoicePdfJobScheduler` enqueues `GenerateInvoicePdfJob`.
- `HangfireWebhookDeliveryJobScheduler` enqueues `DeliverWebhookJob`.
- Hangfire server configured with `WorkerCount = 2`, PostgreSQL storage with 5-min invisibility timeout.

## 4. API Endpoints

Background jobs are not directly exposed via REST endpoints. They are triggered by:
- Domain event handlers (fire-and-forget via Hangfire)
- API actions that enqueue jobs (export creation, refund execution)
- Cron schedules configured in Hangfire
- `OutboxBackgroundService` runs as a hosted `BackgroundService`

## 5. Integration Points

- **Outbox pattern**: `OutboxBackgroundService` provides at-least-once delivery of domain events to both in-process handlers and the message bus.
- **MassTransit**: `OutboxPublisher` publishes via `IPublishEndpoint` when RabbitMQ is configured.
- **PostgreSQL**: Hangfire storage and outbox table share the same database, enabling transactional consistency.
- **Metrics**: `OutboxMetrics` tracks `outbox.messages.published`, `outbox.messages.dead_lettered`, and `outbox.lag_seconds` via `System.Diagnostics.Metrics`.
- **Concurrency**: `FOR UPDATE SKIP LOCKED` prevents multiple workers from processing the same outbox batch.

## 6. File References

| File | Path |
|------|------|
| `OutboxBackgroundService.cs` | `src/ECommerce.Infrastructure/Outbox/OutboxBackgroundService.cs` |
| `OutboxMessage.cs` | `src/ECommerce.Infrastructure/Outbox/OutboxMessage.cs` |
| `OutboxPublisher.cs` | `src/ECommerce.Infrastructure/Messaging/OutboxPublisher.cs` |
| `OutboxMetrics.cs` | `src/ECommerce.Infrastructure/Messaging/OutboxMetrics.cs` |
| `DependencyInjection.cs` (Jobs) | `src/ECommerce.Infrastructure/Jobs/DependencyInjection.cs` |
| `SendNotificationJob.cs` | `src/ECommerce.Infrastructure/Jobs/SendNotificationJob.cs` |
| `NightlyReconciliationJob.cs` | `src/ECommerce.Infrastructure/Jobs/NightlyReconciliationJob.cs` |
| `GenerateExportJob.cs` | `src/ECommerce.Infrastructure/Jobs/GenerateExportJob.cs` |
| `RetryFailedRefundsJob.cs` | `src/ECommerce.Infrastructure/Jobs/RetryFailedRefundsJob.cs` |
| `ProcessProductImportJob.cs` | `src/ECommerce.Infrastructure/Jobs/ProcessProductImportJob.cs` |
| `GenerateInvoicePdfJob.cs` | `src/ECommerce.Infrastructure/Jobs/GenerateInvoicePdfJob.cs` |
| `DeliverWebhookJob.cs` | `src/ECommerce.Infrastructure/Jobs/DeliverWebhookJob.cs` |
| `ExpiredCartPurgeJob.cs` | `src/ECommerce.Infrastructure/Jobs/ExpiredCartPurgeJob.cs` |
| `PromotionScheduleEnforcerJob.cs` | `src/ECommerce.Infrastructure/Jobs/PromotionScheduleEnforcerJob.cs` |
| `LiveOpsMetricsJob.cs` | `src/ECommerce.Infrastructure/Jobs/LiveOpsMetricsJob.cs` |
