# 25 — Eventing, Outbox & MassTransit

## 1. Overview

Domain events are persisted in an outbox table within the same transaction as business state, then dispatched asynchronously to in-process handlers and (optionally) to RabbitMQ via MassTransit. This guarantees at-least-once delivery without dual-write problems. The outbox is polled by `OutboxBackgroundService`; messages that exceed the retry budget are dead-lettered.

## 2. Domain Entities

| Entity | Namespace | Purpose |
|--------|-----------|---------|
| `IDomainEvent` | `Domain.Abstractions` | Marker interface for all domain events. |
| `IHasDomainEvents` | `Domain.Abstractions` | Aggregate interface exposing `DomainEvents` collection. |
| `OutboxMessage` | `Infrastructure.Outbox` | Outbox row: `Id`, `AggregateId`, `EventType` (assembly-qualified), `Content` (JSON), `OccurredOn`, `ProcessedOn`, `Attempts`, `Error`. |
| `OutboxBackgroundService` | `Infrastructure.Outbox` | Hosted service that polls and dispatches outbox messages. |
| `OutboxPublisher` | `Infrastructure.Messaging` | Dispatches to `IEventDispatcher` (in-process) and `IPublishEndpoint` (MassTransit). |
| `OutboxMetrics` | `Infrastructure.Messaging` | OpenTelemetry-style metrics for outbox health. |
| `InboxMessage` | `Infrastructure.Messaging` | Deduplication inbox for incoming bus messages (idempotent consumers). |

### Key Domain Events

| Event | Aggregate | Triggered By |
|-------|-----------|-------------|
| `PaymentCaptured` | Payment | `Payment.Capture()` |
| `PaymentRefunded` | Payment | `Payment.MarkRefunded()` |
| `RefundRequested` | Refund | `Refund.Create()` |
| `RefundApproved` | Refund | `Refund.Approve()` |
| `RefundCompleted` | Refund | `Refund.MarkCompleted()` |
| `InvoiceIssued` | Invoice | `Invoice.Create()` |
| `InvoiceCredited` | Invoice | `Invoice.ApplyCreditNote()` |
| `CreditNoteIssued` | CreditNote | `CreditNote.Create()` |
| `ReviewSubmitted` | ProductReview | `ProductReview.Create()` |
| `ReviewPublished` | ProductReview | `ProductReview.Publish()` |
| `ReconciliationDriftDetected` | PaymentReconciliationRecord | `MarkDrift()`, `MarkUnmatched()` |

### Outbox Processing Flow

```
Domain mutation (in UoW)
  -> AddDomainEvent() on aggregate
  -> EventDispatcher writes OutboxMessage row (same DB transaction)
  -> Commit

OutboxBackgroundService (polling loop)
  -> SELECT ... FOR UPDATE SKIP LOCKED (batch of N)
  -> For each message:
       -> Deserialize EventType -> IDomainEvent
       -> OutboxPublisher.PublishAsync():
            1. IEventDispatcher.DispatchAsync()  [in-process handlers]
            2. IPublishEndpoint.Publish()         [MassTransit -> RabbitMQ]
       -> Mark ProcessedOn
  -> Commit transaction
```

## 3. Key Operations

| Operation | Component | Description |
|-----------|-----------|-------------|
| **Domain event creation** | `BaseEntity<T>.AddDomainEvent()` | Appends event to aggregate's in-memory collection |
| **Event persistence** | `EventDispatcher` / `UnitOfWork.SaveChangesAsync()` | Writes `OutboxMessage` rows within the same DB transaction |
| **Outbox polling** | `OutboxBackgroundService.ExecuteAsync()` | Polls at configurable interval, processes batch with `SKIP LOCKED` |
| **In-process dispatch** | `IEventDispatcher.DispatchAsync()` | Routes events to `IEventHandler<T>` implementations in the same process |
| **Bus publish** | `IPublishEndpoint.Publish()` | Publishes to RabbitMQ via MassTransit with outbox message ID |
| **Dead-lettering** | `OutboxBackgroundService` | After `MaxAttempts` (5), message is marked processed and `RecordDeadLetter()` fires |
| **Inbox deduplication** | `InboxMessage` / `InboxMessageRepository` | Prevents duplicate processing of bus messages |

### MassTransit Configuration

Configured in `DependencyInjection.AddMessageBus()`:
- **Transport**: RabbitMQ (connection string from `ConnectionStrings:RabbitMq`)
- **Consumers**: `OrderPlacedConsumer`, `OrderCancelledConsumer`, `OrderShippedConsumer`
- **Queue**: Single shared quorum queue configured with `SetQuorumQueue()` for HA
- **Graceful degradation**: If RabbitMQ connection string is empty, bus registration is skipped entirely

### Consumer Processing

Consumers receive events from the bus and handle cross-cutting concerns:
- `OrderPlacedConsumer` triggers order confirmation notifications
- `OrderCancelledConsumer` triggers cancellation notifications
- `OrderShippedConsumer` triggers shipping notifications

All consumers use the `InboxMessage` pattern for idempotent processing.

## 4. API Endpoints

The eventing layer has no direct API endpoints. It operates as infrastructure:
- **Outbox is internal**: `OutboxBackgroundService` runs as a hosted service.
- **Domain events are fire-and-forget**: Controllers trigger domain mutations that emit events.
- **Bus consumers are endpoint-driven**: RabbitMQ delivers messages to registered consumers.

## 5. Integration Points

- **Same-transaction persistence**: Outbox messages are written in the same EF Core transaction as business state, eliminating dual-write inconsistency.
- **MassTransit + RabbitMQ**: `OutboxPublisher` uses `IPublishEndpoint` to publish to RabbitMQ with quorum queues for durability.
- **In-process event handlers**: `IEventHandler<T>` implementations (e.g., `InvoiceOnPaymentCapturedHandler`, `LowStockAlertNotificationHandler`, `NotificationOrderNotifier`) handle cross-cutting concerns without going through the bus.
- **Inbox pattern**: `InboxMessage` entity and `InboxMessageRepository` provide deduplication for incoming bus messages.
- **Metrics**: `OutboxMetrics` exposes `outbox.messages.published`, `outbox.messages.dead_lettered`, and `outbox.lag_seconds` for monitoring.
- **Feature flags**: `IFeatureFlagService` can gate notification dispatch triggered by domain events.

## 6. File References

| File | Path |
|------|------|
| `IDomainEvent.cs` | `src/ECommerce.Domain/Abstractions/IDomainEvent.cs` |
| `IHasDomainEvents.cs` | `src/ECommerce.Domain/Abstractions/IHasDomainEvents.cs` |
| `OutboxBackgroundService.cs` | `src/ECommerce.Infrastructure/Outbox/OutboxBackgroundService.cs` |
| `OutboxMessage.cs` | `src/ECommerce.Infrastructure/Outbox/OutboxMessage.cs` |
| `OutboxMessageConfiguration.cs` | `src/ECommerce.Infrastructure/Outbox/OutboxMessageConfiguration.cs` |
| `OutboxPublisher.cs` | `src/ECommerce.Infrastructure/Messaging/OutboxPublisher.cs` |
| `OutboxMetrics.cs` | `src/ECommerce.Infrastructure/Messaging/OutboxMetrics.cs` |
| `InboxMessage.cs` | `src/ECommerce.Infrastructure/Messaging/InboxMessage.cs` |
| `InboxMessageRepository.cs` | `src/ECommerce.Infrastructure/Messaging/InboxMessageRepository.cs` |
| `DependencyInjection.cs` (Messaging) | `src/ECommerce.Infrastructure/Messaging/DependencyInjection.cs` |
| `EventDispatcher.cs` | `src/ECommerce.Infrastructure/Common/EventDispatcher.cs` |
| `UnitOfWork.cs` | `src/ECommerce.Infrastructure/Common/UnitOfWork.cs` |
| `InvoiceOnPaymentCapturedHandler.cs` | `src/ECommerce.Infrastructure/Invoicing/InvoiceOnPaymentCapturedHandler.cs` |
| `CreditNoteOnPaymentRefundedHandler.cs` | `src/ECommerce.Infrastructure/Invoicing/CreditNoteOnPaymentRefundedHandler.cs` |
| `NotificationOrderNotifier.cs` | `src/ECommerce.Infrastructure/Notifications/NotificationOrderNotifier.cs` |
| `LowStockAlertNotificationHandler.cs` | `src/ECommerce.Infrastructure/Notifications/LowStockAlertNotificationHandler.cs` |
