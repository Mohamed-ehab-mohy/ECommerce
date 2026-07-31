# Sprint 7 — Order Lifecycle & v1.0 MVP Release (US-D-005..009; US-M-006,007)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 1 | Goal:** Complete order lifecycle and release v1.0.
> **Source of truth:** `docs/04a` FR-04; `docs/08-api-design.md` §6.4, §7.4; `docs/03c-sprint-plan.md` §Sprint 7.
> **Dependencies:** S2–S6. **Blocks:** v1.0 consumers.
> **Risk:** Scope creep into v1.0 → freeze list enforced by PO.
> **Exit — v1.0 MVP (M2, M3 baseline):** Checkout-to-order E2E green; 1 PSP; audit/flags/jobs live; CI gates green; load smoke < targets.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-D-005, US-D-006 | Order confirmation + history + timeline | 6 | [ ] |
| US-D-007 | Cancellation with restock/refund path | 3 | [ ] |
| US-D-008, US-D-009 | Reorder + support lookup | 5 | [ ] |
| US-M-006, US-M-007 | Health endpoints + API versioning | 4 | [ ] |
| T-OPS-002 | v1.0 staging deploy + smoke suite | 4 | [ ] |
| T-TST-001 | Baseline load smoke (checkout path) | 3 | [ ] |

---

## US-D-005,006 — Confirmation, History, Timeline

### Scope
- Order confirmation (event → notification), `GET /api/v1/orders` history (cursor paginated), `GET /api/v1/orders/{orderNumber}` detail + timeline.

### Acceptance
- [ ] History paginates correctly; timeline shows state transitions with timestamps.

### Commit
`feat(orders): order confirmation, history and timeline`

---

## US-D-007 — Cancellation + Restock/Refund Path

### Scope
- `POST /api/v1/orders/{orderNumber}/cancel`; allowed until fulfillment starts.
- Restock allocated items; refund path for paid orders (refund execution in S12 — wire stub now, ledger updates now).

### Acceptance
- [ ] Cancel restocks atomically; paid orders get refund stub invoked; notify customer.

### Commit
`feat(orders): cancellation with restock and refund stub`

---

## US-D-008,009 — Reorder + Support Lookup

### Scope
- `POST /api/v1/orders/{orderNumber}/reorder` (re-validates availability).
- `GET /api/v1/support/orders` lookup by number/email/customer (permission-gated).

### Acceptance
- [ ] Reorder copies items with availability checks; support lookup masked.

### Commit
`feat(orders): reorder and support lookup`

---

## US-M-006,007 — Health Endpoints + API Versioning

### Scope
- Health endpoints (already baseline in S1) → formalize response contract.
- API versioning: URL versioning `v1`, version policies, `Deprecation` header for deprecated.

### Acceptance
- [ ] `/api/v1/health/live`, `ready` JSON; deprecated endpoints flagged.

### Commit
`feat(platform): health endpoints contract and api versioning`

---

## T-OPS-002 — v1.0 Staging Deploy + Smoke Suite

### Scope
- Staging environment (docker-compose or k8s-lite) + smoke suite (critical journeys: register→browse→cart→checkout→order).

### Acceptance
- [ ] Smoke suite green on staging; deploy reproducible.

### Commit
`chore(deploy): v1.0 staging deploy and smoke suite`

---

## T-TST-001 — Baseline Load Smoke (Checkout Path)

### Scope
- k6 (or NBomber) baseline on checkout path against staging; record metrics; assert p95 < 800 ms, error < 0.5%.

### Acceptance
- [ ] Baseline report recorded; thresholds met or gap documented.

### Commit
`test(perf): baseline load smoke on checkout path`

---

## Sprint Exit — v1.0 MVP
- [ ] Full order lifecycle E2E; cancellation refund path; staging deployment.
- [ ] Checkout-to-order E2E green; 1 PSP; audit/flags/jobs live; CI gates green; load smoke < targets.
- [ ] US-D-005..009; US-M-006,007 green.
- [ ] CI green; no sev ≥ 2 defects open.
