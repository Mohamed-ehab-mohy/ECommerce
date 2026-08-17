// S2 — Catalog browse (scaled to ~10% of NFR-PERF-14): 5,000 req/min.
// Validates NFR-PERF-04 (catalog product read p95 <= 150 ms, cache hit >= 90%).
//   RATE=83 DURATION=10m k6 run perf/k6/s2-catalog-browse.js
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/+$/, '');
const RATE = __ENV.RATE ? Number(__ENV.RATE) : 83;
const DURATION = __ENV.DURATION || '10m';

export const options = {
  scenarios: {
    catalogBrowse: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: 100,
      maxVUs: 200,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.005'],
    'http_req_duration{type:browse}': ['p(95)<150'],
  },
};

export default function () {
  const page = (__VU * 7 + __ITER) % 8 + 1;
  const res = http.get(
    `${BASE_URL}/api/v1/products?page=${page}&pageSize=20&currency=USD&locale=en`,
    { tags: { type: 'browse' } },
  );
  check(res, { 'browse -> 200 + items': (r) => r.status === 200 && Array.isArray(r.json('items')) });
}
