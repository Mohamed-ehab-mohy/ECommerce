#!/usr/bin/env bash
# T-TST-001 runner: baseline load smoke on the checkout path.
# Runs the k6 scenario in Docker so it works on any host (CI included).
#
# Usage:
#   On Linux/CI:        BASE_URL=http://localhost:8080 bash perf/k6/run-checkout-path.sh
#   On Docker Desktop:  BASE_URL=http://host.docker.internal:8080 bash perf/k6/run-checkout-path.sh
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
VUS="${VUS:-5}"
DURATION="${DURATION:-30s}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

exec docker run --rm -i \
  -e BASE_URL="$BASE_URL" \
  -e VUS="$VUS" \
  -e DURATION="$DURATION" \
  -v "$SCRIPT_DIR:/scripts:ro" \
  grafana/k6:latest run /scripts/checkout-path.js
