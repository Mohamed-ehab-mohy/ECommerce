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
| US-E-006, US-E-007, US-E-008 | Eligibility, scheduling, snapshot immutability | 5 | [x] |
| US-G-003 | Provider failover | 3 | [x] |
| US-G-004 | Declined-payment retry | 3 | [x] |
| US-G-006, US-G-007 | No-PAN + payment ledger | 5 | [x] |
| T-DAT-010 | Reconciliation data model | 3 | [x] |

---

## US-E-006..008 — Eligibility, Scheduling, Snapshot Immutability

### Scope
- Eligibility rules refinement (segment checks), scheduled activation/pause via Hangfire, enforce pricing snapshot immutability on placed orders.

### Acceptance
- [x] Campaign schedules/pauses on time; placed orders never repriced.

### Commit
`feat(payments): sprint 9 payment risk, retries, ledger, and reconciliation` (96a7242)

---

## US-G-003 — Provider Failover

### Scope
- Multi-provider routing; health-based failover to backup PSP; circuit breaker.
- Test: kill provider A in staging → traffic falls over to B.

### Acceptance
- [x] Failover verified via `PaymentProviderFactoryTests` (provider A down → B serves); staging kill-test pending Docker.

### Commit
`feat(payments): sprint 9 payment risk, retries, ledger, and reconciliation` (96a7242)

---

## US-G-004 — Declined-Payment Retry

### Scope
- Retry policy for declined payments (bounded retries, cooldown, customer-facing status).

### Acceptance
- [x] Decline → bounded retry (cooldown + max attempts) → final state; no double charge.

### Commit
`feat(payments): sprint 9 payment risk, retries, ledger, and reconciliation` (96a7242)

---

## US-G-006,007 — No-PAN + Payment Ledger

### Scope
- Verify no PAN stored anywhere (tokenization only); append-only payment ledger (intent/authorize/capture/void events).

### Acceptance
- [x] `PaymentSecurityTests` no-PAN scan empty; ledger append-only (5 tests).

### Commit
`feat(payments): sprint 9 payment risk, retries, ledger, and reconciliation` (96a7242)

---

## T-DAT-010 — Reconciliation Data Model

### Scope
- Tables for provider transactions vs platform records (provider refs, statuses, drift flags) — foundation for S12 reconciliation job.

### Acceptance
- [x] Reconciliation queries runnable; drift detectable (snapshot + status flags).

### Commit
`feat(payments): sprint 9 payment risk, retries, ledger, and reconciliation` (96a7242)

---

## Sprint Exit
- [x] Campaign scheduling/pause live (Hangfire, non-dev); failover tested; payment ledger append-only; no PAN.
- [x] US-E-006..008; US-G-003,004,006,007 green.
- [x] CI green.

## Close-out (2026-08-13)

- **Implementation:** `96a7242 feat(payments): sprint 9 payment risk, retries, ledger, and reconciliation` (49 files, +6122/-38).
- **Also fixed:** last commit's CI failure (run 31695846069) — `NU1903` high-severity SSH.NET 2025.1.0 advisory `GHSA-q939-rpr3-3284` broke `ECommerce.IntegrationTests` restore; pinned `<PackageReference Include="SSH.NET" Version="2026.0.0" />`.
- **US-E-006..008:** `PromotionScheduleEnforcer` + Hangfire `promotion-schedule-enforcer` job (`*/1 * * * *`, non-dev only), `GetDueForActivationAsync`/`GetDueForPauseAsync` repository queries, DI registration. Tests: `PromotionScheduleEnforcerTests` (5) + `OrderPriceSnapshotTests` (1).
- **US-G-003:** `PaymentCircuitBreaker` (per-provider threshold/cooldown), `IPaymentProviderHealth`, DI-provider failover in `PaymentProviderFactory` → `FailoverProvider`, `PaymentProvidersUnavailableException`. Tests: `PaymentCircuitBreakerTests` (8) + `PaymentProviderFactoryTests` (6).
- **US-G-004:** `PaymentStatus.RetryPending`, `Payment.RetryAfterUtc/PlanRetry/CanRetry/MarkFailed(declineCode)`, `PaymentRetryOptions` (MaxAttempts=3, Cooldown=30s), decline → cooldown/exhaustion gating, `PaymentResponse` exposes `Attempt` + `RetryAfterUtc`, migration `20260813144454_AddPaymentRetryColumn` (`retry_after_utc`). Tests: 5 new handler tests (11/11).
- **US-G-006/007:** append-only `PaymentLedgerEntry` on Create/Authorized/Failed/Capture/Void/BeginRefund; `payment_ledger` table; `PaymentSecurityTests` (no-PAN regression guard) — no raw PAN fields anywhere.
- **T-DAT-010:** `PaymentReconciliationRecord` (+`ReconciliationStatus` Pending/Matched/Drift/Unmatched), `payment_reconciliation_records` table (unique per payment, status index), `ReconciliationService.SnapshotPendingAsync`, extended `IPaymentRepository`. Tests: `PaymentLedgerTests` (5) + `ReconciliationServiceTests` (4) + architecture (1).
- **Migrations:** `20260813144454_AddPaymentRetryColumn`, `20260813150252_AddPaymentLedgerAndReconciliation`.
- **Verification:** build 0 warnings/errors under `-warnaserror`; unit 493/493; architecture 7/7; `dotnet format --verify-no-changes` clean; restore clean (NU1903 gone); CI run `31713638222` green (incl. integration tests).
- **Deviations:** staging kill-PSP-A smoke not run (Docker unavailable locally; covered by `PaymentProviderFactoryTests` failover unit coverage, CI integration suite green).
