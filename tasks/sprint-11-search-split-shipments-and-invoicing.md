# Sprint 11 — Search, Split Shipments & Invoicing (US-B-005,006; US-H-005,006; US-I-001..003)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 2 | Goal:** Search GA, split fulfillment, and finance records.
> **Source of truth:** `docs/04a` FR-02 (search), FR-08 (split), FR-09 (finance); `docs/07-data-model-erd.md`.
> **Dependencies:** S10 (shipments), S8 (pricing snapshot for invoices). **Blocks:** S12.
> **Risk:** Search relevance + performance (p95 ≤ 300 ms at load).
> **Exit:** US-B-005,006; US-H-005,006; US-I-001..003 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-B-005, US-B-006 | Full-text search + filters | 8 | [ ] |
| US-H-005, US-H-006 | Split shipments + address correction | 4 | [ ] |
| US-I-001 | Invoice generation (PDF) | 3 | [ ] |
| US-I-002 | Credit notes | 2 | [ ] |
| US-I-003 | Tax calculation | 2 | [ ] |
| T-DAT-013 | Search index service + relevance tuning | 4 | [ ] |
| T-DAT-014 | Invoice/PDF background job | 2 | [ ] |

---

## T-DAT-013 — Search Index Service

### Scope
- Search index (PostgreSQL FTS or external index per ADR), indexing on `ProductUpdated`, relevance tuning (name > description > brand).
- Facets: categories, brands, price ranges, ratings.

### Acceptance
- [ ] Search p95 ≤ 300 ms at load; facets accurate.

### Commit
`feat(search): search index service and relevance`

---

## US-B-005,006 — Full-Text Search + Filters

### Scope
- `GET /api/v1/products?q=…&filters…` returning items + facets (per `08` §7.5), cursor pagination.

### Acceptance
- [ ] Facets and filters correct; relevance ordering deterministic.

### Commit
`feat(catalog): full-text search with filters and facets`

---

## US-H-005,006 — Split Shipments + Address Correction

### Scope
- Split order into multiple shipments (per warehouse/stock), address correction pre-shipment (audited).

### Acceptance
- [ ] Split tracked per shipment; address corrected only before ship.

### Commit
`feat(fulfillment): split shipments and address correction`

---

## US-I-001,003 — Invoice Generation + Tax Calculation

### Scope
- Invoice on `Paid` event; PDF via background job; tax calc via tax service adapter (stub → real).

### Acceptance
- [ ] Invoice generated per order; PDF downloadable; tax correct per jurisdiction.

### Commit
`feat(finance): invoice generation and tax calculation`

---

## T-DAT-014 — Invoice/PDF Background Job

### Scope
- Hangfire job: render invoice → PDF (object store), status tracking.

### Acceptance
- [ ] Job idempotent; PDF stored and retrievable.

### Commit
`feat(finance): invoice pdf background job`

---

## US-I-002 — Credit Notes

### Scope
- Credit note issuance linked to refunds (data model + generation).

### Acceptance
- [ ] Credit note created on refund; linked reference correct.

### Commit
`feat(finance): credit notes`

---

## Sprint Exit
- [ ] Search GA with facets; split shipments tracked; invoices issued on Paid.
- [ ] US-B-005,006; US-H-005,006; US-I-001..003 green.
- [ ] CI green.
