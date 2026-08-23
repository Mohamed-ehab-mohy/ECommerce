// Shared checkout journey for the T-TST-003 load suite (S1/S4/S5/S8).
//
// Mirrors the staging smoke journey (scripts/staging-smoke.sh): register ->
// login -> browse -> cart -> checkout -> authorize -> place. Each iteration
// uses a unique user + idempotency key so concurrent runs are independent.
//
// Usage (from a scenario script):
//   import { runCheckoutJourney } from './lib/checkout.js';
//   const outcome = runCheckoutJourney({ productId, sku, prefix });
//   outcome.placed === true | false
import http from 'k6/http';
import { check } from 'k6';

export function runCheckoutJourney({ productId, sku, prefix, currency = 'USD' }) {
  const BASE_URL = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/+$/, '');
  const PASSWORD = __ENV.PASSWORD || 'Load#2026!k6';
  const tag = `${prefix}.${__VU}.${__ITER}`;
  const email = `${tag}@example.com`;
  const idempotencyKey = `${tag}-${Date.now()}`;

  const reg = call('POST', BASE_URL, '/api/v1/auth/register', {
    type: 'register',
    body: { email, password: PASSWORD, displayName: 'Load Tester', locale: 'en', currency },
  });
  if (!check(reg, { 'register -> 201': (r) => r.status === 201 })) {
    return { placed: false, stage: 'register' };
  }

  const login = call('POST', BASE_URL, '/api/v1/auth/login', {
    type: 'login',
    body: { email, password: PASSWORD },
  });
  const token = login.json('accessToken');
  if (!check(login, { 'login -> 200 + token': (r) => r.status === 200 && !!r.json('accessToken') }) || !token) {
    return { placed: false, stage: 'login' };
  }

  const products = call('GET', BASE_URL, `/api/v1/products?page=1&pageSize=50&currency=${currency}`, { type: 'products' });
  if (!check(products, {
    'products -> 200 + seeded sku': (r) => r.status === 200 && (r.json('items') || []).some((item) => item.sku === sku),
  })) {
    return { placed: false, stage: 'products' };
  }

  const add = call('POST', BASE_URL, `/api/v1/carts/me/items?currency=${currency}`, {
    type: 'cart-add',
    token,
    body: { productId, quantity: 1 },
  });
  if (!check(add, { 'cart add -> 200': (r) => r.status === 200 })) {
    return { placed: false, stage: 'cart-add' };
  }

  const cart = call('GET', BASE_URL, `/api/v1/carts/me?currency=${currency}`, { type: 'cart-get', token });
  const cartId = cart.json('id');
  if (!check(cart, { 'cart get -> 200 + id': (r) => r.status === 200 && !!r.json('id') }) || !cartId) {
    return { placed: false, stage: 'cart-get' };
  }

  const checkout = call('POST', BASE_URL, '/api/v1/checkouts', {
    type: 'checkout',
    token,
    body: {
      cartId,
      customerEmail: email,
      currency,
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
  if (!check(checkout, {
    'checkout -> 201 + paymentId': (r) => r.status === 201 && !!r.json('checkoutId') && !!r.json('payment.paymentId'),
  }) || !checkoutId || !paymentId) {
    return { placed: false, stage: 'checkout' };
  }

  const authorize = call('POST', BASE_URL, `/api/v1/payments/${paymentId}/authorize`, { type: 'authorize' });
  if (!check(authorize, { 'authorize -> 200 + status 1': (r) => r.status === 200 && r.json('status') === 1 })) {
    return { placed: false, stage: 'authorize' };
  }

  const place = call('POST', BASE_URL, `/api/v1/checkouts/${checkoutId}/place`, {
    type: 'place',
    token,
    idempotencyKey,
  });
  const placed = place.status === 200 && !!place.json('orderNumber');
  check(place, { 'place -> 200 + orderNumber': (r) => placed });

  return { placed, stage: placed ? 'placed' : 'place', status: place.status };
}

function call(method, baseUrl, path, { token, body, idempotencyKey, type } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  if (idempotencyKey) {
    headers['Idempotency-Key'] = idempotencyKey;
  }
  return http.request(method, `${baseUrl}${path}`, body ? JSON.stringify(body) : null, {
    headers,
    tags: { type: type || 'other' },
  });
}
