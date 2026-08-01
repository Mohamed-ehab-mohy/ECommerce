# Sprint 2 — Identity Core & Audit Foundation (US-A-001..009, US-M-001)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 1 | Goal:** Secure registration/login and the audit backbone.
> **Source of truth:** `docs/04a-functional-requirements-specification.md` (FR-01), `docs/08-api-design.md` §3, `docs/09-security-architecture.md`, `docs/03a-user-stories.md` US-A-001..009, US-M-001.
> **Dependencies:** S1. **Blocks:** S3 (RBAC on endpoints), S5 (checkout auth).
> **Exit:** US-A-001,002,003,009 pass DoD with unit + integration tests.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-A-001 | Register & verify (email) | 3 | [x] |
| US-A-002 | Login with JWT + rotating refresh | 3 | [x] |
| US-A-003 | Password reset | 2 | [x] |
| US-A-004 | Profile & address management | 3 | [ ] |
| US-A-009 | Lockout policy | 2 | [ ] |
| US-M-001 | Audit log (tamper-evident) | 3 | [ ] |
| T-SEC-001 | ASP.NET Identity + JWT infra + Data Protection | 4 | [x] |
| T-DAT-001 | Audit middleware + hash-chain store | 3 | [ ] |

---

## T-SEC-001 — ASP.NET Identity + JWT Infra + Data Protection

### Scope
- Auth infra: RSA-signed JWT (issuer/audience), access token 15 min; opaque refresh token hashed at rest, device-bound, 30-day rotation.
- `RefreshToken` entity + repository (family/device revocation).
- Data Protection: `SetApplicationName("ECommerce")` baseline.
- Lockout fields + `RecordFailedLogin`/`RecordSuccessfulLogin` on `Customer`.

### Acceptance
- [x] Password hashed (bcrypt), never plaintext.
- [x] Access token verifies signature/issuer/audience.
- [x] Refresh rotation single-use; reuse revokes family (theft detection). *(End-to-end verified in US-A-002 integration tests.)*

### Commit
`feat(identity): add jwt access and refresh token infrastructure`

---

## US-A-001 — Register & Verify (email)

### Scope
- `POST /api/v1/auth/register` per `08-api-design.md` §7.1.
- Email uniqueness, format validation, password policy (≥12, breach check).
- Verification token (24h) + email send (SMTP adapter stub).
- `CustomerRegistered` domain event → outbox.

### Acceptance
- [x] 201 + `pendingVerification`; duplicate → 409; invalid → 422.
- [ ] Unverified users blocked from restricted actions. *(Deferred: no restricted endpoints exist yet; `EmailVerified` flag + auth middleware land with US-A-002.)*
- [x] Token single-use, expires 24h.
- [x] Unit + integration tests (Testcontainers).

### Commit
`feat(identity): register and email verification`

---

## US-A-002 — Login with JWT + Rotating Refresh

### Scope
- `POST /api/v1/auth/login`, `/refresh`, `/logout`, `/logout-all`.
- Lockout integration (5 fails → 15 min, 423 + `retryAfter`, reset on success), verified-email gate (403).
- Rotation concurrency-safe (single-use refresh; reuse revokes family).

### Acceptance
- [x] Token pair issued; access 15 min; refresh rotates.
- [x] Concurrent refresh of same token → family revoked (QAS-style concurrency test).
- [x] `logout-all` revokes all device tokens.

### Commit
`feat(identity): login with rotating refresh tokens`

---

## US-A-003 — Password Reset

### Scope
- `POST /api/v1/auth/forgot-password` (always 202), `POST /api/v1/auth/reset-password`.
- Single-use 30-min token; invalidates sessions on success; re-verify email per security architecture.

### Acceptance
- [x] 202 for unknown email (anti-enumeration).
- [x] Reset token invalid after use or 30 min.

### Commit
`feat(identity): password reset flow`

---

## US-A-004 — Profile & Address Management

### Scope
- `GET/PATCH /api/v1/me`, `GET/POST/DELETE /api/v1/me/addresses`.
- PII masking in logs; validation per address rules.

### Acceptance
- [ ] Update profile persists; addresses CRUD scoped to owner.

### Commit
`feat(identity): profile and address management`

---

## US-A-009 — Lockout Policy

### Scope
- 5 failed attempts → 15 min lockout (per `08` §7.2); per-IP adaptive limits.
- 423 `ERR_AUTH_003` response with `retryAfter`.

### Acceptance
- [ ] Lockout enforced; unlock after window; reset on success.

### Commit
`feat(identity): lockout policy`

---

## US-M-001 — Audit Log (Tamper-Evident) + T-DAT-001

### Scope
- `AuditEntry` entity + append-only store.
- Hash-chain: each entry includes hash of previous.
- Audit middleware captures who/what/when/from/to for protected commands.
- `GET /api/v1/audit-logs` (permission-gated, filterable).
- FRS-M-001 compliance.

### Acceptance
- [ ] Write to audit is append-only; hash chain verifiable.
- [ ] Audit capture for login, profile change, address change.
- [ ] Integration test verifies chain integrity.

### Commit
`feat(audit): tamper-evident audit log with hash chain`

---

## Sprint Exit
- [ ] Auth flows E2E green; refresh rotation + family revocation tested; audit writes verified.
- [ ] US-A-001,002,003,009 pass DoD with unit + integration tests.
- [ ] CI green; no sev ≥ 2 defects open.
