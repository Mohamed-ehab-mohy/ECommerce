# Sprint 4 — Cart, Inventory Ledger & Caching (US-C-001,003,004; US-B-003,004; US-F-001,002)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 1 | Goal:** Cart persistence and the stock ledger foundation.
> **Source of truth:** `docs/04a` FR-03, FR-06; `docs/06-system-architecture.md` §7.4 (caching); `docs/07-data-model-erd.md` cart + inventory schemas.
> **Dependencies:** S3. **Blocks:** S5.
> **Exit:** US-C-001,003,004; US-B-003,004; US-F-001,002 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-C-001 | Guest cart persistence | 3 | [ ] |
| US-C-003, US-C-004 | Cart mutations + totals | 5 | [ ] |
| US-B-003, US-B-004 | Localized + multi-currency product pricing | 10 | [ ] |
| US-F-001 | Warehouse management | 3 | [ ] |
| US-F-002 | Stock ledger (append-only) | 3 | [ ] |
| T-DAT-003 | Redis cache + cart repository | 5 | [x] |
| T-DAT-004 | Currency/locale configuration service | 3 | [ ] |

---

## T-DAT-003 — Redis Cache + Cart Repository

### Scope
- Redis registration in Infrastructure (connection, healthcheck).
- `ICartRepository` with cache-aside (`cart:{ownerKey}`, TTL 30 d), write-through on mutation.
- Stampede protection (per-key lock, 100 ms).

### Acceptance
- [x] Cart survives restart; mutations update cache + store.
- [x] Cache hit ratio logged; invalidation on mutation.

### Commit
`feat(cart): redis cache-aside cart repository`

---

## T-DAT-004 — Currency/Locale Configuration Service

### Scope
- Config service: supported locales (10), currencies (5), default conversion rates feed (stub FX).
- `Money` conversions with rounding rules.

### Acceptance
- [ ] Pricing displayed in requested currency with correct rounding.

### Commit
`feat(pricing): currency and locale configuration service`

---

## US-C-001,003,004 — Cart Persistence + Mutations + Totals

### Scope
- `GET/POST/PATCH/DELETE /api/v1/carts/me(…)/items`.
- Guest cart via `X-Cart-Key`; item qty 1..99; inactive product → 409.
- Totals recompute server-side (subtotal, item discount, shipping, tax, total).
- Optimistic version for concurrency.

### Acceptance
- [ ] Cart totals correct in 5 currencies.
- [ ] Concurrent mutations safe (QAS-style).
- [ ] Guest cart persists 30 days.

### Commit
`feat(cart): cart mutations and server-side totals`

---

## US-B-003,004 — Localized + Multi-Currency Pricing

### Scope
- `product_translations` for 10 locales; `product_prices` per currency.
- `GET /api/v1/products` localized by `locale` + `currency` query.

### Acceptance
- [ ] Same product returns localized name + converted price.

### Commit
`feat(catalog): localized and multi-currency product pricing`

---

## US-F-001 — Warehouse Management

### Scope
- `Warehouse` aggregate + CRUD (`/api/v1/warehouses`, permission-gated).
- Fields: code, name, address, timezone, status.

### Acceptance
- [ ] Warehouse CRUD; code unique; audit on change.

### Commit
`feat(inventory): warehouse management`

---

## US-F-002 — Stock Ledger (Append-Only)

### Scope
- `StockItem` (sku, warehouse, on_hand, allocated, available) + `StockMovement` ledger (append-only, reason-coded).
- Ledger writes trigger recompute of `on_hand`; no in-place edits.

### Acceptance
- [ ] Ledger append-only verified (no UPDATE on movements).
- [ ] Movement inserts atomic with stock recompute.

### Commit
`feat(inventory): append-only stock ledger`

---

## Sprint Exit
- [ ] Cart totals correct in 5 currencies; ledger append-only verified; cache hit ratio baseline.
- [ ] US-C-001,003,004; US-B-003,004; US-F-001,002 green.
- [ ] CI green.
