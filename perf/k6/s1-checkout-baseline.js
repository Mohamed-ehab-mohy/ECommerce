// S1 — Checkout baseline (scaled to ~10% of NFR-PERF-11): 100 orders/min sustained.
// Validates NFR-PERF-01 (order placement p95 <= 1.5 s), NFR-PERF-02 (checkout p95 <= 1.2 s).
// Thresholds use the sprint-13 release gate (p95 < 800 ms on the checkout-path ops).
//
//   VUS=3 DURATION=10m k6 run perf/k6/s1-checkout-baseline.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { runCheckoutJourney } from './lib/checkout.js';

const PRODUCT_ID = __ENV.PRODUCT_ID || '10000000-0000-0000-0000-000000000001';
const SKU = __ENV.SKU || 'LOAD-01';
const VUS = __ENV.VUS ? Number(__ENV.VUS) : 3;
const DURATION = __ENV.DURATION || '10m';
const RUN = __ENV.RUN || String(Date.now());

export const options = {
  scenarios: {
    checkoutBaseline: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { target: VUS, duration: '5s' },
        { target: VUS, duration: DURATION },
        { target: 0, duration: '5s' },
      ],
      gracefulStop: '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.005'],
    'http_req_duration{type:checkout}': ['p(95)<800'],
    'http_req_duration{type:authorize}': ['p(95)<800'],
    'http_req_duration{type:place}': ['p(95)<800'],
  },
};

export default function () {
  const outcome = runCheckoutJourney({
    productId: PRODUCT_ID,
    sku: SKU,
    prefix: `s1.${RUN}`,
  });
  check(outcome, { 'order placed': (o) => o.placed });
  sleep(1);
}
