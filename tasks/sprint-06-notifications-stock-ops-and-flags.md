# Sprint 6 — Notifications, Stock Ops & Feature Flags (US-J-001..006; US-F-004..006; US-C-002,005; US-M-002)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 1 | Goal:** Notification backbone and operational stock controls.
> **Source of truth:** `docs/04a` FR-10, FR-06; `docs/09-security-architecture.md` §14 (audit); `docs/06-system-architecture.md`.
> **Dependencies:** S5 (events), S1 (jobs). **Blocks:** S7.
> **Exit:** US-J-001..006; US-F-004..006; US-C-002,005; US-M-002 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-J-001, US-J-005 | Lifecycle notifications (event-driven) | 5 | [ ] |
| US-J-002, US-J-003, US-J-006 | Channels, preferences, PII-safe payloads | 6 | [ ] |
| US-J-004 | Localized templates | 3 | [ ] |
| US-F-004, US-F-005, US-F-006 | Stock adjustments + transfers + low-stock alerts | 5 | [ ] |
| US-C-002, US-C-005 | Cart merge + price-change warnings | 5 | [ ] |
| US-M-002 | Feature flags (kill-switch) | 3 | [ ] |
| T-DAT-007 | Email/SMS adapters + template store | 4 | [ ] |
| T-DAT-008 | Hangfire infrastructure | 3 | [ ] |

---

## T-DAT-008 — Hangfire Infrastructure

### Scope
- Hangfire server + dashboard (`/hangfire`, permission-gated), PostgreSQL storage.
- Job registration pattern; idempotent/retryable jobs; metrics.

### Acceptance
- [ ] Scheduled job runs; dashboard accessible to ops role only.

### Commit
`feat(jobs): hangfire infrastructure with postgres storage`

---

## T-DAT-007 — Email/SMS Adapters + Template Store

### Scope
- `INotificationProvider` port; SMTP email adapter + SMS stub.
- Template store (localized, placeholders, fallback locale).

### Acceptance
- [ ] Email sends via SMTP adapter; template renders with fallback.

### Commit
`feat(notifications): email/sms adapters and template store`

---

## US-J-001..006 — Lifecycle Notifications, Preferences, Templates

### Scope
- Event-driven notifications on `OrderPlaced`, `OrderShipped`, etc. via consumers.
- Preferences (opt-in/out per channel/type); PII-safe payloads (no full PII in logs/events).
- Localized templates with fallback; retries via Hangfire.

### Acceptance
- [ ] OrderPlaced triggers email; preferences respected; retries on failure.

### Commit
`feat(notifications): lifecycle notifications with preferences`

---

## US-F-004..006 — Stock Adjustments, Transfers, Low-Stock Alerts

### Scope
- Adjustments (reason-coded, negative requires approval) + transfers between warehouses + low-stock thresholds/alerts (event-driven).
- All via append-only ledger; audited.

### Acceptance
- [ ] Negative adjustment without approval → 422; transfer updates both warehouses atomically.

### Commit
`feat(inventory): stock adjustments, transfers and low-stock alerts`

---

## US-C-002,005 — Cart Merge + Price-Change Warnings

### Scope
- On login: merge guest cart into user cart (conflict policy).
- Price-change warnings when prices move between add and checkout.

### Acceptance
- [ ] Merge deterministic; warnings surface before checkout.

### Commit
`feat(cart): cart merge on login and price-change warnings`

---

## US-M-002 — Feature Flags (Kill-Switch)

### Scope
- Flag registry + service (Redis cache TTL 30 s), admin endpoints, kill-switch semantics.

### Acceptance
- [ ] Toggle reflects in ≤ 60 s without deploy; kill-switch halts flag-gated path.

### Commit
`feat(flags): feature flags with kill-switch`

---

## Sprint Exit
- [ ] Notifications flow from events with retries; stock ops audited; flags toggle in 60 s.
- [ ] US-J-001..006; US-F-004..006; US-C-002,005; US-M-002 green.
- [ ] CI green.
