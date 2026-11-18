# Implementation Plan: Distributed rate limiter state store (`IRateLimiterStateStore`) over the cache providers

> **Issue**: [#704](https://github.com/dlrivada/Encina/issues/704)
> **Type**: Feature
> **Complexity**: High (7 phases, 6 pull requests of 9, 13, 9, 6, 12 and 4 files)
> **Estimated Scope**: about 900 lines of production code (about 150 lines of Lua and script glue), about 1800 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, resilience)

---

## Summary

`AdaptiveRateLimiter` (`src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs`) keeps its buckets in a `ConcurrentDictionary<string, RateLimitBucket>` per process (line 23), so with N instances the effective limit is N times the configured one. The issue proposed a `DistributedAdaptiveRateLimiter` over `ICacheProvider.GetOrSetAsync` and `SetAsync` with a read-increment-write of a `RateLimitBucket`.

The maintainer changed the design before this plan: SPIKE #2691 decision **R8, option 8b** (2026-10-24, ADR-055 section R8, recorded as a comment on #704): a new abstraction `IRateLimiterStateStore`, implemented in each cache package with its best atomic operation (Lua scripts in the Redis family, compare-and-swap in Memcached, local state in Memory and the Hybrid L1), `ICacheProvider` unchanged, a fall back to the local limiter with a log entry when the cache fails, counter keys that carry only type, tenant and client, and no personal data. The issue's own sketch is also wrong on its own terms: `GetOrSetAsync` followed by `SetAsync` loses updates between instances, which is exactly why R8 rejected option 8a.

Re-checked against `origin/main` (822c310c7), the code and the decisions leave five facts that shape this plan:

1. **The consumer does not exist yet.** `Encina.Resilience` and the partitioned rate-limiting core of #2414 (decisions D1 = 1a, D2 = 2a, D3 = 3a, 2026-10-23) are not in `src/`; `IRateLimiter` is still the `Encina.Polly` interface. This plan delivers the **state store**, its **fallback** and its **implementations** and proves them with a contract suite; wiring a limiter to the store belongs to the partitioned-core work of #2414 (the open issue #740, tenant-scoped rate limiting, applies its tenant partition) and is not planned here.
2. **The store must not decide the partition.** The bucket partition (request type, tenant, principal) is the open SPIKE #3448 (`needs-decision`) and #2414 D3 (type by tenant, fail closed). The store therefore takes an opaque, already composed partition: an optional tenant id and a scope string. Whatever #3448 decides, the store does not change.
3. **The adaptive three-state machine is not a store concern.** The issue's acceptance "Normal -> Throttled -> Recovering" conflicts with #2414 D2 (SRE-style probabilistic rejection in an opt-in decorator that counts transient errors). The store provides what both need, shared success and failure counters over a window; the state machine or the probabilistic rule stays in the decorator of #2414 D2.
4. **Only the Redis family needs scripts.** Valkey, Dragonfly, Garnet and KeyDB are built by calling `AddEncinaRedisCache` (`Encina.Caching.Valkey/ServiceCollectionExtensions.cs`), so one Redis implementation registers for five products; each product still needs its own integration run, because Lua support differs.
5. **Memcached has no package yet.** #277 (open) owns the provider; its store is a follow-up (an issue file is drafted).

**Affected packages:** `Encina` (abstraction, in-process store, fallback decorator, registration, EventIds), `Encina.Caching.Redis` (Lua store; the Valkey, Dragonfly, Garnet and KeyDB packages inherit it with no code change), `Encina.Caching.Memory` and `Encina.Caching.Hybrid` (registration of the in-process store). `Encina.Polly` is **not** changed (the old limiter is replaced wholesale by the #2414 and #2691 work).

**Provider category**: Caching (8). Memory (L1) and Hybrid (L1): in-process state, per instance by design (R8). Redis, Valkey, Dragonfly, Garnet, KeyDB: Lua, atomic per partition across instances. Memcached (#277): compare-and-swap, follow-up. Database providers are not involved.

**Sibling plans and decisions**: ADR-055 (R1, R8, "Negative" list: eight store implementations), #2414 (D1 to D3), #3448 (partition, open), #740 (tenant-scoped rate limiting, open: the consumer), #695 (the store-cache foundation: `StoreCacheKey` is reused for keys; its Design Choice 5 is Settled), #277 (Memcached provider plan on `main`), #728 and ADR-054 (telemetry conventions).

---

## Design Choices

<details>
<summary><strong>1. Where `IRateLimiterStateStore` lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The core `Encina` package, namespace `Encina.RateLimiting`** | Every cache package and the planned `Encina.Resilience` already reference `Encina`; no new project edge; the in-process store, the fallback decorator and the abstraction share one assembly, so the fallback needs no cache package | The core grows a small rate-limiting surface |
| **B) `Encina.Caching` (`Abstractions/`)** | Next to `ICacheProvider`; the cache packages reference it already | `Encina.Resilience` would reference `Encina.Caching`, which references `Encina.Messaging` and `Encina.DistributedLock` (`Encina.Caching.csproj`): a heavy chain for one interface, against R8's "depends only on the abstraction, never on a concrete cache" |
| **C) `Encina.Resilience` (planned)** | The consumer owns its port | The package does not exist; every cache package would reference a Polly-based package, inverting the dependency |

### Chosen Option: **A - the core package** (recommended, pending the maintainer)

### Rationale

- B wins if the maintainer wants every cache-related abstraction in one place and accepts the dependency chain; C wins once `Encina.Resilience` exists and the maintainer prefers the port next to its consumer, with the cache packages referencing it. A wins now because it is buildable today and adds no project reference. The precedent is `IDistributedLockProvider`, which lives in its own small package that the cache packages reference rather than in `Encina.Caching`.
- Moving the interface later is a namespace change inside the pre-1.0 window; the plan builds A.
- Class: Dilemma

</details>

<details>
<summary><strong>2. The contract: which atomic operations the store offers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Sliding-window acquire plus outcome counters: `TryAcquireAsync(partition, window, limit, permits)` (atomic: expire old entries, count, admit or deny, return count and retry-after), `RecordOutcomeAsync(partition, outcome, window)`, `GetOutcomeCountsAsync(partition, window)`, `ResetAsync(partition)`; all return `Either<EncinaError, T>`** | Matches the algorithm of the limiter that exists (a queue of request timestamps trimmed to the window, `AdaptiveRateLimiter.CleanupExpiredTimestamps`, line 181) and what both adaptive designs need (success and failure counts); exact across instances | A token bucket needs another method when #2414 adopts it (additive) |
| **B) A also with `TryAcquireTokenBucketAsync`** | Covers both algorithms the spike names | Builds and tests eight implementations of an algorithm nobody has chosen; the interface grows before the consumer exists |
| **C) Generic atomic primitives (`IncrementAsync`, compare-and-set)** | Reusable | This is option 8a of the spike, which the maintainer rejected: a sliding window needs more than an increment |

### Chosen Option: **A - sliding window plus outcome counters** (recommended, pending the maintainer)

### Rationale

- B wins when the maintainer already knows the core will ship a token bucket and wants the store complete at once; A wins because the interface is the cost of eight implementations and the token bucket is an additive method later. C is rejected by decision R8. Permits and limits are validated: a non-positive window, limit or permit count, or an empty scope, returns `Left` with `encina.ratelimit.partition_invalid` (`ErrorCategory.Validation`, ADR-062).
- Class: Dilemma

</details>

<details>
<summary><strong>3. The partition, the tenant and the key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RateLimiterPartition(string? TenantId, string Scope)` where `Scope` is opaque and composed by the core; the key is `StoreCacheKey.Build(prefix, tenantId, "rate-limit", <scope id>)` with the tenant segment `t:{encoded}` when a tenant is given and absent when it is not** | Independent of the open SPIKE #3448; a tenant and "no tenant" can never share a key, and the reserved tenant `default` is an ordinary tenant id distinct from "no tenant" (the injectivity of #695's builder); keys sit under the tenant namespace that the DSR purge of #2407 removes | The store cannot validate what the scope means |
| **B) A typed `(RequestType, TenantId, ClientKey)` record** | The store can index and report per field | Encodes the answer of #3448 into the abstraction; any other partition (a principal, an endpoint) needs an interface change in eight packages |

### Chosen Option: **A - an opaque scope with an injective tenant segment** (recommended, pending the maintainer)

### Rationale

- B wins if the maintainer closes #3448 with a fixed partition and wants per-field store metrics; A wins while the partition is open. The store never gates: a missing tenant is not an error here, because the fail-closed denial of a request without tenant context is the core's rule (#2414 D3, SPEC-002 DEC-006); the store receives `null` only when the core has opted out explicitly and logged it, and never maps `null` to the `default` tenant.
- Class: Dilemma

</details>

<details>
<summary><strong>4. What the key contains: raw scope or a hash of it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The scope is hashed (SHA-256, lowercase hex) before it enters the key: `{prefix}:[t:{tenant}:]x-store:rate-limit:{hash}`; the tenant stays readable and separately encoded** | A client identifier that is personal data (an IP address, an API key, a user id) never appears in a key, a log, a metric or a monitor of the cache server (R8: "no personal data"; GDPR Art. 5(1)(c) data minimisation); keys have a bounded length | Keys are not human readable; a per-client purge recomputes the hash; two different scopes of one tenant collide with probability about 2^-128 instead of never |
| **B) The scope is percent-encoded by `StoreCacheKey.Segment` and used raw** | Exactly injective; readable keys; a purge by pattern per client | A raw client identifier sits in plaintext in the cache and in its monitoring; compliance depends on the core never passing personal data |

### Chosen Option: **A - hash the scope** (recommended, pending the maintainer)

### Rationale

- B wins when operators need to inspect and purge one client's counter by hand and the core guarantees a non-personal scope; A wins by default because the store cannot know what the core puts in the scope, and a limiter keyed by IP address is the common case. The tenant segment is not hashed, so no two different tenants can ever collide and the purge-by-tenant of #2407 keeps working.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Fallback when the cache fails</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ResilientRateLimiterStateStore` decorator: on an infrastructure failure of the primary store (a `Left` with `encina.ratelimit.state_store_failed`, or an exception other than cancellation) it serves the call from an `InProcessRateLimiterStateStore`; a `Left` with `encina.ratelimit.partition_invalid` is a caller error, is returned as it is and never switches the decorator and logs once on the switch (event 400); it probes the primary again after `RetryPrimaryAfter` (interval and default decided in Design Choice 11) and logs the recovery (event 401); the failure is counted every time** | The requirement of R8 and of #704 ("falls back to the local limiter and logs it"); a dead cache is not called on every request (no added latency); the same algorithm runs locally, so the semantics do not change, only the scope (per instance) | While on the fallback the effective limit is N times the configured one again, which is the documented price |
| **B) Propagate the `Left`** | No hidden degradation | A cache outage turns into a request outage or, with a fail-closed consumer, a denial of every request |
| **C) Fail open: admit every request while the cache is down** | Never blocks traffic | Disables the security control during an outage (#2414 D3 treats rate limiting as a security control) |

### Chosen Option: **A - an in-process fallback with a probe interval**

### Rationale

- **Class:** Settled
- **Settled by:** ADR-055 R8 ("If the cache fails, the limiter falls back to the local limiter and logs it (#704)") and the #704 acceptance criterion "Fallback to local rate limiting on cache failure". B and C are excluded by that decision; the failure is not swallowed: it is counted, logged and visible in the metric, and the call is served by the local store (the explicit maintainer decision over AGENTS.md section 3's rule for background `Left` values, which concerns operations that would otherwise report success without doing the work). The local store applies the same limit, so a request over the limit is still denied.
- The decorator lives in the assembly that holds the abstraction, whichever package Design Choice 1 chooses; the event ids 400-401 are registered for that assembly. Time comes from `TimeProvider`; the probe is a timestamp comparison, no timer, so nothing in the decorator can throw on a background thread (AGENTS.md section 3).

</details>

<details>
<summary><strong>6. One in-process store for the fallback, Memory and Hybrid</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `InProcessRateLimiterStateStore` in the core; `Encina.Caching.Memory` and `Encina.Caching.Hybrid` register it as their `IRateLimiterStateStore`, and the fallback decorator uses it** | The algorithm exists once; the fallback needs a cache-free store anyway; the contract suite runs against it first | R8 and ADR-055 speak of one implementation per cache; here two of the eight are a registration of a shared class |
| **B) A separate implementation class inside each of the two packages, and a third for the fallback** | Literal reading of "one per cache" | Three copies of one algorithm to keep in step |

### Chosen Option: **A - one shared in-process store** (recommended, pending the maintainer)

### Rationale

- B wins if the maintainer reads "implemented in each cache package" as a requirement for a class in each package (for example to give the Hybrid L1 a different eviction); A wins because Memory and Hybrid state is local by decision R8 and a copy adds nothing. The store keeps per-partition state in a `ConcurrentDictionary` with a per-entry lock and removes entries whose window has fully elapsed (a sweep on access, no timer), so memory is bounded by the active partitions.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Which clock the Redis script uses</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The caller passes `TimeProvider.GetUtcNow()` in milliseconds to the script** | AGENTS.md section 3 (time from `TimeProvider`); the tests use a fake clock; the same code path runs against all five products | Instances with skewed clocks see a window shifted by the skew |
| **B) The script reads the server clock (`TIME`)** | One clock for all instances; exact windows across instances with unsynchronised hosts | The tests cannot control time; scripts that read the clock are restricted or replicated differently on some products |

### Chosen Option: **A - the caller's clock** (recommended, pending the maintainer)

### Rationale

- B wins in deployments whose application hosts are not time synchronised and where the window must be exact to the second; A wins by default because production hosts run NTP (a skew of milliseconds against windows of seconds) and a fake clock is the only way to test windows deterministically. The documentation states the skew bound.
- Class: Dilemma

</details>

<details>
<summary><strong>8. Error codes, EventIds and where they are emitted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two codes in the core: `encina.ratelimit.state_store_failed` (`ErrorCategory.Internal`, ADR-062 rule R4: a provider failure or an unexpected script reply) and `encina.ratelimit.partition_invalid` (`ErrorCategory.Validation`), built by a **public** static `RateLimiterStateStoreErrors` so `Encina.Caching.Redis` can return them; a new range `RateLimiterState = (400, 419)` from the family block 400-449 with ids 400 and 401, both emitted by the decorator in the core assembly, so the allocation-test mapping of `Encina` covers them; the Redis store logs nothing and records no metric, it returns `Left` and the decorator logs and counts** | Categorised codes (ADR-062); ids registered first and packed (ADR-021); no `InternalsVisibleTo` entry and no event emitted from an assembly outside the mapping | One public static class (PublicAPI lines) |
| **B) Keep the errors, log and telemetry `internal` and add `InternalsVisibleTo` for `Encina.Caching.Redis` plus a mapping of that assembly to the range** | Nothing public | A new IVT of the core for one package, and an allocation-test mapping of a second assembly to a core range |
| **C) Reuse `store_cache.failed` of #695** | No new code | That code lives in `Encina.Caching`, which the core cannot reference; it would also mislabel a limiter failure as a messaging store failure |

### Chosen Option: **A - public error factory, ids emitted only by the decorator** (recommended, pending the maintainer)

### Rationale

- B wins when the maintainer prefers nothing new public and accepts an `InternalsVisibleTo` entry plus a second mapped assembly; A wins because it needs neither. Both are correct under the chosen placement of Design Choice 1 (under its options B or C the same types sit in `Encina.Caching` or `Encina.Resilience`, the decorator and the ids move with them, and the Redis package already references both). Every new code states its `ErrorCategory` (ADR-062, rule R4 for provider failures) and the range is registered before use with ids packed (AGENTS.md section 7, ADR-021) from the family block 400-449; `src/Encina/Encina.csproj` has no `InternalsVisibleTo` for `Encina.Caching.Redis` today.
- Class: Dilemma
- Event ids: 400 primary store failed and the decorator switched to the fallback (Warning), 401 primary store recovered (Information). Exceptions through `ForLogging()`; an unexpected script reply is a `Left` with the code, counted by the decorator.

</details>

<details>
<summary><strong>9. Scope of this issue: the store, or also the limiter that uses it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The store, its fallback and its implementations only; the limiter that consumes them (coordinated counting end to end, the opt-in over the local limiter, the adaptive state machine or SRE rejection, the shared error rate) is a follow-up issue to be opened (draft: `artifacts/issues/distributed-rate-limiter-on-state-store.md`)** | Buildable and testable today (the consumer package and the #2414 decisions on the core are not in `src/`); the partition is still open (#3448, `needs-decision`); one pull request series per concern | The issue's `DistributedAdaptiveRateLimiter`, three-state machine, shared error-rate and "local limiter stays the default" criteria are met only when the follow-up lands; the issue closes only with it |
| **B) Also wire the existing `Encina.Polly` `AdaptiveRateLimiter` to the store now (a `DistributedAdaptiveRateLimiter : IRateLimiter` with the state machine over the shared counters)** | Meets the issue's criteria as written | Builds on a limiter that #2414 D1 and ADR-055 R1 replace completely (`Encina.Polly` is removed), and on a partition (#3448) that is undecided; the work would be discarded |
| **C) A, plus the state machine as a decorator over the store inside this issue** | Delivers the shared error-rate and the three states without waiting for the core | The issue's three-state machine conflicts with #2414 D2 (SRE-style probabilistic rejection counting transient errors only); building it first fixes a design the decision replaces |

### Chosen Option: **A - the store and its implementations, with the consumer as a follow-up issue** (recommended, pending the maintainer)

### Rationale

- B wins only if the maintainer wants a working distributed limiter on `Encina.Polly` before the resilience consolidation lands and accepts discarding it; C wins if the maintainer decides the adaptive rule now and wants it inside this issue; A wins because everything the store provides is needed by any of the other designs and nothing is discarded. The issue's acceptance criteria that A defers are listed in the follow-up draft, so none is dropped silently; #704 stays open until that follow-up (or the maintainer's decision to split the issue) is recorded.
- Class: Dilemma

</details>

<details>
<summary><strong>10. Metric names and tags</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Counter `encina.ratelimit.state_store.operations` on a new `Meter` named `Encina.RateLimiting` (one `Encina.*` name per package, ADR-054 D3; it is not the plain `Encina` name that D3 retires) with tags `operation` (`acquire`, `record`, `read`, `reset`), `outcome` (`ok`, `denied`, `failure`, `fallback`) and `store` (the primary store's type name); the tenant id is a tag only when a tenant is present; no scope, key hash or client identifier** | Lowercase dotted names under the existing `encina.ratelimit.*` family; the tenant tag only when multi-tenancy is on | The tenant tag raises cardinality (documented by #906) |
| **B) No tenant tag at all** | No cardinality risk | Breaks the rule that the tenant is a tag when multi-tenancy is on |

### Chosen Option: **A - one counter, three tags**

### Rationale

- **Class:** Trivial
- **Settled by:** ADR-055 settled-points table (telemetry follows ADR-054: one `Meter` named `Encina.<Package>`, lowercase dotted instrument names, tenant tag only when multi-tenancy is on, error code or exception type only) and ADR-054 D4 and D6 (no key, scope or client identifier in a tag, log or span); the instrument name extends the `encina.ratelimit.exceeded` family of #2414. The tag values are fixed sets, so cardinality is bounded except for the tenant tag.

</details>

<details>
<summary><strong>11. Probe interval and shape of the fallback decorator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A two-field state (`Volatile`) with a timestamp: after a failure the decorator serves from the fallback until `RetryPrimaryAfter` (default 5 seconds, setter accepts 1 second to 5 minutes) has passed, then the next call probes the primary; no timer, no lock** | No call to a dead cache on every request (no added latency); nothing can throw on a background thread; small | A fixed interval, no jitter, so many instances probe at about the same moment |
| **B) Try the primary on every call and fall back per call, with no state** | Recovers at once; simplest | Every request pays the failing call (a timeout per request while the cache is down) |
| **C) A Polly circuit breaker from `Encina.Resilience`** | A tested half-open state machine | The package does not exist yet, and the core cannot reference it (ADR-055 R1) |

### Chosen Option: **A - timestamp probe, 5 seconds** (recommended, pending the maintainer)

### Rationale

- B wins when the cache is local and its failures are instant; C wins once `Encina.Resilience` exists and the maintainer wants one breaker implementation for the whole family; A wins now. The default and the range are the plan's own and are asked here.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Abstraction, in-process store and the contract suite (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 9 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina/RateLimiting/IRateLimiterStateStore.cs` | new (public interface and its records: `RateLimiterPartition`, `SlidingWindowResult`, `OutcomeCounts`, `RateLimiterOutcome`) |
| 2 | `src/Encina/RateLimiting/InProcessRateLimiterStateStore.cs` | new (public sealed) |
| 3 | `src/Encina/RateLimiting/RateLimiterStateStoreErrors.cs` | new (public static; the two codes with their categories, so `Encina.Caching.Redis` can return them, Design Choice 8) |
| 4 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/RateLimiting/InProcessRateLimiterStateStoreTests.cs` | new |
| 6 | `tests/Encina.GuardTests/Core/RateLimiting/RateLimiterStateStoreGuardTests.cs` | new |
| 7 | `tests/Encina.ContractTests/RateLimiting/RateLimiterStateStoreContractTests.cs` | new (abstract suite; the in-process store is its first subclass) |
| 8 | `tests/Encina.PropertyTests/RateLimiting/SlidingWindowProperties.cs` | new |
| 9 | `.github/coverage-manifest/Encina.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Create** the interface and the records: `ValueTask<Either<EncinaError, SlidingWindowResult>> TryAcquireAsync(RateLimiterPartition partition, TimeSpan window, int limit, int permits, CancellationToken ct)`; `ValueTask<Either<EncinaError, Unit>> RecordOutcomeAsync(RateLimiterPartition partition, RateLimiterOutcome outcome, TimeSpan window, CancellationToken ct)`; `ValueTask<Either<EncinaError, OutcomeCounts>> GetOutcomeCountsAsync(RateLimiterPartition partition, TimeSpan window, CancellationToken ct)`; `ValueTask<Either<EncinaError, Unit>> ResetAsync(RateLimiterPartition partition, CancellationToken ct)`. `SlidingWindowResult(bool Acquired, int CurrentCount, TimeSpan RetryAfter)`; `OutcomeCounts(long Successes, long Failures)`; `RateLimiterOutcome { Success, Failure }`. XML docs state that the store never gates a missing tenant and never logs a scope.
2. **Create** `InProcessRateLimiterStateStore(TimeProvider? timeProvider = null)`: state per `(tenant, scope)` pair as a tuple key (no string concatenation, so it is injective by construction); a per-entry lock over a timestamp queue; outcome counters in `window / 10` buckets; a sweep of fully expired entries on access; every argument validated and returned as `Left` `partition_invalid`.
3. **Contract suite**: an abstract class with the behaviors every implementation must show (admits up to the limit then denies with a positive `RetryAfter`; the window slides with a fake clock; a tenant and no tenant and the `default` tenant are three independent partitions; two scopes of one tenant are independent; permits greater than one are all-or-nothing; `ResetAsync` clears; outcome counts expire with the window; invalid arguments return `partition_invalid`; 64 concurrent acquirers never admit more than the limit). The Redis family reuses this suite in PR 5.
4. **Property test** (FsCheck): for random interleavings of acquires and clock advances the admitted count inside any window never exceeds the limit.
5. **Guard tests**: null checks and the validation `Left` cases; the constructor accepts null for `timeProvider` (valid result test).
6. **Manifest and PublicAPI** through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #704, decision R8 of SPIKE #2691 (ADR-055): a rate limiter state store per cache.
This PR adds only the abstraction, an in-process implementation and the contract suite that every later implementation must pass.

TASK:
1. Create IRateLimiterStateStore and its records in src/Encina/RateLimiting, and InProcessRateLimiterStateStore.
2. Write the abstract contract suite, the unit tests, the guard tests and the FsCheck property.
3. Ask mechanical-fixer for the PublicAPI lines and the manifest.

KEY RULES:
- ROP: every operation returns Either<EncinaError, T>; invalid arguments are a Left with the code encina.ratelimit.partition_invalid
  (ErrorCategory.Validation, passed with the category: argument).
- Time comes from TimeProvider. Partition keys are tuples or injective strings: a tenant, no tenant and the tenant "default" never collide.
- The store never gates a missing tenant and never logs or tags a scope.
- Zero warnings; CRAP of every method at most 10; tests execute the real class.

REFERENCE FILES:
- src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs (the window algorithm to preserve)
- docs/architecture/adr/055-resilience-family.md (R8)
- docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 2: Fallback decorator, logs and registration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina/RateLimiting/ResilientRateLimiterStateStore.cs` | new (public sealed decorator) |
| 2 | `src/Encina/RateLimiting/RateLimiterStateStoreOptions.cs` | new (public: `RetryPrimaryAfter`) |
| 3 | `src/Encina/RateLimiting/RateLimiterStateStoreLog.cs` | new (internal; ids 400-401) |
| 4 | `src/Encina/RateLimiting/RateLimiterStateTelemetry.cs` | new (internal; the counter of Phase 6) |
| 5 | `src/Encina/RateLimiting/RateLimiterStateServiceCollectionExtensions.cs` | new (public `AddEncinaRateLimiterStateFallback`) |
| 6 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`RateLimiterState = (400, 419)`) |
| 7 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (maps `Encina` to the new range) |
| 8 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/RateLimiting/ResilientRateLimiterStateStoreTests.cs` | new |
| 10 | `tests/Encina.UnitTests/RateLimiting/RateLimiterStateRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 11 | `tests/Encina.GuardTests/Core/RateLimiting/RateLimiterStateStoreGuardTests.cs` | modify |
| 12 | `.github/coverage-manifest/Encina.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/704-rate-limiter-state-store.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Register** `RateLimiterState = (400, 419)` first and map `Encina` to it in the allocation test; ids packed: 400, 401 (Design Choice 8).
2. **Create** `ResilientRateLimiterStateStore(IRateLimiterStateStore primary, IRateLimiterStateStore fallback, IOptions<RateLimiterStateStoreOptions> options, TimeProvider timeProvider, ILogger<ResilientRateLimiterStateStore> logger)`: every call goes to the primary unless the decorator is in the fallback state and the probe time has not arrived; only a `Left` with `state_store_failed` or an exception from the primary (cancellation excepted: it propagates) switches to the fallback; a `Left` with `partition_invalid` is returned as it is and never switches (Design Choice 5); logs event 400 once, counts the failure and serves the call from the fallback; after `RetryPrimaryAfter` the next call probes the primary and, on success, logs 401. State is two fields (`Volatile`), no timer, no lock on the hot path.
3. **Create** `AddEncinaRateLimiterStateFallback(this IServiceCollection services, Action<RateLimiterStateStoreOptions>? configure = null)`: `ArgumentNullException.ThrowIfNull(services)`; registers `InProcessRateLimiterStateStore` as the fallback (keyed internally, never as the public `IRateLimiterStateStore`), `Configure<RateLimiterStateStoreOptions>`, `TryAddSingleton(TimeProvider.System)`, and decorates the registered `IRateLimiterStateStore` with the resilient store; it throws `InvalidOperationException` at registration when no store is registered (call it after the cache package registration). All services are singletons, so no scoped service is captured.
4. **Tests (unit)**: primary success never touches the fallback; a `Left` with `state_store_failed` and a throwing primary switch and log once; a `Left` with `partition_invalid` is returned as it is, does not switch, does not log and is not counted as a failure; further calls inside the probe interval do not call the primary; after the interval the probe succeeds and logs recovery; a failing probe stays on the fallback and resets the interval; cancellation propagates and does not switch; a request over the limit is still denied on the fallback; the logs never contain the scope.
5. **Tests (guard)** and the **DI test**: the registration builds with `ValidateOnBuild` and `ValidateScopes`, the missing-store case throws at registration, and calling it twice decorates once.
6. **Manifest, PublicAPI and changelog** through `mechanical-fixer` (fragment: "Added `IRateLimiterStateStore`, an in-process implementation and `AddEncinaRateLimiterStateFallback`, the shared state abstraction for distributed rate limiting").

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
PR 1 of issue #704 is merged: IRateLimiterStateStore, InProcessRateLimiterStateStore and the contract suite exist. Decision R8 says a cache
failure falls back to the local limiter and is logged.

TASK:
1. Register EventIdRanges.RateLimiterState = (400, 419) first and map it in EncinaEventIdAllocationTests; write RateLimiterStateStoreLog (400-401).
2. Create ResilientRateLimiterStateStore, its options and AddEncinaRateLimiterStateFallback as described in the plan.
3. Write the unit, guard and DI tests; ask mechanical-fixer for the PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- No timer and no background loop: the probe is a timestamp comparison, so nothing can throw on a background thread.
- Cancellation propagates; every other failure is counted, logged by exception type through ForLogging(), and served by the fallback.
- No scope, tenant value or client identifier in a log or tag. Ids 400-401 only, packed.
- Singletons only. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Caching/Sharding/ShardingCacheServiceCollectionExtensions.cs (decoration precedent)
```

</details>

### Phase 3: The Redis-family store with Lua (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 9 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching.Redis/RateLimiting/RedisRateLimiterStateStore.cs` | new (public sealed) |
| 2 | `src/Encina.Caching.Redis/RateLimiting/RateLimiterScripts.cs` | new (internal; the Lua text as constants) |
| 3 | `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` | modify (`TryAddSingleton<IRateLimiterStateStore, RedisRateLimiterStateStore>()` in the shared registration path, so Valkey, Dragonfly, Garnet and KeyDB get it) |
| 4 | `src/Encina.Caching.Redis/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/Caching/Redis/RedisRateLimiterStateStoreTests.cs` | new |
| 6 | `tests/Encina.GuardTests/Caching/Redis/RedisRateLimiterStateStoreGuardTests.cs` | new |
| 7 | `tests/Encina.UnitTests/Caching/Redis/RedisRateLimiterRegistrationTests.cs` | new (the five wrapper registrations resolve one store; `ValidateOnBuild`) |
| 8 | `.github/coverage-manifest/Encina.Caching.Redis.json` | modify (`mechanical-fixer`) |
| 9 | `changelog.d/704-redis-rate-limiter-state-store.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `RedisRateLimiterStateStore(IConnectionMultiplexer connection, IOptions<CachingOptions> cachingOptions, TimeProvider timeProvider)` (no logger and no telemetry: the decorator in the core logs and counts, Design Choice 8). Each operation is one `IDatabase.ScriptEvaluateAsync` of a prepared script with **one key** (so a Redis Cluster slot is never crossed): the sliding-window acquire (sorted set of admitted permits scored by the caller's millisecond clock; remove entries older than the window, count, admit all permits or none, set the key expiry to the window, return admitted flag, count and retry-after), the outcome counter (a hash of window buckets, increments and removes expired buckets in one script), the counts read and the reset. Keys from `StoreCacheKey.Build(KeyPrefix, tenantId, "rate-limit", hash)`, the hash from Design Choice 4. A reply that is not the expected shape returns `Left` `state_store_failed` (built by the public `RateLimiterStateStoreErrors`); the decorator logs it by error code, never by content.
2. **Registration**: the existing `AddEncinaRedisCache` path registers it with `TryAdd`, so an application's own store wins and `AddEncinaValkeyCache`, `AddEncinaDragonflyCache`, `AddEncinaGarnetCache` and `AddEncinaKeyDbCache` (which call it) need no change.
3. **Tests (unit)**: with a substitute `IDatabase`, the arguments passed to the script (one key, millisecond clock from the fake time provider, window, limit, permits), parsing of each reply shape, an unexpected reply, a throwing database returns `Left` and never throws, cancellation propagates, keys of a tenant, no tenant and the tenant `default` differ, and two scopes hash to different keys. The Lua text itself is exercised by the integration run of PR 5 (the unit flag cannot execute Lua; the manifest justification says so).
4. **Guard tests**: constructor null checks, `partition` and option validation returning `Left`.
5. **Manifest, PublicAPI and changelog** through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #704 are merged. This PR adds the Redis-family implementation: Lua scripts over StackExchange.Redis, registered by
AddEncinaRedisCache so Valkey, Dragonfly, Garnet and KeyDB inherit it.

TASK:
1. Create RedisRateLimiterStateStore and RateLimiterScripts as described in the plan: one script per operation, exactly one key per script.
2. Register it with TryAddSingleton in the shared Redis registration path; never override an application's own store.
3. Write the unit and guard tests and the registration test; ask mechanical-fixer for the PublicAPI line, the manifest and the changelog.

KEY RULES:
- The clock is passed in by the caller from TimeProvider; the script never reads the server clock.
- Every key comes from StoreCacheKey with a hashed scope and the tenant segment; no raw scope in a key, log or tag.
- ROP: a database failure or an unexpected reply is a Left with encina.ratelimit.state_store_failed (Internal); cancellation propagates.
- Scripts take the key through KEYS and every value through ARGV (Dragonfly requires declared keys).
- Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Caching.Redis/RedisCacheProvider.cs, ServiceCollectionExtensions.cs
- src/Encina.Caching.Valkey/ServiceCollectionExtensions.cs
- src/Encina/RateLimiting (PR 1 and PR 2)
- the RedisRateLimiting pattern named by decision R8 (System.Threading.RateLimiting limiters over store-side atomic scripts)
```

</details>

### Phase 4: Memory and Hybrid registration (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 6 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching.Memory/ServiceCollectionExtensions.cs` | modify (`TryAddSingleton<IRateLimiterStateStore, InProcessRateLimiterStateStore>()`) |
| 2 | `src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs` | modify (same registration) |
| 3 | `tests/Encina.UnitTests/Caching/Memory/MemoryRateLimiterStateRegistrationTests.cs` | new (runs the contract suite of PR 1 against the resolved store) |
| 4 | `tests/Encina.UnitTests/Caching/Hybrid/HybridRateLimiterStateRegistrationTests.cs` | new |
| 5 | `.github/coverage-manifest/Encina.Caching.Memory.json` | modify (`mechanical-fixer`) |
| 6 | `.github/coverage-manifest/Encina.Caching.Hybrid.json` | modify (`mechanical-fixer`) |

Tasks: register the shared in-process store (Design Choice 6) in both packages with `TryAdd`; the tests resolve it from a provider built with `ValidateOnBuild` and `ValidateScopes`, assert it is the in-process type, and run the contract suite subclass so the behavior is proven for the package registration. The documentation states that Memory and Hybrid state is per instance, so the effective limit with N instances is N times the configured one (R8). No public API line: the type is public in `Encina` (PR 1) and the registrations add none.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Issue #704, decision R8: Memory and the Hybrid L1 keep local state. InProcessRateLimiterStateStore exists in the Encina core (PR 1).

TASK:
Register it with TryAddSingleton in AddEncinaMemoryCache and AddEncinaHybridCache, write the registration tests that also run the contract
suite, and ask mechanical-fixer for the two manifests.

KEY RULES:
- TryAdd only: an application's own store wins in any registration order.
- Document that the state is per instance; never claim cross-instance coordination for these two caches.

REFERENCE FILES:
- src/Encina.Caching.Memory/ServiceCollectionExtensions.cs
- src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs
- tests/Encina.ContractTests/RateLimiting/RateLimiterStateStoreContractTests.cs
```

</details>

### Phase 5: Integration over the Redis family and concurrency (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

PR 5 holds exactly these 12 files (the existing `RedisFixture.Connection` is already public, so that fixture is not edited):

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Infrastructure/Caching/RateLimiterStateStoreIntegrationSuite.cs` | new (abstract: the PR 1 contract behaviors against a real server, plus two clients sharing one limit) |
| 2 | `tests/Encina.IntegrationTests/Infrastructure/Caching/RedisRateLimiterStateStoreTests.cs` | new (`[Collection(RedisCollection.Name)]`, the existing `RedisFixture`) |
| 3-6 | `tests/Encina.IntegrationTests/Infrastructure/Caching/{Valkey,Dragonfly,Garnet,KeyDb}RateLimiterStateStoreTests.cs` | new (4) |
| 7-10 | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/{Valkey,Dragonfly,Garnet,KeyDb}Fixture.cs` | new (4: a Testcontainers generic container of the vendor image with the Redis wire port, tag pinned in the fixture) |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisCompatibleCollections.cs` | new (the four collection definitions) |
| 12 | `.github/coverage-manifest/Encina.Caching.Redis.json` | modify (`mechanical-fixer`: the integration targets) |

Scenarios, run once per product (Redis, Valkey, Dragonfly, Garnet, KeyDB): the PR 1 contract behaviors; two independent `RedisRateLimiterStateStore` instances over two connections admit at most the limit in total (the multi-instance coordination the issue asks for); the key expires with the window; `ResetAsync` clears; a killed connection makes the resilient decorator fall back and recover when the container is back (decorator over the real store). `Encina.IntegrationTests` already references `Encina.Caching.Redis`, `Encina.Caching.Valkey` and the other wrapper packages are not needed: the wrappers only call `AddEncinaRedisCache`, so the tests construct the store against each product's container.

A product whose script engine cannot pass the suite is not worked around: the failing behavior is recorded as a gap issue before the pull request merges, because AGENTS.md section 5 requires every cache provider to support the feature. Memcached (#277) is a follow-up, and Hybrid's L2 is not used by this feature.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
PR 1 to PR 4 of issue #704 are merged. This PR proves the Redis-family store against a real server of each product.

TASK:
1. Write RateLimiterStateStoreIntegrationSuite (the contract behaviors against a real server, plus two clients sharing one limit) and one
   thin class per product with its fixture and collection.
2. Add the four Testcontainers fixtures and their collections; keep the Redis one and expose its connection to the shared suite.
3. Ask mechanical-fixer for the integration targets in the manifest.

KEY RULES:
- Shared collection fixtures only; never create a per-class container; FakeTimeProvider for windows, no Thread.Sleep.
- Tests execute the real store, the real scripts and a real server; no mocks.
- A product that fails a behavior is reported, never skipped silently or worked around.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
- tests/Encina.ContractTests/RateLimiting/RateLimiterStateStoreContractTests.cs
```

</details>

### Phase 6: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry** (`RateLimiterStateTelemetry`, internal, on the `Encina.RateLimiting` meter and the existing `ActivitySource` "Encina" of `EncinaDiagnostics`): counter `encina.ratelimit.state_store.operations` with tags `operation` (`acquire`, `record`, `read`, `reset`), `outcome` (`ok`, `denied`, `failure`, `fallback`) and `store` (the primary store's type name); the tenant id is a tag only when a tenant is present (ADR-054 D4, the cardinality is documented by #906); no scope, key hash or client identifier in a tag or span (ADR-054 D6). Only the decorator of PR 2 records it, so the Redis store needs no access to the internal class (Design Choices 8 and 10).
2. **Logs**: ids 400-401 only (Design Choice 8); no scope or key in a message; exceptions through `ForLogging()`.
3. **Error codes**: `encina.ratelimit.state_store_failed` (`ErrorCategory.Internal`) and `encina.ratelimit.partition_invalid` (`ErrorCategory.Validation`), both with the category passed explicitly (ADR-062). Nothing is added to the consumer's `encina.ratelimit.exceeded`.
4. **Coverage targets** (per file, per flag; baseline: the new files do not exist; the manifest targets today are `Encina` unit 70, guard 20, contract 15, `Encina.Caching.Redis` guard 20, unit 60, integration 29, and Memory and Hybrid as in their manifests; measured values replace the estimates before each pull request opens, with the commands of AGENTS.md section 9):

| File | PR | unit | guard | contract | property | integration | Justification |
|------|----|------|-------|----------|----------|-------------|---------------|
| `RateLimiting/InProcessRateLimiterStateStore.cs` | 1 | 95 | 60 | 80 | 70 | 0 | Every branch unit tested with a fake clock; guard tests reach the validation paths; the contract suite and the window property reach the admit and slide paths; in-process, nothing to integrate |
| `RateLimiting/IRateLimiterStateStore.cs` | 1 | 90 | 50 | 0 | 0 | 0 | Records and their equality and validation; the interface has no executable code |
| `RateLimiting/ResilientRateLimiterStateStore.cs` | 2 | 95 | 70 | 0 | 0 | 40 | Switch, probe, recovery and cancellation unit tested; guard tests reach the constructor checks; the PR 5 run reaches a real failure and recovery |
| `RateLimiting/RateLimiterStateServiceCollectionExtensions.cs` | 2 | 95 | 90 | 0 | 0 | 0 | DI test and guard tests reach every line |
| `RateLimiting/RateLimiterStateStoreLog.cs`, `RateLimiterStateTelemetry.cs` | 2 | 90 | 0 | 0 | 0 | 0 | Asserted through the decorator tests with a fake logger and a meter listener; internal types without `InternalsVisibleTo` for `Encina.GuardTests` |
| `RateLimiting/RateLimiterStateStoreErrors.cs` | 1 | 100 | 0 | 0 | 0 | 0 | Two factories asserted by the in-process store and decorator tests; the guard flag has no parameter to check |
| `RateLimiting/RateLimiterStateStoreOptions.cs` | 2 | 100 | 100 | 0 | 0 | 0 | The setter validation of `RetryPrimaryAfter` is reachable from both flags |
| `ServiceCollectionExtensions.cs` of `Encina.Caching.Redis` (the added registration) | 3 | 90 | 0 | 0 | 0 | 40 | The registration test resolves the store through the five wrapper extensions; the guard flag already covers the existing method; the PR 5 run builds the store through it |
| `ServiceCollectionExtensions.cs` of `Encina.Caching.Memory` and of `Encina.Caching.Hybrid` (the added registration) | 4 | 90 | 0 | 0 | 0 | 0 | The registration tests resolve the in-process store with `ValidateOnBuild` and run the contract suite; no new parameter |
| `RateLimiting/RedisRateLimiterStateStore.cs` (`Encina.Caching.Redis`) | 3 (integration in 5) | 85 | 60 | 0 | 0 | 80 | Argument building and reply parsing unit tested with a substitute database; the Lua text is executable only against a server, which the PR 5 run does for five products |
| `RateLimiting/RateLimiterScripts.cs` (`Encina.Caching.Redis`) | 3 (integration in 5) | 0 | 0 | 0 | 0 | 80 | Constants only: no unit-executable code; the integration run executes every script |

5. **Load**: a load test is implemented (the feature is concurrent by nature): PR 6.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
The state store of issue #704 exists in PR 1 to PR 5. Telemetry, logs, error codes and coverage targets are added with the PR that
introduces each caller and are measured per flag.

TASK:
Add RateLimiterStateTelemetry in PR 2 (only the decorator records it); run each flag's test project with coverage, compare with the plan's targets, and
record the targets and one-sentence justifications through mechanical-fixer.

KEY RULES:
- EventIds only 400-401, registered first. Both error codes name their ErrorCategory with the category: argument.
- No scope, key, hash, client identifier or tenant value in a log, tag or span except the tenant tag when a tenant is present.
- Documentation never types coverage figures.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, EncinaDiagnostics.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- .github/coverage-manifest/Encina.json
```

</details>

### Phase 7: Benchmark, load test and documentation (pull request 6)

<details>
<summary><strong>Tasks</strong></summary>

PR 6 holds exactly these 4 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.BenchmarkTests/Encina.Caching.Redis.Benchmarks/RateLimiterStateStoreBenchmarks.cs` | new (acquire on the in-process store versus the Redis store against a local server; `BenchmarkSwitcher` already runs the project; verify the filter with `--list flat`) |
| 2 | `tests/Encina.LoadTests/Caching/RateLimiterStateStoreLoadTests.cs` | new (many concurrent acquirers against one partition never exceed the limit; many partitions) |
| 3 | `docs/features/rate-limiter-state-store.md` | new (`docs-writer`: what the store is, which caches coordinate across instances and which are per instance, the fallback and its price, the clock-skew bound, the hashed scope; no hand-typed figures) |
| 4 | `docs/INVENTORY.md` | modify (`docs-writer`: the `RateLimiting` folders of `Encina` and `Encina.Caching.Redis`) |

Steps: XML documentation on every public type is part of PR 1 to PR 3; the page states the cost of the fallback (limit times N while on it); the benchmark is the evidence for the per-request latency of the Redis store, which the maintainer reads before enabling the distributed limiter by default anywhere. Build with `dotnet build --configuration Release` (0 warnings), `dotnet test` of each flag, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
Issue #704 is implemented in PR 1 to PR 5. PR 6 adds the benchmark, the load test and the documentation.

TASK:
Write the benchmark (in-process versus Redis acquire) and the load test, verify the filter with --list flat, ask docs-writer for the feature
page and the INVENTORY entry, and run the build, the tests and the coverage checks.

KEY RULES:
- State per-instance versus cross-instance behavior of each cache, the fallback price and the clock-skew bound in the page.
- BenchmarkSwitcher, never BenchmarkRunner.Run<T>; results go under artifacts/performance.
- Documentation cites coverage with cov: markers, never typed numbers.

REFERENCE FILES:
- tests/Encina.BenchmarkTests/Encina.Caching.Redis.Benchmarks
- tests/Encina.LoadTests (an existing caching load test for the layout)
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| ADR-055 (R8, R1, "Negative" list) | The decision: a state store per cache, fallback to the local limiter, keys without personal data |
| #2414 decisions D1 to D3 | The partitioned core, the adaptive decorator, tenant by default and fail closed (the consumer's rules) |
| AGENTS.md section 5 (caching, 8 providers) | Every cache provider must support the feature: five through the Redis family, two local, Memcached as a follow-up |
| AGENTS.md section 3 | ROP, `TimeProvider`, background loops never throw (no timer here), fail-closed gates (the core's), singletons only |
| ADR-062, ADR-054 D4 and D6, ADR-021 | Error categories, the tenant tag and telemetry rules, EventId registry |
| GDPR Art. 5(1)(c) | Data minimisation: a client identifier never reaches a key (Design Choice 4) |
| Named comparable products | `RedisRateLimiting` (sliding window and token bucket over Lua scripts), `System.Threading.RateLimiting.PartitionedRateLimiter` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| AdaptiveRateLimiter, IRateLimiter | src/Encina.Polly/RateLimiting | the window algorithm to preserve; replaced wholesale by the #2414 work, not edited here |
| StoreCacheKey | src/Encina.Caching/MessagingStores (#695) | injective key and tenant segment; the Redis store references it |
| RedisCacheProvider, AddEncinaRedisCache | src/Encina.Caching.Redis | connection, key prefix and the registration path the wrappers share |
| Valkey, Dragonfly, Garnet, KeyDB wrappers | src/Encina.Caching.{Valkey,Dragonfly,Garnet,KeyDB} | inherit the registration |
| MemoryCacheProvider, HybridCacheProvider | src/Encina.Caching.Memory, src/Encina.Caching.Hybrid | registration of the in-process store |
| EncinaDiagnostics, EventIdRanges | src/Encina/Diagnostics | the core `ActivitySource` and the EventId registry |
| RedisFixture, RedisCollection | tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures | the only cache container fixture on `main`; four are added |
| ResiliencePipeline-based limiter of #2414 | planned `Encina.Resilience` (#740) | the consumer, not part of this plan |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (new range `RateLimiterState`) | 400-419 (from the family block 400-449) | 400-401 used by this plan; 402-419 free; 420-434 are #705 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 10 | 4 | New: interface and records, in-process store, errors, decorator, options, log, telemetry, extension, Redis store, scripts; modified: `EventIdRanges.cs`, and the `ServiceCollectionExtensions.cs` of `Encina.Caching.Redis`, `.Memory` and `.Hybrid` |
| Tests | 24 | 2 | New: unit, guard, contract, property (PR 1); decorator unit and DI (PR 2); Redis unit, guard and registration (PR 3); two registration tests (PR 4); suite, five product classes, four fixtures, collections (PR 5); benchmark and load test (PR 6); modified: the allocation test, the Guard file of PR 1 (PR 2) |
| Documentation | 1 | 1 | feature page; `INVENTORY.md` |
| Configuration | 2 | 6 | Changelog fragments (2); manifests (`Encina`, `Encina.Caching.Redis`, `.Memory`, `.Hybrid`), PublicAPI (`Encina`, `Encina.Caching.Redis`) |
| **Total** | **37** | **13** | 6 pull requests: 9, 13, 9, 6, 12 and 4 files (53 file touches: 49 distinct files, because the `Encina` manifest and PublicAPI file, the guard test file of PR 1 and the Redis manifest are touched in two pull requests each), each at most 15 files and about 900 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #704 under decision R8 of SPIKE #2691 (ADR-055): an
IRateLimiterStateStore implemented per cache, with a fallback to a local store on cache failure. The consumer (the partitioned limiter of
#2414, issue #740) is not part of this plan.

IMPLEMENTATION OVERVIEW:
1. PR 1: IRateLimiterStateStore (sliding window acquire, outcome counters, reset) in the core, InProcessRateLimiterStateStore, the abstract
   contract suite, unit, guard and property tests.
2. PR 2: ResilientRateLimiterStateStore (fallback with a probe interval), options, logs 400-401 and the range 400-419,
   AddEncinaRateLimiterStateFallback, telemetry.
3. PR 3: RedisRateLimiterStateStore with one Lua script per operation and one key per script, registered by AddEncinaRedisCache.
4. PR 4: Memory and Hybrid register the in-process store.
5. PR 5: integration of the suite against real Redis, Valkey, Dragonfly, Garnet and KeyDB servers, with two clients sharing one limit.
6. PR 6: benchmark, load test, documentation.

KEY PATTERNS:
- The store takes an opaque partition (optional tenant, scope string); it never gates a missing tenant and never maps no tenant to "default".
- The scope is hashed before it enters a key; the tenant segment is readable and separately encoded (StoreCacheKey).
- A cache failure falls back to the in-process store with a log entry; cancellation propagates; no timer, no background loop.
- The clock comes from TimeProvider and is passed to the script; every script touches exactly one key.
- Two error codes with categories (Internal, Validation); EventIds 400-401 only; no scope, key or client identifier in telemetry or logs.
- Apply Design Choices 1 to 4, 6 to 9 and 11 as the maintainer answers them; until then build the recommended option of each.

REFERENCE FILES:
- docs/architecture/adr/055-resilience-family.md (R8)
- src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs
- src/Encina.Caching.Redis/RedisCacheProvider.cs, ServiceCollectionExtensions.cs
- src/Encina.Caching.Memory/ServiceCollectionExtensions.cs, src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.json, Encina.Caching.Redis.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature is cache-backed shared state for a limiter, through a dedicated abstraction because `ICacheProvider` has no atomic primitive (R8); five products through Lua, two local, Memcached as a follow-up. |
| 2 | OpenTelemetry | ✅ Include | Counter `encina.ratelimit.state_store.operations` with `operation`, `outcome` and `store` tags; tenant tag only when a tenant is present (ADR-054 D4); no scope or key (D6). |
| 3 | Structured Logging | ✅ Include | New range `RateLimiterState` 400-419, ids 400-401, exceptions through `ForLogging()`. |
| 4 | Health Checks | ❌ N/A | The fallback is the availability mechanism and is visible as a metric; the cache packages have their own health checks. |
| 5 | Validation | ✅ Include | Window, limit, permits and scope validated on every call, returned as `Left` with `partition_invalid` (Validation). |
| 6 | Resilience | ✅ Include | The fallback decorator with a probe interval; a cache outage never blocks requests and never silently disables the limit (the in-process store still enforces it). |
| 7 | Distributed Locks | ❌ N/A | Atomicity comes from one Lua script per operation, not from a lock (option 8c of the spike was not chosen). |
| 8 | Transactions | ❌ N/A | No persistence and no unit of work; a script is atomic on its key. |
| 9 | Idempotency | ❌ N/A | Rate limiting counts requests; retried requests count again by design. |
| 10 | Multi-Tenancy | ✅ Include | The partition carries an optional tenant, the key has an injective tenant segment, "no tenant" never equals `default`, and the store never gates (the fail-closed denial is the core's, #2414 D3). |
| 11 | Module Isolation | ❌ N/A | Not module scoped; a module id, if the core wants one, is part of the opaque scope. |
| 12 | Audit Trail | ❌ N/A | A rate limit decision is not an audit event of this component; the consumer records `encina.ratelimit.exceeded`. |

**Deferred items:** one follow-up issue to be opened, drafted: the Memcached implementation of the store with compare-and-swap, which needs the Memcached provider of #277 (draft: `artifacts/issues/memcached-rate-limiter-state-store.md`). A second follow-up issue is drafted for what Design Choice 9 option A defers from the issue's acceptance criteria (draft: `artifacts/issues/distributed-rate-limiter-on-state-store.md`): a limiter that counts across instances end to end through the store, stays opt-in with the local limiter as the default, and shares the error rate for the adaptive rule (the issue's `DistributedAdaptiveRateLimiter`, three-state machine and shared error-rate criteria, reconciled with #2414 D2). It depends on the partitioned core of #2414 (the open SPIKE; #740, tenant-scoped rate limiting, is the open issue that applies its tenant partition) and on the partition decision of #3448 (open, `needs-decision`).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| Decision R8 and ADR-055 | ✅ Available | Merged; the design is decided |
| `Encina.Caching.Redis`, `.Memory`, `.Hybrid` and the wrapper packages | ✅ Available | Valkey, Dragonfly, Garnet and KeyDB call `AddEncinaRedisCache` |
| `StoreCacheKey` of #695 | ⏭️ Sibling | Needed by PR 3 only (the key builder and its tenant segment); PR 1 and PR 2 have no dependency on it |
| `RedisFixture` and Testcontainers | ✅ Available | The four product fixtures are added by PR 5 |
| #277 (Memcached provider) | ⏭️ Sibling | Not required; the follow-up issue waits for it |
| #740 and #3448 (consumer and partition) | ⏭️ Sibling | Not required; the store works with any partition and ships without a consumer |
| Maintainer answers to Design Choices 1 to 4, 6 to 9 and 11 | Pending | Asked when work starts |

One real prerequisite: #695 PR 1 before PR 3 of this plan.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 4, 6 to 9 and 11) when work starts, records the answers in a `## Maintainer Decisions` section, then briefs six workers in the phase order (PR 1 and PR 2 may start at once).
2. After PR 5, open the Memcached follow-up issue from its draft once #277 has a provider to build on, and comment on #740 that `IRateLimiterStateStore` is available to the partitioned core.
