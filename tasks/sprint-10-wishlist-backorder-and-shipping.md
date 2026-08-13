# Sprint 10 — Wishlist, Backorder & Shipping v1 (US-C-006,007; US-F-007,008; US-H-001,002,003,007)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 2 | Goal:** Wishlist, backorder handling, and carrier integration.
> **Source of truth:** `docs/04a` FR-03 (wishlist), FR-06 (backorder), FR-08 (shipping); `docs/07-data-model-erd.md`.
> **Dependencies:** S5 (order events). **Blocks:** S11.
> **Risk:** Carrier API drift → contract tests against sandboxes.
> **Exit:** US-C-006,007; US-F-007,008; US-H-001,002,003,007 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-C-006, US-C-007 | Wishlist + move-to-cart | 4 | [x] |
| US-F-007, US-F-008 | Backorder tracking + fill | 4 | [x] |
| US-H-001, US-H-002 | Fulfillment queues + pick lists | 5 | [ ] |
| US-H-003 | Shipment creation + tracking | 3 | [ ] |
| US-H-007 | Delivery confirmation | 2 | [ ] |
| T-DAT-011 | Carrier adapter (2 carriers) + rate cache | 5 | [ ] |
| T-DAT-012 | Pick-list generation service | 3 | [ ] |

---

## US-C-006,007 — Wishlist + Move-to-Cart

### Scope
- `GET/POST/DELETE /api/v1/wishlist/items`, move-to-cart re-validates availability.

### Acceptance
- [x] Wishlist CRUD; move validates availability.

### Commit
`feat(wishlist): wishlist and move-to-cart`

---

## US-F-007,008 — Backorder Tracking + Fill

### Scope
- Backorder status on orders when stock insufficient but backorderable; fill job on restock (event-driven).

### Acceptance
- [x] Backorder created; fills automatically when stock arrives.

### Commit
`feat(inventory): backorder tracking and fill`

---

## US-H-001,002 — Fulfillment Queues + Pick Lists

### Scope
- Fulfillment tasks per warehouse (queue, assign picker, picked, packed states); pick-list generation service.
- Warehouse hub events (SignalR baseline wiring).

### Acceptance
- [ ] Task state machine enforced; pick list generated per warehouse.

### Commit
`feat(fulfillment): fulfillment queues and pick lists`

---

## US-H-003,007 — Shipment Creation + Tracking + Delivery Confirmation

### Scope
- Create shipment per task (carrier label), tracking numbers, delivery confirmation event.

### Acceptance
- [ ] Shipment created via carrier adapter; tracking updates event-driven.

### Commit
`feat(fulfillment): shipment creation and tracking`

---

## T-DAT-011 — Carrier Adapter + Rate Cache

### Scope
- `ICarrierAdapter` port; 2 carrier sandbox adapters; shipping rate cache (TTL 10 min); contract tests per carrier.

### Acceptance
- [ ] Rate quote from both sandboxes; cached; contract tests green.

### Commit
`feat(shipping): carrier adapters and rate cache`

---

## T-DAT-012 — Pick-List Generation Service

### Scope
- Service to batch order items into pick lists (location-aware, batch size rules).

### Acceptance
- [ ] Pick list generation deterministic and testable.

### Commit
`feat(fulfillment): pick list generation service`

---

## Sprint Exit
- [ ] Warehouse fulfillment queue E2E with carrier labels.
- [ ] US-C-006,007; US-F-007,008; US-H-001,002,003,007 green.
- [ ] CI green.
