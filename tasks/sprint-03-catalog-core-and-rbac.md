# Sprint 3 — Catalog Core & RBAC (US-B-001,002; US-A-005,006)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 1 | Goal:** Products, taxonomy, and permission enforcement.
> **Source of truth:** `docs/04a-functional-requirements-specification.md` (FR-02), `docs/06a-domain-model.md` Catalog context, `docs/07-data-model-erd.md` catalog schema, `docs/03a-user-stories.md`.
> **Dependencies:** S1, S2. **Blocks:** S4.
> **Exit:** US-B-001,002 and US-A-005,006 green.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| US-B-001 | Product CRUD with SKU/slug uniqueness | 5 | [ ] |
| US-B-002 | Categories & brands | 4 | [ ] |
| US-A-006 | RBAC enforcement + role management | 3 | [ ] |
| US-A-005 | Support account lookup | 2 | [ ] |
| T-SEC-002 | Permission matrix seeded + policy infrastructure | 4 | [ ] |
| T-DAT-002 | Catalog schema + indexes | 2 | [x] |

---

## T-DAT-002 — Catalog Schema + Indexes

### Scope
- Tables per `07-data-model-erd.md`: `products`, `product_variants`, `categories`, `category_hierarchy`, `brands`, `product_translations`, `product_prices`.
- Conventions: snake_case, `decimal(18,4)` for money, audit fields.
- Indexes: SKU unique, slug unique, `(locale)` for translations, price index for search.

### Acceptance
- [x] Migration `CatalogMigration` additive; EF mapping matches ERD.
- [x] Integration test verifies uniqueness constraints.

### Commit
`feat(infra): catalog schema and indexes`

---

## US-B-001 — Product CRUD

### Scope
- Domain aggregate `Product` (rich model: name, description, slug, SKU, status, price currency).
- `POST/GET/PATCH/DELETE /api/v1/products` (+ `/products/{id}` public detail).
- SKU/slug uniqueness, versioned update, `ProductUpdated` event.
- Errors: 409 SKU conflict; 422 validation.

### Acceptance
- [ ] CRUD E2E; uniqueness enforced at DB level.
- [ ] Deactivate (soft-delete) hides from public reads.
- [ ] Audit captured on every mutation.

### Commit
`feat(catalog): product crud with sku/slug uniqueness`

---

## US-B-002 — Categories & Brands

### Scope
- Category tree (depth ≤ 5, no cycles), brand management.
- `GET /api/v1/categories`, brands endpoints.
- Public product list by category/brand.

### Acceptance
- [ ] Cycle/depth violations rejected (400).
- [ ] Tree API returns nested structure.

### Commit
`feat(catalog): categories hierarchy and brands`

---

## T-SEC-002 + US-A-006 — RBAC Enforcement + Role Management

### Scope
- Permission codes registry (from `11-identity-and-permissions.md` placeholder → define baseline set).
- Seed roles: `Customer`, `Staff`, `Finance`, `Admin`, `SuperAdmin`.
- Policy infrastructure: `AuthorizationBehavior` in MediatR pipeline (per `06` §7.1), permission claims.
- `POST /api/v1/roles`, role-permission assignment endpoints (SuperAdmin).
- Enforce on catalog endpoints: `catalog.product.write`, `catalog.product.delete`.

### Acceptance
- [ ] Every protected endpoint returns 403 with permission id when missing.
- [ ] RBAC on endpoints works; role assignment audited.
- [ ] Default deny (no permission = denied).

### Commit
`feat(authz): rbac permission matrix and policy infrastructure`

---

## US-A-005 — Support Account Lookup

### Scope
- `GET /api/v1/customers` + `GET /api/v1/customers/{id}` (staff, permission-gated), PII masking in responses.

### Acceptance
- [ ] Support can search by email; PII masked for non-privileged roles.

### Commit
`feat(identity): support customer lookup with pii masking`

---

## Sprint Exit
- [ ] Product slice with validation + audit; every endpoint permission-mapped (403 tested).
- [ ] US-B-001,002 and US-A-005,006 green.
- [ ] CI green.
