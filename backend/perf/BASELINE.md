# T-TST-001 — Baseline Load Smoke: Checkout Path

Recorded baseline for the checkout path against the staging stack.

## Run details

- **Date:** 2026-08-10
- **Environment:** Local Docker Desktop staging stack (`deploy/staging`), API on `http://localhost:8080`, Postgres 16.
- **Scenario:** `perf/k6/checkout-path.js` — full journey per iteration: register -> login -> browse products -> add to cart -> initiate checkout -> authorize payment -> place order.
- **Load profile:** 5 VUs, ramp-up 5 s, steady 30 s, ramp-down 5 s, `sleep(1)` between iterations.
- **Stock:** `SMOKE-PROD` reset to 100 on-hand before the run.

## Results

| Metric | Value | Threshold | Status |
|--------|------:|----------:|--------|
| Requests | 760 (18.5 req/s) | — | — |
| Error rate (`http_req_failed`) | 0.00 % | < 0.5 % | PASS |
| Check success rate | 100 % (760/760) | — | — |
| `checkout` (initiate) p95 | 21.14 ms | < 800 ms | PASS |
| `authorize` p95 | 21.33 ms | < 800 ms | PASS |
| `place` p95 | 31.39 ms | < 800 ms | PASS |

Per-operation latency:

| Operation | avg | med | p90 | p95 | max |
|-----------|---:|---:|---:|---:|---:|
| checkout (initiate) | 14.03 ms | 12.87 ms | 18.17 ms | 21.14 ms | 30.64 ms |
| authorize | 14.78 ms | 13.63 ms | 19.23 ms | 21.33 ms | 50.05 ms |
| place | 22.88 ms | 21.60 ms | 30.05 ms | 31.39 ms | 51.78 ms |
| all requests (incl. register/login) | 113.26 ms | 16.28 ms | 423.08 ms | 456.61 ms | 624.71 ms |

## Threshold assessment

- `p95 < 800 ms` on the checkout-path operations: **met** (worst op is `place` at 31.39 ms).
- `error rate < 0.5 %`: **met** (0.00 %).
- Exit code 0 (k6 reports no crossed thresholds).

## Notes

- The `p95 < 800 ms` target is scoped to the checkout-path operations (initiate, authorize, place) per the sprint acceptance. Global request p95 (456.61 ms) is dominated by `register`/`login`, which run PBKDF2 password hashing and write audit/outbox rows; those are setup steps, not part of the checkout path.
- The API exposes `payment.paymentId` on the checkout initiation response (`payment.paymentId`), which lets the load scenario authorize without a direct DB lookup.
- To re-run: `BASE_URL=http://localhost:8080 bash perf/k6/run-checkout-path.sh` (Docker) or `k6 run perf/k6/checkout-path.js`. Reset stock first (`UPDATE stock_items SET on_hand=100, allocated=0 WHERE sku='SMOKE-PROD';`) or re-apply `deploy/staging/seed.sql` against a fresh database.
