# Implementation Plan: Distributed rate limiter on `IRateLimiterStateStore` (coordinated counting, shared error rate, opt-in default)

> **Issue**: [#4542](https://github.com/dlrivada/Encina/issues/4542)
> **Type**: Feature (labels `needs-decision`, `area-caching`, `area-resilience`, `area-rate-limiting`)
> **Complexity**: High (6 phases, 7 pull requests of 14, 10, 14, 8, 3, 6 and 13 files)
> **Estimated Scope**: about 450 lines of production code, about 1,500 lines of tests, documentation and manifests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities
> **Related plans and decisions** (cited by issue number, never by link): #704 (the state store, its fallback decorator and the Redis-family store), #3995 (Memcached store), #277 (Memcached provider), #4380 (creates `Encina.Resilience`), #4382, #728 (resilience telemetry), #740 (tenant partition of today's limiter), #2414 (decisions D1 to D3), #3448 (partition spike, open), #2691 (decision R8, closed), #138 and #139 (other admission strategies of the family)

---

## Summary

Build the limiter that consumes `IRateLimiterStateStore` so that rate limiting is coordinated across application instances, the error rate used by the adaptive rule is shared, and today's local limiter stays the default. #704 delivers only the store, its fallback and its implementations (its Design Choice 9); this plan is the consumer that #704 defers, and it is the "follow-up" #704 names.

State of `origin/main` (read after `git fetch`; every path below was opened):

- **The limiter that exists is per process and synchronous.** `AdaptiveRateLimiter` keeps `ConcurrentDictionary<string, RateLimitBucket> _buckets` (`src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs:23`), so N instances admit N times the configured limit. `IRateLimiter` has `void RecordSuccess` (`IRateLimiter.cs:24`), `void RecordFailure` (`:30`), `GetState` (`:37`) and `Reset` (`:43`): none can await an asynchronous store or return a `Left`. `RateLimitingPipelineBehavior` (`src/Encina.Polly/Behaviors/RateLimitingPipelineBehavior.cs`) keys the bucket by `typeof(TRequest).Name` alone (`:57-58`), denies with an uncoded `EncinaError.New` (`:79`) and records outcomes after `nextStep` (`:98`, `:105`, `:109`).
- **Nothing of the chain is in `src/`.** `grep` finds no `IRateLimiterStateStore` or `RateLimiterState` in `src` (#704 is a plan on `main`, its issue is open), `src/Encina.Resilience` does not exist (#4380 creates it in 24 pull requests; its PR-13 copies `AdaptiveRateLimiter` into it as a stand-in), and the partitioned core of #2414 D1 has no implementation issue (#4380 and #740 say the same). ADR-055 R1 puts the rate limiting core in `Encina.Resilience`, and R8 says that package "depends only on the abstraction, never on a concrete cache".
- **Three recorded decisions shape it.** R8 (ADR-055, 2026-10-24): a state store per cache, a fall back to the local limiter with a log entry, "a request without tenant context is still denied". #2414 D2 = 2a: adaptive throttling is an opt-in decorator with SRE-style probabilistic rejection counting only transient errors. #2414 D3 = 3a: request type by tenant when tenancy is registered, a request without tenant denied unless it opts out explicitly and the opt-out is logged. A #2414 answer says distributed limiting is "a separate spike"; ADR-055 R8 is that spike's outcome and is the later decision.
- **Two conflicts the issue states.** #704's "Normal, Throttled, Recovering" wording versus #2414 D2 (Design Choice 3), and the open partition spike #3448 (Design Choice 4). Both stay Dilemmas.
- **EventIds.** `EventIdRanges.Polly = (3900, 3949)` is registered (`src/Encina/Diagnostics/EventIdRanges.cs:358`). Ids 3900-3921 are used, 3922-3924 belong to #740 and 3925-3930 to #4380; no plan on `main` claims 3931-3949. This plan takes 3931-3933 (Design Choice 1 changes the range with the placement). The `Polly` range moves to 60050-60099 in #4007 OP-2 (the comment at `EventIdRanges.cs:358`), and the ids move with it; the 402-404 alternative is in #704's range, which that migration does not list.

What this plan delivers, in `Encina.Resilience` under `RateLimit/Distributed/` (placement per Design Choice 1): `IDistributedRateLimiter` (asynchronous, `Either`-based), `StateStoreRateLimiter` (sliding window counted across instances through `TryAcquireAsync`), `AdaptiveRejectionRateLimiter` (opt-in decorator over the shared outcome counts), the tenant gate, options with a validator, `AddEncinaDistributedRateLimiting`, telemetry, logs, tests on the five Redis-family products (and Memcached after #3995), a benchmark, a load test, documentation, and the adoption by the rate-limit slot of #4380.

**Affected packages**: `Encina.Resilience` (new limiter; the package comes from #4380), test projects. `Encina` (the store) and the cache packages are #704's and are not changed, except a public marker interface on #704's two stores in PR 1b (Design Choice 8).

**Provider category**: Caching (8). The limiter is store-agnostic; coordination across instances comes from the store of each cache (provider table in Research).

---

## Design Choices

<details>
<summary><strong>1. Where the limiter lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Resilience`, folder `RateLimit/Distributed/`** | ADR-055 R1 and R8: the rate limiting core is in that package and it depends only on the abstraction; the code is written once where #4380's rate-limit slot and #728's meter live | The package does not exist: the work starts after #4380 PR-1 to PR-3 (project, options, EventId mapping) and the adoption needs PR-11 and PR-13 |
| **B) Core `Encina`, namespace `Encina.RateLimiting`, next to the store of #704** | Buildable as soon as #704 PR 1 and PR 2 are on `main`; ids 402-404 of #704's range `RateLimiterState (400, 419)` are free | Deviates from ADR-055 R1 and R8; the assembly and namespace move later; the core grows a resilience surface |
| **C) `Encina.Polly`** | Exists today | #4380 removes the package; the work is discarded |

### Chosen Option: **A - `Encina.Resilience`** (recommended, pending the maintainer)

### Rationale

- B wins if the maintainer needs a coordinated limiter before #4380's first three pull requests land and accepts the move; C wins only if #4380 is cancelled. A wins because ADR-055 names the package and nothing is rewritten.
- Under B the log ids are 402-404 (inside #704's range, mapped to `Encina` by #704 PR 2) and the meter is the core's; the rest of the plan is unchanged.
- Class: Dilemma

</details>

<details>
<summary><strong>2. The contract: an asynchronous `Either` interface, a BCL `RateLimiter`, or `IRateLimiter`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new `IDistributedRateLimiter`: `AcquireAsync(target, policy, ct)` and `RecordOutcomeAsync(target, policy, outcome, ct)`, both `ValueTask<Either<EncinaError, T>>`; a denial is `Right(decision)` with `RetryAfter`, a failure is `Left`** | Carries the store's `Left` and the tenant gate's `Left` without exceptions (ROP); records outcomes asynchronously with errors visible | The Polly rate-limiter slot of #4380 needs a small adapter (PR 6); a new public interface |
| **B) A subclass of `System.Threading.RateLimiting.RateLimiter`** | Plugs into Polly's `RateLimiterStrategyOptions` directly; the seam #139 uses | `AttemptAcquire` is synchronous (it cannot await the store); a `RateLimitLease` cannot carry a `Left`, so a store failure becomes an exception; the API has no place to record outcomes, so the adaptive rule is impossible |
| **C) Implement today's `IRateLimiter`** | The existing behavior would use it unchanged | Its recording methods are `void` (`IRateLimiter.cs:24,30`): they could only block on the store or drop the error, both against AGENTS.md section 3 |

### Chosen Option: **A - an asynchronous `Either` contract** (recommended, pending the maintainer)

### Rationale

- B wins if the maintainer wants the distributed limiter to be one more `RateLimiter` for any Polly user and gives up the shared error rate; C wins only for a quick wire-up that #4380 and #2414 D1 replace. A wins because acceptance needs a failing store to be a coded error and outcomes to reach the store.
- Class: Dilemma

</details>

<details>
<summary><strong>3. The adaptive rule: the three-state machine of #704 or SRE-style probabilistic rejection (the issue's own Dilemma 1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) SRE-style rejection: with `requests = successes + failures` and `accepts = successes` over the window, reject with probability `max(0, (requests - K * accepts) / (requests + 1))` when `requests` is at least a minimum throughput (`K` default 2.0, minimum throughput default 10); a rejected request consumes no permit and is not recorded as an outcome** | #2414 D2 = 2a (recorded decision); stateless, so it works on the shared counts with no shared state machine; smooth degradation; no flapping between stages | Probabilistic: a single request is not reproducible; no named stages to log |
| **B) Normal, Throttled (10 percent capacity), Recovering, as `AdaptiveRateLimiter` has today (`RateLimitBucket`, `AdaptiveRateLimiter.cs:214,235`)** | Predictable stages and a log line per change (the wording of #704) | The state itself is not in the store (`IRateLimiterStateStore` has counters only), so instances derive it from the same counts but time their recovery separately and can disagree for up to a window; contradicts D2 |

### Chosen Option: **A - SRE-style rejection** (recommended, pending the maintainer)

### Rationale

- B wins when operators want predictable stages and a log entry at every transition (the three events then take ids 3934-3935 of the same range, or 405-406 under Design Choice 1 option B); A wins because it is the recorded decision and the only one that is exact on shared counters. Both read `GetOutcomeCountsAsync` and write `RecordOutcomeAsync`, so the store does not change.
- `Failure` means a transient failure only (#2414 D2 and the accounting rule of #2274, open): classifying an error as transient is the consumer's job; the limiter documents it and never classifies. A rejection draws a random number from `Random.Shared` (not a security use); an internal constructor takes a `Func<double>` for tests.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Who composes the partition (the issue's own Dilemma 2, the question of #3448)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The caller composes a `DistributedRateLimitTarget(Scope, TenantId, TenantMode)`; the limiter builds `RateLimiterPartition(tenantId?, scope)` and enforces the tenant mode (Design Choice 6); `TenantMode` is `Single` (no tenant dimension), `Required` or `Agnostic` (explicit opt-out)** | Independent of #3448: whatever it decides (type, tenant, principal, edge), only the caller changes; the store already takes an opaque scope | The limiter cannot check what the scope means |
| **B) The limiter computes request type by tenant itself** | One place for the rule of #2414 D3 | Encodes one answer to #3448 in the limiter; a second partition (a principal, a client IP) needs a change here |

### Chosen Option: **A - the caller composes the target** (recommended, pending the maintainer)

### Rationale

- B wins if the maintainer closes #3448 with the type-by-tenant partition and wants no second place that could differ; A wins while #3448 and #740 are open. The limiter never maps "no tenant" to the tenant `default`. `default` is a reserved tenant segment (the single-tenant default tenant is the tenant resolver's job); a tenant id equal to `default` is passed to the store as given and the store keeps a tenant, no tenant and `default` apart (#704: tuple state in memory, the injective `StoreCacheKey` encoding for caches); this plan's contract tests assert the three independent partitions at the limiter level. That convention is stated here and recommended, pending the maintainer.
- Class: Dilemma

</details>

<details>
<summary><strong>5. A store failure without the fallback never admits</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not admitted: the store's `Left` (`state_store_failed`, `Internal`, ADR-062 R4) is returned and logged; with `AddEncinaRateLimiterStateFallback` (#704) the decorator serves the call locally and this path is not reached** | Fail closed | A cache outage without the fallback registered denies the limited requests |
| **B) Fail open: admit while the store is down** | Never blocks traffic | Disables a security control during an outage |

### Chosen Option: **A - never admit on a store failure**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (fail-closed gates; errors are never swallowed), #2414 D3 (the maintainer treats rate limiting as a security control) and ADR-055 R8 (the availability mechanism is the fallback, not admission). The claim is only that a store failure never becomes an admission, whatever the contract of Design Choice 2: under option A it is the store's `Left` (the shape the table shows), under B or C it is a rejected lease or an exception, never an admission. A database or network failure of a cache is a provider failure, `Internal` (ADR-062 R4); R7 (`Unavailable`) covers outbound codes only.

</details>

<details>
<summary><strong>6. A request that needs a tenant and has none is never admitted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) When tenancy is registered (`MultiTenancyMarker`), the consumer builds `TenantMode.Required`; a null or blank `TenantId` is denied (under Design Choice 2 option A a `Left`, `ErrorCategory.InvalidRequest`) before any store call, so the denial holds on the fallback and with any store; `Agnostic` is the explicit opt-out and is counted with the tag `tenant_mode=agnostic`; the consumer logs it by request type; `Single` exists only for applications without the marker** | The acceptance "a request without tenant context is still denied" | A denial carrier for callers |
| **B) Map a missing tenant to `default` or to the global partition (or build `Single` when the tenant cannot be resolved)** | No error | One tenant's traffic shares a bucket with every request without tenant; hides a missing context |

### Chosen Option: **A - deny before the store; never build `Single` when the marker is registered**

### Rationale

- **Class:** Settled
- **Settled by:** #2414 D3 = 3a (denied without tenant unless an explicit, logged opt-out), ADR-055 R8 ("a request without tenant context is still denied"), SPEC-002 DEC-006 and AGENTS.md section 3 (security gates fail closed when context is missing), ADR-062 R3 (a missing tenant is `InvalidRequest`). The claim is that a request without tenant is never admitted when tenancy is registered, under every option of Design Choices 2 and 4: the carrier of the denial is the `Left` under option A of Design Choice 2 and a failed lease or exception under B or C; the consumer of PR 6 decides the mode from `MultiTenancyMarker`, never from whether a resolver is available. The limiter logs no scope and no tenant value.

</details>

<details>
<summary><strong>7. Granularity of the opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registration level: nothing changes until `AddEncinaDistributedRateLimiting()` is called; once registered, the rate-limit slot uses it for every `[RateLimit]` request type** | The issue's sample; pay-for-what-you-use (AGENTS.md section 1); one switch | A type that protects a per-instance resource is also coordinated |
| **B) Registration plus a per-type flag (`Distributed = true` on the rate-limit options of a type, #4380's R3 model)** | Each limit says whether it protects a shared quota or a local resource | Two switches; a flag that is on without a registration needs a startup error |

### Chosen Option: **A - registration level** (recommended, pending the maintainer)

### Rationale

- B wins when one application mixes shared downstream quotas with per-instance protections; A wins because it is what the issue asks and B can be added to the per-type options later. The local `AdaptiveRateLimiter` stays the default: the registration is the only thing that selects the new path.
- Class: Dilemma

</details>

<details>
<summary><strong>8. A process-local store behind the distributed limiter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Log once (Warning, id 3933) when the resolved store is process-local: a public marker interface `IProcessLocalRateLimiterStateStore` in core `Encina`, implemented by `InProcessRateLimiterStateStore` and by the fallback decorator when its primary implements it; the limiter tests `store is IProcessLocalRateLimiterStateStore` (no `InternalsVisibleTo` is needed and none is added)** | Memory and Hybrid caches are per instance by decision R8, so a misconfigured production is visible | Noise in development and tests; a public marker and edits to two #704 files, done in PR 1b after #704 PR 2 |
| **B) Say nothing** | No code | The limit is silently N times the configured one |
| **C) Fail startup** | Cannot misconfigure | Blocks legitimate single-instance and test use |

### Chosen Option: **A - one warning** (recommended, pending the maintainer)

### Rationale

- B wins if the documentation page is trusted to carry it; C wins for a deployment that forbids local state; A wins as the cheapest visible signal.
- Class: Dilemma

</details>

<details>
<summary><strong>9. Where the outcome of a request is recorded after the handler ran</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A small post-call observer contract in `Encina.Resilience`, invoked by the admission behavior of #4380 PR-4 after `nextStep`, implemented by the adoption (PR 6)** | One recording point for every admission strategy (#138 and #139 need the outcome too); keeps the Polly pipeline free of recording | #4380 PR-1 defines only `IResilienceCallContributor`, a call before the handler; the observer is a new contract in #4380's surface |
| **B) The rate-limit behavior records itself around `nextStep`, as `RateLimitingPipelineBehavior` does today (`:98`, `:105`, `:109`)** | No new contract | A second behavior around the handler, against the two-behavior design of ADR-055 R2 |

### Chosen Option: **A - a post-call observer** (recommended, pending the maintainer)

### Rationale

- B wins if #4380 keeps a rate-limit behavior of its own; A wins when the admission behavior is the single place. It matters only to PR 6; PR 1 to PR 5 do not depend on it.
- Class: Dilemma

</details>

<details>
<summary><strong>10. Freshness of the shared counts</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Read `GetOutcomeCountsAsync` on every adaptive call, before `TryAcquireAsync`** | Exact shared error rate; no staleness rules | Two store round trips per adaptive request on the Redis family |
| **B) Cache the counts per partition for a short interval (an option, default 1 second)** | One round trip most of the time | A stale rate (up to the interval); a per-partition cache that needs eviction |

### Chosen Option: **A - read every time** (recommended, pending the maintainer)

### Rationale

- B wins if the benchmark of PR 5 shows the second round trip is the cost that matters; A wins first because it is exact and a cache is additive. Adaptive rejection is off unless enabled (Design Choice 3), so non-adaptive requests pay one round trip.
- Class: Dilemma

</details>

<details>
<summary><strong>11. Cancellation, telemetry, logs and names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cancellation propagates as `OperationCanceledException`, never a `Left` and never counted; one counter `encina.resilience.rate_limiter.distributed.decisions` on the package `Meter("Encina.Resilience")` with tags `decision` (`allowed`, `limit_reached`, `adaptive_rejected`, `store_failed`, `tenant_missing`, `invalid`) and `tenant_mode` (`single`, `tenant`, `agnostic`), and a tenant tag only when a tenant is present; logs by error code or exception type only** | Follows the conventions | The tenant tag raises cardinality (documented by #906) |
| **B) No tenant tag** | No cardinality risk | Breaks the rule when multi-tenancy is on |

### Chosen Option: **A - the conventions**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (cancellation is not an error, as in ADR-052; `EncinaError.Message` and the scope never reach logs or tags), ADR-054 D3, D4 and D6 (one meter per package, tenant tag when multi-tenancy is on, no key or client identifier in telemetry) and ADR-055 R5 (`Meter("Encina.Resilience")`). Which class holds the `Meter` instance is not part of this choice; it is a prerequisite (#728) listed below.

</details>

<details>
<summary><strong>12. The error code of a missing tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `encina.tenancy.tenant_required` (`InvalidRequest`, ADR-062 R3), the code the plans of #740, #1257 and #760 name for the same condition; the constant is defined by the first of those pull requests to land and this plan requires it (prerequisite); the limiter reuses it and defines no code of its own** | One code and one client handling for "no tenant"; no new public name | Waits for that constant (it is not in `src` today; `Encina.Tenancy` has `tenancy.tenant_required` with another prefix, `TenancyErrors.TenantRequiredCode`) |
| **B) A code of its own, `encina.ratelimit.tenant_required` (`InvalidRequest`)** | No dependency; the limiter is usable without the tenant plans | Two codes for one condition |

### Chosen Option: **A - reuse the tenancy code** (recommended, pending the maintainer)

### Rationale

- B wins if the limiter ships before any tenant plan; A wins because the condition is one. The error is built in `DistributedRateLimiterErrors.TenantRequired()` only under B; under A that class is not created.
- Class: Dilemma

</details>

---

## Implementation Phases

Every pull request is at most 15 files and 1,500 lines by the file names below, builds alone, and registers what it introduces. The prerequisites of each are in "Prerequisites & Dependencies".

### Phase 1: Contract, counting limiter, tenant gate, registration (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 14 files (13 under the recommended answer of Design Choice 12, which drops row 4; folder `src/Encina.Resilience/RateLimit/Distributed/`, called `D/` below):

| # | File | Change |
|---|------|--------|
| 1 | `D/IDistributedRateLimiter.cs` | new (interface and records `DistributedRateLimitTarget`, `TenantMode`, `DistributedRateLimitPolicy`, `DistributedRateLimitDecision`, `DistributedRateLimitReason`) |
| 2 | `D/DistributedRateLimitPartitions.cs` | new (internal static: target to `RateLimiterPartition`, the tenant gate) |
| 3 | `D/StateStoreRateLimiter.cs` | new (public sealed) |
| 4 | `D/DistributedRateLimiterErrors.cs` | new only under Design Choice 12 option B (public static: `TenantRequired()`); dropped under the recommended option A |
| 5 | `D/DistributedRateLimiterLogMessages.cs` | new (internal; ids 3931-3932; 3933 is added by PR 1b) |
| 6 | `D/DistributedRateLimiterTelemetry.cs` | new (internal; the counter of Design Choice 11) |
| 7 | `D/DistributedRateLimiterServiceCollectionExtensions.cs` | new (`AddEncinaDistributedRateLimiting`) |
| 8 | `src/Encina.Resilience/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Resilience/RateLimit/StateStoreRateLimiterTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Resilience/RateLimit/DistributedRateLimiterRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 11 | `tests/Encina.GuardTests/Resilience/RateLimit/StateStoreRateLimiterGuardTests.cs` | new |
| 12 | `tests/Encina.ContractTests/Resilience/RateLimit/DistributedRateLimiterContractTests.cs` | new (abstract suite; the counting limiter over `InProcessRateLimiterStateStore` is its first subclass) |
| 13 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (`ProjectReference` to `Encina.Resilience`; the project references neither it nor `Encina.Polly` today) |
| 14 | `.github/coverage-manifest/Encina.Resilience.json` | modify (`mechanical-fixer`; the manifest is created by #4380 PR-1) |

Tasks:

1. **Create** the contract. `AcquireAsync(DistributedRateLimitTarget target, DistributedRateLimitPolicy policy, CancellationToken cancellationToken = default)` returns `ValueTask<Either<EncinaError, DistributedRateLimitDecision>>`; `RecordOutcomeAsync(target, policy, RateLimiterOutcome outcome, cancellationToken)` returns `ValueTask<Either<EncinaError, Unit>>` (`RateLimiterOutcome` is #704's). `DistributedRateLimitPolicy(TimeSpan Window, int Limit, bool Adaptive = false)`; `DistributedRateLimitDecision(bool Allowed, DistributedRateLimitReason Reason, int CurrentCount, int Limit, TimeSpan? RetryAfter)`; `DistributedRateLimitReason { Allowed, LimitReached, AdaptiveRejection }`. XML docs: a denial is `Right`, never an error; `Failure` means a transient failure the consumer classified; a denied request is not recorded.
2. **Create** `DistributedRateLimitPartitions.TryCreate(target)`: `Single` and `Agnostic` give `RateLimiterPartition(null, scope)`; `Required` with a null or blank tenant returns `Left` with `encina.tenancy.tenant_required` (`ErrorCategory.InvalidRequest`, ADR-062 R3, constant message; built by the tenancy plans' factory under Design Choice 12 option A, by `DistributedRateLimiterErrors.TenantRequired()` as `encina.ratelimit.tenant_required` under option B); otherwise `RateLimiterPartition(tenantId, scope)` with the id as given.
3. **Create** `StateStoreRateLimiter(IRateLimiterStateStore store, ILogger<StateStoreRateLimiter> logger)`: builds the partition (a `Left` is returned and counted as `tenant_missing`), calls `store.TryAcquireAsync(partition, policy.Window, policy.Limit, 1, ct)`, maps `SlidingWindowResult` to the decision; a `Left` from the store is returned as it is, counted (`store_failed`, or `invalid` for `partition_invalid`) and logged with id 3931 by error code (not for `partition_invalid`, a caller error). `RecordOutcomeAsync` calls `store.RecordOutcomeAsync`; a `Left` is returned and logged with id 3932. The process-local warning (id 3933) is PR 1b. Time comes from the store's own `TimeProvider`; the limiter reads no clock.
4. **Create** `AddEncinaDistributedRateLimiting(this IServiceCollection services)`: `ArgumentNullException.ThrowIfNull(services)`; `TryAddSingleton<IDistributedRateLimiter, StateStoreRateLimiter>()`. It resolves the store by constructor injection, so a missing `IRateLimiterStateStore` fails at the first resolve and in `ValidateOnBuild`, in any registration order; an application's own `IDistributedRateLimiter` wins. Singletons only (no scoped service is captured).
5. **Tests**: unit (admit up to the limit then deny with a positive `RetryAfter`; tenant `Required` without tenant returns `tenant_required` and never reaches the store (a substitute store asserts zero calls) and also when the store is the fallback decorator; a failing store returns its `Left` and logs once with no scope in the message; `partition_invalid` is returned and not logged; cancellation propagates and logs nothing); contract (tenant `A`, no tenant and the tenant `default` are three independent limits; two scopes of one tenant are independent; 64 concurrent acquirers never admit more than the limit); guard, all in `StateStoreRateLimiterGuardTests.cs` (null store and logger of the constructor, null target and policy of both methods, `DistributedRateLimitPartitions.TryCreate(null)`, `AddEncinaDistributedRateLimiting(null)`; a nullable parameter that accepts null gets a valid-result test).
6. **PublicAPI and manifest** through `mechanical-fixer`. No changelog fragment here (PR 2 carries it, with the options).

**PR 1b (the process-local warning, Design Choice 8)**, after PR 1 and #704 PR 2, holds exactly these 10 files: `src/Encina/RateLimiting/IProcessLocalRateLimiterStateStore.cs` (new public marker), `src/Encina/RateLimiting/InProcessRateLimiterStateStore.cs` and `ResilientRateLimiterStateStore.cs` (modify: both implement the marker; the decorator's `IsProcessLocal` answers from its primary), `src/Encina/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json`, `tests/Encina.UnitTests/RateLimiting/ProcessLocalMarkerTests.cs` (new), `D/StateStoreRateLimiter.cs` and `D/DistributedRateLimiterLogMessages.cs` (modify: the constructor logs 3933 once), `tests/Encina.UnitTests/Resilience/RateLimit/StateStoreRateLimiterTests.cs` (modify: the warning appears once and not for a shared store) and `.github/coverage-manifest/Encina.Resilience.json`. The marker has one property, `bool IsProcessLocal { get; }`, so the decorator answers from its primary and no `InternalsVisibleTo` is added.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4542. #704 (IRateLimiterStateStore, InProcessRateLimiterStateStore, the fallback
decorator) and #4380 PR-1 to PR-3 (the Encina.Resilience project, its EventId mapping) are merged.

TASK:
1. Create IDistributedRateLimiter and its records, DistributedRateLimitPartitions, StateStoreRateLimiter, DistributedRateLimiterErrors, the log
   class (ids 3931-3932), the telemetry counter and AddEncinaDistributedRateLimiting in src/Encina.Resilience/RateLimit/Distributed.
2. Write the unit, DI, guard and contract tests (the contract suite is abstract; the counting limiter over the in-process store is its first subclass).
3. Ask mechanical-fixer for the PublicAPI lines and the manifest.

KEY RULES:
- ROP: a denial is Right(decision); a store failure or a missing tenant is Left; cancellation is an OperationCanceledException, never a Left.
- A request with TenantMode.Required and no tenant is denied before any store call. Never map no tenant to "default".
- No scope, tenant value, key or EncinaError.Message in a log or tag; exceptions through ForLogging().
- The fallback is #704's decorator; this limiter never admits on a store failure.
- Singletons only; the registration must build with ValidateOnBuild and ValidateScopes. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Polly/RateLimiting/IRateLimiter.cs and AdaptiveRateLimiter.cs (the behavior being replaced)
- docs/architecture/adr/055-resilience-family.md (R1, R8), docs/architecture/adr/062-error-category-contract.md
- docs/plans/rate-limiter-state-store-implementation-plan-704.md (the store contract)
```

</details>

### Phase 2: Adaptive rejection, options and registration of the decorator (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `D/AdaptiveRejectionRateLimiter.cs` | new (public sealed decorator) |
| 2 | `D/DistributedRateLimiterOptions.cs` | new (`Adaptive.Enabled` false, `Adaptive.Multiplier` 2.0, `Adaptive.MinimumThroughput` 10) |
| 3 | `D/DistributedRateLimiterOptionsValidator.cs` | new (internal `IValidateOptions`) |
| 4 | `D/DistributedRateLimiterServiceCollectionExtensions.cs` | modify (the overload with `Action<DistributedRateLimiterOptions>? configure`) |
| 5 | `src/Encina.Resilience/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Resilience/RateLimit/AdaptiveRejectionRateLimiterTests.cs` | new |
| 7 | `tests/Encina.UnitTests/Resilience/RateLimit/DistributedRateLimiterOptionsTests.cs` | new (validator) |
| 8 | `tests/Encina.UnitTests/Resilience/RateLimit/DistributedRateLimiterRegistrationTests.cs` | modify |
| 9 | `tests/Encina.GuardTests/Resilience/RateLimit/AdaptiveRejectionRateLimiterGuardTests.cs` | new |
| 10 | `tests/Encina.ContractTests/Resilience/RateLimit/DistributedRateLimiterContractTests.cs` | modify (adds the adaptive subclass) |
| 11 | `tests/Encina.PropertyTests/Resilience/RateLimit/DistributedRateLimiterProperties.cs` | new |
| 12 | `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` | modify (`ProjectReference` to `Encina.Resilience`) |
| 13 | `.github/coverage-manifest/Encina.Resilience.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/4542-distributed-rate-limiter.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `AdaptiveRejectionRateLimiter(IDistributedRateLimiter inner, IRateLimiterStateStore store, IOptions<DistributedRateLimiterOptions> options, ILogger<AdaptiveRejectionRateLimiter> logger)`. `AcquireAsync`: when `policy.Adaptive` is false, call `inner`. Otherwise build the partition first (`tenant_required` before any read), read `store.GetOutcomeCountsAsync`, and on a `Left` return it (counted, logged 3931). With `requests = Successes + Failures` below `MinimumThroughput` call `inner`; otherwise compute the probability of Design Choice 3 and, when the draw is below it, return `Right(decision)` with `Reason = AdaptiveRejection`, `Allowed = false`, consuming no permit; else call `inner`. `RecordOutcomeAsync` is delegated to `inner`.
2. **Create** the options (no secret, so no `ToString` override or `[JsonIgnore]` is needed) and the validator: `Multiplier` at least 1.0 and finite, `MinimumThroughput` at least 1; a failure fails startup.
3. **Modify** the registration: `AddEncinaDistributedRateLimiting(Action<DistributedRateLimiterOptions>? configure = null)` adds `Configure`, the validator as `IValidateOptions<DistributedRateLimiterOptions>` with `ValidateOnStart`, `StateStoreRateLimiter` as a singleton, and `TryAddSingleton<IDistributedRateLimiter>` through a factory that returns the decorator when `Adaptive.Enabled` is true and the counting limiter otherwise (a non-generic factory, the application's own registration wins).
4. **Tests**: the probability for fixed counts (no rejection below the minimum throughput or when failures are low; rejection near 1.0 when everything fails; the injected draw decides); a rejected request calls neither `TryAcquireAsync` nor `RecordOutcomeAsync`; a `Left` of the counts read is returned and logged; `tenant_required` before any read; the shared error rate: two limiters over one in-process store, failures recorded through one reduce admissions through the other (unit, with the real store); the validator cases; the DI test for both modes; property (`DistributedRateLimiterProperties.cs`, two properties): (1) for any counts the probability is in [0, 1], non-decreasing in failures and zero when `failures <= (K - 1) * successes`; (2) the issue's property: for random interleavings of acquires over two `StateStoreRateLimiter` instances sharing one in-process store and fake-clock advances, the admitted count inside any window never exceeds the limit. Guard cases in `AdaptiveRejectionRateLimiterGuardTests.cs`: the constructor's four parameters, null target and policy, and the validator called with null options.
5. **PublicAPI, manifest and the changelog fragment** (`added`: "Added `IDistributedRateLimiter`, the state-store limiter and the opt-in adaptive rejection decorator, registered by `AddEncinaDistributedRateLimiting`") through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
PR 1 of issue #4542 is merged: IDistributedRateLimiter, StateStoreRateLimiter, the tenant gate and AddEncinaDistributedRateLimiting exist.
Decision #2414 D2: adaptive throttling is an opt-in decorator with SRE-style probabilistic rejection counting transient errors only.

TASK:
Create AdaptiveRejectionRateLimiter, the options and validator, extend the registration, and write the unit, guard, contract-subclass and property tests.
Ask mechanical-fixer for the PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- Build the partition (tenant gate) before reading the counts. A rejected request consumes no permit and is not recorded as an outcome.
- Registration completeness: options, validator (ValidateOnStart), both limiters; a DI test with ValidateOnBuild and ValidateScopes proves it; IOptions<T>, never T.
- A store Left is returned and logged by code; cancellation propagates. Randomness only through a Func<double> seam in an internal constructor.
- Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs (the thresholds the old limiter uses)
- src/Encina.Resilience/RateLimit/Distributed (PR 1)
```

</details>

### Phase 3: Integration over the Redis family and Memcached (pull requests 3 and 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 8 files (the product fixtures and collections are #704 PR 5's):

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Infrastructure/Caching/DistributedRateLimiterIntegrationSuite.cs` | new (abstract) |
| 2-6 | `tests/Encina.IntegrationTests/Infrastructure/Caching/{Redis,Valkey,Dragonfly,Garnet,KeyDb}DistributedRateLimiterTests.cs` | new (5, one thin class per product in the product's collection) |
| 7 | `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` | modify (`ProjectReference` to `Encina.Resilience` when #4380 has not added it; `Encina.Caching.Redis` is already referenced) |
| 8 | `.github/coverage-manifest/Encina.Resilience.json` | modify (`mechanical-fixer`: the integration targets) |

Scenarios, once per product: two limiters over two connections admit at most the limit in total; tenant `A`, no tenant and the tenant `default` have separate limits on a real server; a request with `TenantMode.Required` and no tenant is denied before the server is touched; with the fallback decorator and a killed connection the limiter keeps enforcing the limit locally and recovers (the decorator is #704's, the limiter on top is this plan's); with adaptive rejection on, failures recorded through the first limiter reduce admissions through the second (the shared error rate); the keys expire with the window. A product that cannot pass a behavior is reported and not worked around (AGENTS.md section 5).

PR 4 (conditional on #3995 and #277 on `main`) holds exactly 3 files: `tests/Encina.IntegrationTests/Infrastructure/Caching/MemcachedDistributedRateLimiterTests.cs` (the same suite on the Memcached container of #277's fixture), the `Encina.IntegrationTests.csproj` edit if the Memcached project reference is missing, and the manifest row. Memcached coordinates through #3995's compare-and-swap store, so the limiter code does not change.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #4542 are merged, and so are PR 1 to PR 5 of #704 (the Redis-family store, the four extra product fixtures and collections).

TASK:
Write DistributedRateLimiterIntegrationSuite (abstract) and one thin class per product that supplies the connection of its existing fixture.
Ask mechanical-fixer for the integration targets.

KEY RULES:
- Shared collection fixtures only; never a per-class container; FakeTimeProvider for windows, no Thread.Sleep.
- Tests execute the real limiter, the real store and a real server; no mocks. A product that fails a behavior is reported, never skipped.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Caching (the #704 suite and fixtures)
- docs/plans/rate-limiter-state-store-implementation-plan-704.md (Phase 5)
```

</details>

### Phase 4: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry** (`DistributedRateLimiterTelemetry`, internal): the counter and tags of Design Choice 11 on the package meter held by #728; the tenant tag only when a tenant is present (ADR-054 D4, cardinality documented by #906); no scope, key hash or client identifier (ADR-054 D6). The denial counter `encina.ratelimit.exceeded` of #2414 is emitted by the consumer that maps a denial to its error (PR 6), not by the limiter.
2. **Logs** (`DistributedRateLimiterLogMessages`, `[LoggerMessage]`): 3931 state store failed, request not admitted (Warning, error code only), 3932 outcome recording failed (Warning, error code only), 3933 process-local store behind the distributed limiter (Warning, once; PR 1b). Ids packed, inside the registered `Polly` range (3900-3949); `Encina.Resilience` is mapped to it by #4380 PR-3 in `EncinaEventIdAllocationTests`, so no registry edit and no new range. Under Design Choice 1 option B the ids are 402-404 in #704's `RateLimiterState (400, 419)`.
3. **Error codes**: the missing-tenant code is `encina.tenancy.tenant_required` (`InvalidRequest`, ADR-062 R3; Design Choice 12 option A, option B would add `encina.ratelimit.tenant_required` with the same category), passed with the category. The codes `state_store_failed` (`Internal`, R4) and `partition_invalid` (`Validation`) are #704's and are returned unchanged. No `FailedPrecondition` (not allowed before #3420) and no new denial code: the consumer uses `encina.ratelimit.exceeded` (`EncinaErrorCodes.cs:93`, `RateLimited`) with `EncinaErrorMetadataKeys.RetryAfter` (`EncinaErrorMetadataKeys.cs:22`).
4. **Coverage targets** (per file, per flag; each value is entered by the pull request named in brackets, with the tests of that pull request; baseline: none of these files exists and `.github/coverage-manifest/Encina.Resilience.json` does not exist on `main` today, #4380 PR-1 creates it, so the package-level values are read from it when each PR opens with `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications` and the per-flag runs of AGENTS.md section 9):

| File | unit | guard | contract | property | integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `StateStoreRateLimiter.cs` | 95 [1] | 30 [1] | 80 [1] | 60 [2] | 70 [3] | Every branch unit tested with a substitute store; guard tests reach the constructor and argument checks; the contract suite reaches admit, deny and tenant paths; the integration run reaches a real store and a real failure |
| `AdaptiveRejectionRateLimiter.cs` | 95 [2] | 25 [2] | 70 [2] | 60 [2] | 60 [3] | The probability and every early return are unit tested with an injected draw; guard tests reach the constructor checks; the property covers the formula; two limiters share counts on a real server |
| `DistributedRateLimitPartitions.cs` | 100 [1] | 15 [1] | 0 | 0 | 60 [3] | Pure tenant gate with four branches; one guard test passes a null target; the contract and property suites reach it through the limiters, and a contract target of 0 is allowed because the type is internal and has no contract of its own |
| `IDistributedRateLimiter.cs` | 90 [1] | 0 | 0 | 0 | 0 | Records and an interface: no executable parameter checks; the records are asserted by the limiter tests |
| `DistributedRateLimiterErrors.cs` (only under Design Choice 12 option B) | 100 [1] | 0 | 0 | 0 | 0 | One parameterless factory; the guard flag has nothing to check |
| `IProcessLocalRateLimiterStateStore.cs` (core, PR 1b) | 0 | 0 | 0 | 0 | 0 | An interface with one property: no executable code; its implementations are covered below |
| `InProcessRateLimiterStateStore.cs`, `ResilientRateLimiterStateStore.cs` (core, PR 1b) | rows set by #704 (no row on `main` today: the files are not there yet); this PR adds none and lowers none | | | | | The added `IsProcessLocal` property is asserted by `ProcessLocalMarkerTests.cs` |
| `DistributedRateLimitAdapter.cs` (PR 6) | 95 [6] | 25 [6] | 0 | 0 | 60 [6] | The mapping of a decision, a denial and a store `Left` unit tested; guard tests reach the constructor and null checks; the two-host test runs it on a real server |
| `DistributedRateLimitStoreException.cs` (PR 6) | 100 [6] | 0 | 0 | 0 | 0 | An internal exception carrying one `EncinaError`; no validated parameter |
| `DistributedRateLimitTargets.cs` (PR 6) | 100 [6] | 15 [6] | 0 | 0 | 60 [6] | A pure mapping of attribute and request to target and policy; one guard test passes nulls; reached by the end-to-end test |
| `IResilienceOutcomeObserver.cs` (PR 6) | 0 | 0 | 0 | 0 | 0 | An interface: no executable code |
| Files of #4380 touched in PR 6 (`RateLimitStrategyFactory.cs`, `RateLimiterRejectionMapper.cs`, the admission behavior) | targets set by #4380 (no row on `main`: not there yet); this PR keeps them | | | | | Their new branches are covered by the adapter and end-to-end tests |
| `DistributedRateLimiterOptions.cs` | 100 [2] | 0 | 0 | 0 | 0 | Property defaults; no method with parameters |
| `DistributedRateLimiterOptionsValidator.cs` | 100 [2] | 25 [2] | 0 | 0 | 0 | One method with five rules; a guard test passes a null options object |
| `DistributedRateLimiterServiceCollectionExtensions.cs` | 95 [1] | 30 [1] | 0 | 0 | 60 [3] | The DI test and both registration modes reach every line; the guard flag reaches the `services` check; the integration suite builds services through it |
| `DistributedRateLimiterLogMessages.cs`, `DistributedRateLimiterTelemetry.cs` | 90 [1] | 0 | 0 | 0 | 0 | Asserted through the limiter tests with a fake logger and a meter listener; internal, no parameters that are validated |

The #704 files touched in PR 1b have no manifest row on `main` today (the files do not exist yet): this plan keeps the targets #704 sets and adds no row of its own.
5. **Load and benchmark** in PR 5. CRAP: no method is expected above complexity 4; the probability method is a single arithmetic expression with one guard.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Issue #4542, the limiter exists in PR 1 and PR 2. Telemetry, logs, error codes and targets are added with the pull request that introduces each caller.

TASK:
Add the counter, the three log messages and the error code with the files of PR 1, run each flag's test project with coverage, compare with the plan's
table and record the targets with their one-sentence justifications through mechanical-fixer.

KEY RULES:
- EventIds 3931-3933 only, packed, inside the registered Polly range (or 402-404 under Design Choice 1 option B). Never invent a range.
- No scope, key, hash, client identifier or tenant value in a log, tag or span except the tenant tag when a tenant is present.
- Documentation never types coverage figures; targets never go down.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, docs/architecture/adr/062-error-category-contract.md
- .github/coverage-manifest/Encina.Resilience.json
```

</details>

### Phase 5: Benchmark, load test and documentation (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

PR 5 holds exactly these 6 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.BenchmarkTests/Encina.Caching.Redis.Benchmarks/DistributedRateLimiterBenchmarks.cs` | new (counting versus adaptive acquire on the in-process store and a local Redis; `BenchmarkSwitcher`, filter checked with `--list flat`; the evidence for Design Choice 10) |
| 2 | `tests/Encina.BenchmarkTests/Encina.Caching.Redis.Benchmarks/Encina.Caching.Redis.Benchmarks.csproj` | modify (`ProjectReference` to `Encina.Resilience`) |
| 3 | `tests/Encina.LoadTests/Caching/DistributedRateLimiterLoadTests.cs` | new (many concurrent acquirers across two limiters never exceed the limit; many partitions) |
| 4 | `tests/Encina.LoadTests/Encina.LoadTests.csproj` | modify (references `Encina.Resilience`; it references neither it nor `Encina.Caching.Redis` today) |
| 5 | `docs/features/distributed-rate-limiting.md` | new (`docs-writer`: which caches coordinate and which are per instance, the opt-in, the fallback and its price, the tenant rule, the adaptive rule and its transient-error requirement, the clock-skew bound of #704; no hand-typed figures) |
| 6 | `docs/INVENTORY.md` | modify (`docs-writer`) |

Run the load test locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0`; results under `artifacts/performance/` and `artifacts/load-metrics/`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Issue #4542 is implemented in PR 1 to PR 4. PR 5 adds the benchmark, the load test and the documentation.

TASK:
Write the benchmark and the load test, verify the benchmark filter with --list flat, ask docs-writer for the feature page and the INVENTORY entry.

KEY RULES:
- BenchmarkSwitcher, never BenchmarkRunner.Run<T>; results go under artifacts/. State per-instance versus cross-instance behavior of each cache.
- Documentation cites coverage with cov: markers, never typed numbers; no private client application is named.

REFERENCE FILES:
- tests/Encina.BenchmarkTests/Encina.Caching.Redis.Benchmarks, tests/Encina.LoadTests (a caching load test for the layout)
- .claude/skills/encina-docs/SKILL.md
```

</details>

### Phase 6: Adoption by the rate-limit slot of `Encina.Resilience` (pull request 6)

<details>
<summary><strong>Tasks</strong></summary>

PR 6 starts when #4380 PR-4 (the behaviors), PR-11 (`RateLimiterRejectionMapper`) and PR-13 (`RateLimit/RateLimitStrategyFactory.cs`, the `AdaptiveRateLimiter` copy and `RateLimitAttribute`) are on `main`. It holds exactly these 13 files (paths of #4380's plan today; the admission behavior's file name is the one #4380 PR-4 creates; each is re-read when the brief is written, and a renamed file keeps the count):

| # | File (under `src/Encina.Resilience/` unless a path is given) | Change |
|---|------|--------|
| 1 | `RateLimit/RateLimitStrategyFactory.cs` | modify: when `IDistributedRateLimiter` is registered (Design Choice 7 option A) the slot's `RateLimiter` delegate is the adapter; otherwise today's path |
| 2 | `RateLimit/Distributed/DistributedRateLimitAdapter.cs` | new, internal: a `Right` denial becomes a failed lease with the retry-after; a store `Left` is carried through the exception of row 3 |
| 3 | `RateLimit/Distributed/DistributedRateLimitStoreException.cs` | new, internal: transport of one `EncinaError` through Polly, turned back into the same `Left` by the mapper (the only exception here) |
| 4 | `RateLimit/Distributed/DistributedRateLimitTargets.cs` | new, internal: the mapping below |
| 5 | `RateLimiterRejectionMapper.cs` | modify (maps the exception of row 3) |
| 6 | `IResilienceOutcomeObserver.cs` | new, public: the post-call observer of Design Choice 9 |
| 7 | the admission behavior file of #4380 PR-4 | modify: calls the observer after `nextStep` |
| 8 | `PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `.github/coverage-manifest/Encina.Resilience.json` | modify (`mechanical-fixer`) |
| 10 | `tests/Encina.UnitTests/Resilience/RateLimit/DistributedRateLimitAdapterTests.cs` | new (adapter, mapping and observer) |
| 11 | `tests/Encina.GuardTests/Resilience/RateLimit/DistributedRateLimitAdapterGuardTests.cs` | new (adapter constructor, targets nulls) |
| 12 | `tests/Encina.IntegrationTests/Resilience/DistributedRateLimitEndToEndTests.cs` | new (a `[RateLimit]` request sent through two hosts on the Redis fixture: the end-to-end acceptance, plus a tenant-less request denied) |
| 13 | `changelog.d/4542-distributed-rate-limiting-adoption.changed.md` | new (`mechanical-fixer`) |

The package README is documented by PR 5's page. The mapping (row 4): `Window = TimeSpan.FromSeconds(attribute.WindowSizeSeconds)`, `Limit = attribute.MaxRequestsPerWindow`, `Adaptive = attribute.EnableAdaptiveThrottling` (`RateLimitAttribute`, moved unchanged by #4380 PR-13); `ErrorThresholdPercent`, `CooldownSeconds` and `RampUpFactor` belong to the local three-state limiter and are ignored by the SRE rule (documented), `MinimumThroughputForThrottling` is the per-type minimum throughput when set above the option default. `Scope = typeof(TRequest).FullName`. Tenant: when `MultiTenancyMarker` is registered the mode is `Required` with `IRequestContext.TenantId` (a request without tenant is denied; there is no `Single` fallback when the resolver of #1257 or the policy of #740 is absent), `Agnostic` only for a request type with the explicit opt-out (logged by request type, as the #740 plan does), and `Single` only when the marker is not registered.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
Issue #4542, PR 1 to PR 5 merged. #4380 PR-4, PR-11 and PR-13 are merged: the admission behavior, the rejection mapper and the rate-limit strategy factory exist.

TASK:
Make the rate-limit slot use IDistributedRateLimiter when it is registered, record outcomes after the handler, and prove it end to end with two hosts on a real Redis.

KEY RULES:
- The local limiter stays the default: nothing changes unless AddEncinaDistributedRateLimiting was called.
- A denial maps to encina.ratelimit.exceeded (RateLimited) with the retry-after metadata; a store failure maps to its own Left; never an admission.
- Re-read the real file names of #4380 before editing; do not add a second behavior around the handler.

REFERENCE FILES:
- docs/plans/resilience-foundation-implementation-plan-4380.md (Phases 4 and 5), docs/architecture/adr/055-resilience-family.md
- src/Encina.Resilience/RateLimit/Distributed (PR 1 and PR 2)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| ADR-055 (R1, R5, R8) | The rate limiting core is in `Encina.Resilience`, which depends only on the store abstraction; a cache failure falls back; a request without tenant is still denied |
| #2414 decisions D1 to D3 | The partitioned core, the opt-in adaptive decorator with SRE-style rejection, tenant by default and fail closed |
| AGENTS.md sections 3, 5, 6, 7, 9 | ROP, fail closed, cancellation, caching category (8 providers), EventIds, per-flag targets and CRAP |
| ADR-062 (R3, R4, R7) | `tenant_required` is `InvalidRequest`; a store failure is `Internal`; R7 is outbound only |
| ADR-054 D3, D4, D6 | One meter per package, tenant tag when multi-tenancy is on, no scope or key in telemetry |
| Named comparable products | Google SRE client-side throttling (`max(0, (requests - K * accepts) / (requests + 1))`), `RedisRateLimiting` (limiters over store-side atomic scripts), `System.Threading.RateLimiting` |

### Cache provider matrix (AGENTS.md section 5)

| Provider | Coordinated across instances | Where it comes from |
|----------|:-:|-----|
| Memory (L1) | no, per instance | #704 registers the in-process store; Design Choice 8 warns |
| Hybrid (L1) | no, per instance | same |
| Redis | yes | #704 Lua store; integration run in PR 3 |
| Valkey, Dragonfly, Garnet, KeyDB | yes | inherit Redis; one integration class each in PR 3 |
| Memcached | yes | #3995 compare-and-swap store; integration in PR 4 once #3995 and #277 are on `main` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `IRateLimiter`, `AdaptiveRateLimiter`, `RateLimitingPipelineBehavior` | `src/Encina.Polly` | The behavior being replaced; removed by #4380 PR-14; read for the thresholds |
| `IRateLimiterStateStore`, `InProcessRateLimiterStateStore`, `ResilientRateLimiterStateStore` | #704 (`src/Encina/RateLimiting`, plan on `main`) | The only dependency of the limiter |
| `Encina.Resilience` project, `ResilienceServiceCollectionExtensions`, EventId mapping, manifest | #4380 PR-1 to PR-3 | Where the code lives |
| `RateLimiterRejectionMapper`, `RateLimitStrategyFactory` | #4380 PR-11, PR-13 | The adoption (PR 6) |
| `ResilienceTelemetry` meter holder | #728 | The counter's `Meter` |
| `EncinaErrorCodes.RateLimitExceeded`, `EncinaErrorMetadataKeys.RetryAfter`, `EncinaErrors.Create` | `src/Encina/Errors`, `src/Encina/Results` | Denial mapping and error construction |
| `MultiTenancyMarker`, tenant resolver and policy | `src/Encina/Abstractions`, #1257, #740 | The tenant mode (PR 6) |
| `RedisFixture` and the four product fixtures | `tests/Encina.IntegrationTests/Infrastructure/Caching` (#704 PR 5) | PR 3 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (inside the registered `Polly` range) | 3931-3933 | 3931 store failed, 3932 outcome recording failed, 3933 process-local store (PR 1b); 3922-3924 are #740, 3925-3930 are #4380; 3934-3949 stay free (3934-3935 if Design Choice 3 option B is chosen). The `Polly` range moves to 60050-60099 in #4007 OP-2 and the ids move with it. Under Design Choice 1 option B: 402-404 inside #704's `RateLimiterState (400, 419)` |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 10 | 3 | New: interface, partitions, two limiters, errors, logs, telemetry, options, validator, registration (PR 1, PR 2); 5 new in PR 6 and the marker of PR 1b; modified: 4 files in PR 6 and the two #704 stores in PR 1b |
| Tests | 15 | 6 | Unit, guard, contract, property (PR 1, PR 2); integration suite and six product classes (PR 3, PR 4); benchmark and load test (PR 5); modified: csproj files and the shared test files |
| Documentation | 2 | 1 | Feature page, changelog fragment; `INVENTORY.md` |
| Configuration | 0 | 11 | Manifests: `Encina.Resilience.json` in PR 1, 1b, 2, 3, 4 and 6, `Encina.json` in 1b (7); `PublicAPI.Unshipped.txt` in PR 1, 1b (core), 2 and 6 (4) |
| **Total** | about 29 | about 22 | 7 pull requests of 14, 10, 14, 8, 3, 6 and 13 files (PR 1 has 13 under Design Choice 12 option A), each within 15 files and 1,500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #4542 builds the limiter that consumes IRateLimiterStateStore (#704):
coordinated counting across instances, a shared error rate for the adaptive rule, opt-in registration with the local limiter as the default.

IMPLEMENTATION OVERVIEW:
1. PR 1: IDistributedRateLimiter, StateStoreRateLimiter, the tenant gate, logs 3931-3932, the counter, AddEncinaDistributedRateLimiting. PR 1b: the process-local marker and warning (3933).
2. PR 2: AdaptiveRejectionRateLimiter (SRE-style, opt-in), options and validator, property tests, the changelog fragment.
3. PR 3 and PR 4: integration on Redis, Valkey, Dragonfly, Garnet, KeyDB (and Memcached after #3995): two limiters share one limit.
4. PR 5: benchmark, load test, documentation.
5. PR 6: the rate-limit slot of Encina.Resilience uses the limiter when registered; outcomes are recorded after the handler.

KEY PATTERNS:
- A denial is Right(decision); a store failure or a missing tenant is Left; cancellation propagates; nothing is admitted on a failure.
- The caller composes the target; TenantMode.Required without a tenant is denied before the store; no tenant is never "default".
- No scope, tenant value, key or EncinaError.Message in a log or tag; EventIds 3931-3933 only, inside the registered Polly range.
- Singletons only, IOptions<T>, ValidateOnStart, a DI test with ValidateOnBuild and ValidateScopes; the application's own registration wins.
- Apply Design Choices 1 to 4, 7 to 10 and 12 as the maintainer answers them; until then build the recommended option of each.

REFERENCE FILES:
- docs/plans/rate-limiter-state-store-implementation-plan-704.md, docs/plans/resilience-foundation-implementation-plan-4380.md
- docs/architecture/adr/055-resilience-family.md, docs/architecture/adr/062-error-category-contract.md
- src/Encina.Polly/RateLimiting, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The shared state lives in the caches through the store of #704; five products coordinate, two are per instance, Memcached follows #3995 |
| 2 | OpenTelemetry | ✅ Include | Counter `encina.resilience.rate_limiter.distributed.decisions` with `decision` and `tenant_mode` tags; the tenant tag only when a tenant is present (ADR-054 D4); `encina.ratelimit.exceeded` is the consumer's |
| 3 | Structured Logging | ✅ Include | Ids 3931-3933, by error code only, no scope |
| 4 | Health Checks | ❌ N/A | The fallback decorator of #704 is visible as a metric and the cache packages have their own health checks |
| 5 | Validation | ✅ Include | Options validator with `ValidateOnStart`; window, limit and scope are validated by the store and returned as `partition_invalid` |
| 6 | Resilience | ✅ Include | The limiter is a strategy of `Encina.Resilience`; a cache outage is the fallback's job and never an admission |
| 7 | Distributed Locks | ❌ N/A | Atomicity is in the store's script (R8 rejected the lock option) |
| 8 | Transactions | ❌ N/A | No persistence and no unit of work |
| 9 | Idempotency | ❌ N/A | A retried request counts again by design |
| 10 | Multi-Tenancy | ✅ Include | `TenantMode` with the fail-closed rule, no tenant never mapped to `default`, the tenant tag, per-tenant partitions through the store |
| 11 | Module Isolation | ❌ N/A | Not module scoped; a module id, if wanted, belongs to the caller's scope string |
| 12 | Audit Trail | ❌ N/A | A decision is metered, not audited; the consumer records `encina.ratelimit.exceeded` |

**Deferred items:** none needs a new issue: Memcached is #3995 (open), the tenant policy is #740 (open), the partition spike is #3448 (open) and the Resilience package is #4380 (open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #704 PR 1 and PR 2 (store abstraction, in-process store, fallback decorator, range `RateLimiterState`) | Pending | Plan on `main`, code not yet; required by PR 1 |
| #704 PR 3 to PR 5 (Redis-family store, Memory and Hybrid registration, product fixtures) | Pending | Required by PR 3 |
| #4380 PR-1 to PR-3 (project, options skeleton, EventId mapping, manifest) | Pending | Required by PR 1; PR-4, PR-11 and PR-13 by PR 6 |
| #728 meter holder | Pending | Required by PR 1 for the counter's `Meter` |
| #3995 and #277 (Memcached store and provider) | Pending | PR 4 only |
| The constant `encina.tenancy.tenant_required` (from the first of #740, #1257, #760 to land) | Pending | Required by PR 1 under Design Choice 12 option A; not in `src` today |
| #740, #1257, #3448 (tenant policy and resolver, partition spike) | Open | Not required: PR 6 reads the tenant from `IRequestContext.TenantId` and builds `Required` whenever `MultiTenancyMarker` is registered |
| Maintainer answers to Design Choices 1 to 4, 7 to 10 and 12 | Pending | Asked when work starts |

The issue carries `needs-decision` and has no blocking dependency recorded.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 4, 7 to 10 and 12) when work starts and records the answers in a `## Maintainer Decisions` section.
2. When #704 PR 1 and PR 2 and #4380 PR-1 to PR-3 are on `main`, brief PR 1 and PR 2 in order; PR 3 follows #704 PR 5; PR 6 is briefed when #4380 PR-13 is on `main`.
