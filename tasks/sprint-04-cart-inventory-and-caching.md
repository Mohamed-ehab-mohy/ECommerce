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
| US-C-001 | Guest cart persistence | 3 | [x] |
| US-C-003, US-C-004 | Cart mutations + totals | 5 | [x] |
| US-B-003, US-B-004 | Localized + multi-currency product pricing | 10 | [x] |
| US-F-001 | Warehouse management | 3 | [ ] |
| US-F-002 | Stock ledger (append-only) | 3 | [ ] |
| T-DAT-003 | Redis cache + cart repository | 5 | [x] |
| T-DAT-004 | Currency/locale configuration service | 3 | [x] |

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
- [x] Pricing displayed in requested currency with correct rounding.

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
- [x] Cart totals correct in 5 currencies (flat shipping $9.90 USD converted; tax 5% on subtotal−discount; 4-dp math, 2-dp display).
- [x] Concurrent mutations safe (optimistic version → 409).
- [x] Guest cart persists 30 days (cache-aside `cart:{ownerKey}` write-through, TTL 30 d).

### Commit
`feat(cart): cart mutations and server-side totals`

---

## US-B-003,004 — Localized + Multi-Currency Pricing

### Scope
- `product_translations` for 10 locales; `product_prices` per currency.
- `GET /api/v1/products` localized by `locale` + `currency` query.

### Acceptance
- [x] Same product returns localized name + converted price.

### Closing (2026-08-06)
- Read path was already implemented (factory, repository includes, controller query params, catalogs, `Money`).
- Closed the write-side gap: `CreateProductCommandValidator`/`UpdateProductCommandValidator` now reject unsupported `currency`/`locale` against the catalogs (prevents a stored unsupported currency from later throwing `GetRate` → 500 on the public endpoint).
- Added `GetProductQueryValidator` (parity with the list validator: unsupported `locale`/`currency` now fail validation on GET too).
- Added acceptance coverage: single product with en+ar translations and USD+EUR prices resolves per-request name/price, converts when the requested currency has no own row, and rejects unsupported values.
- Gate: Release 0 warnings; format clean; Unit 246; Architecture 6; Integration 43; no pending model changes.

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
