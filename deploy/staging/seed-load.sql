-- T-TST-003 load-suite fixtures (idempotent). Applied after EF migrations create the schema.
-- Dedicated SKUs per scenario so runs never interfere with each other's stock:
--   LOAD-01 -> S1 checkout baseline + S8 webhook flood (2,000 units)
--   LOAD-04 -> S4 flash-sale burst (2,000 units)
--   RACE-05 -> S5 stock-concurrency: exactly 10 units for 1,000 concurrent buyers
--   Browse/search pool (24 products) -> S2/S3/S6 catalog and search load
--   Webhook endpoint -> S8 delivery flood receiver on the host (port 9099)

INSERT INTO warehouses (id, code, name, address, timezone, status, created_at, updated_at, is_deleted)
VALUES ('00000000-0000-0000-0000-000000000001', 'SMOKE-WH', 'Smoke Warehouse', '1 Smoke St, Test City', 'UTC', 'Active', now(), now(), false)
ON CONFLICT (id) DO NOTHING;

-- ---- S1 / S8: LOAD-01 ----------------------------------------------
INSERT INTO products (id, sku, slug, category_id, brand_id, status, is_featured, image_urls, attributes, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000001', 'LOAD-01', 'load-one', NULL, NULL, 'Active', true, '[]', '{}', now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_translations (product_id, locale, name, description, meta_title, meta_description)
VALUES ('10000000-0000-0000-0000-000000000001', 'en', 'Load Widget', 'Baseline order-flow product', 'Load Widget', NULL)
ON CONFLICT (product_id, locale) DO NOTHING;

INSERT INTO product_prices (product_id, currency, list_amount, offer_amount, updated_at)
VALUES ('10000000-0000-0000-0000-000000000001', 'USD', 24.9900, 19.9900, now())
ON CONFLICT (product_id, currency) DO NOTHING;

INSERT INTO stock_items (id, sku, warehouse_id, on_hand, allocated, version, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000101', 'LOAD-01', '00000000-0000-0000-0000-000000000001', 2000, 0, 1, now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO stock_movements (id, stock_item_id, type, quantity, on_hand_delta, allocated_delta, reason, reference, note, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000102', '10000000-0000-0000-0000-000000000101', 'Receipt', 2000, 2000, 0, 'LOAD-SEED', 'load-s1', NULL, now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_search_documents (product_id, locale, name, description, sku, brand, brand_id, category, category_id, list_amount, currency, rating_average, rating_count)
VALUES ('10000000-0000-0000-0000-000000000001', 'en', 'Load Widget', 'Baseline order-flow product', 'LOAD-01', NULL, NULL, NULL, NULL, 24.9900, 'USD', 0, 0)
ON CONFLICT (product_id, locale) DO NOTHING;

-- ---- S4: LOAD-04 -----------------------------------------------------
INSERT INTO products (id, sku, slug, category_id, brand_id, status, is_featured, image_urls, attributes, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000002', 'LOAD-04', 'load-four', NULL, NULL, 'Active', true, '[]', '{}', now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_translations (product_id, locale, name, description, meta_title, meta_description)
VALUES ('10000000-0000-0000-0000-000000000002', 'en', 'Flash Widget', 'Flash-sale burst product', 'Flash Widget', NULL)
ON CONFLICT (product_id, locale) DO NOTHING;

INSERT INTO product_prices (product_id, currency, list_amount, offer_amount, updated_at)
VALUES ('10000000-0000-0000-0000-000000000002', 'USD', 9.9900, 4.9900, now())
ON CONFLICT (product_id, currency) DO NOTHING;

INSERT INTO stock_items (id, sku, warehouse_id, on_hand, allocated, version, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000201', 'LOAD-04', '00000000-0000-0000-0000-000000000001', 2000, 0, 1, now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO stock_movements (id, stock_item_id, type, quantity, on_hand_delta, allocated_delta, reason, reference, note, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000202', '10000000-0000-0000-0000-000000000201', 'Receipt', 2000, 2000, 0, 'LOAD-SEED', 'load-s4', NULL, now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_search_documents (product_id, locale, name, description, sku, brand, brand_id, category, category_id, list_amount, currency, rating_average, rating_count)
VALUES ('10000000-0000-0000-0000-000000000002', 'en', 'Flash Widget', 'Flash-sale burst product', 'LOAD-04', NULL, NULL, NULL, NULL, 9.9900, 'USD', 0, 0)
ON CONFLICT (product_id, locale) DO NOTHING;

-- ---- S5: RACE-05 (exactly 10 units) ----------------------------------
INSERT INTO products (id, sku, slug, category_id, brand_id, status, is_featured, image_urls, attributes, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000003', 'RACE-05', 'race-five', NULL, NULL, 'Active', false, '[]', '{}', now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_translations (product_id, locale, name, description, meta_title, meta_description)
VALUES ('10000000-0000-0000-0000-000000000003', 'en', 'Rare Collector Item', 'Last ten units in stock', 'Rare Collector Item', NULL)
ON CONFLICT (product_id, locale) DO NOTHING;

INSERT INTO product_prices (product_id, currency, list_amount, offer_amount, updated_at)
VALUES ('10000000-0000-0000-0000-000000000003', 'USD', 499.9900, 449.9900, now())
ON CONFLICT (product_id, currency) DO NOTHING;

INSERT INTO stock_items (id, sku, warehouse_id, on_hand, allocated, version, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000301', 'RACE-05', '00000000-0000-0000-0000-000000000001', 10, 0, 1, now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO stock_movements (id, stock_item_id, type, quantity, on_hand_delta, allocated_delta, reason, reference, note, created_at, updated_at, is_deleted)
VALUES ('10000000-0000-0000-0000-000000000302', '10000000-0000-0000-0000-000000000301', 'Receipt', 10, 10, 0, 'LOAD-SEED', 'load-s5', NULL, now(), now(), false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_search_documents (product_id, locale, name, description, sku, brand, brand_id, category, category_id, list_amount, currency, rating_average, rating_count)
VALUES ('10000000-0000-0000-0000-000000000003', 'en', 'Rare Collector Item', 'Last ten units in stock', 'RACE-05', NULL, NULL, NULL, NULL, 499.9900, 'USD', 0, 0)
ON CONFLICT (product_id, locale) DO NOTHING;

-- ---- S2/S3/S6 browse + search pool (24 products) ---------------------
DO $$
DECLARE
    i INT;
    suffix TEXT;
    p_id UUID;
    name TEXT;
    theme TEXT;
    themes TEXT[] := ARRAY['wireless headphones', 'mechanical keyboard', 'gaming mouse', '4k monitor', 'webcam', 'usb hub'];
BEGIN
    FOR i IN 1..24 LOOP
        suffix := lpad(i::text, 2, '0');
        p_id := ('20000000-0000-0000-0000-' || lpad((i)::text, 12, '0'))::uuid;
        theme := themes[((i - 1) % 6) + 1];
        name := initcap(theme) || ' ' || suffix;

        INSERT INTO products (id, sku, slug, category_id, brand_id, status, is_featured, image_urls, attributes, created_at, updated_at, is_deleted)
        VALUES (p_id, 'CAT-' || suffix, 'cat-' || suffix, NULL, NULL, 'Active', false, '[]', '{}', now(), now(), false)
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO product_translations (product_id, locale, name, description, meta_title, meta_description)
        VALUES (p_id, 'en', name, 'Catalog browse fixture', name, NULL)
        ON CONFLICT (product_id, locale) DO NOTHING;

        INSERT INTO product_prices (product_id, currency, list_amount, offer_amount, updated_at)
        VALUES (p_id, 'USD', (10.0 + i)::numeric(18,4), NULL, now())
        ON CONFLICT (product_id, currency) DO NOTHING;

        INSERT INTO product_search_documents (product_id, locale, name, description, sku, brand, brand_id, category, category_id, list_amount, currency, rating_average, rating_count)
        VALUES (p_id, 'en', name, 'Catalog browse fixture', 'CAT-' || suffix, NULL, NULL, NULL, NULL, (10.0 + i)::numeric(18,4), 'USD', 0, 0)
        ON CONFLICT (product_id, locale) DO NOTHING;
    END LOOP;
END $$;

-- ---- S8: webhook endpoint pointing at the host receiver (port 9099) ---
INSERT INTO webhook_endpoints (id, name, url, secret, is_active, suspended_until_utc, secret_rotated_at_utc, event_types, created_at, updated_at, is_deleted)
VALUES (
    '30000000-0000-0000-0000-000000000001',
    'S8 Load Receiver',
    'http://host.docker.internal:9099/wh',
    'load-secret-2026',
    true,
    NULL,
    NULL,
    '["order.placed","order.paid"]'::jsonb,
    now(), now(), false
)
ON CONFLICT (id) DO NOTHING;
