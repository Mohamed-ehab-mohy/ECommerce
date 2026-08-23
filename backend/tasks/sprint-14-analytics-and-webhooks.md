# Sprint 14 — Analytics & Webhooks (US-L-001,002,006,007; US-M-004,008)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 3 | Goal:** Enterprise reporting and partner integrations.
> **Source of truth:** `docs/04a` FR-14 (analytics), FR-13 (webhooks); `docs/08-api-design.md` §8 (webhooks).
> **Dependencies:** S13. **Blocks:** S16.
> **Risk:** Report performance on large datasets → covering indexes + async.
> **Exit:** US-L-001,002,006,007; US-M-004,008 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-L-001, US-L-002 | Sales + product performance analytics | 5 | [x] |
| US-L-006, US-L-007 | Finance reports + async exports | 5 | [x] |
| US-M-004 | Signed webhooks with replay | 5 | [x] |
| US-M-008 | Bulk operations with error reports | 3 | [x] |
| T-DAT-017 | Reporting query service + export jobs | 4 | [x] |
| T-DAT-018 | Webhook dispatcher + delivery log + replay endpoint | 4 | [x] |

---

## T-DAT-017 — Reporting Query Service + Export Jobs

### Scope
- Read-model query service for reports (covering indexes); async export jobs (CSV) with status + download.

### Acceptance
- [x] Reports run within budget on large dataset; export job downloadable.

### Commit
`feat(reports): reporting query service and export jobs`

---

## US-L-001,002 — Sales + Product Performance Analytics

### Scope
- `GET /api/v1/reports/sales`, `reports/inventory` (time-series aggregates, filters).

### Acceptance
- [x] Aggregates correct per seeded data; dashboard-ready.

### Commit
`feat(reports): sales and product performance analytics`

---

## US-L-006,007 — Finance Reports + Async Exports

### Scope
- Finance report endpoint + async export of any report.

### Acceptance
- [x] Finance report matches ledger; export completes async.

### Commit
`feat(reports): finance reports and async exports`

---

## T-DAT-018 — Webhook Dispatcher + Delivery Log + Replay

### Scope
- Dispatcher (HMAC-SHA256 signed), delivery log, retries (5), suspension + alert, `POST /api/v1/webhooks/replay`.
- Register endpoints: `POST /api/v1/webhook-endpoints`, secret rotate.

### Acceptance
- [x] Delivery signed; retries/suspension per policy; replay verified.

### Commit
`feat(integrations): signed webhook dispatcher with replay`

---

## US-M-004 — Signed Webhooks

### Scope
- Event catalog (`order.placed`, `order.paid`, `order.shipped`, `order.cancelled`, `refund.completed`, `product.updated`, `stock.low`) per `08` §8.2.

### Acceptance
- [x] Partner receives signed event; verification test passes.

### Commit
`feat(integrations): signed webhook event catalog`

---

## US-M-008 — Bulk Operations with Error Reports

### Scope
- Bulk ops (status changes, exports) with per-item error reports.

### Acceptance
- [x] Partial success reported per item.

### Commit
`feat(platform): bulk operations with error reports`

---

## Sprint Exit
- [x] Partner webhook delivered with HMAC + replay verified; analytics dashboards usable.
- [x] US-L-001,002,006,007; US-M-004,008 green.
- [x] CI green.
