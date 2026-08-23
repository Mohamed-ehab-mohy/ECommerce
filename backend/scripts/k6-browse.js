import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },
    { duration: '1m', target: 50 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';

export default function () {
  // Health check
  const healthRes = http.get(`${BASE_URL}/api/v1/health/live`);
  check(healthRes, { 'health 200': (r) => r.status === 200 });

  // Browse products
  const catalogRes = http.get(`${BASE_URL}/api/v1/products`);
  check(catalogRes, { 'catalog 200': (r) => r.status === 200 });

  sleep(1);
}
