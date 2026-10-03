> test info



test suite: `Encina`

test name: `send-burst`

session id: `2026-10-03_02-32-21_a214b21f`

> scenario stats



scenario: `send_flow`

  - duration: `00:00:30`

load simulations:

  - `inject`, rate: `240000`, interval: `00:00:01`, during: `00:00:30`

|scenario and steps|ok stats|
|---|---|
|scenario name|`send_flow`|
|requests|total = `7200000`, ok = `7200000`, fail = `0`|
|RPS (req/sec)|total = `240000`/s, ok = `240000`/s, fail = `0`/s|
|latency (ms)|min = `0`, mean = `0.02`, max = `409.51`, StdDev = `0.97`|
|latency percentile (ms)|p50 = `0`, p75 = `0`, p95 = `0`, p99 = `0.01`|




