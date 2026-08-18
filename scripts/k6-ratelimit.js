import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '10s', target: 5 },
    { duration: '2m', target: 50 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';

export default function () {
  // Rapid-fire read endpoints to test rate limiting
  for (let i = 0; i < 5; i++) {
    const res = http.get(`${BASE_URL}/api/v1/products`);
    check(res, { 'status is 200 or 429': (r) => r.status === 200 || r.status === 429 });
    sleep(0.05);
  }

  // Auth endpoint (more restricted)
  const authRes = http.post(`${BASE_URL}/api/v1/auth/login`, JSON.stringify({
    email: `ratelimit_${__VU}@test.com`,
    password: 'WrongPassword',
  }), { headers: { 'Content-Type': 'application/json' } });
  check(authRes, { 'auth is 401 or 429': (r) => r.status === 401 || r.status === 429 });

  sleep(1);
}
