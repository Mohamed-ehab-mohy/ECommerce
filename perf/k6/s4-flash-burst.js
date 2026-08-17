// S4 — Flash-sale burst (scaled to ~10% of NFR-PERF-11): 2x order load for 60 s, 3 cycles.
// 200 orders/min bursts against the S4 SKU.
//   VUS=8 CYCLES=3 k6 run perf/k6/s4-flash-burst.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { runCheckoutJourney } from './lib/checkout.js';

const PRODUCT_ID = __ENV.PRODUCT_ID || '10000000-0000-0000-0000-000000000002';
const SKU = __ENV.SKU || 'LOAD-04';
const VUS = __ENV.VUS ? Number(__ENV.VUS) : 8;
const CYCLES = __ENV.CYCLES ? Number(__ENV.CYCLES) : 3;
const BURST = '60s';
const RUN = __ENV.RUN || String(Date.now());

export const options = {
  scenarios: {
    flashBurst: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: buildStages(CYCLES, VUS),
      gracefulStop: '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.005'],
    'http_req_duration{type:place}': ['p(95)<1500'],
  },
};

function buildStages(cycles, vus) {
  const stages = [];
  for (let i = 0; i < cycles; i++) {
    stages.push({ target: vus, duration: '5s' });
    stages.push({ target: vus, duration: BURST });
    stages.push({ target: 0, duration: '5s' });
  }
  return stages;
}

export default function () {
  const outcome = runCheckoutJourney({
    productId: PRODUCT_ID,
    sku: SKU,
    prefix: `s4.${RUN}`,
  });
  check(outcome, { 'order placed': (o) => o.placed });
  sleep(0.5);
}
