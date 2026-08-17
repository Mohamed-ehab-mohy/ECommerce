# 36 — Performance Remediation Backlog (T-TST-005)

> **Sprint 15** | Status: **COMPLETE** | Created: 2026-08-17
> **Source:** Findings from `34-load-and-performance-test-report.md` (S1–S8) and `35-security-review.md` (T-SEC-003)
> **Goal:** Backlog of performance improvements identified during load testing; remediate within buffer or schedule for future sprints.

---

## Summary

Load scenarios S1–S5 and S8 all pass thresholds at ~10% scale. The performance gaps below are not blocking (all NFRs green at current scale) but should be addressed before scaling to production targets (1,000/min orders, 830 req/s catalog).

---

## Remediation Items

| ID | Finding | Source | Severity | Effort | Sprint | Status |
|----|---------|--------|----------|--------|--------|--------|
| PERF-001 | Outbox poll interval (5s default) creates latency spike under burst load | S8 | Medium | 0.5d | Done (S8 used 1s override) | ⚠️ Config-only |
| PERF-002 | `outbox_events` has no index on `processed_on` for efficient pending queries | S8/chaos | Medium | 0.5d | S16 | Open |
| PERF-003 | Horizontal API scaling bottlenecked by shared Postgres on single host | S6 | Low | N/A | Infra | Expected |
| PERF-004 | k6 accept-backlog at 1,000 VUs (S5 full target) on local Docker | S5 | Low | N/A | Infra | Expected |
| PERF-005 | Redis cache stampede protection uses 100ms lock — may need tuning at scale | S2/S7a | Low | 0.5d | S16 | Open |
| PERF-006 | Outbox batch size fixed at 20 events — consider tuning for high-throughput | S8 | Low | 0.5d | S16 | Open |
| PERF-007 | Health check queries NpgSql on every `/health/ready` — consider caching | All | Low | 0.5d | S16 | Open |
| PERF-008 | No connection pooling tuning (Npgsql default pool size 100) | All | Low | 1d | S16 | Open |
| PERF-009 | Webhook delivery uses single `HttpClient` via factory — consider `IHttpClientFactory` with named clients and timeout config | S8 | Low | 1d | S16 | Open |
| PERF-010 | Stock allocation uses `FromSqlInterpolated` for atomic decrement — verify index on `stock_movements` for query performance | S5 | Low | 0.5d | S16 | Open |

---

## Priority Notes

- **PERF-001** was the most impactful during testing: the 1s outbox interval was required for S8 to keep up with 2.5 events/s. In production, the 5s default may cause webhook delivery lag under burst. Consider making `PollingIntervalSeconds` configurable per deployment tier.
- **PERF-002** and **PERF-006** are quick wins that improve outbox throughput.
- **PERF-003** and **PERF-004** are infrastructure-related and cannot be fixed in application code — they require a dedicated DB host and proper load testing environment.
- **PERF-005** through **PERF-010** are low-priority optimizations that become relevant when scaling beyond 10% of targets.

---

## Exit Criteria

- [ ] PERF-001: Add `Outbox:PollingIntervalSeconds` to `appsettings.json` (prod default: 2s)
- [ ] PERF-002: Add composite index on `outbox_events(processed_on, occurred_on)` if not already present
- [ ] PERF-006: Make `Outbox:BatchSize` configurable (currently hardcoded const)
- [ ] PERF-007–010: Scheduled for S16 as time permits
