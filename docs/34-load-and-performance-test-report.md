# 34 — Load & Performance Test Report (v1.1)

> **Sprint 13 — T-TST-002** | Status: **PENDING — awaiting staging environment** (Docker daemon / staging stack not available at commit time).
> Target: full commercial flow at **1,000 orders/min**; **p95 < 800 ms**, **error rate < 0.5%**, **0 SLO burn**.

## Load Profile

| Parameter | Value |
|-----------|-------|
| Throughput | 1,000 orders/min (~16.7 orders/s) |
| Journey | register → login → browse → cart → checkout → authorize → place (`perf/k6/checkout-path.js`) |
| Duration | ramp 5s → sustain → ramp down |
| Virtual users | derived to sustain the order rate (≈ 40–50 VUs at ~2.4s/iteration) |
| Env vars | `BASE_URL`, `PRODUCT_ID=00000000-0000-0000-0000-000000000004`, `SKU=SMOKE-PROD`, `VUS`, `DURATION` |

## Thresholds (asserted by k6)

| Metric | Threshold |
|--------|-----------|
| `http_req_failed` | rate < 0.005 |
| `http_req_duration{type:checkout}` | p(95) < 800 ms |
| `http_req_duration{type:authorize}` | p(95) < 800 ms |
| `http_req_duration{type:place}` | p(95) < 800 ms |

## Run Steps (when staging is available)

```bash
docker compose -f deploy/staging/docker-compose.staging.yml up -d
BASE_URL=http://localhost:8080 bash scripts/staging-smoke.sh        # sanity first
BASE_URL=http://localhost:8080 VUS=50 DURATION=10m k6 run perf/k6/checkout-path.js
```

## Results

| Date | Orders/min | p95 (ms) | Error rate | SLO burn | Result |
|------|-----------:|---------:|-----------:|:--------:|:------:|
| —    | —         | —        | —          | —        | Pending |

## Remediation Buffer (NFR-PERF)

If the target is missed: profile hotspots (EF query plan, outbox poll lag, Hangfire worker count, Redis backplane message size), then scale Hangfire workers / bump outbox poll interval / add read-model caching before re-running.
