#!/usr/bin/env bash
# v1.0 staging smoke suite.
# Covers the critical journeys: register -> browse -> cart -> checkout -> order,
# plus the v1 health/versioning contract and the cancellation/refund path.
#
# Usage: BASE_URL=http://localhost:8080 scripts/staging-smoke.sh
set -uo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
PASSWORD="${SMOKE_PASSWORD:-Smoke#$(date +%s)2026!}"
EMAIL="smoke.$(date +%s)@example.com"
SKU="SMOKE$(date +%s | tail -c 6)"
SLUG="smoke-product-$(date +%s | tail -c 6)"
IDEMPOTENCY_KEY="smoke-$(date +%s)"

FAILURES=0
PASS_COUNT=0

report() {
    local label="$1"
    local ok="$2"
    if [[ "$ok" -eq 0 ]]; then
        echo "  PASS  $label"
        PASS_COUNT=$((PASS_COUNT + 1))
    else
        echo "  FAIL  $label"
        FAILURES=$((FAILURES + 1))
    fi
}

curl_json() {
    curl -fsS --max-time 30 "$@"
}

curl_headers() {
    curl -fsSI --max-time 30 "$@"
}

echo "== v1 health + versioning contract =="

code=$(curl -s -o /tmp/smoke-live.json -w '%{http_code}' "$BASE_URL/api/v1/health/live")
version=$(curl -fsSI --max-time 30 "$BASE_URL/api/v1/health/live" | tr -d '\r' | awk -F': ' 'tolower($1)=="x-api-version"{print $2}')
[[ "$code" == "200" && "$(jq -r .version /tmp/smoke-live.json)" == "1.0" && "$version" == "1.0" ]]
report "GET /api/v1/health/live returns 200 + X-API-Version: 1.0" $?

code=$(curl -s -o /tmp/smoke-ready.json -w '%{http_code}' "$BASE_URL/api/v1/health/ready")
status=$(jq -r .status /tmp/smoke-ready.json)
[[ "$code" == "200" && "$status" == "Healthy" ]]
report "GET /api/v1/health/ready returns Healthy (Postgres check)" $?

code=$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/health/live")
deprecation=$(curl -fsSI --max-time 30 "$BASE_URL/health/live" | tr -d '\r' | awk -F': ' 'tolower($1)=="deprecation"{print $2}')
[[ "$code" == "200" && "$deprecation" == "true" ]]
report "Legacy GET /health/live flagged Deprecation: true" $?

code=$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/api/v2/health/live")
[[ "$code" == "404" ]]
report "GET /api/v2/health/live returns 404" $?

echo "== critical journey: register -> browse -> cart -> checkout -> order =="

reg_code=$(curl -s -o /tmp/smoke-register.json -w '%{http_code}' \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"displayName\":\"Smoke Tester\",\"locale\":\"en\",\"currency\":\"USD\"}" \
    "$BASE_URL/api/v1/auth/register")
userId=$(jq -r .userId /tmp/smoke-register.json)
[[ "$reg_code" == "201" && "$userId" != "null" && "$userId" != "" ]]
report "POST /api/v1/auth/register" $?

login_code=$(curl -s -o /tmp/smoke-login.json -w '%{http_code}' \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" \
    "$BASE_URL/api/v1/auth/login")
token=$(jq -r .accessToken /tmp/smoke-login.json)
[[ "$login_code" == "200" && "$token" != "null" && "$token" != "" ]]
report "POST /api/v1/auth/login" $?

auth_header=(-H "Authorization: Bearer $token" -H "Content-Type: application/json")

wh_code=$(curl -s -o /tmp/smoke-warehouse.json -w '%{http_code}' \
    "${auth_header[@]}" \
    -d '{"code":"SMOKE-WH","name":"Smoke Warehouse","address":"1 Smoke St, Test City","timezone":"UTC"}' \
    "$BASE_URL/api/v1/warehouses")
warehouseId=$(jq -r .id /tmp/smoke-warehouse.json)
[[ "$wh_code" == "201" && "$warehouseId" != "null" && "$warehouseId" != "" ]]
report "POST /api/v1/warehouses" $?

st_code=$(curl -s -o /dev/null -w '%{http_code}' \
    "${auth_header[@]}" \
    -d "{\"sku\":\"$SKU\",\"warehouseId\":\"$warehouseId\",\"type\":\"Receipt\",\"quantity\":100,\"reason\":\"SMOKE-SEED\",\"reference\":\"smoke-stock\"}" \
    "$BASE_URL/api/v1/stock/movements")
[[ "$st_code" == "204" ]]
report "POST /api/v1/stock/movements (receipt 100 units)" $?

prod_code=$(curl -s -o /tmp/smoke-product.json -w '%{http_code}' \
    "${auth_header[@]}" \
    -d "{\"sku\":\"$SKU\",\"slug\":\"$SLUG\",\"name\":\"Smoke Product\",\"description\":\"Smoke seed product\",\"currency\":\"USD\",\"listAmount\":49.99,\"offerAmount\":39.99,\"categoryId\":null,\"brandId\":null,\"isFeatured\":true,\"status\":\"Active\",\"locale\":\"en\"}" \
    "$BASE_URL/api/v1/products")
productId=$(jq -r .id /tmp/smoke-product.json)
[[ "$prod_code" == "201" && "$productId" != "null" && "$productId" != "" ]]
report "POST /api/v1/products (active)" $?

browse_code=$(curl -s -o /tmp/smoke-browse.json -w '%{http_code}' \
    -H "Content-Type: application/json" \
    "$BASE_URL/api/v1/products?page=1&pageSize=50&currency=USD")
found=$(jq --arg sku "$SKU" '[.items[] | select(.sku == $sku)] | length' /tmp/smoke-browse.json)
[[ "$browse_code" == "200" && "$found" -ge 1 ]]
report "GET /api/v1/products lists the seeded product" $?

cart_code=$(curl -s -o /tmp/smoke-cart.json -w '%{http_code}' \
    "${auth_header[@]}" \
    -d "{\"productId\":\"$productId\",\"quantity\":2}" \
    "$BASE_URL/api/v1/carts/me/items?currency=USD")
itemCount=$(jq -r '.items | length' /tmp/smoke-cart.json)
[[ "$cart_code" == "200" && "$itemCount" -ge 1 ]]
report "POST /api/v1/carts/me/items adds product" $?

cartId=$(curl -s "${auth_header[@]}" "$BASE_URL/api/v1/carts/me?currency=USD" | jq -r .id)
[[ "$cartId" != "null" && "$cartId" != "" ]]
report "GET /api/v1/carts/me resolves cart id" $?

checkout_code=$(curl -s -o /tmp/smoke-checkout.json -w '%{http_code}' \
    "${auth_header[@]}" \
    -d "{\"cartId\":\"$cartId\",\"customerEmail\":\"$EMAIL\",\"currency\":\"USD\",\"shippingAddress\":{\"fullName\":\"Smoke Tester\",\"phone\":null,\"street\":\"1 Smoke St\",\"city\":\"Test City\",\"region\":null,\"country\":\"US\",\"postalCode\":\"12345\"},\"billingAddress\":null,\"shippingMethodId\":\"standard\",\"paymentMethod\":{\"providerKey\":\"mock\",\"methodType\":\"card\"}}" \
    "$BASE_URL/api/v1/checkouts")
checkoutId=$(jq -r .checkoutId /tmp/smoke-checkout.json)
[[ "$checkout_code" == "201" && "$checkoutId" != "null" && "$checkoutId" != "" ]]
report "POST /api/v1/checkouts initiates checkout" $?

order_code=$(curl -s -o /tmp/smoke-order.json -w '%{http_code}' \
    "${auth_header[@]}" \
    -H "Idempotency-Key: $IDEMPOTENCY_KEY" \
    -X POST "$BASE_URL/api/v1/checkouts/$checkoutId/place")
orderNumber=$(jq -r .orderNumber /tmp/smoke-order.json)
[[ "$order_code" == "200" && "$orderNumber" =~ ^E-[0-9]{8}-[0-9]{6}$ ]]
report "POST /api/v1/checkouts/{id}/place returns order $orderNumber" $?

orders_code=$(curl -s -o /tmp/smoke-orders.json -w '%{http_code}' \
    "${auth_header[@]}" \
    "$BASE_URL/api/v1/orders")
listed=$(jq --arg on "$orderNumber" '[.items[] | select(.orderNumber == $on)] | length' /tmp/smoke-orders.json)
[[ "$orders_code" == "200" && "$listed" -ge 1 ]]
report "GET /api/v1/orders lists the placed order" $?

cancel_code=$(curl -s -o /tmp/smoke-cancel.json -w '%{http_code}' \
    "${auth_header[@]}" \
    -d '{"reason":"Smoke cleanup"}' \
    -X POST "$BASE_URL/api/v1/orders/$orderNumber/cancel")
cancelStatus=$(jq -r .status /tmp/smoke-cancel.json)
[[ "$cancel_code" == "200" && "$cancelStatus" == "Cancelled" ]]
report "POST /api/v1/orders/{number}/cancel (restock + refund stub)" $?

echo
echo "== results: $PASS_COUNT passed, $FAILURES failed =="
[[ "$FAILURES" -eq 0 ]]
