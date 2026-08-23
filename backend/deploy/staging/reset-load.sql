-- T-TST-003 reset script: returns load fixtures to a clean state between runs.
-- Zeroes allocations and removes load-suite orders. stock_movements is
-- append-only (triggers reject DELETE/UPDATE), so the ledger retains the
-- historical movements while the runtime counters are reset directly.

DELETE FROM webhook_deliveries WHERE endpoint_id = '30000000-0000-0000-0000-000000000001';

DELETE FROM orders o
USING order_items oi
WHERE oi.order_id = o.id
  AND oi.product_id IN (
    '10000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000003'
  );

UPDATE stock_items
SET allocated = 0, version = version + 1, updated_at = now()
WHERE id IN (
    '10000000-0000-0000-0000-000000000101',
    '10000000-0000-0000-0000-000000000201',
    '10000000-0000-0000-0000-000000000301'
);
