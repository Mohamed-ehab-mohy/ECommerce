# Sprint 13 — Real-time & GA Release (US-N-001..004; US-K-005)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 2 | Goal:** SignalR live features and v1.1 GA.
> **Source of truth:** `docs/04a` FR-12 (real-time), FR-11 (review voting); `docs/08-api-design.md` §9 (hubs); `docs/06-system-architecture.md` §7.4 (backplane).
> **Dependencies:** S12. **Blocks:** v1.1 consumers.
> **Risk:** Load target miss → NFR-PERF remediation buffer.
> **Exit — v1.1 GA (M4):** Full commercial flows green; 1,000 orders/min load test passes; observability complete; dashboards live.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-N-001 | Customer order hub | 3 | [ ] |
| US-N-002, US-N-004 | Warehouse hub + resume on reconnect | 4 | [ ] |
| US-N-003 | Live operational tiles | 2 | [ ] |
| US-K-005 | Review voting | 1 | [ ] |
| T-DAT-016 | Redis backplane + hub auth + replay | 4 | [ ] |
| T-TST-002 | v1.1 staging deploy + load test (1,000 orders/min) | 5 | [ ] |

---

## T-DAT-016 — Redis Backplane + Hub Auth + Replay

### Scope
- Redis SignalR backplane; JWT hub auth (`u:{userId}` groups); `lastEventId` replay on reconnect; envelope per `08` §9.

### Acceptance
- [ ] Event delivered to correct user group; reconnect replays missed events.

### Commit
`feat(realtime): redis backplane, hub auth and event replay`

---

## US-N-001 — Customer Order Hub

### Scope
- `orderHub`: push `OrderStatusChanged`, `OrderTimelineUpdated` to `u:{userId}`.

### Acceptance
- [ ] Order events push live to customer group.

### Commit
`feat(realtime): customer order hub`

---

## US-N-002,004 — Warehouse Hub + Resume

### Scope
- `warehouseHub`: `NewFulfillmentTask`, `TaskStatusChanged`, `StockAlert` to `wh:{id}`; client resume via lastEventId.

### Acceptance
- [ ] Warehouse events push; reconnect resumes without duplicates.

### Commit
`feat(realtime): warehouse hub with resume`

---

## US-N-003 — Live Operational Tiles

### Scope
- `adminHub` live metrics (order rate, stock alerts, recon drift) to `admins`.

### Acceptance
- [ ] Admin dashboard updates live; permission-gated.

### Commit
`feat(realtime): live operational tiles`

---

## US-K-005 — Review Voting

### Scope
- Vote helpful/not helpful on reviews; unique per customer+review.

### Acceptance
- [ ] Vote idempotent per user; aggregate updated.

### Commit
`feat(reviews): review helpfulness voting`

---

## T-TST-002 — v1.1 Staging Deploy + Load Test (1,000 orders/min)

### Scope
- Full load suite: catalog browse + cart + checkout + payment sandbox at 1,000 orders/min.
- Assert: p95 < 800 ms, error < 0.5%, 0 SLO burn; record in `34-load-and-performance-test-report.md`.

### Acceptance
- [ ] 1,000 orders/min load test passes; report recorded.

### Commit
`test(perf): v1.1 load test at 1000 orders per minute`

---

## Sprint Exit — v1.1 GA
- [ ] Full commercial flows green; 1,000 orders/min load test passes; observability complete; dashboards live.
- [ ] US-N-001..004; US-K-005 green.
- [ ] CI green.
