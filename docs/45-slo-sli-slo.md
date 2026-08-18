# SLO/SLI Definitions

## Service Level Objectives

| SLO | SLI | Target | Window |
|-----|-----|--------|--------|
| API Availability | Successful requests / Total requests | 99.9% | 30-day rolling |
| API Latency P95 | Requests < 500ms / Total requests | 99.0% | 30-day rolling |
| API Latency P99 | Requests < 2000ms / Total requests | 95.0% | 30-day rolling |
| Checkout Success Rate | Completed checkouts / Initiated checkouts | 99.5% | 30-day rolling |
| Payment Success Rate | Successful payments / Total payment attempts | 99.0% | 30-day rolling |
| Search Availability | Successful search queries / Total search queries | 99.5% | 30-day rolling |
| Outbox Processing Lag | Messages processed within 60s / Total messages | 99.0% | 1-hour window |
| Data Durability | Successful writes / Total write attempts | 99.999% | Rolling |

## Error Budget

- **API Availability SLO: 99.9%** = 43.8 minutes downtime per 30-day window
- **Error Budget Remaining** tracked via Prometheus metric `slo_error_budget_remaining`
- **Alert at 50%** error budget consumed (warning), **page at 80%** (critical)

## SLI Implementation

### Prometheus Metrics
- `http_request_duration_seconds` — request latency histogram
- `http_requests_total{status="2xx"}` — successful request count
- `checkout_initiated_total` / `checkout_completed_total` — checkout funnel
- `payment_attempt_total{status="succeeded|failed"}` — payment success rate
- `outbox_lag_seconds` — message processing delay
- `search_query_duration_seconds` — search latency

### Alerting Rules
| Alert | Condition | Severity | Action |
|-------|-----------|----------|--------|
| HighErrorRate | 5xx > 1% for 5m | critical | Page on-call |
| HighLatencyP95 | P95 > 500ms for 5m | warning | Investigate |
| ErrorBudgetBurn | > 50% consumed in 24h | warning | Pause deployments |
| ErrorBudgetExhausted | > 80% consumed in 24h | critical | Stop deploys, fix |
| OutboxBacklog | Lag > 60s for 5m | warning | Check consumers |
| PaymentFailureRate | > 2% for 15m | critical | Check provider health |

## Runbook

When an SLO alert fires:
1. Check Grafana dashboard for affected service
2. Review recent deployments (last 2 hours)
3. Check error logs in Seq with CorrelationId
4. If payment-related, check Stripe dashboard
5. If search-related, check Elasticsearch cluster health
6. Escalate if error budget burn rate > 2x expected
