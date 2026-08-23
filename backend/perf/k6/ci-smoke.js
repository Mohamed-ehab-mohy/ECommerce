// CI Smoke — Quick catalog browse to gate PRs for performance regressions.
// Validates p95 < 500 ms, error rate < 1 % on GET /api/v1/products.
//
//   k6 run perf/k6/ci-smoke.js
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/+$/, '');

export const options = {
  scenarios: {
    catalogBrowse: {
      executor: 'constant-vus',
      vus: 5,
      duration: '60s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{type:browse}': ['p(95)<500'],
  },
};

export default function () {
  const page = (__VU * 7 + __ITER) % 8 + 1;
  const res = http.get(
    `${BASE_URL}/api/v1/products?page=${page}&pageSize=20&currency=USD&locale=en`,
    { tags: { type: 'browse' } },
  );
  check(res, {
    'browse -> 200 + items': (r) => r.status === 200 && Array.isArray(r.json('items')),
  });
}
