# 23 — Audit Logging & Observability

## 1. Overview

The audit module provides tamper-evident, append-only logging of all significant platform actions. Each `AuditEntry` is chained via SHA-256 hashes (similar to blockchain), enabling integrity verification. Audit records capture actor, action, entity, before/after snapshots, IP, user agent, and trace ID for full observability.

## 2. Domain Entities

| Entity | Namespace | Purpose |
|--------|-----------|---------|
| `AuditEntry` | `Domain.Audit` | Single audit record. Contains actor, action, entity, before/after JSON, IP, user agent, trace ID, and hash chain. |
| `AuditChain` (static) | `Domain.Audit` | Computes and verifies SHA-256 hash chain across entries. |
| `AuditActions` (static) | `Domain.Audit` | Constants for all auditable action types (45+ actions). |
| `AuditActorType` (enum) | `Shared.Audit` | `User`, `System`, `Impersonated`. |

### Hash Chain Integrity

Each `AuditEntry` stores:
- `PreviousHash`: hash of the preceding entry
- `Hash`: `SHA256(PreviousHash | "|" | CanonicalPayload)`

`AuditChain.Verify()` walks entries in ID order, recomputing hashes and confirming the chain is unbroken. `CanonicalPayload()` is a JSON-serialized object of `{Action, ActorId, ActorType, EntityType, EntityId, Before, After, OccurredAt}`.

### Action Categories

Key actions tracked (from `AuditActions`):
- **Identity**: `identity.login`, `identity.profile.updated`, `identity.address.added/removed`, `identity.role.created/assigned/permissions.changed`, `identity.account.closed/erased`, `identity.impersonation.started`
- **Catalog**: `catalog.product.created/updated/deactivated`, `catalog.category.created/updated`, `catalog.brand.created/updated`, `catalog.import.run`, `catalog.bulk.status.change`
- **Inventory**: `inventory.warehouse.created/updated/deactivated`, `inventory.stock.movement.posted`
- **Payments**: `payments.refund.created/approved/executed/failed`, `finance.reconciliation.run/drift`
- **Promotions**: `promotions.promotion.created/updated/activated/paused/scheduled`, `promotions.coupon.created`
- **Reviews**: `reviews.review.submitted/moderated/removed`
- **Platform**: `platform.feature.flag.changed`, `notifications.preference.updated`

## 3. Key Operations

| Operation | Flow | Key Files |
|-----------|------|-----------|
| **Audit write-through** | Use-case handler -> `IAuditEntryRepository.AppendAsync()` -> batched save in `UnitOfWork` | `AuditEntryRepository.cs` |
| **Get latest hash** | `IAuditEntryRepository.GetLatestHashAsync()` fetches chain tip for new entry | `AuditEntryRepository.cs` |
| **Query audit log** | Filtered by actor, action, entity type, entity ID, date range | `AuditEntryRepository.cs` |
| **Verify chain integrity** | `AuditChain.Verify(entries)` recomputes hashes and validates the full chain | `AuditChain.cs` |
| **Correlation propagation** | `TraceId` on `AuditEntry` links to the HTTP request trace ID | `AuditEntry.cs` |

### Entry Creation Pattern

```
AuditEntry.Create(
    actorId, actorType, action, entityType, entityId,
    beforeJson, afterJson, ip, userAgent, traceId,
    previousHash, utcNow)
```

The `previousHash` is fetched from the repository's `GetLatestHashAsync()` before the entry is appended. Hash is computed inline during `Create()`.

### Timestamp Normalization

`AuditEntry.Create()` truncates timestamps to millisecond precision to ensure deterministic hash computation.

## 4. API Endpoints

Audit entries are not exposed via a dedicated REST controller. They are written by use-case handlers and can be queried through the `IAuditEntryRepository` port. The `AuditLogQuery` supports filtering by:
- `ActorId`, `Action`, `EntityType`, `EntityId`
- `From` / `To` date range
- `Page` / `PageSize` pagination

## 5. Integration Points

- **Every write use-case**: Handlers append an `AuditEntry` after successful mutations, using the `IAuditEntryRepository`.
- **HTTP context**: `Ip`, `UserAgent`, and `TraceId` are extracted from the current request and embedded in each entry.
- **Hash chain**: `AuditChain.Compute()` uses SHA-256 to create a tamper-evident chain. `AuditChain.Verify()` provides batch verification.
- **Trace correlation**: `TraceId` links audit entries to distributed traces for end-to-end observability.

## 6. File References

| File | Path |
|------|------|
| `AuditEntry.cs` | `src/ECommerce.Domain/Audit/AuditEntry.cs` |
| `AuditChain.cs` | `src/ECommerce.Domain/Audit/AuditChain.cs` |
| `AuditActions.cs` | `src/ECommerce.Domain/Audit/AuditActions.cs` |
| `AuditActorType.cs` | `src/ECommerce.Shared/Audit/AuditActorType.cs` |
| `AuditEntryRepository.cs` | `src/ECommerce.Infrastructure/Audit/AuditEntryRepository.cs` |
