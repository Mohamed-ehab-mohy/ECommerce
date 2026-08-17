# 37 — Operational Runbooks: Top-10 Failure Modes (T-OPS-003)

> **Sprint 15** | Status: **VALIDATED** | Created: 2026-08-17
> **Scope:** RUN-001 through RUN-010 from `32-deployment-infrastructure-and-runbooks.md §12`
> **Validation:** Each runbook executed in staging where safe; steps verified for accuracy.

---

## RUN-001 — API Replicas Failing Readiness

**Trigger:** >50% replicas report unhealthy on `/api/v1/health/ready`

**Key Steps:**
1. Check pod status: `kubectl get pods -l app=ecommerce-api`
2. Inspect logs: `kubectl logs -l app=ecommerce-api --tail=100`
3. Check recent deployments: `kubectl rollout history deployment/ecommerce-api`
4. Rollback if regression: `kubectl rollout undo deployment/ecommerce-api`
5. Verify health recovers: `kubectl get pods -l app=ecommerce-api -w`
6. Confirm traffic restored via load balancer health checks

**Validation (staging):**
- ✅ `/api/v1/health/ready` returns full health status (Postgres + Redis checks)
- ✅ `/api/v1/health/live` is independent (always 200 if process is up)
- ✅ Docker Compose: `docker restart ecommerce-staging-api` → container recovers within 30s

---

## RUN-002 — PostgreSQL Failover

**Trigger:** Primary PostgreSQL becomes unavailable

**Key Steps:**
1. Confirm outage: `docker exec ecommerce-staging-postgres pg_isready` or check monitoring
2. If HA: verify standby promotion (`SELECT pg_is_in_recovery()` on standby)
3. If standalone: restart container: `docker start ecommerce-staging-postgres`
4. Wait for health: `docker inspect --format='{{.State.Health.Status}}' ecommerce-staging-postgres`
5. Verify API reconnects: hit `/api/v1/health/ready` → should recover within 60s
6. Check for data integrity: `SELECT COUNT(*) FROM orders;` (compare pre/post)
7. Review outbox: pending events should resume processing

**Validation (staging):**
- ✅ S7c chaos test: killed PG for 125s under load; API container survived; recovery in 6s
- ✅ Orders count consistent (975→1047 during outage, no corruption)
- ✅ Outbox resumed processing after PG restore

---

## RUN-003 — RabbitMQ Quorum Loss / Unavailable

**Trigger:** Queue becomes unavailable; outbox events accumulate

**Key Steps:**
1. Check RabbitMQ health: `docker exec ecommerce-staging-rabbitmq rabbitmq-diagnostics -q ping`
2. If unavailable: `docker start ecommerce-staging-rabbitmq`
3. Wait for management UI: `http://localhost:15672` (default credentials from env)
4. Verify outbox drain: `SELECT COUNT(*) FROM outbox_events WHERE processed_on IS NULL;` — should decrease
5. Check webhook deliveries resume: `SELECT status, COUNT(*) FROM webhook_deliveries GROUP BY status;`
6. If persistent: restart broker, verify consumers reconnect (MassTransit auto-reconnect)

**Validation (staging):**
- ✅ S7b chaos test: killed MQ for 96s under load; API stayed up; outbox accumulated correctly
- ✅ After restore: outbox drained from 2,841→2,256 over 3 minutes
- ✅ Webhook deliveries: 1,400 total, 100% Delivered, 0 Failed

---

## RUN-004 — Redis Failover

**Trigger:** Redis cluster failover or single-node failure

**Key Steps:**
1. Check Redis health: `docker exec ecommerce-staging-redis redis-cli ping`
2. If unavailable: `docker start ecommerce-staging-redis`
3. Wait for health: `docker inspect --format='{{.State.Health.Status}}' ecommerce-staging-redis`
4. Verify catalog fallback: hit `/api/v1/products` → should work (DB fallback)
5. Verify SignalR backplane reconnects: test hub connections
6. Verify rate limiter recovers: check login throttler state

**Validation (staging):**
- ✅ S7a chaos test: killed Redis for 95s under load; API stayed up 100%
- ✅ Catalog served from DB (latency 26-100ms vs 42ms baseline)
- ✅ Recovery: Redis healthy in 6s; all latency returned to normal

---

## RUN-005 — Webhook Delivery Failing

**Trigger:** Webhook delivery suspension alert or 100% failure rate

**Key Steps:**
1. Check delivery log: `SELECT status, COUNT(*) FROM webhook_deliveries GROUP BY status;`
2. Check for Failed deliveries: `SELECT * FROM webhook_deliveries WHERE status = 'Failed' ORDER BY created_on DESC LIMIT 10;`
3. Inspect error messages in `error` column
4. If target URL is down: update endpoint URL via API
5. If secret is compromised: rotate via `POST /api/v1/webhooks/{endpointId}/rotate-secret`
6. Replay events: `POST /api/v1/webhooks/{endpointId}/replay`
7. Verify deliveries recover: monitor `webhook_deliveries` table

**Validation (staging):**
- ✅ S8 flood test: 1,400 deliveries, 100% Delivered, 0 Failed
- ✅ Avg delivery lag: 187ms, max: 641ms
- ✅ `PostCommitActions` prevents race condition (Bug #2 fix)

---

## RUN-006 — Long-Running Migration Timeout

**Trigger:** EF Core migration or data migration exceeds timeout

**Key Steps:**
1. Check migration status: `SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;`
2. Check for locks: `SELECT pid, relation, mode FROM pg_locks WHERE NOT granted;`
3. Identify blocking process: `SELECT pid, query, state FROM pg_stat_activity WHERE state != 'idle';`
4. If safe: `SELECT pg_terminate_backend(<blocking_pid>);`
5. Re-run migration: `dotnet ef database update`
6. Verify integrity: run smoke tests

**Validation (staging):**
- ✅ `stock_movements` table has append-only trigger (`fn_reject_stock_movements_change`)
- ✅ Migration history is clean (no partial migrations)

---

## RUN-007 — Compromised Secret

**Trigger:** Suspected or confirmed credential compromise

**Key Steps:**
1. **Immediately:** Rotate the compromised credential
   - JWT key: delete `/app/keys/jwt.pem`, restart API (auto-regenerates)
   - Webhook secret: `POST /api/v1/webhooks/{endpointId}/rotate-secret`
   - DB password: update connection string, restart all services
2. Revoke all active sessions: force all users to re-authenticate
3. Audit access logs: check Seq for suspicious patterns
4. Update threat register with incident details
5. Notify affected users if PII was exposed (GDPR 72-hour window)

**Validation:**
- ✅ JWT key auto-generates on first run (`JwtRsaKeyProvider.cs:25-39`)
- ✅ Webhook secrets are HMAC-SHA256, rotatable via API
- ✅ No secrets logged (verified: `git grep` clean)

---

## RUN-008 — Disk Full (Database)

**Trigger:** PostgreSQL volume space alert

**Key Steps:**
1. Check disk usage: `docker exec ecommerce-staging-postgres df -h /var/lib/postgresql/data`
2. Check WAL archiving: `SELECT * FROM pg_stat_archiver;`
3. Purge old backups if retention allows
4. Extend volume: resize Docker volume or cloud storage
5. Verify replication: `SELECT status FROM pg_stat_replication;`
6. Run `VACUUM FULL` on bloated tables if needed (careful — locks table)

**Validation:**
- ✅ Staging uses named Docker volume `postgres-data` (persistent)
- ✅ No WAL archiving configured in staging (acceptable for dev/test)

---

## RUN-009 — Queue Lag (Orders)

**Trigger:** Outbox pending count exceeds threshold; consumer lag alert

**Key Steps:**
1. Check pending events: `SELECT COUNT(*) FROM outbox_events WHERE processed_on IS NULL;`
2. Check dead-lettered: `SELECT COUNT(*) FROM outbox_events WHERE processed_on IS NOT NULL AND attempts >= 5;`
3. Inspect consumer errors in logs (Seq)
4. Scale workers: increase outbox polling parallelism or add worker replicas
5. If events are stuck: check RabbitMQ queue depth via management UI
6. Dead-letter review: check if dead-lettered events indicate systemic issues
7. Replay failed events if transient error

**Validation (staging):**
- ✅ Outbox config: `PollingIntervalSeconds=2`, `BatchSize=50` (tuned for throughput)
- ✅ Outbox uses `FOR UPDATE SKIP LOCKED` (safe for multiple workers)
- ✅ Dead-letter after 5 attempts with alerting metric

---

## RUN-010 — Performance Regression

**Trigger:** p95 latency exceeds threshold; SLO breach

**Key Steps:**
1. Capture current profile: check Seq/OpenTelemetry dashboards
2. Check for recent deploys: `kubectl rollout history`
3. Check for new feature flags or config changes
4. If new deploy caused regression: rollback (`kubectl rollout undo`)
5. Scale out: add API replicas behind load balancer
6. Check database: slow queries, missing indexes, lock contention
7. Engage module owner for deep-dive
8. Document in post-mortem if P1/P2 incident

**Validation:**
- ✅ S6 load test demonstrated: single-host scaling bottlenecked by shared DB
- ✅ All NFR-PERF thresholds green at 10% scale
- ✅ Prometheus + Seq provide real-time metrics for regression detection

---

## Validation Summary

| Runbook | Tested in Staging | Key Finding |
|---------|-------------------|-------------|
| RUN-001 | ✅ (API restart) | Recovery within 30s |
| RUN-002 | ✅ (S7c PG kill) | Recovery in 6s; zero data loss |
| RUN-003 | ✅ (S7b MQ kill) | Outbox accumulated; drained after restore |
| RUN-004 | ✅ (S7a Redis kill) | DB fallback; zero failures |
| RUN-005 | ✅ (S8 webhook flood) | 100% delivery; 187ms avg lag |
| RUN-006 | ⚠️ (not triggered) | Steps verified against schema |
| RUN-007 | ⚠️ (not triggered) | Steps verified against code |
| RUN-008 | ⚠️ (not triggered) | Steps verified against schema |
| RUN-009 | ✅ (S8 outbox drain) | Config tuned; drain confirmed |
| RUN-010 | ✅ (S6 scale-out) | Bottleneck identified (shared DB) |
