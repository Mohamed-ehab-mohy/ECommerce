// T-TST-001 baseline load smoke on the checkout path.
//
// Full journey per iteration: register -> login -> browse -> cart -> checkout ->
// authorize -> place. Mirrors the staging smoke journey (scripts/staging-smoke.sh)
// so the load profile matches a real checkout-to-order flow.
//
// Thresholds (per sprint 07 acceptance):
//   - error rate < 0.5%  (http_req_failed)
//   - p95 < 800 ms on the checkout-path operations (initiate, authorize, place)
//
// Usage:
//   BASE_URL=http://localhost:8080 VUS=5 DURATION=30s k6 run perf/k6/checkout-path.js
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/+$/, '');
const PRODUCT_ID = __ENV.PRODUCT_ID || '00000000-0000-0000-0000-000000000004';
const SKU = __ENV.SKU || 'SMOKE-PROD';
const VUS = __ENV.VUS ? Number(__ENV.VUS) : 5;
const DURATION = __ENV.DURATION || '30s';
const PASSWORD = 'Load#2026!k6';
const RUN = __ENV.RUN || String(Date.now());

export const options = {
  scenarios: {
    checkoutPath: {
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

function call(method, path, { token, body, idempotencyKey, type } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  if (idempotencyKey) {
    headers['Idempotency-Key'] = idempotencyKey;
  }
  return http.request(method, `${BASE_URL}${path}`, body ? JSON.stringify(body) : null, {
    headers,
    tags: { type: type || 'other' },
  });
}

export default function () {
  const tag = `load.${RUN}.${__VU}.${__ITER}`;
  const email = `load-${RUN}-${__VU}-${__ITER}@example.com`;
  const idempotencyKey = `load-${RUN}-${__VU}-${__ITER}`;

  const reg = call('POST', '/api/v1/auth/register', {
    type: 'register',
    body: { email, password: PASSWORD, displayName: 'Load Tester', locale: 'en', currency: 'USD' },
  });
  check(reg, { 'register -> 201': (r) => r.status === 201 });

  const login = call('POST', '/api/v1/auth/login', {
    type: 'login',
    body: { email, password: PASSWORD },
  });
  const token = login.json('accessToken');
  if (!check(login, { 'login -> 200 + token': (r) => r.status === 200 && !!r.json('accessToken') }) || !token) {
    return;
  }

  const products = call('GET', '/api/v1/products?page=1&pageSize=50&currency=USD', { type: 'products' });
  check(products, {
    'products -> 200 + seeded sku': (r) =>
      r.status === 200 &&
      (r.json('items') || []).some((item) => item.sku === SKU),
  });

  const add = call('POST', '/api/v1/carts/me/items?currency=USD', {
    type: 'cart-add',
    token,
    body: { productId: PRODUCT_ID, quantity: 1 },
  });
  check(add, { 'cart add -> 200': (r) => r.status === 200 });

  const cart = call('GET', '/api/v1/carts/me?currency=USD', { type: 'cart-get', token });
  const cartId = cart.json('id');
  if (!check(cart, { 'cart get -> 200 + id': (r) => r.status === 200 && !!r.json('id') }) || !cartId) {
    return;
  }

  const checkout = call('POST', '/api/v1/checkouts', {
    type: 'checkout',
    token,
    body: {
      cartId,
      customerEmail: email,
      currency: 'USD',
      shippingAddress: {
        fullName: 'Load Tester',
        phone: null,
        street: '1 Load St',
        city: 'Load City',
        region: null,
        country: 'US',
        postalCode: '12345',
      },
      billingAddress: null,
      shippingMethodId: 'standard',
      paymentMethod: { providerKey: 'mock', methodType: 'card' },
    },
  });
  const checkoutId = checkout.json('checkoutId');
  const paymentId = checkout.json('payment.paymentId');
  const checkoutOk = check(checkout, {
    'checkout -> 201 + paymentId': (r) => r.status === 201 && !!r.json('checkoutId') && !!r.json('payment.paymentId'),
  });
  if (!checkoutOk || !checkoutId || !paymentId) {
    return;
  }

  const authorize = call('POST', `/api/v1/payments/${paymentId}/authorize`, { type: 'authorize' });
  const authorized = check(authorize, {
    'authorize -> 200 + status 1': (r) => r.status === 200 && r.json('status') === 1,
  });
  if (!authorized) {
    return;
  }

  const place = call('POST', `/api/v1/checkouts/${checkoutId}/place`, {
    type: 'place',
    token,
    idempotencyKey,
  });
  check(place, { 'place -> 200 + orderNumber': (r) => r.status === 200 && !!r.json('orderNumber') });

  sleep(1);
}
