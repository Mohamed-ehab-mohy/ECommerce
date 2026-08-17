# Data Retention Policy

This document describes the data retention strategy for the ECommerce platform, covering lifecycle management of transient and audit data, and compliance with regulatory requirements.

## Retention Schedule

| Data Type | Retention Period | Mechanism | Notes |
|---|---|---|---|
| Audit logs | 7 years | Configurable via `AuditLogRetentionYears` | Longest retention for compliance |
| Expired carts | 30 days | `ExpiredCartPurgeJob` (Hangfire) | Carts with no activity |
| Stock reservations | 30 minutes | `StockReservationExpiryJob` | Released back to available inventory |
| Payment intents | 1 hour | `PaymentTimeoutJob` | Cancelled if no completion |
| Outbox messages | 7 days | `OutboxCleanupJob` | After confirmed delivery |
| Dead-lettered messages | 30 days | `DeadLetterCleanupJob` | For debugging and replay |

## Configurable Retention Periods

Retention periods are stored in `appsettings.json` under the `DataRetention` section:

```json
{
  "DataRetention": {
    "AuditLogRetentionYears": 7,
    "ExpiredCartPurgeDays": 30,
    "StockReservationExpiryMinutes": 30,
    "PaymentTimeoutMinutes": 60,
    "OutboxRetentionDays": 7,
    "DeadLetterRetentionDays": 30
  }
}
```

Override via environment variables:

```bash
DataRetention__AuditLogRetentionYears=10
DataRetention__ExpiredCartPurgeDays=60
```

## Background Job Details

### ExpiredCartPurgeJob

- Runs daily at 03:00 UTC
- Deletes carts with no `LastModifiedDate` activity for the configured period
- Soft-deletes first (marks as `IsDeleted`), hard-deletes after additional 7 days
- Emits metric: `ecommerce_carts_purged_total`

### StockReservationExpiryJob

- Runs every 5 minutes
- Finds reservations older than the configured expiry window
- Releases stock back to available inventory
- Publishes `StockReservationExpiredEvent` for audit trail
- Emits metric: `ecommerce_stock_reservations_expired_total`

### PaymentTimeoutJob

- Runs every 5 minutes
- Finds payment intents in `Pending` status older than the configured timeout
- Marks payments as `TimedOut`
- Triggers stock reservation release via domain event
- Emits metric: `ecommerce_payments_timed_out_total`

## Audit Log Retention

Audit logs are immutable append-only records. They are retained for 7 years by default to satisfy regulatory and internal compliance requirements.

- Stored in the `AuditLogs` table with `CreatedAt` index
- Partitioned monthly (when using PostgreSQL partitioning)
- Cleanup runs via `AuditLogRetentionJob` (weekly, Sunday 04:00 UTC)
- Logs older than the retention period are **hard-deleted** — no soft-delete for audit data
- A pre-deletion export to cold storage (S3/GCS) is recommended before purge

## Compliance

### GDPR Article 5(1)(e) — Storage Limitation

> Personal data shall be kept in a form which permits identification of data subjects for no longer than is necessary for the purposes for which the personal data are processed.

The retention policy enforces storage limitation by:

1. **Automatic expiration** — background jobs remove data beyond the configured retention window without manual intervention
2. **Configurable periods** — retention durations can be adjusted per-deployment to satisfy jurisdiction-specific requirements
3. **Audit trail** — all retention-related deletions are themselves logged for accountability
4. **Right to erasure** — individual data subject deletion requests are handled separately via the GDPR deletion endpoint and bypass the retention schedule

### Data Subject Deletion Flow

When a GDPR deletion request is received:

1. Personal data is anonymized across all bounded contexts
2. Audit logs referencing the subject are redacted (PII fields replaced with `[REDACTED]`)
3. Soft-deleted records are hard-deleted
4. A deletion certificate is generated for compliance records

## Operational Notes

- Monitor `ecommerce_carts_purged_total`, `ecommerce_stock_reservations_expired_total`, and `ecommerce_payments_timed_out_total` metrics via Grafana
- Alerts fire if purge jobs fail or run significantly behind schedule
- Retention period changes require a deployment — no hot-reload
- Test retention configurations in staging before production changes
