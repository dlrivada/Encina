> test info



test suite: `Encina`

test name: `locking`

session id: `2026-10-10_02-48-29_4132ec91`

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
|latency (ms)|min = `0`, mean = `0.01`, max = `491.73`, StdDev = `0.38`|
|latency percentile (ms)|p50 = `0`, p75 = `0`, p95 = `0`, p99 = `0.01`|




