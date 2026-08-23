-- v1.0 staging fixtures for the smoke suite (idempotent).
-- Applied after EF migrations create the schema.

INSERT INTO warehouses (id, code, name, address, timezone, status, created_at, updated_at, is_deleted)
VALUES ('00000000-0000-0000-0000-000000000001', 'SMOKE-WH', 'Smoke Warehouse', '1 Smoke St, Test City', 'UTC', 'Active', now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO stock_items (id, sku, warehouse_id, on_hand, allocated, version, created_at, updated_at, is_deleted)
VALUES ('00000000-0000-0000-0000-000000000002', 'SMOKE-PROD', '00000000-0000-0000-0000-000000000001', 100, 0, 1, now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO stock_movements (id, stock_item_id, type, quantity, on_hand_delta, allocated_delta, reason, reference, note, created_at, updated_at, is_deleted)
VALUES ('00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000002', 'Receipt', 100, 100, 0, 'SMOKE-SEED', 'smoke-stock', NULL, now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO products (id, sku, slug, category_id, brand_id, status, is_featured, image_urls, attributes, created_at, updated_at, is_deleted)
VALUES ('00000000-0000-0000-0000-000000000004', 'SMOKE-PROD', 'smoke-product', NULL, NULL, 'Active', true, '[]', '{}', now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_translations (product_id, locale, name, description, meta_title, meta_description)
VALUES ('00000000-0000-0000-0000-000000000004', 'en', 'Smoke Product', 'Smoke seed product', NULL, NULL)
ON CONFLICT (product_id, locale) DO NOTHING;

INSERT INTO product_prices (product_id, currency, list_amount, offer_amount, updated_at)
VALUES ('00000000-0000-0000-0000-000000000004', 'USD', 49.9900, 39.9900, now())
ON CONFLICT (product_id, currency) DO NOTHING;
