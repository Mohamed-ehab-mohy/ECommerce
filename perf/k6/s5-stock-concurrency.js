// S5 — Concurrency on last units (NFR-CNS-01): N buyers race on 10 units.
// Domain behavior: exactly 10 orders are Placed (allocating the 10 units);
// the rest are accepted as Backordered, never overselling stock.
// The k6-side assertion is only that enough place calls complete; the
// authoritative checks run against Postgres after the test (see orchestrator):
//   Placed orders == 10, Backordered == VUS - 10, allocated == 10,
//   available == 0, oversold (allocated > on_hand) == 0.
//
//   VUS=100 k6 run perf/k6/s5-stock-concurrency.js
import { Counter } from 'k6/metrics';
import { check } from 'k6';
import { runCheckoutJourney } from './lib/checkout.js';

const PRODUCT_ID = __ENV.PRODUCT_ID || '10000000-0000-0000-0000-000000000003';
const SKU = __ENV.SKU || 'RACE-05';
const VUS = __ENV.VUS ? Number(__ENV.VUS) : 100;
const RUN = __ENV.RUN || String(Date.now());

const placeComplete = new Counter('place_complete');

export const options = {
  scenarios: {
    stockRace: {
      executor: 'shared-iterations',
      vus: VUS,
      iterations: VUS,
    },
  },
  thresholds: {
    place_complete: ['count>=10'],
  },
};

export default function () {
  const outcome = runCheckoutJourney({
    productId: PRODUCT_ID,
    sku: SKU,
    prefix: `s5.${RUN}`,
  });

  if (outcome.stage === 'place' || outcome.stage === 'placed') {
    placeComplete.add(1);
  }

  check(outcome, { 'journey reached place': (o) => o.stage === 'place' || o.stage === 'placed' });
}
