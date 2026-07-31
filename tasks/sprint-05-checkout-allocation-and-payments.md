# Sprint 5 — Checkout, Atomic Allocation & Payments v1 (US-D-001..004; US-F-003; US-G-001,002)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 1 | Goal:** The money-critical path: checkout → allocation → payment auth.
> **Source of truth:** `docs/04a` FR-04, FR-06, FR-07; `docs/06-system-architecture.md` §6.1 (write path), §7.2/7.3 (outbox/idempotency); `docs/07-data-model-erd.md` orders + payments schema.
> **Dependencies:** S4 (cart/stock), S2 (auth), S1 (outbox infra partially). **Blocks:** S6, S7.
> **Risk:** Transactional correctness → QAS-01/05 concurrency tests mandatory before DoD.
> **Exit:** US-D-001..004; US-F-003; US-G-001,002 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-D-001, US-D-002 | Guest checkout + shipping rates | 8 | [ ] |
| US-D-003, US-D-004 | Atomic order placement + reservation | 8 | [ ] |
| US-F-003 | Atomic stock allocation (no oversell) | 5 | [ ] |
| US-G-001, US-G-002 | Provider abstraction + authorize | 6 | [ ] |
| T-DAT-005 | Outbox pattern + MassTransit consumers | 6 | [ ] |
| T-DAT-006 | PSP sandbox adapter (Stripe + mock) | 4 | [ ] |

---

## T-DAT-005 — Outbox + MassTransit Consumers

### Scope
- `outbox_events` table (id, aggregate_id, event_type, payload jsonb, created_at, processed_at, attempts).
- Publisher: poller with `FOR UPDATE SKIP LOCKED`; `outbox_lag_seconds` metric.
- MassTransit + RabbitMQ (quorum queues); inbox dedupe (`inbox_messages`).
- Consumers idempotent; DLQ + alert on max attempts.

### Acceptance
- [ ] Event written in same tx as domain change; delivered at-least-once.
- [ ] Duplicate delivery processed once (QAS-01).
- [ ] DLQ path triggers alert.

### Commit
`feat(messaging): transactional outbox and mass transit consumers`

---

## T-DAT-006 — PSP Sandbox Adapter

### Scope
- `IPaymentProvider` port in Domain; adapter factory.
- Stripe test-mode adapter + `MockPaymentProvider` for local/CI.
- Client token generation, authorize intent; never store PAN.

### Acceptance
- [ ] Both adapters authorize in tests; contract test per adapter.

### Commit
`feat(payments): provider abstraction with stripe and mock adapters`

---

## US-D-001,002 — Guest Checkout + Shipping Rates

### Scope
- `POST /api/v1/checkouts` (auth/anon) + `GET /api/v1/checkouts/{id}`.
- Snapshot pricing, totals compute, stock verify, `checkoutId` + payment client token.
- Shipping rates via stub service (real carriers in S10).

### Acceptance
- [ ] Checkout initiation returns totals + client token; price-change/stock-change detected (409).

### Commit
`feat(checkout): checkout initiation with pricing snapshot`

---

## US-D-003,004 — Atomic Order Placement + Reservation

### Scope
- `POST /api/v1/checkouts/{id}/place` (Idempotency-Key).
- Single transaction: validate payment auth → allocate stock → create order + items + snapshot → set `Pending` → publish `OrderPlaced` via outbox.
- Idempotency: replay returns stored response; different payload same key → 409 `ERR_IDP_001`.

### Acceptance
- [ ] QAS-01 (no oversell) passes under concurrency.
- [ ] QAS-05 (idempotent placement) passes; duplicate placement returns same order.
- [ ] `OrderPlaced` delivered exactly-once-effectively.

### Commit
`feat(orders): atomic order placement with idempotency`

---

## US-F-003 — Atomic Stock Allocation

### Scope
- Reserve stock in tx; DB CHECK `allocated ≤ on_hand`; row lock / `FOR UPDATE` or optimistic with retry.
- Insufficient → 409 with `lines[]`.

### Acceptance
- [ ] No oversell under concurrent checkouts (QAS-01 test).

### Commit
`feat(inventory): atomic stock allocation`

---

## US-G-001,002 — Provider Abstraction + Authorize

### Scope
- `Payment` aggregate: create intent, authorize, store token + reference; never raw PAN.
- Authorize/capture states; webhook ingestion path (signature verify, dedupe by event id).

### Acceptance
- [ ] Auth succeeds in sandbox; provider decline → 402 customer-friendly message.

### Commit
`feat(payments): payment intent authorization`

---

## Sprint Exit
- [ ] End-to-end guest checkout green in sandbox; QAS-01 (no oversell) passes; outbox delivers `OrderPlaced`.
- [ ] US-D-001..004; US-F-003; US-G-001,002 green.
- [ ] CI green.
