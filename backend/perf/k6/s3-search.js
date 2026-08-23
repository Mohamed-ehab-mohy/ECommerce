// S3 — Search (scaled to ~10% of NFR-PERF-15): 1,000 req/min, p95 <= 300 ms.
// Validates NFR-PERF-05 (product search p95 <= 300 ms).
//   RATE=17 DURATION=5m k6 run perf/k6/s3-search.js
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/+$/, '');
const RATE = __ENV.RATE ? Number(__ENV.RATE) : 17;
const DURATION = __ENV.DURATION || '5m';
const QUERIES = ['wireless headphones', 'mechanical keyboard', 'gaming mouse', '4k monitor', 'webcam', 'usb hub'];

export const options = {
  scenarios: {
    search: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: 50,
      maxVUs: 100,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.005'],
    'http_req_duration{type:search}': ['p(95)<300'],
  },
};

export default function () {
  const q = QUERIES[(__VU + __ITER) % QUERIES.length];
  const res = http.get(
    `${BASE_URL}/api/v1/products?q=${encodeURIComponent(q)}&page=1&pageSize=20&currency=USD&locale=en`,
    { tags: { type: 'search' } },
  );
  check(res, {
    'search -> 200': (r) => r.status === 200,
    'search -> has results': (r) => r.status === 200 && Array.isArray(r.json('items')),
  });
}
