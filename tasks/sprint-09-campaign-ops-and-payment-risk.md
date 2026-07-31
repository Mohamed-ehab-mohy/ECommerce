# Sprint 9 — Campaign Operations & Payment Risk (US-E-006..008; US-G-003,004,006,007)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 2 | Goal:** Campaign ops, failover, and payment ledger.
> **Source of truth:** `docs/04a` FR-05, FR-07; `docs/06c-bounded-contexts.md`.
> **Dependencies:** S8. **Blocks:** S12 (reconciliation).
> **Exit:** US-E-006..008; US-G-003,004,006,007 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-E-006, US-E-007, US-E-008 | Eligibility, scheduling, snapshot immutability | 5 | [ ] |
| US-G-003 | Provider failover | 3 | [ ] |
| US-G-004 | Declined-payment retry | 3 | [ ] |
| US-G-006, US-G-007 | No-PAN + payment ledger | 5 | [ ] |
| T-DAT-010 | Reconciliation data model | 3 | [ ] |

---

## US-E-006..008 — Eligibility, Scheduling, Snapshot Immutability

### Scope
- Eligibility rules refinement (segment checks), scheduled activation/pause via Hangfire, enforce pricing snapshot immutability on placed orders.

### Acceptance
- [ ] Campaign schedules/pauses on time; placed orders never repriced.

### Commit
`feat(promotions): eligibility, scheduling and snapshot immutability`

---

## US-G-003 — Provider Failover

### Scope
- Multi-provider routing; health-based failover to backup PSP; circuit breaker.
- Test: kill provider A in staging → traffic falls over to B.

### Acceptance
- [ ] Failover verified in staging (kill PSP A).

### Commit
`feat(payments): provider failover and circuit breaker`

---

## US-G-004 — Declined-Payment Retry

### Scope
- Retry policy for declined payments (bounded retries, cooldown, customer-facing status).

### Acceptance
- [ ] Decline → bounded retry → final state; no double charge.

### Commit
`feat(payments): declined payment retry`

---

## US-G-006,007 — No-PAN + Payment Ledger

### Scope
- Verify no PAN stored anywhere (tokenization only); append-only payment ledger (intent/authorize/capture/void events).

### Acceptance
- [ ] Scan of DB for PAN patterns empty; ledger append-only.

### Commit
`feat(payments): no-pan guarantee and append-only payment ledger`

---

## T-DAT-010 — Reconciliation Data Model

### Scope
- Tables for provider transactions vs platform records (provider refs, statuses, drift flags) — foundation for S12 reconciliation job.

### Acceptance
- [ ] Reconciliation queries runnable; drift detectable.

### Commit
`feat(payments): reconciliation data model`

---

## Sprint Exit
- [ ] Campaign scheduling/pause live; failover tested; payment ledger append-only; no PAN.
- [ ] US-E-006..008; US-G-003,004,006,007 green.
- [ ] CI green.
