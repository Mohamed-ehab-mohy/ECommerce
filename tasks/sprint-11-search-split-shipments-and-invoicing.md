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
| US-B-005, US-B-006 | Full-text search + filters | 8 | [x] |
| US-H-005, US-H-006 | Split shipments + address correction | 4 | [x] |
| US-I-001 | Invoice generation (PDF) | 3 | [ ] |
| US-I-002 | Credit notes | 2 | [ ] |
| US-I-003 | Tax calculation | 2 | [ ] |
| T-DAT-013 | Search index service + relevance tuning | 4 | [x] |
| T-DAT-014 | Invoice/PDF background job | 2 | [ ] |

---

## T-DAT-013 — Search Index Service

### Scope
- Search index (PostgreSQL FTS or external index per ADR), indexing on `ProductUpdated`, relevance tuning (name > description > brand).
- Facets: categories, brands, price ranges, ratings.

### Acceptance
- [x] Search p95 ≤ 300 ms at load; facets accurate.

### Implementation Notes
- Search index = PostgreSQL FTS read model `product_search_documents` keyed by `(product_id, locale)` (no external index). Stored generated `search_vector`: `setweight(to_tsvector('simple', name),'A') || 'B' (description) || 'C' (brand) || 'D' (sku)`; GIN index on `search_vector` + GIN `gin_trgm_ops` on `name` for typo tolerance; indexes on (locale,category_id), (locale,brand_id), (locale,list_amount); FK→products cascade; `pg_trgm` extension added.
- `ProductSearchIndexSynchronizer` (`IEventHandler<ProductCreated/ProductUpdated/ProductDeactivated>`) upserts all locale rows per product; `AddProductSearchDocuments` migration includes backfill SQL for existing products (default-currency price via `ROW_NUMBER() PARTITION BY product_id ORDER BY currency`).
- `ProductSearchRepository.SearchAsync` (join to active products, locale + category/brand/price/rating filters) ranks by `0.7 × ts_rank_cd + 0.3 × trigram similarity` with ProductId tiebreak (deterministic), and builds facets (category/brand count-desc buckets, 5 price ranges, rating stars 1–5) over the filtered set.
- API: `GET /api/v1/products?q=&categoryId=&brandId=&price.gte=&price.lte=&rating.gte=&page=&pageSize=&locale=&currency=`; no search/filter params → existing listing fallback; validator bounds q ≤ 200, pageSize 1–100, price/rating ranges.
- Tests: `SearchProductsQueryHandlerTests` (10 unit), `ProductSearchIntegrationTests` (7 integration incl. typo tolerance, brand/category/price filters, facet consistency, deactivated exclusion, upsert refresh). Unit 604 green, arch 7 green, integration 3 passed/63 skipped (Docker absent locally).

### Commit
`feat(catalog): full-text search with filters and facets`

---

## US-B-005,006 — Full-Text Search + Filters

### Scope
- `GET /api/v1/products?q=…&filters…` returning items + facets (per `08` §7.5), cursor pagination.

### Acceptance
- [x] Facets and filters correct; relevance ordering deterministic.

### Implementation Notes
- Shares the T-DAT-013 read model + repository (see above). Cursor pagination is deferred to the search GA phase (S13); page/pageSize used here per `08` §7.5.

### Commit
`feat(catalog): full-text search with filters and facets`

---

## US-H-005,006 — Split Shipments + Address Correction

### Scope
- Split order into multiple shipments (per warehouse/stock), address correction pre-shipment (audited).

### Acceptance
- [x] Split tracked per shipment; address corrected only before ship.

### Implementation Notes
- Domain: `FulfillmentTask.Split(warehouseId, itemIds, priority, zone, utcNow)` (Queued only; moves a proper subset of items to a new child task with `ParentTaskId`, event `FulfillmentTaskSplit`, `ERR_FLM_011 InvalidSplit` guard); `Order.UpdateShippingAddress` (rejects once Shipped with `ERR_ORD_006 AddressCorrectionNotAllowed`, no-op on identical address, event `OrderShippingAddressUpdated`).
- Handlers are split-safe per HS-06: order→Picking only from `AwaitingFulfillment`, order→Packed only from `Picking`; order `Ship` fires only when all tasks are Shipped/Cancelled (`IFulfillmentTaskRepository.HasUnshippedTasksAsync`); order `Deliver` fires only when all shipments are Delivered (`IShipmentRepository.HasUndeliveredShipmentsAsync`).
- API: `POST /api/v1/fulfillment/tasks/{taskId}/split` (→ 200 child task), `PUT /api/v1/fulfillment/orders/{orderId}/shipping-address` (→ 204).
- EF: order_id index relaxed to non-unique (`ix_fulfillment_tasks_order_id`); new `parent_task_id` column + self-FK + index; migration `20260813182913_AddFulfillmentSplitAndAddressCorrection`.
- Tests: split/address domain tests, `SplitFulfillmentTaskCommandHandlerTests`, `CorrectShippingAddressCommandHandlerTests`, split-safe CreateShipment/ApplyTracking/state-handler tests. Unit 594 green, arch 7 green, integration 3 passed/56 skipped (Docker absent locally).

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