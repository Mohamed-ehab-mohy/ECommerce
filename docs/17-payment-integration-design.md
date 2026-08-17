# 17 — Payment Integration Design

## Overview

The Payment module handles the full payment lifecycle: intent creation, authorization, capture, void, refund, retry, and nightly reconciliation. It abstracts payment providers (Stripe, Adyen, PayPal) behind an `IPaymentProvider` interface, supports bounded retries with cooldown, maintains an append-only payment ledger for audit/reconciliation, and integrates a `ReconciliationService` that compares platform records against provider statements.

## Domain Entities

### Payment (`Domain/Payments/Payment.cs:7`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Payment` | `OrderId`, `CustomerId`, `ProviderKey`, `ProviderToken`, `ClientToken`, `ProviderReference`, `Currency`, `Amount`, `FxRate`, `AuthorizedAmount`, `Status`, `Attempt`, `RetryAfterUtc` | Aggregate root |
| `PaymentAttempt` | `PaymentId`, `AttemptNumber`, `Action`, `Amount`, `Status`, `ProviderResponse`, `TraceId` | Per-attempt record |
| `PaymentLedgerEntry` | `PaymentId`, `Sequence`, `EventType`, `Status`, `Amount`, `ProviderReference`, `Detail`, `OccurredAt` | Append-only audit trail |

### PaymentStatus Enum (`Domain/Payments/PaymentStatus.cs:3`)

```
Created → Authorized → Captured → Refunding → Refunded
   ↓                                             
Failed → RetryPending → (re-attempt)            
   ↓                                             
Cancelled (via Void)                             
   ↓                                             
RefundFailed                                     
```

### Refund (`Domain/Payments/Refund.cs:12`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Refund` | `OrderId`, `PaymentId`, `Amount`, `Currency`, `Reason`, `Restock`, `IdempotencyKey`, `Status`, `ProviderReference`, `FailureDetail`, `ApprovedBy`, `Attempts`, `Items` | Aggregate root |
| `RefundItem` | Line-item detail for partial refunds | |
| `RefundStatus` | `Requested`, `Approved`, `Rejected`, `Executing`, `Completed`, `Failed` | |

### Provider Interface (`Domain/Payments/IPaymentProvider.cs:47`)

| Operation | Request/Result | Description |
|---|---|---|
| `CreateIntentAsync` | `PaymentIntentRequest` → `PaymentIntentResult` | Create payment intent with provider |
| `AuthorizeAsync` | `PaymentAuthorizationRequest` → `PaymentAuthorizationResult` | Authorize against intent |
| `RefundAsync` | `PaymentRefundRequest` → `PaymentRefundResult` | Execute refund (idempotent on refund ID) |
| `ListTransactionsAsync` | from/to window → `ProviderTransaction[]` | Provider-side transactions for reconciliation |

## Key Operations

| Operation | Domain Method | Description |
|---|---|---|
| **Create intent** | `PaymentIntentService.CreateIntentAsync()` | Resolve provider, call `CreateIntentAsync`, create `Payment` aggregate |
| **Authorize** | `Payment.MarkAuthorized()` | Created/Failed/RetryPending → Authorized; records ledger entry |
| **Capture** | `Payment.Capture(amount)` | Authorized → Captured; emits `PaymentCaptured` (triggers invoice issuance) |
| **Void** | `Payment.Void()` | Authorized → Cancelled |
| **Fail** | `Payment.MarkFailed()` | Marks as Failed with decline code; records ledger |
| **Plan retry** | `Payment.PlanRetry(cooldown, maxAttempts)` | Failed → RetryPending with cooldown window |
| **Check retry** | `Payment.CanRetry(utcNow)` | Validates not exhausted and cooldown elapsed |
| **Request refund** | `Payment.RequestRefund()` | Captured → Refunding |
| **Complete refund** | `Payment.MarkRefunded()` | Refunding → Refunded; emits `PaymentRefunded` |

### Refund Lifecycle

1. `RequestRefundCommandHandler` — Creates `Refund` aggregate in `Requested` status
2. `ApproveRefundCommandHandler` — Admin approves → `Approved`
3. `ExecuteRefundCommandHandler` — Calls `IPaymentProvider.RefundAsync()`, handles success/failure, optionally releases stock via `IStockAllocator`
4. Retries on failure up to configured limits

### Reconciliation (`UseCases/Payments/Services/ReconciliationService.cs:17`)

| Operation | Method | Description |
|---|---|---|
| Snapshot pending | `SnapshotPendingAsync()` | Create `PaymentReconciliationRecord` for unreconciled payments |
| Run reconciliation | `RunAsync()` | Compare platform records against provider `ListTransactionsAsync()` results; mark Matched/Drift/Unmatched |

Provider transactions are compared by reference and amount within a 7-day lookback window. Drifts are logged to the audit system.

### Payment Ledger

Every state transition appends an immutable `PaymentLedgerEntry` (US-G-007). Events: `intent_created`, `authorized`, `failed`, `captured`, `voided`, `refund_requested`, `refunded`. Rows are never updated or deleted.

## API Endpoints

### Payments — `PaymentsController.cs:11` — `/api/v1/payments`

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/payments/{paymentId}/authorize` | Trigger authorization for a payment intent |

### Refunds — `RefundsController.cs`

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/refunds` | Request a refund |
| `POST` | `/api/v1/refunds/{id}/approve` | Approve refund |
| `POST` | `/api/v1/refunds/{id}/execute` | Execute refund against provider |

### Reconciliation — `ReconciliationController.cs`

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/reconciliation/run` | Trigger reconciliation run |

## Integration Points

- **Checkout**: `PaymentIntentService` is called during checkout initiation. Payment is authorized before order placement; captured at `PlaceOrderCommandHandler`.
- **Order**: `Payment.Capture()` emits `PaymentCaptured` → triggers invoice issuance. `Payment.Refund()` emits `PaymentRefunded` → triggers credit note.
- **Inventory**: `ExecuteRefundCommandHandler` calls `IStockAllocator.ReleaseAsync()` when `Refund.Restock = true`.
- **Provider Abstraction**: `IPaymentProviderFactory` resolves provider by `Key` (e.g., `"stripe"`, `"adyen"`, `"paypal"`). `IPaymentProviderHealth` tracks provider availability for circuit-breaking.
- **Retry Policy**: `PaymentRetryOptions` / `RefundRetryOptions` configure cooldown and max attempts.
- **Domain Events**: `PaymentCaptured`, `PaymentRefunded`, `RefundRequested`, `RefundApproved`, `RefundRejected`, `RefundExecuting`, `RefundCompleted`, `RefundFailed`, `ReconciliationDriftDetected`.

## File References

| File | Purpose |
|---|---|
| `src/ECommerce.Domain/Payments/Payment.cs` | Payment aggregate with full lifecycle |
| `src/ECommerce.Domain/Payments/PaymentStatus.cs` | 9-state payment status enum |
| `src/ECommerce.Domain/Payments/PaymentLedgerEntry.cs` | Append-only ledger entry |
| `src/ECommerce.Domain/Payments/PaymentAttempt.cs` | Per-attempt record |
| `src/ECommerce.Domain/Payments/IPaymentProvider.cs` | Provider abstraction + DTOs |
| `src/ECommerce.Domain/Payments/Refund.cs` | Refund aggregate |
| `src/ECommerce.Domain/Payments/RefundStatus.cs` | Refund lifecycle enum |
| `src/ECommerce.Domain/Payments/RefundItem.cs` | Refund line item |
| `src/ECommerce.API/Controllers/PaymentsController.cs` | Payment REST API |
| `src/ECommerce.API/Controllers/RefundsController.cs` | Refund REST API |
| `src/ECommerce.API/Controllers/ReconciliationController.cs` | Reconciliation REST API |
| `src/ECommerce.UseCases/Payments/Services/PaymentIntentService.cs` | Intent creation orchestration |
| `src/ECommerce.UseCases/Payments/Services/ReconciliationService.cs` | Nightly reconciliation service |
| `src/ECommerce.UseCases/Payments/Handlers/AuthorizePaymentCommandHandler.cs` | Authorization handler |
| `src/ECommerce.UseCases/Payments/Handlers/ExecuteRefundCommandHandler.cs` | Refund execution handler |
| `src/ECommerce.UseCases/Payments/Handlers/RunReconciliationCommandHandler.cs` | Reconciliation trigger |
| `src/ECommerce.UseCases/Payments/Options/PaymentRetryOptions.cs` | Retry configuration |
