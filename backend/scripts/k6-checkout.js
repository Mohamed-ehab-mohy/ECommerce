import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 10 },
    { duration: '1m', target: 30 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.02'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';

export default function () {
  // Register
  const email = `loadtest_${Date.now()}_${__VU}@test.com`;
  const registerRes = http.post(`${BASE_URL}/api/v1/auth/register`, JSON.stringify({
    email,
    password: 'Test1234!',
    firstName: 'Load',
    lastName: 'Test',
  }), { headers: { 'Content-Type': 'application/json' } });
  check(registerRes, { 'register 201 or 409': (r) => r.status === 201 || r.status === 409 });

  sleep(0.5);

  // Login
  const loginRes = http.post(`${BASE_URL}/api/v1/auth/login`, JSON.stringify({
    email,
    password: 'Test1234!',
    deviceId: `k6-${__VU}`,
  }), { headers: { 'Content-Type': 'application/json' } });
  check(loginRes, { 'login 200': (r) => r.status === 200 });

  const token = loginRes.json('accessToken') || '';
  const authHeaders = { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } };

  sleep(0.3);

  // Browse products
  const products = http.get(`${BASE_URL}/api/v1/products`, authHeaders);
  check(products, { 'products 200': (r) => r.status === 200 });

  sleep(0.3);

  // Add to cart (if product available)
  const cartRes = http.post(`${BASE_URL}/api/v1/cart/items`, JSON.stringify({
    productId: '00000000-0000-0000-0000-000000000001',
    quantity: 1,
  }), authHeaders);
  check(cartRes, { 'add to cart ok': (r) => r.status === 200 || r.status === 201 || r.status === 400 });

  sleep(0.3);

  // View profile
  const profile = http.get(`${BASE_URL}/api/v1/me`, authHeaders);
  check(profile, { 'profile 200': (r) => r.status === 200 });
}
