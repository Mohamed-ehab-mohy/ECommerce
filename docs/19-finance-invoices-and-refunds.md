# 19 — Finance, Invoices & Refunds

## 1. Overview

This module covers the financial backbone of the platform: payment processing, append-only ledger entries, invoice issuance, credit notes, refund lifecycle, and nightly provider reconciliation. It enforces double-entry-style auditability (US-G-007) and idempotent provider interactions (QAS-04).

## 2. Domain Entities

| Entity | Namespace | Purpose |
|--------|-----------|---------|
| `Payment` | `Payments` | Aggregate root. Tracks authorization, capture, void, and refund states. Owns attempts and ledger entries. |
| `PaymentAttempt` | `Payments` | Immutable record of a single provider interaction (authorize, capture, refund). |
| `PaymentLedgerEntry` | `Payments` | Append-only ledger row. Never updated or deleted; provides reconciliation-ready audit trail (T-DAT-010). |
| `Refund` | `Payments` | Request aggregate with approval workflow: `Requested → Approved → Executing → Completed/Failed`. |
| `RefundItem` | `Payments` | Line-level detail for a refund (sku, quantity, reason). |
| `PaymentReconciliationRecord` | `Payments` | Snapshot comparing platform vs provider status. Filled by the S12 nightly job. |
| `Invoice` | `Invoicing` | Immutable financial document issued on payment capture (FR-09-001). Tracks `CreditedTotal`. |
| `InvoiceLine` | `Invoicing` | Line item on an invoice (sku, description, amount). |
| `CreditNote` | `Invoicing` | Reduces outstanding invoice balance when a refund is processed (FR-09-002). |

### Key Status Enums

- **`PaymentStatus`** — `Created → Authorized → Captured → Refunded`; `Failed → RetryPending` branch.
- **`RefundStatus`** — `Requested → Approved → Executing → Completed | Failed`; `Rejected` terminal.
- **`ReconciliationStatus`** — `Pending → Matched | Drift | Unmatched`.
- **`InvoiceStatus`** — `Issued → Paid → PartiallyRefunded → Refunded | Cancelled`.

### Refund Approval Workflow

```
Refund.Create()  ──→  RefundStatus.Requested
       │
  Approve()       ──→  RefundStatus.Approved  ──→  BeginExecution()  ──→  RefundStatus.Executing
  Reject()        ──→  RefundStatus.Rejected                                          │
                                                                        ┌───────────────┴──────────┐
                                                               MarkCompleted()            MarkFailed()
                                                                (Completed)                (→ retry job)
```

### Append-Only Ledger

Every `Payment` state transition appends a `PaymentLedgerEntry` via `RecordLedger()`. Event types: `intent_created`, `authorized`, `failed`, `captured`, `voided`, `refund_requested`, `refunded`. Entries are immutable — the ledger is the single source of truth for finance reports.

## 3. Key Operations

| Operation | Trigger | Domain Method | Infrastructure |
|-----------|---------|---------------|----------------|
| Authorize payment | API call | `Payment.MarkAuthorized()` | `PaymentsController` |
| Capture payment | API call | `Payment.Capture()` → emits `PaymentCaptured` | `PaymentsController` |
| Void payment | API call | `Payment.Void()` | `PaymentsController` |
| Request refund | API call | `Refund.Create()` → emits `RefundRequested` | `RefundsController` |
| Approve refund | API call | `Refund.Approve()` → emits `RefundApproved` | `RefundsController` |
| Execute refund | API call | `Refund.BeginExecution()` → provider call → `MarkCompleted()`/`MarkFailed()` | `RefundsController` |
| Retry failed refund | Hangfire job | `RetryFailedRefundsJob` re-dispatches `ExecuteRefundCommand` | `RetryFailedRefundsJob.cs` |
| Issue invoice | Domain event | `PaymentCaptured` → `InvoiceIssuanceService.IssueForPaymentCapturedAsync()` | `InvoiceOnPaymentCapturedHandler.cs` |
| Issue credit note | Domain event | `PaymentRefunded` → `InvoiceIssuanceService.IssueForRefundAsync()` | `CreditNoteOnPaymentRefundedHandler.cs` |
| Nightly reconciliation | Cron job (`0 2 * * *`) | `ReconciliationService.RunAsync()` | `NightlyReconciliationJob.cs` |

## 4. API Endpoints

| Method | Route | Controller | Description |
|--------|-------|------------|-------------|
| `POST` | `/api/v1/payments/{paymentId}/authorize` | `PaymentsController` | Authorize a payment |
| `POST` | `/api/v1/orders/{orderNumber}/refunds` | `RefundsController` | Request a refund (idempotent) |
| `POST` | `/api/v1/refunds/{refundId}/approve` | `RefundsController` | Approve a pending refund |
| `POST` | `/api/v1/refunds/{refundId}/execute` | `RefundsController` | Execute an approved refund |
| `POST` | `/api/v1/reconciliation/run` | `ReconciliationController` | Trigger reconciliation (admin) |
| `GET` | `/api/v1/invoices` | `InvoicesController` | List invoices (paginated, filterable by status) |
| `GET` | `/api/v1/invoices/{invoiceId}` | `InvoicesController` | Get invoice detail |
| `GET` | `/api/v1/invoices/{invoiceId}/pdf` | `InvoicesController` | Download invoice PDF |
| `GET` | `/api/v1/invoices/{invoiceId}/credit-notes` | `InvoicesController` | List credit notes for an invoice |

## 5. Integration Points

- **Domain events → Invoicing**: `PaymentCaptured` triggers invoice issuance; `PaymentRefunded` triggers credit note issuance.
- **Domain events → Outbox → MassTransit**: `PaymentCaptured`, `PaymentRefunded`, `RefundRequested`, `RefundApproved`, `RefundCompleted` are published via outbox.
- **Hangfire scheduled jobs**: `NightlyReconciliationJob` (daily 2 AM), `RetryFailedRefundsJob`, `GenerateInvoicePdfJob`.
- **Finance report module**: `ReportingQueryService.GetFinanceAsync()` reads `PaymentLedgerEntry` and `Payment` tables directly.

## 6. File References

| File | Path |
|------|------|
| `Payment.cs` | `src/ECommerce.Domain/Payments/Payment.cs` |
| `PaymentLedgerEntry.cs` | `src/ECommerce.Domain/Payments/PaymentLedgerEntry.cs` |
| `Refund.cs` | `src/ECommerce.Domain/Payments/Refund.cs` |
| `RefundStatus.cs` | `src/ECommerce.Domain/Payments/RefundStatus.cs` |
| `PaymentReconciliationRecord.cs` | `src/ECommerce.Domain/Payments/PaymentReconciliationRecord.cs` |
| `Invoice.cs` | `src/ECommerce.Domain/Invoicing/Invoice.cs` |
| `CreditNote.cs` | `src/ECommerce.Domain/Invoicing/CreditNote.cs` |
| `PaymentsController.cs` | `src/ECommerce.API/Controllers/PaymentsController.cs` |
| `RefundsController.cs` | `src/ECommerce.API/Controllers/RefundsController.cs` |
| `ReconciliationController.cs` | `src/ECommerce.API/Controllers/ReconciliationController.cs` |
| `InvoicesController.cs` | `src/ECommerce.API/Controllers/InvoicesController.cs` |
| `InvoiceOnPaymentCapturedHandler.cs` | `src/ECommerce.Infrastructure/Invoicing/InvoiceOnPaymentCapturedHandler.cs` |
| `CreditNoteOnPaymentRefundedHandler.cs` | `src/ECommerce.Infrastructure/Invoicing/CreditNoteOnPaymentRefundedHandler.cs` |
| `NightlyReconciliationJob.cs` | `src/ECommerce.Infrastructure/Jobs/NightlyReconciliationJob.cs` |
| `RetryFailedRefundsJob.cs` | `src/ECommerce.Infrastructure/Jobs/RetryFailedRefundsJob.cs` |
