# Implementation Plan: Memcached implementation of `IRateLimiterStateStore` with compare-and-swap

> **Issue**: [#3995](https://github.com/dlrivada/Encina/issues/3995) (`area-caching`, `area-resilience`, `area-rate-limiting`, `p1-recommended`)
> **Type**: Feature
> **Complexity**: Medium (one store, a version-token seam over the Memcached client, a bounded retry loop, a binary window codec)
> **Estimated Scope**: ~600-800 lines of production code + ~1,500-2,000 lines of tests (about 28 files and up to about 2,400 line-changes in total with manifests, PublicAPI and documentation), delivered in four pull requests of at most 15 files and 1,500 lines each
> **Related plans of the same family**: builds on the plan of #704 (the abstraction `IRateLimiterStateStore`, the in-process store, the fallback decorator, the Redis-family store and the contract suite) and on the plan of #277 (the `Encina.Caching.Memcached` package: options, client registration, `AddEncinaMemcachedCache`, log class, Memcached container fixture). #3989 and #3991 are unrelated plans written in the same run. Sibling plans are referred to by issue number only.

---

## Summary

Decision R8 of SPIKE #2691 (ADR-055) says every cache package implements `IRateLimiterStateStore` with its best atomic operation: Lua in the Redis family, local state in Memory and the Hybrid L1, and compare-and-swap in Memcached. Memcached has no scripting and no server-side window structure, so atomicity comes from the version token of `gets` and `cas`. This plan adds `MemcachedRateLimiterStateStore` to the Memcached package so that Memcached is a full member of the eight cache providers for distributed rate limiting (AGENTS.md section 5).

### What the code does today (2026-11-24, `main` at `278c164f1`)

1. **Neither dependency is in `src/`.** `src/Encina.Caching.Memcached` and `src/Encina/RateLimiting` do not exist; the plans of #277 and #704 are on `main`, both issues are open. #3995 is blocked by #277 (open, `status:approved`, no open maintainer decision of its own that this plan depends on). The names used here are the ones those plans specify.
2. **The contract (#704 plan, Design Choice 2)**: `TryAcquireAsync(partition, window, limit, permits, ct)` returns `SlidingWindowResult(Acquired, CurrentCount, RetryAfter)`, `RecordOutcomeAsync`, `GetOutcomeCountsAsync` (`OutcomeCounts(Successes, Failures)`) and `ResetAsync`, all `ValueTask<Either<EncinaError, T>>`; the partition is `RateLimiterPartition(string? TenantId, string Scope)`; failures are `encina.ratelimit.state_store_failed` (`ErrorCategory.Internal`) and `encina.ratelimit.partition_invalid` (`ErrorCategory.Validation`); the abstract `RateLimiterStateStoreContractTests` includes "64 concurrent acquirers never admit more than the limit". The fallback decorator `ResilientRateLimiterStateStore` serves a failed primary from the in-process store.
3. **The key (#704 plan, Design Choices 3 and 4)**: `StoreCacheKey.Build(prefix, tenantId, "rate-limit", <scope hash>)` with the scope hashed (SHA-256, lowercase hex) and a tenant segment `t:{encoded}` present only when a tenant is given; `null` is never mapped to `default`. `StoreCacheKey` is the builder of the store-cache foundation #695 (open).
4. **Memcached limits that shape the design** (Memcached protocol and server documentation): a key is at most 250 bytes with no whitespace or control characters; an item defaults to 1 MB; `add` stores only when the key is absent; `cas` stores only when the token returned by `gets` is unchanged; the server has no clock command; the LRU may evict an item before its expiry. The client chosen by #277 (EnyimMemcachedCore, Design Choice 2 there, pending) returns `false` on failure instead of throwing; whether its CAS calls accept a `CancellationToken` is confirmed against the package when work starts, and when they do not, cancellation is honoured between attempts.
5. **The metric tag** `store` of `encina.ratelimit.state_store.operations` is the primary store's type name (#704 plan, Design Choice 10), so the store emits nothing itself; the issue's `store=memcached` becomes the value `MemcachedRateLimiterStateStore`.

### Scope

- **Implements**: #3995 in `Encina.Caching.Memcached`: the store, its options and validator, the window codec, the key helper, an internal version-token seam over the client, the registration in `AddEncinaMemcachedCache`, and the contract, integration and DI tests.
- **Affected packages**: `Encina.Caching.Memcached` only (plus tests). The abstraction, the decorator and the contract suite are #704's and are not changed here.
- **Provider category**: Caching (8). The eight-row table below names the provider owning each row; this plan owns the Memcached row.
- **Corrections to the issue text**: the `store` tag value is the type name, not `memcached`; outcome counters use `incr` rather than the window item (Design Choice 3); the registration is `TryAddSingleton` inside the shared Memcached registration (Design Choice 4).
- **Split into pull requests** (counts include tests, manifest rows and PublicAPI lines; the numbering is the merge order; every pull request builds alone and registers the services it introduces):

  | PR | Content (phase) | Files | Lines (est.) | Depends on |
  |----|-----------------|-------|--------------|------------|
  | 1 | `RateLimitWindowCodec` and `MemcachedRateLimiterKeys` (both internal, with their XML docs), manifest, codec, keys and guard tests (Phase 1) | 6 | ~450 | #277 PRs 1 to 3, #695 (`StoreCacheKey`), #704 PR 1 (`RateLimiterPartition`) |
  | 2 | Options and validator, `IMemcachedVersionedStore`, `EnyimVersionedStore`, `MemcachedRateLimiterStateStore`, registration of all of them in `AddEncinaMemcachedCache` (the options and the validator are registered by the PR that adds them), XML docs, PublicAPI, manifest, store unit, validator unit, guard and DI tests (Phase 2) | 12 | ~1,100 | 1, #704 PRs 1 and 2 |
  | 3 | Contract subclass, two-client integration test, benchmark and load justification files, manifest (Phase 3) | 5 | ~600 | 2, #704 contract suite, #277 Memcached fixture |
  | 4 | Changelog fragment, package README, configuration page (Phase 4) | 3 | ~200 | 3 |

  Total: 4 PRs, 26 files, about 2,350 line-changes (sum of the per-PR upper estimates); no PR exceeds 15 files or 1,500 lines.

---

## Design Choices

<details>
<summary><strong>1. How the sliding window is stored and updated</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One item per partition holding the admitted timestamps (a binary log), updated by `gets` and `cas` in a bounded loop; an absent item is created with `add`** | Exact sliding window, the algorithm of the in-process store and the Redis script; permits greater than one are all-or-nothing; satisfies the exact contract suite and the "never more than the limit in total" criterion | Item size grows with the limit (8 bytes per admitted request): a limit above `MaxWindowEntries` is rejected with `partition_invalid`; a hot partition contends on one key |
| **B) Fixed-window counters combined as a weighted sliding window with `incr`** | O(1) size, no retry loop, no contention | An approximation: it can admit more or fewer than the limit at a window boundary; "all-or-nothing permits" is a read followed by an increment, not atomic; fails the exact contract cases |
| **C) A lock around read and write** | Simple | Option 8c of the spike, rejected by R8; two round trips plus a lock per request |

### Chosen Option: **A — timestamp log with compare-and-swap** (recommended, pending the maintainer)

### Rationale

B wins when limits are very large (hundreds of thousands per window), the timestamp log would not fit in an item, and an approximate limit is acceptable; A wins by default because the issue asks for compare-and-swap, R8 names it (ADR-055 R8 rejected a lock around read and write, option 8c, so C is out), and the contract suite of #704 is exact. Choosing B means the maintainer relaxes the issue's criteria "passes the whole contract suite" and "two clients sharing one limit never admit more than the limit in total": B can admit more at a window boundary and its all-or-nothing permits are not atomic. Under A: an absent item is created with `add` (stores only when absent) and updated with `cas` (stores only when the token returned by `gets` is unchanged), never a plain `set`, which could overwrite a concurrent creation and admit more than the limit (Memcached protocol specification); timestamps come from the caller's `TimeProvider` because Memcached has no clock command (AGENTS.md section 3), and the documentation states the clock-skew bound. The item is `[format version: 1 byte][count: 4 bytes][timestamps: 8 bytes each, Unix milliseconds]`; its expiry is the window rounded up to whole seconds plus one second, so the server drops an idle partition without ever expiring a live window early. `MaxWindowEntries` (default 10,000, 80 KB per item, validated at startup) is the one deviation from "any positive limit": above it the store returns `partition_invalid`, and the documentation tells operators to use the Redis family for larger limits. The contract suite of #704 is not on `main` yet, so its largest limit is unknown; PR 3 checks it, and if the suite uses a limit above 10,000 the default is raised to the suite's maximum (an item stays under the 1 MB server limit up to about 130,000 entries). A window longer than 30 days is also `partition_invalid`, because a Memcached expiry above 30 days is read as an absolute Unix time. LRU eviction under memory pressure resets a window (the next request recreates it); this is stated in the documentation, as it is for Redis eviction policies.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The retry policy when a compare-and-swap is lost</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Retry immediately up to `MaxCasAttempts` (default 8)** | No delay on the request path; deterministic tests | Under heavy contention (the contract suite's 64 concurrent acquirers) many callers lose every round and return `Left`, which the decorator turns into fallback to local state, defeating the shared limit |
| **B) Retry up to `MaxCasAttempts` (default 10) with a short jittered delay (0 to `2^min(attempt, 4)` milliseconds, capped by `MaxRetryDelay`, default 8 ms; 0 disables) taken through `TimeProvider`** | Spreads contenders so repeated collisions are less likely (no convergence is guaranteed); still bounded in attempts and in time | A delay on a contended path (bounded by about 70 ms in the worst case with the defaults: nine waits of at most 8 ms); tests need the delay disabled or a fake clock |
| **C) No retry: a lost race is a `Left`** | Simplest | A normal concurrent request becomes a failure and triggers fallback |

### Chosen Option: **B — bounded attempts with jittered delay** (recommended, pending the maintainer)

### Rationale

A wins for a low-contention deployment where one partition is rarely hit by many instances at once and the maintainer wants no delay at all (set `MaxRetryDelay` to 0: B degrades into A); B wins when a partition is hot, which is the case a distributed limiter exists for. C is excluded by the acceptance criterion (a lost race is retried a bounded number of times). When the attempts are exhausted the store returns `Left` with `encina.ratelimit.state_store_failed` (`ErrorCategory.Internal`) and logs nothing itself (the decorator of #704 logs and counts the failure); a caller that cancels while waiting propagates `OperationCanceledException`. With 64 concurrent acquirers on one item each round admits one writer, so a caller can lose every attempt and receive `Left`; that is the designed behavior, not a defect. The unit tests set `MaxRetryDelay` to 0 and a fake seam that loses a chosen number of races. PR 3 runs 64 concurrent acquirers against a real server with the defaults, measures how many receive `Left`, and sizes the defaults of `MaxCasAttempts` and `MaxRetryDelay` from that measurement; the contract subclass accepts `state_store_failed` as an outcome of that single concurrent case, states it in a comment, and still asserts that the admitted total never exceeds the limit.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How outcome counters (success and failure) are kept</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A second item per partition holding bucketed counters, updated by `gets` and `cas` like the window** | One technique, one codec | Every recorded outcome contends on one more item and loses races exactly when traffic is high; a read-modify-write for a plain increment |
| **B) One key per bucket (`window / 10` buckets, as the in-process store) updated with the atomic `incr` (created with `add` on first use, expiry window plus one bucket); a read is one multi-get of at most 10 keys** | `incr` is atomic, so recording never retries and costs one round trip; the same bucket granularity as the in-process store | Up to 10 keys per partition; a race between `add` and `incr` on the first write of a bucket must be handled (an `incr` that finds no key retries `add`, once) |

### Chosen Option: **B — `incr` per bucket** (recommended, pending the maintainer)

### Rationale

A wins when the number of keys matters more than contention (a very large partition count and little traffic per partition); B wins by default because outcomes are recorded on every request, and `incr` is Memcached's own atomic counter, the "best atomic operation" R8 asks for. Counts are exact per bucket and approximate at the window edge by one bucket, the same as the in-process store, which the contract case "outcome counts expire with the window" accepts. The key is `{window key}:o:{bucket index}`, so the length check of Design Choice 5 covers it.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the store is registered</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside `AddEncinaMemcachedCache` with `TryAddSingleton<IRateLimiterStateStore, MemcachedRateLimiterStateStore>()`, as the plan of #704 does for the Redis family** | Same shape for every cache package; an application's own store wins in any registration order (a `TryAdd` before it is skipped, a registration after it replaces it); the issue's acceptance wording | An application that registers two cache providers gets the limiter state of whichever `AddEncina*Cache` ran first, silently |
| **B) A separate `AddEncinaMemcachedRateLimiterState()` that the application calls** | Explicit opt-in; no ambiguity with a second cache provider | One more call to remember; differs from the Redis family |

### Chosen Option: **A — registered by the Memcached extension with `TryAdd`** (recommended, pending the maintainer)

### Rationale

B wins for an application that uses Memcached as a cache and Redis for rate limiting (or the reverse) and wants no order dependence; choosing B means the maintainer relaxes the issue's criterion "registered with `TryAdd`; an application's own store wins in any registration order" (the extension is then the application's own call) and the issue's statement that it has no open design choices. A wins by default for consistency and because resolving the type is free until a limiter asks for `IRateLimiterStateStore`. The documentation names the first-registration rule. The registration also adds the options, their validator and the seam the store resolves, proven by a DI test with `ValidateOnBuild` and `ValidateScopes` in both registration orders against an application-supplied store.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. A composed key longer than 250 bytes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject: the store returns `Left` with `encina.ratelimit.partition_invalid` (`ErrorCategory.Validation`) when the composed key exceeds 250 bytes** | Keys stay readable and injective (the percent-encoded tenant segment and the hex hash never collide); the caller learns about a bad tenant id at once | A very long tenant id cannot be limited on Memcached |
| **B) Hash the tenant segment when the key would be too long** | Every tenant works | Two key forms (readable and hashed) can in principle collide, and a tenant whose id looks like the hashed form breaks injectivity; the tenant namespace is no longer readable |

### Chosen Option: **A — reject** (recommended, pending the maintainer)

### Rationale

B wins when tenant ids are unbounded and an application cannot constrain them; A wins by default because, under the hashed scope that #704 recommends (a 64-character hash), only the prefix and the tenant segment can overflow (a key budget of about 160 bytes for them after the fixed parts and the outcome suffix), and a bounded tenant id length is expected from the tenant rules of #1257. Injectivity of tenant-qualified keys matters more than coverage of pathological ids: no two (tenant, scope) pairs may share a key and a tenant and "no tenant" never do. The store treats the tenant as an opaque string, following the opaque-partition rule of #704, and takes no position on whether `default` is reserved (#1257); if #704 chooses a raw scope instead of the hash, the budget shrinks and the same check applies.

**Class:** Dilemma

</details>

### Smaller decisions (register)

| Id | Decision | Class | Basis |
|----|----------|-------|-------|
| R1 | A client failure or an unexpected reply is `Left` `encina.ratelimit.state_store_failed` (`Internal`); an invalid argument is `Left` `partition_invalid` (`Validation`); cancellation propagates as `OperationCanceledException` and is never a `Left`; messages are constants without keys, scopes or server text | Settled | ADR-062 rules R4 and R5; AGENTS.md section 3 (cancellation is not an error, ADR-052); the issue's acceptance (exhausted retries return `Left`) |
| R2 | The store logs nothing and allocates no EventId: a failure is returned as `Left` and the decorator of #704 logs and counts it. This avoids ids 6959-6979, which the plan of #277 reserves for #723 | Trivial | AGENTS.md section 3 (errors are returned, never swallowed) and section 7 (no id without a log message) |
| R3 | Unit tests drive the store through an internal `IMemcachedVersionedStore` seam that offers the operations of the chosen options, with a fake that loses chosen races; the real client sits behind `EnyimVersionedStore`, covered by the integration flag | Trivial | AGENTS.md section 9 (unit tests mock dependencies; contract and integration tests run real code); the package grants `InternalsVisibleTo` to the test projects (#277 plan) |
| R4 | The store keeps only timestamps and counters, and every item expires with its window, so DSR erasure needs no purge on Memcached; the scope segment is whatever #704 composes | Trivial | GDPR Article 5(1)(c); #704 plan (R8 "no personal data") |
| R5 | The options carry no secret; their validator rejects non-positive numbers and a negative delay | Trivial | Pattern of the options validators of the caching packages |
| R6 | A malformed or unknown-version item is `Left` `state_store_failed` until it expires or `ResetAsync` deletes it; it is never read as an empty window | Trivial | AGENTS.md section 3 (a failed lookup is not reported as success) |

---

## Implementation Phases

### Phase 1: Codec and key helper (`Encina.Caching.Memcached`)

> **Goal**: the pure parts, testable without a server; both types are internal, so this pull request adds no service to register and no public API.

<details>
<summary><strong>Tasks</strong></summary>

1. `RateLimitWindowCodec` (internal static, XML docs): `Encode(IReadOnlyList<long> timestampsMs)` and `TryDecode(ReadOnlySpan<byte>, out List<long>)` for the item format of Design Choice 1; a malformed or unknown-version item decodes to failure (the store then returns `state_store_failed`, decision R6).
2. `MemcachedRateLimiterKeys` (internal static, XML docs): `Window(prefix, partition)` and `OutcomeBucket(windowKey, index)` through `StoreCacheKey`, with the 250-byte check of Design Choice 5 returning `Either<EncinaError, string>`; it uses `RateLimiterPartition` of #704 PR 1.
3. Tests: codec round trip and malformed inputs (including a truncated count and a count larger than the payload), key injectivity cases (tenant versus no tenant, a tenant named `default`, tenants differing by case or by `:`), length boundary at 250 bytes; guard tests for the parameter checks. The manifest rows go through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT: Encina.Caching.Memcached (#277) provides the client registration and the log class; #704 defines IRateLimiterStateStore,
RateLimiterPartition and the key rules (StoreCacheKey, hashed scope, tenant segment only when a tenant is given).
TASK: Add the window codec and the key helper with their tests; log nothing and allocate no EventId.
KEY RULES: injective keys; a key over 250 bytes is Left partition_invalid; never map a null tenant to default; a malformed item is
an error, never an empty window; Either returns; constant messages; no [Obsolete].
REFERENCE FILES: docs/plans/rate-limiter-state-store-implementation-plan-704.md, docs/plans/memcached-cache-provider-implementation-plan-277.md.
```

</details>

### Phase 2: Options, the store, the seam and the registration

> **Goal**: `MemcachedRateLimiterStateStore` passes the unit cases and is registered together with everything it resolves.

<details>
<summary><strong>Tasks</strong></summary>

0. `MemcachedRateLimiterStateStoreOptions` (public sealed, XML docs): `MaxCasAttempts` (10), `MaxRetryDelay` (8 ms), `MaxWindowEntries` (10,000); `MemcachedRateLimiterStateStoreOptionsValidator` (`IValidateOptions`: non-positive numbers and a negative delay are rejected).
1. `IMemcachedVersionedStore` (internal) and `EnyimVersionedStore` (internal sealed, over the Enyim `IMemcachedClient`): `GetWithVersion(key)`, `TryAdd(key, bytes, expiry)`, `TryReplace(key, bytes, expiry, version)` (`cas`), `Increment(key, delta, expiry)` and `Delete(key)`; a client `false` or exception becomes a failure result, cancellation is honoured before each call.
2. `MemcachedRateLimiterStateStore` (public sealed, `IRateLimiterStateStore`): constructor `(IMemcachedVersionedStore store, IOptions<MemcachedRateLimiterStateStoreOptions> options, TimeProvider? timeProvider = null)` (no logger: decision R2), with XML docs. `TryAcquireAsync`: validate (a window above 30 days is `partition_invalid`), build the key, loop up to `MaxCasAttempts`: read the item, trim timestamps older than `now - window`, compare the remaining count plus `permits` with `limit`, on denial return `Acquired = false` with `RetryAfter` from the oldest timestamp (no write), on admission append `permits` timestamps and `TryReplace` (or `TryAdd` when absent); a lost race waits the jittered delay (Design Choice 2) and retries; exhaustion is `state_store_failed`. `RecordOutcomeAsync` and `GetOutcomeCountsAsync` use the bucket keys of Design Choice 3; `ResetAsync` deletes the window item and the bucket keys. Every method keeps cyclomatic complexity at 5 or less (one private method per step) so CRAP stays at or below 10.
3. `ServiceCollectionExtensions.AddEncinaMemcachedCache` (#277): `TryAddSingleton` of the seam, the options with validator through `TryAddEnumerable`, and `IRateLimiterStateStore` (Design Choice 4).
4. Tests: unit cases with the fake seam (admit to the limit then deny with a positive `RetryAfter`; the window slides with `FakeTimeProvider`; permits all-or-nothing; two lost races then success; exhaustion returns `Left`; invalid arguments; a window above 30 days; a malformed item; cancellation propagates; tenant, no tenant and `default` are three partitions); guard tests (constructor null checks; `timeProvider` null is the valid system-clock case); DI test with `ValidateOnBuild` and `ValidateScopes`, registering an application store before and after `AddEncinaMemcachedCache`. PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: Memcached has no scripting: gets returns a version token, add creates only when absent, cas stores only when the token is
unchanged, incr is atomic. The decorator of #704 turns a Left into fallback to local state.
TASK: Implement the seam, the store and the registration; keep the retry loop bounded.
KEY RULES: add to create, cas to update, never set for the window; bounded attempts then Left state_store_failed (Internal);
cancellation propagates and is never a Left; time from TimeProvider; asynchronous client calls only; no key, scope or server text in
messages, logs or tags; TryAddSingleton and TryAddEnumerable, an application store wins in any order.
REFERENCE FILES: docs/plans/rate-limiter-state-store-implementation-plan-704.md (Phases 1 and 2), docs/plans/memcached-cache-provider-implementation-plan-277.md (Phases 2 and 3).
```

</details>

### Phase 3: Contract, integration and justification files

> **Goal**: the whole contract suite passes against a real server and two clients share one limit.

<details>
<summary><strong>Tasks</strong></summary>

1. `MemcachedRateLimiterStateStoreContractTests : RateLimiterStateStoreContractTests` (#704) in `tests/Encina.ContractTests/RateLimiting`, using the Memcached container fixture of #277 and the real store.
2. `MemcachedRateLimiterStateStoreIntegrationTests`: two store instances with separate clients sharing one limit never admit more than the limit in total (single partition, many acquirers, default options); a partition whose item was deleted behind the store's back is recreated; a tenant partition does not affect another.
3. Benchmark and load justification files `{TestProject}/RateLimiting/MemcachedRateLimiterStateStore.md` (justification format of AGENTS.md section 9): the algorithm and its hot-path shape are benchmarked once for the Redis family in #704, and concurrency is covered by the 64-acquirer contract case and the two-client integration test.
4. Manifest rows: contract and integration targets of the store and the adapter (below), with their tests in the same pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT: The abstract contract suite of #704 exists; the Memcached container fixture comes from #277.
TASK: Add the contract subclass, the two-client integration tests and the justification files.
KEY RULES: real store and real server (no seam); shared fixture, no per-class container; FakeTimeProvider where time is
controlled; no Thread.Sleep; tests never swallow exceptions.
REFERENCE FILES: docs/testing/integration-tests.md, tests/Encina.ContractTests (message-broker container contract tests).
```

</details>

### Phase 4: Documentation and finalization

> **Goal**: operators know the limits of the Memcached store.

<details>
<summary><strong>Tasks</strong></summary>

1. The XML docs ship with the types in PRs 1 and 2 (no CS1591 in any PR); their remarks state the item-size cap, eviction, the clock-skew bound and the 250-byte key rule. This phase adds only the following pages.
2. `changelog.d/3995-memcached-rate-limiter-state-store.added.md`; `src/Encina.Caching.Memcached/README.md` and the caching configuration page: the registration order rule (Design Choice 4), `MaxCasAttempts`, `MaxRetryDelay`, `MaxWindowEntries`, and the advice to use the Redis family for very large limits.
3. No ADR: ADR-055 R8 already decides it.
4. Verification: zero warnings, tests, every flag at its target below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT: The feature is code-complete after Phase 3.
TASK: Documentation, changelog fragment, verification.
KEY RULES: never type coverage figures by hand; English only; follow the encina-docs skill; never edit CHANGELOG.md; never name a
private application.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| ADR-055 R8 | Per-cache state store; compare-and-swap in Memcached |
| ADR-054 D3, D4, D6 | One meter per package, tenant tag, no key or client identifier in tags, logs or spans |
| ADR-062 rules R4, R5 | Categories of the errors; cancellation is not a fault |
| Memcached protocol and server documentation | `gets`, `cas`, `add`, `incr`, 250-byte keys, 1 MB items, LRU eviction |
| GDPR Article 5(1)(c) | Data minimisation: hashed scope, no personal data in values |

### Cache provider matrix (AGENTS.md section 5)

| Provider | `IRateLimiterStateStore` | Owner |
|----------|--------------------------|-------|
| Memory (L1) | in-process store | #704 |
| Hybrid (L1+L2) | in-process store | #704 |
| Redis | Lua script | #704 |
| Valkey | inherits the Redis store | #704 |
| Dragonfly | inherits the Redis store | #704 |
| Garnet | inherits the Redis store | #704 |
| KeyDB | inherits the Redis store | #704 |
| Memcached | compare-and-swap and `incr` | this plan |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IRateLimiterStateStore`, `RateLimiterPartition`, `ResilientRateLimiterStateStore`, `RateLimiterStateStoreContractTests` | `Encina` (#704, after it lands) | Contract, fallback, contract suite |
| `StoreCacheKey` | `Encina.Caching` (#695, after it lands) | Injective key building |
| `AddEncinaMemcachedCache`, `MemcachedLogMessages`, package options, Memcached fixture | `Encina.Caching.Memcached` (#277, after it lands) | Client, registration, container |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching.Memcached` (`CachingMemcached` 6950-6979 registered by #277) | none used | The store logs nothing (decision R2). The plan of #277 uses 6950-6958 in `MemcachedLogMessages` and reserves 6959-6979 for #723, so this plan takes no id; a later log message needs an id outside that reservation, assigned by the orchestrator |

### Per-file coverage targets (AGENTS.md section 9)

Per flag: U unit, G guard, C contract, P property, I integration; entered in `.github/coverage-manifest/Encina.Caching.Memcached.json` by the PR in the column, with that PR's tests, never lowered. For an existing file the row is the whole-file target (the manifest value when the PR opens, or the value shown if higher). The contract and integration targets of PR 2 files are set in PR 3, with its tests.

| File | PR | U | G | C | P | I | Justification |
|------|----|---|---|---|---|---|---------------|
| `RateLimitWindowCodec.cs` | 1 | 95 | 30 | 0 | 0 | 0 | Pure encode and decode with malformed inputs; guard tests reach only the argument checks |
| `MemcachedRateLimiterKeys.cs` | 1 | 95 | 30 | 0 | 0 | 0 | Pure key composition and the length boundary; guard reaches only the argument checks |
| `MemcachedRateLimiterStateStoreOptions.cs` | 2 | 90 | 0 | 0 | 0 | 0 | Plain options and defaults; no parameters to guard |
| `MemcachedRateLimiterStateStoreOptionsValidator.cs` | 2 | 95 | 30 | 0 | 0 | 0 | Pure validation, every rejection reachable; guard reaches the null check only |
| `IMemcachedVersionedStore.cs` | 2 | 0 | 0 | 0 | 0 | 0 | Interface, no executable lines |
| `EnyimVersionedStore.cs` | 2 | 0 | 15 | 0 | 0 | 0 | A thin adapter over the Enyim client: only its argument checks are reachable without a server; its other lines get the integration target in PR 3 |
| `MemcachedRateLimiterStateStore.cs` | 2 | 95 | 25 | 0 | 0 | 0 | Fake-seam unit cases cover every branch; guard tests reach the constructor and argument checks only (measured evidence: the package guard target of `Encina.Cdc.json` is 15 and `OutboxCdcHandler.cs` measures 15.3 %) |
| `ServiceCollectionExtensions.cs` (whole file) | 2 | 80 | 15 | 0 | 0 | 0 | The DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`; guard reaches argument checks only; the existing file's other methods are not touched |
| `MemcachedRateLimiterStateStore.cs` | 3 | 95 | 25 | 85 | 0 | 85 | The contract suite and the two-client test run the real store against a server |
| `EnyimVersionedStore.cs` | 3 | 0 | 15 | 0 | 0 | 85 | Every line runs against the real client in the integration tests |

Property is 0 on every row: the store's invariants (never more than the limit) are asserted by the contract suite and the concurrent integration case, and the codec round trip is covered by unit cases, so a separate FsCheck test would add no coverable line.

CRAP stays at or below 10 on every changed method: the store has one private method per step (validate, read, trim, decide, write, wait).

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production, new or edited under `src/` | 8 | PR 1: codec, keys (2); PR 2: options, validator, seam, adapter, store, registration extension (6) |
| PublicAPI and manifests | 4 | PR 1: manifest; PR 2: PublicAPI and manifest; PR 3: manifest |
| Tests | 9 | PR 1: 3 (codec, keys, guard); PR 2: 4 (store unit, validator unit, guard, DI); PR 3: 2 (contract subclass, integration class) |
| Justification `.md` | 2 | PR 3: benchmark and load |
| Docs, changelog | 3 | PR 4 |

The categories add to 26 distinct touches, as do the per-PR counts of the split table: 6 + 12 + 5 + 3.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with nullable reference types. You are implementing issue #3995:
the Memcached implementation of IRateLimiterStateStore (decision R8 of ADR-055) with compare-and-swap. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: Phase 1 adds the window codec and the key helper. Phase 2 adds the options, the version-token
seam, the Enyim adapter, MemcachedRateLimiterStateStore and the registration in AddEncinaMemcachedCache. Phase 3 adds the contract
subclass, the two-client integration test and the justification files. Phase 4 documents. Deliver as the four pull requests of
the Summary, after the #277, #695 and #704 pull requests each one names.

KEY PATTERNS: add to create and cas to update (never set for the window); bounded retries with jitter then Left
state_store_failed; incr for outcome buckets; keys injective and at most 250 bytes (hashed scope, percent-encoded tenant segment,
null tenant never mapped to default); time from TimeProvider; cancellation propagates; constant error messages without keys or
scopes; TryAdd registration that works in any order; DI proof with ValidateOnBuild and ValidateScopes.

REFERENCE FILES: docs/plans/rate-limiter-state-store-implementation-plan-704.md, docs/plans/memcached-cache-provider-implementation-plan-277.md,
docs/architecture/adr/055-resilience-family.md, .github/coverage-manifest/Encina.Caching.Memcached.json (created by #277).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | The feature is cache-backed limiter state, implemented against the Memcached client of #277; `ICacheProvider` is unchanged |
| 2 | OpenTelemetry | ✅ | The counter of #704 is emitted by the decorator with `store` set to the store's type name; no instrument of its own, no key, scope or client identifier in any tag (ADR-054 D6) |
| 3 | Structured Logging | ✅ | The store returns `Left` and the decorator of #704 logs it (its ids 400-401); no EventId is allocated here (decision R2) |
| 4 | Health Checks | ❌ | The Memcached provider has its own check (#277); the state store adds no dependency beyond the same server |
| 5 | Validation | ✅ | Options validator; the contract suite validates window, limit, permits and scope; the key length check |
| 6 | Resilience | ✅ | Bounded retries with jitter inside the store; a Memcached outage falls back to local state through the decorator of #704, no new fallback code |
| 7 | Distributed Locks | ❌ | Compare-and-swap, no lock (ADR-055 R8) |
| 8 | Transactions | ❌ | No persistence; items expire with the window |
| 9 | Idempotency | ❌ | A retried request counts again by design |
| 10 | Multi-Tenancy | ✅ | The partition carries the tenant as an injective percent-encoded segment; the store never gates a missing tenant, the core does (#2414 D3) |
| 11 | Module Isolation | ❌ | Not module scoped |
| 12 | Audit Trail | ❌ | Not an audit event |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #277 (Memcached cache provider) | open, plan on `main` | Provides the package, client registration, log class and container fixture; PR 1 starts after its PRs 2 and 3 |
| #704 (rate limiter state store) | open, plan on `main` | Provides the abstraction, the decorator and the contract suite; PR 2 starts after its PRs 1 and 2 |
| #695 (store cache foundation) | open, plan on `main` | Provides `StoreCacheKey`; PR 1 starts after the PR that adds it |
| #2691 (SPIKE, decision R8) | closed | The decision this plan implements |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the Dilemmas (choices 1 to 5) when work on the issue starts; none is decided here.
2. Publish the plan link as a comment on #3995.
3. Begin PR 1 after the #277, #695 and #704 pull requests it names.
4. Each phase is a self-contained commit; the final commit references `Fixes #3995`.
