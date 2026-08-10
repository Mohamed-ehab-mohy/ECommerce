#!/usr/bin/env bash
# v1.0 staging smoke suite.
# Covers the critical journeys: register -> browse -> cart -> checkout -> order,
# plus the v1 health/versioning contract and the cancellation/refund path.
#
# Usage: BASE_URL=http://localhost:8080 bash scripts/staging-smoke.sh
set -uo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
PASSWORD="${SMOKE_PASSWORD:-Smoke#$(date +%s)2026!}"
EMAIL="smoke.$(date +%s)@example.com"
SKU="SMOKE$(date +%s | tail -c 6)"
SLUG="smoke-product-$(date +%s | tail -c 6)"
IDEMPOTENCY_KEY="smoke-$(date +%s)"
WORKDIR="$(mktemp -d)"

FAILURES=0
PASS_COUNT=0

LAST_CODE=""
LAST_BODY_FILE="$WORKDIR/last-body.json"

report() {
    local label="$1"
    local ok="$2"
    if [[ "$ok" -eq 0 ]]; then
        echo "  PASS  $label"
        PASS_COUNT=$((PASS_COUNT + 1))
    else
        echo "  FAIL  $label (HTTP ${LAST_CODE:-n/a})"
        if [[ -s "$LAST_BODY_FILE" ]]; then
            echo "  ----> $(cat "$LAST_BODY_FILE" | head -c 800)"
        fi
        FAILURES=$((FAILURES + 1))
    fi
}

# curl -s writes body to $LAST_BODY_FILE and code to $LAST_CODE
req() {
    curl -s -o "$LAST_BODY_FILE" -w '%{http_code}' --max-time 30 "$@"
}

echo "== v1 health + versioning contract =="

LAST_CODE=$(req "$BASE_URL/api/v1/health/live")
version=$(curl -fsSI --max-time 30 "$BASE_URL/api/v1/health/live" | tr -d '\r' | awk -F': ' 'tolower($1)=="x-api-version"{print $2}')
[[ "$LAST_CODE" == "200" && "$(jq -r .version "$LAST_BODY_FILE")" == "1.0" && "$version" == "1.0" ]]
report "GET /api/v1/health/live returns 200 + X-API-Version: 1.0" $?

LAST_CODE=$(req "$BASE_URL/api/v1/health/ready")
status=$(jq -r .status "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "200" && "$status" == "Healthy" ]]
report "GET /api/v1/health/ready returns Healthy (Postgres check)" $?

LAST_CODE=$(req "$BASE_URL/health/live")
deprecation=$(curl -fsSI --max-time 30 "$BASE_URL/health/live" | tr -d '\r' | awk -F': ' 'tolower($1)=="deprecation"{print $2}')
[[ "$LAST_CODE" == "200" && "$deprecation" == "true" ]]
report "Legacy GET /health/live flagged Deprecation: true" $?

LAST_CODE=$(req "$BASE_URL/api/v2/health/live")
[[ "$LAST_CODE" == "404" ]]
report "GET /api/v2/health/live returns 404" $?

echo "== critical journey: register -> browse -> cart -> checkout -> order =="

LAST_CODE=$(req -H "Content-Type: application/json" \
    -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"displayName\":\"Smoke Tester\",\"locale\":\"en\",\"currency\":\"USD\"}" \
    "$BASE_URL/api/v1/auth/register")
userId=$(jq -r .userId "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "201" && "$userId" != "null" && "$userId" != "" ]]
report "POST /api/v1/auth/register" $?

LAST_CODE=$(req -H "Content-Type: application/json" \
    -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" \
    "$BASE_URL/api/v1/auth/login")
token=$(jq -r .accessToken "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "200" && "$token" != "null" && "$token" != "" ]]
report "POST /api/v1/auth/login" $?

auth_header=(-H "Authorization: Bearer $token" -H "Content-Type: application/json")

LAST_CODE=$(req "${auth_header[@]}" \
    -d '{"code":"SMOKE-WH","name":"Smoke Warehouse","address":"1 Smoke St, Test City","timezone":"UTC"}' \
    "$BASE_URL/api/v1/warehouses")
warehouseId=$(jq -r .id "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "201" && "$warehouseId" != "null" && "$warehouseId" != "" ]]
report "POST /api/v1/warehouses" $?

LAST_CODE=$(req "${auth_header[@]}" \
    -d "{\"sku\":\"$SKU\",\"warehouseId\":\"$warehouseId\",\"type\":\"Receipt\",\"quantity\":100,\"reason\":\"SMOKE-SEED\",\"reference\":\"smoke-stock\"}" \
    "$BASE_URL/api/v1/stock/movements")
[[ "$LAST_CODE" == "204" ]]
report "POST /api/v1/stock/movements (receipt 100 units)" $?

LAST_CODE=$(req "${auth_header[@]}" \
    -d "{\"sku\":\"$SKU\",\"slug\":\"$SLUG\",\"name\":\"Smoke Product\",\"description\":\"Smoke seed product\",\"currency\":\"USD\",\"listAmount\":49.99,\"offerAmount\":39.99,\"categoryId\":null,\"brandId\":null,\"isFeatured\":true,\"status\":\"Active\",\"locale\":\"en\"}" \
    "$BASE_URL/api/v1/products")
productId=$(jq -r .id "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "201" && "$productId" != "null" && "$productId" != "" ]]
report "POST /api/v1/products (active)" $?

LAST_CODE=$(req -H "Content-Type: application/json" \
    "$BASE_URL/api/v1/products?page=1&pageSize=50&currency=USD")
found=$(jq --arg sku "$SKU" '[.items[] | select(.sku == $sku)] | length' "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "200" && "$found" -ge 1 ]]
report "GET /api/v1/products lists the seeded product" $?

LAST_CODE=$(req "${auth_header[@]}" \
    -d "{\"productId\":\"$productId\",\"quantity\":2}" \
    "$BASE_URL/api/v1/carts/me/items?currency=USD")
itemCount=$(jq -r '.items | length' "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "200" && "$itemCount" -ge 1 ]]
report "POST /api/v1/carts/me/items adds product" $?

LAST_CODE=$(req "${auth_header[@]}" "$BASE_URL/api/v1/carts/me?currency=USD")
cartId=$(jq -r .id "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "200" && "$cartId" != "null" && "$cartId" != "" ]]
report "GET /api/v1/carts/me resolves cart id" $?

LAST_CODE=$(req "${auth_header[@]}" \
    -d "{\"cartId\":\"$cartId\",\"customerEmail\":\"$EMAIL\",\"currency\":\"USD\",\"shippingAddress\":{\"fullName\":\"Smoke Tester\",\"phone\":null,\"street\":\"1 Smoke St\",\"city\":\"Test City\",\"region\":null,\"country\":\"US\",\"postalCode\":\"12345\"},\"billingAddress\":null,\"shippingMethodId\":\"standard\",\"paymentMethod\":{\"providerKey\":\"mock\",\"methodType\":\"card\"}}" \
    "$BASE_URL/api/v1/checkouts")
checkoutId=$(jq -r .checkoutId "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "201" && "$checkoutId" != "null" && "$checkoutId" != "" ]]
report "POST /api/v1/checkouts initiates checkout" $?

LAST_CODE=$(req "${auth_header[@]}" \
    -H "Idempotency-Key: $IDEMPOTENCY_KEY" \
    -X POST "$BASE_URL/api/v1/checkouts/$checkoutId/place")
orderNumber=$(jq -r .orderNumber "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "200" && "$orderNumber" =~ ^E-[0-9]{8}-[0-9]{6}$ ]]
report "POST /api/v1/checkouts/{id}/place returns order $orderNumber" $?

LAST_CODE=$(req "${auth_header[@]}" "$BASE_URL/api/v1/orders")
listed=$(jq --arg on "$orderNumber" '[.items[] | select(.orderNumber == $on)] | length' "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "200" && "$listed" -ge 1 ]]
report "GET /api/v1/orders lists the placed order" $?

LAST_CODE=$(req "${auth_header[@]}" \
    -d '{"reason":"Smoke cleanup"}' \
    -X POST "$BASE_URL/api/v1/orders/$orderNumber/cancel")
cancelStatus=$(jq -r .status "$LAST_BODY_FILE")
[[ "$LAST_CODE" == "200" && "$cancelStatus" == "Cancelled" ]]
report "POST /api/v1/orders/{number}/cancel (restock + refund stub)" $?

rm -rf "$WORKDIR"

echo
echo "== results: $PASS_COUNT passed, $FAILURES failed =="
[[ "$FAILURES" -eq 0 ]]
