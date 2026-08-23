# Sprint 12 — Refunds, Reconciliation & Reviews (US-I-004..007; US-B-007,008; US-K-001..004)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 2 | Goal:** Money-safety: refunds, reconciliation, and reviews.
> **Source of truth:** `docs/04a` FR-07 (refunds), FR-09 (reconciliation), FR-02 (bulk import), FR-11 (reviews); `docs/08-api-design.md` §7.6.
> **Dependencies:** S9 (ledger), S11 (invoices). **Blocks:** S13.
> **Risk:** Refund idempotency → QAS-04 duplicate-execution test.
> **Exit:** US-I-004..007; US-B-007,008; US-K-001..004 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-I-004, US-I-006 | Refund workflow (policy + idempotent execution) | 6 | [x] |
| US-I-005, US-I-007 | Reconciliation feed + financial audit trail | 4 | [x] |
| US-B-007, US-B-008 | Bulk import + availability-aware catalog | 4 | [x] |
| US-K-001, US-K-002, US-K-003, US-K-004 | Review submission + moderation + aggregation | 5 | [x] |
| T-DAT-015 | Reconciliation job + drift flags | 4 | [x] |

---

## US-I-004,006 — Refund Workflow (Policy + Idempotent Execution)

### Scope
- `POST /api/v1/orders/{orderNumber}/refunds` (Idempotency-Key), approval flow (`/refunds/{id}/approve`), execute via originating provider.
- Policy: amount ≤ refundable; restock option; state machine requested→approved→executed.

### Acceptance
- [x] QAS-04 duplicate-execution test: refund never duplicates.
- [x] Exceeds refundable → 422/409; restock atomic.

### Commit
`feat(finance): idempotent refund workflow`

---

## US-I-005,007 + T-DAT-015 — Reconciliation Feed + Job + Audit Trail

### Scope
- Nightly reconciliation job (provider vs platform), drift flags + alerts, financial audit trail per record.

### Acceptance
- [x] Recon run detects seeded drift; 0 undetected drift in report.

### Commit
`feat(finance): reconciliation feed, job and drift flags`

---

## US-B-007,008 — Bulk Import + Availability-Aware Catalog

### Scope
- Bulk product import (`POST /api/v1/imports/products`, async) with error report; availability-aware catalog reads.

### Acceptance
- [x] Import produces per-row error report; partial success supported.

### Commit
`feat(catalog): bulk import with error reports`

---

## US-K-001..004 — Reviews: Submit, Moderation, Aggregation

### Scope
- Submit (verified purchase, unique per customer+product), moderation queue + publish/reject, rating aggregation into product read model.

### Acceptance
- [x] Unverified cannot review; moderation enforces publish flow; aggregates correct.

### Commit
`feat(reviews): submission, moderation and aggregation`

---

## Sprint Exit
- [x] Refunds never duplicate; nightly reconciliation reports 0 undetected drift; reviews moderated.
- [x] US-I-004..007; US-B-007,008; US-K-001..004 green.
- [x] CI green.
