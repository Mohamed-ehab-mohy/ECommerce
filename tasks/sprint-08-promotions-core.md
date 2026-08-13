# Sprint 8 — Promotions Core (US-E-001..005)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 2 | Goal:** Discount engine and atomic coupon redemption.
> **Source of truth:** `docs/04a` FR-05; `docs/06a-domain-model.md` Pricing context; `docs/06c-bounded-contexts.md` Pricing contract.
> **Dependencies:** S5 (checkout pricing), S7 (release). **Blocks:** S9.
> **Risk:** Coupon race → QAS-02 concurrency test mandatory.
> **Exit:** US-E-001..005 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-E-001, US-E-005 | Discount types + caps + non-negative invariant | 4 | [x] |
| US-E-002 | Promotion campaigns with conditions | 4 | [x] |
| US-E-003 | Coupon lifecycle + atomic redemption | 3 | [x] |
| US-E-004 | Stacking matrix + priority | 3 | [x] |
| T-DAT-009 | Pricing pipeline refactor (snapshot-aware) | 5 | [x] |

---

## US-E-001,005 — Discount Types + Caps + Non-Negative Invariant

### Scope
- Discount types: percentage, fixed amount, buy-X-get-Y (later), shipping discount.
- Caps (max discount amount), non-negative totals invariant (total ≥ 0).

### Acceptance
- [x] Discount never yields negative total; caps enforced.

### Commit
`feat(promotions): discount types, caps and non-negative invariant`

---

## US-E-002 — Promotion Campaigns with Conditions

### Scope
- Campaign = rules (customer segment, cart value, product list, dates) → action (discount).
- CRUD + activation state machine (draft/active/paused/ended).

### Acceptance
- [x] Eligibility evaluated; inactive campaign never applies.

### Commit
`feat(promotions): campaign engine with conditions`

---

## US-E-003 — Coupon Lifecycle + Atomic Redemption

### Scope
- Coupon: code, limits (single/multi/per-customer), dates, counter.
- Atomic claim (`UPDATE ... WHERE usage_left > 0` or row lock) to prevent overuse; dedupe per order.

### Acceptance
- [x] QAS-02 race test: N concurrent redemptions never exceed limit.

### Commit
`feat(promotions): atomic coupon lifecycle`

---

## US-E-004 — Stacking Matrix + Priority

### Scope
- Stacking rules per promotion type + priority ordering; document matrix in code.

### Acceptance
- [x] Stacking order deterministic; conflicts resolved by priority.

### Commit
`feat(promotions): stacking matrix and priority`

---

## T-DAT-009 — Pricing Pipeline Refactor (Snapshot-Aware)

### Scope
- Refactor checkout pricing to consume `PricingResult` (per `06c` Conformist contract): itemDiscounts, cartDiscount, shippingDiscount, appliedRuleIds, totals.
- Order pricing snapshot immutability after placement.

### Acceptance
- [x] Order holds snapshot; later promotion changes don't affect placed order.

### Commit
`refactor(pricing): snapshot-aware pricing pipeline`

---

## Sprint Exit
- [x] Coupon redemption race test passes; pricing snapshot holds order immutability.
- [x] US-E-001..005 green.
- [x] CI green.

## Close-out (2026-08-13)

- **Implementation:** `832529f feat(promotions): sprint 8 promotions core with atomic coupon redemption` (81 files).
- **Docs:** `docs/08-api-design.md` §6.5/§7.7–7.10 (promotions/coupons/cart-coupon contracts).
- **Verification:**
  - Unit suite: 462/462 passing (incl. pricing engine, stacking, coupon lifecycle, handlers).
  - Integration (real Postgres): QAS-02 `CouponRedemptionIntegrationTests` 2/2 — 20 concurrent redemptions against a 10-use coupon yield exactly 10 successes + 10 `COUPON_EXHAUSTED`; per-customer limit (1) enforced atomically.
  - `dotnet build` 0 warnings/errors; `dotnet format --verify-no-changes` clean.
- **Deviations:** promotion conditions/actions/eligibility persisted as JSONB on `promotions` (not child tables); FRS-E-004 "priority" implemented as evaluation order item → cart → shipping with deterministic best-of/additive stacking (ties by `CreatedAt`, then `Id`).
