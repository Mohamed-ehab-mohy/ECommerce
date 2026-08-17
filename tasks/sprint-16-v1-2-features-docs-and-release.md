# Sprint 16 — v1.2 Features, Documentation & Release (US-A-007,008; US-L-003..005)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 3 | Goal:** Enterprise extras, documentation, and final release.
> **Source of truth:** `docs/04a` FR-01 (closure/impersonation), FR-14 (reports); `docs/01-project-charter.md` roadmap.
> **Dependencies:** S15.
> **Risk:** Doc scope → prioritized by reference value.
> **Exit — v1.2 (M6):** Program DoD 100%; docs approved; runbooks live; release v1.2 shipped.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-A-007, US-A-008 | Account closure/erasure + impersonation | 5 | [x] |
| US-L-003, US-L-004, US-L-005 | Inventory, promotion, fulfillment reports | 5 | [x] |
| T-OPS-004 | Documentation set completion + ADRs + onboarding | 5 | [x] |
| T-OPS-005 | v1.2 release + release notes + archive | 3 | [x] |

---

## US-A-007,008 — Account Closure/Erasure + Impersonation

### Scope
- Closure: anonymize PII, revoke tokens, keep orders per retention (GDPR DSAR path).
- Impersonation: `auth.impersonate` permission, second approver, session marked, full audit.

### Acceptance
- [ ] Erasure anonymizes PII; orders retained; DSAR workflow works.
- [ ] Impersonation audited end-to-end; step-up enforced.

### Commit
`feat(identity): account closure, erasure and impersonation`

---

## US-L-003,004,005 — Inventory, Promotion, Fulfillment Reports

### Scope
- Reporting endpoints for inventory, promotion performance, fulfillment SLAs.

### Acceptance
- [ ] Reports accurate vs ledger; permission-gated.

### Commit
`feat(reports): inventory, promotion and fulfillment reports`

---

## T-OPS-004 — Documentation Set Completion + ADRs + Onboarding

### Scope
- Complete remaining roadmap docs (`02-glossary`, `10-auth`, `11-permissions`, `12–29` module designs, `33-ADRs`, `34`, `35`), finalize `36-onboarding`, validate runbooks.

### Acceptance
- [ ] Roadmap docs all Baseline; ADRs recorded; onboarding verified by a new hire.

### Commit
`docs: complete documentation set and adrs`

---

## T-OPS-005 — v1.2 Release + Notes + Archive

### Scope
- Final release: tags, release notes (auto-generated), archive branch, handover.

### Acceptance
- [ ] v1.2 released; release notes published; archive tagged.

### Commit
`chore(release): ship v1.2`

---

## Sprint Exit — v1.2 (M6)
- [ ] Program DoD 100%; docs approved; runbooks live; release v1.2 shipped.
- [ ] CI green.
