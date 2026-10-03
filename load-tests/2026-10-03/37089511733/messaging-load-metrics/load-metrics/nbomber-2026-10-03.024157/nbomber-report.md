> test info



test suite: `Encina`

test name: `messaging`

session id: `2026-10-03_02-41-57_f3087fbf`

> scenario stats



scenario: `send_flow`

  - duration: `00:01:00`

load simulations:

  - `inject`, rate: `240000`, interval: `00:00:01`, during: `00:01:00`

|scenario and steps|ok stats|
|---|---|
|scenario name|`send_flow`|
|requests|total = `14400000`, ok = `14400000`, fail = `0`|
|RPS (req/sec)|total = `240000`/s, ok = `240000`/s, fail = `0`/s|
|latency (ms)|min = `0`, mean = `0.02`, max = `382.78`, StdDev = `0.39`|
|latency percentile (ms)|p50 = `0`, p75 = `0`, p95 = `0`, p99 = `0.01`|




