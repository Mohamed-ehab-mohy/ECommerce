# Sprint 15 — Hardening, Load & Security (T-TST-003..005; T-SEC-003; T-OPS-003)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 3 | Goal:** Prove NFRs and harden the platform.
> **Source of truth:** `docs/05-non-functional-requirements.md` §14 (load suite), `docs/09-security-architecture.md`, `docs/30-test-strategy-and-quality-gates.md`, `docs/32-deployment-infrastructure-and-runbooks.md` §12 (runbooks).
> **Dependencies:** S14. **Blocks:** S16.
> **Exit (M5):** All NFR gates green; security review passed; runbooks validated.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| T-TST-003 | Full load suite S1–S8 (per `05` §14) | 6 | [ ] |
| T-TST-004 | Fault injection + chaos (Redis/MQ/DB) | 4 | [ ] |
| T-SEC-003 | Security review + ASVS walkthrough + SAST results | 4 | [ ] |
| T-TST-005 | Performance remediation backlog | 3 | [ ] |
| T-OPS-003 | Runbooks for top-10 failure modes | 3 | [ ] |

---

## T-TST-003 — Full Load Suite (per `05` §14)

### Scope
- Execute S1–S8 load scenarios from `05-non-functional-requirements.md`: peak order surge (1,000/min), catalog browse, mixed workload, search (p95 ≤ 300 ms), long-duration soak.
- Capture into `34-load-and-performance-test-report.md`.

### Acceptance
- [ ] All NFR-PERF thresholds met; evidence documented.

### Commit
`test(perf): full load suite s1-s8`

---

## T-TST-004 — Fault Injection + Chaos

### Scope
- Kill Redis/MQ/DB in staging; verify degradation matrix (`06` §P7) + auto-recovery; record findings.

### Acceptance
- [ ] Failover/graceful degradation validated; no data corruption.

### Commit
`test(chaos): fault injection for redis, mq and db`

---

## T-SEC-003 — Security Review + ASVS + SAST

### Scope
- OWASP ASVS L2 walkthrough, SAST results review, dependency/container scans, pen-test (external or internal), collate in `35-security-review.md`.

### Acceptance
- [ ] All critical/high findings remediated or risk-accepted with sign-off.

### Commit
`docs(security): security review and asvs walkthrough`

---

## T-TST-005 — Performance Remediation Backlog

### Scope
- Backlog from load suite (indexes, query tuning, cache adjustments); remediate within buffer.

### Acceptance
- [ ] Remediation items closed or scheduled with owners.

### Commit
`perf: apply load suite remediation`

---

## T-OPS-003 — Runbooks Top-10 Failure Modes

### Scope
- Implement + validate runbooks RUN-001..010 from `32` §12 (API down, DB failover, MQ loss, Redis failover, webhook failure, migration timeout, secret compromise, disk full, queue lag, perf regression).

### Acceptance
- [ ] Each runbook executed once in staging; steps validated.

### Commit
`docs(ops): validated runbooks for top failure modes`

---

## Sprint Exit — M5
- [ ] All NFR gates green; security review passed; runbooks validated.
- [ ] CI green.
