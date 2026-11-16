# Implementation Plan: Cache invalidation after the commit and a generic pub/sub invalidation subscriber

> **Issue**: [#2408](https://github.com/dlrivada/Encina/issues/2408)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 5 pull requests of 15, 7, 7, 15 and 14 files)
> **Estimated Scope**: about 700 lines of production code, about 1400 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching category)

---

## Summary

`CacheInvalidationPipelineBehavior` (`src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs`, registered by `AddEncinaCaching` as an open generic `IPipelineBehavior<,>`) runs `nextStep()` and then evicts, but it declares no pipeline stage, so whether it runs before or after the transaction depends on registration order. A concurrent reader can re-cache the old value when the eviction runs before the commit. The only subscriber of invalidation messages is the CDC one (`src/Encina.Cdc/Caching/CacheInvalidationSubscriberService.cs`, internal), so an application with L1 caches on several instances publishes messages that nobody consumes.

Re-checked against `origin/main` (e694eb032): the post-commit mechanism of #2184 exists. `IRequestPostCommitProcessor<TRequest, TResponse>` (`src/Encina/Abstractions/IRequestPostCommitProcessor.cs`) runs only after the inner pipeline, including the commit, returned `Right`, and never after a rollback; `PipelineBuilder.WrapProcessors` wraps every registered processor. What #2184 has not finished: no built-in behavior declares a stage yet and the startup placement validation is pending, which is why this plan does not rely on the stage of the existing behavior (Design Choice 1).

The plan (a) replaces the behavior by `CacheInvalidationPostCommitProcessor<TRequest, TResponse>` and a singleton `DelayedInvalidationScheduler` for `[InvalidatesCache(DelayMilliseconds = ...)]`, (b) moves the subscriber into `Encina.Caching` as a generic hosted service over `IPubSubProvider` with bounded retry and a health check, registered by `AddEncinaCacheInvalidationSubscriber()`, and (c) makes the CDC registration reuse it. The tenant segment of the key is out of scope (#1913, closed); the message keeps today's raw key pattern (Design Choice 4).

**Affected packages:** `Encina.Caching` (processor, scheduler, subscriber, options, logs), `Encina.Cdc` (loses its subscriber, reuses the generic one), `Encina` (EventId range registration only). `Encina.OpenTelemetry` needs no change: the meter and activity source are created in `Encina.Caching` and subscribed by name.

**Provider category**: Caching (8). Behavior of the processor is provider-independent (`ICacheProvider`, `IPubSubProvider`). Cross-instance delivery needs a real backplane: Redis and the four Redis-protocol products (Valkey, Dragonfly, Garnet, KeyDB, whose wrappers call `AddEncinaRedisCache`, which registers `RedisPubSubProvider`) have it; Memory registers `MemoryPubSubProvider`, which is in-process only; Hybrid registers no `IPubSubProvider`; Memcached (planned, #277) has no pub/sub, a documented gap.

**Sibling plans (caching family)**: this plan owns `CacheInvalidationPostCommitProcessor`, the generic subscriber, `CacheInvalidationSubscriberOptions` and the EventId range 300-319. #2402 (Hybrid `RemoveByPatternAsync` removes only tracked keys) decides whether the post-commit eviction is complete for Hybrid; #1954 (type-wide pattern misses per-user entries) decides what a pattern matches. Neither is a prerequisite for the code of this plan; both are named in Prerequisites. #2407 (DSR erasure: purge caches) is a sibling in the same EventId block 300-349: it takes 320-329 and plans a comment on #2408; this plan keeps 300-319 and the two plans never share an id.

**Decision record on the issue**: the maintainer comment on #2408 of 2026-10-17 reads "Settled by elimination, no maintainer decision needed. Answer: invalidate in the post-commit stage (from #2184) and move the pub/sub subscriber into `Encina.Caching` as a generic hosted service reused by CDC." It fixes the stage and the subscriber's home (Design Choice 6); it does not say how the stage is reached (Design Choice 1, still a Dilemma) and does not mention a switch (Design Choice 2 rests on AGENTS.md alone).

---

## Design Choices

<details>
<summary><strong>1. Mechanism that places the eviction after the commit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CacheInvalidationPostCommitProcessor<,>` implementing `IRequestPostCommitProcessor<,>`, registered as an open generic; the behavior is removed** | Runs only after the commit returned `Right` and never after a rollback, by the contract of the interface, whatever the registration order; works today (the builder already wraps these processors); no dependence on the unfinished stage declarations of built-in behaviors | The class changes shape (`Process` instead of `Handle`); 13 unit tests, 7 guard tests and 4 benchmark classes retarget or are replaced |
| **B) Keep the behavior and declare `[PipelineStage(PipelineStage.Caching)]` on it** | Smallest change; tests keep their shape | Correct only if `TransactionPipelineBehavior` also declares its stage; today no built-in behavior does (no `PipelineStage(` attribute in `src/` outside doc comments), so an undeclared transaction behavior falls into `Application` (1500), which is inside `Caching` (1000) and the eviction would still run before the commit |
| **C) Keep the behavior and document the order** | No code | Today's failure mode |

### Chosen Option: **A - post-commit processor** (recommended, pending the maintainer)

### Rationale

- The maintainer comment of 2026-10-17 on #2408 says "invalidate in the post-commit stage (from #2184)"; Options A and B both claim it (A through the post-commit processor, B through a declared stage), so this is a Dilemma, not a Settled choice.
- Option B wins once #2184 declares the stage of `TransactionPipelineBehavior` and the maintainer wants the least churn; Option A wins now because its guarantee does not depend on that later phase.
- A post-commit step that throws propagates (fail-fast, `PostCommitProcessorStep`); the processor keeps the existing `CachingOptions.ThrowOnCacheErrors` contract (`CachingOptions.cs` line 106): off, a failed eviction is logged by exception type and counted, on, it throws.
- Class: Dilemma

</details>

<details>
<summary><strong>2. No `InvalidateAfterCommit` switch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Always post-commit; no option** | One behavior, the correct one; no dead configuration | Applications that relied on pre-commit eviction lose it (none exist: pre-1.0) |
| **B) `InvalidateAfterCommit`, default `false`** | Matches the sketch in the issue body | Keeps today's failure mode reachable and default |
| **C) `InvalidateAfterCommit`, default `true`** | Correct by default | Keeps the legacy path in the code as a compatibility switch |

### Chosen Option: **A - always post-commit**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 1 and section 3 (no legacy code, compatibility aliases or compatibility layers; pre-1.0 best solution first). B and C both keep the pre-commit path as a configuration switch, which those rules forbid; only A has no switch. The 2026-10-17 comment on #2408 supports the post-commit stage only (Design Choice 1); the `InvalidateAfterCommit` sketch is in the issue body, not in that comment.

</details>

<details>
<summary><strong>3. Delayed invalidation (`DelayMilliseconds`)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A singleton `DelayedInvalidationScheduler` that owns the timers (`TimeProvider.CreateTimer`), catches every failure in the callback, and disposes the timers when the host stops** | Fixes two defects of the current code: `_ = InvalidateWithDelayAsync(...)` runs on the request's `CancellationToken` (cancelled when the request completes, so the delayed eviction can silently never run) and its exceptions are unobserved; deterministic with `FakeTimeProvider`; follows the timer rule of AGENTS.md section 3 | One more class and registration; the scheduler is a plain singleton that the container disposes (no hosted service, so PR 1 needs no hosting reference) |
| **B) Keep fire-and-forget on the request token** | No new class | Both defects stay |
| **C) Remove `DelayMilliseconds`** | Less code | Removes a public attribute member users may rely on; a design change beyond this issue |

### Chosen Option: **A - owned timers** (recommended, pending the maintainer)

### Rationale

- The delay path must be reimplemented anyway because the processor replaces the behavior; Option B would copy a known defect into new code.
- Option C wins if the maintainer judges delayed eviction unnecessary once eviction is post-commit; the attribute member is used in tests and benchmarks today (`CacheInvalidationPipelineBehaviorTests.cs`).
- The scheduler is a singleton and receives only singletons (`ICacheProvider`, `IPubSubProvider?`, `IOptions<CachingOptions>`, `TimeProvider`, `ILogger<>`); the processor stays transient and hands it `(pattern, broadcast, correlationId, delay)`. No scoped service is injected into a singleton.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Wire contract of the invalidation message</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the raw key pattern string**, validated by the subscriber (non-empty, bounded length, no control characters) | The contract already shared by the behavior and `QueryCacheInvalidationCdcHandler`; no format switch between publishers and subscribers, so every pull request of the split builds and works alone | The subscriber cannot tag its telemetry with the tenant (ADR-054 D4): the message has no tenant field, and parsing the tenant out of the key prefix would be fragile; no origin id, so the publishing instance evicts a second time on its own echo (idempotent) |
| **B) A versioned envelope `{ v, tenant, pattern, origin }` through `IPubSubProvider.PublishAsync<T>`** | Tenant tag on subscriber metrics; echo suppression; room to evolve | Publishers (processor, CDC handler) and the subscriber must switch in one step, because the format is not backward readable and a dual reader would be a compatibility layer (AGENTS.md section 1): one pull request over the size limit, or a window in which cross-instance invalidation silently stops |

### Chosen Option: **A - raw pattern** (recommended, pending the maintainer)

### Rationale

- Option B wins when subscriber telemetry must carry the tenant (ADR-054 D4 applies "in every package" when multi-tenancy is on) and a single atomic change is acceptable; until then the subscriber omits the tenant tag and the counters carry `outcome` only. The deviation is stated in the observability guide.
- The pattern already carries the tenant segment (`DefaultCacheKeyGenerator.GeneratePatternFromTemplate`, `{KeyPrefix}:t:{EscapeSegment(tenant)}:` when the context has a tenant). The processor never invents or drops a tenant: it publishes exactly the pattern it evicted locally, so a message can never be wider than the local eviction. Key generation is not touched; #1913 owns its tenant behavior.
- Messages and logs carry the pattern only through `CacheKeyRedactor.Redact` (ADR-054 D6: no cache keys in telemetry); the CDC subscriber today logs the raw message (`CacheInvalidationSubscriberService.cs` line 67), which this plan fixes when it moves.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Subscriber failure handling</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Bounded retry (default 3 attempts, exponential delay through `TimeProvider`), then an Error log by exception type, a failure counter and the message is dropped** | Never swallowed silently (retry, then a visible failure); the handler never throws into the provider's callback thread; stale entries expire by their TTL | A dropped message leaves a stale entry until its TTL |
| **B) Rethrow so the provider or host reacts** | Loud | A throwing callback ends the subscription or stops the host (a `BackgroundService` default); one bad message disables invalidation |
| **C) Re-publish a failed message to a dead-letter channel** | No loss | Pub/sub has no consumer or retention for it; a new mechanism for a cache |

### Chosen Option: **A - retry, then count and log** (recommended, pending the maintainer)

### Rationale

- The issue text says "logged by type and retried, never swallowed silently"; A and C both satisfy it, C with a mechanism the backplane cannot back.
- Option B wins when losing one invalidation is judged worse than losing the subscription (a cache with no TTL).
- Fails closed on input: a message that is empty, over 1024 characters or holds control characters is rejected, logged by reason code and counted, never passed to `RemoveByPatternAsync`.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Where the subscriber lives and how CDC reuses it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CacheInvalidationSubscriberService` and its health check move to `Encina.Caching`; `AddEncinaCacheInvalidationSubscriber()` registers them once with `TryAddEnumerable`; CDC calls the same extension and adds its channel to `CacheInvalidationSubscriberOptions.Channels`** | One implementation; a double registration is a no-op; the two default channels are the same string (`sm:cache:invalidate`) | `Encina.Caching` gains a `Microsoft.Extensions.Hosting.Abstractions` reference |
| **B) Leave the subscriber in CDC and add a second one in Caching** | No move | Two subscribers on one channel evict twice |

### Chosen Option: **A - generic subscriber in `Encina.Caching`**

### Rationale

- **Class:** Settled
- **Settled by:** the issue body of #2408 ("`CacheInvalidationSubscriberService` moves to `Encina.Caching` as a generic hosted service over `IPubSubProvider`, with the CDC variant reusing it") and its decision comment of 2026-10-17. Option B is the duplication the issue removes.

</details>

<details>
<summary><strong>7. Subscriber with no pub/sub provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail at host start with a clear exception** (the subscriber needs `IPubSubProvider` and `ICacheProvider`; the hosted service checks both in `StartAsync` and logs EventId 314 by type) | Registration completeness: a requested subscriber that can never receive is an error, not a silent no-op | An application that registers the subscriber without a backplane stops at start |
| **B) Log a warning and do nothing** | Never blocks start | A multi-instance application believes its L1 caches stay coherent while nothing listens |

### Chosen Option: **A - fail at start** (recommended, pending the maintainer)

### Rationale

- Today the CDC registration adds the service whenever `UsePubSubBroadcast` is on and fails later, at activation, with a DI error; A makes the failure explicit and early.
- Option B wins for development setups that register everything and run without a backplane; the maintainer may prefer a switch, which is an extra option and a reason to ask.
- Class: Dilemma

</details>

<details>
<summary><strong>8. EventIds</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new range `CachingInvalidation = (300, 319)` from the family block 300-349** | Cannot collide with the sibling caching plans, which also planned ids inside `Caching` 3500-3549 (3514-3549 free) | One more registry entry and one allocation-test mapping |
| **B) Ids 3514-3528 inside the existing `Caching` range** | No registry change | Collides with whatever the sibling plans take in the same 36 free ids |

### Chosen Option: **A - new range 300-319**

### Rationale

- **Class:** Trivial
- **Settled by:** the assignment of block 300-349 to this family in the plan batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`, 2026-11-15) and AGENTS.md section 7 (register the range in `EventIdRanges.cs` before use; the allocation test maps it to `Encina.Caching`). The existing behavior ids 3500-3503 stay with the behavior until it is deleted. The sibling #2407 takes 320-329 of the same block; this plan keeps 300-319.

</details>

---

## Implementation Phases

### Phase 1: Post-commit processor and delayed scheduler (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 15 files (row 8 is two files; the limit is 15):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Behaviors/CacheInvalidationPostCommitProcessor.cs` | new |
| 2 | `src/Encina.Caching/Behaviors/DelayedInvalidationScheduler.cs` | new |
| 3 | `src/Encina.Caching/Diagnostics/CacheInvalidationLog.cs` | new (ids 300-305, Phase 5) |
| 4 | `src/Encina.Caching/Diagnostics/CacheInvalidationTelemetry.cs` | new (meter, activity source, Phase 5) |
| 5 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (register processor and scheduler; stop registering the behavior) |
| 6 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingInvalidation = (300, 319)`) |
| 7 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Caching` to both ranges) |
| 8 | `src/Encina.Caching/PublicAPI.Unshipped.txt` and `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`; two files) |
| 9 | `tests/Encina.UnitTests/Caching/CacheInvalidationPostCommitProcessorTests.cs` | new |
| 10 | `tests/Encina.GuardTests/Infrastructure/Caching/CacheInvalidationPostCommitProcessorGuardTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (line 104-121 `AddEncinaCaching_WhenCacheInvalidationEnabled_RegistersInvalidationBehavior` asserts an `IPipelineBehavior<,>` named `CacheInvalidation*`; it asserts the `IRequestPostCommitProcessor<,>` and `DelayedInvalidationScheduler` descriptors instead; the DI test with `ValidateOnBuild` and `ValidateScopes` is added here, including the case with no `IPubSubProvider`) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/2408-post-commit-cache-invalidation.changed.md` | new (`mechanical-fixer`) |
| 14 | `tests/Encina.UnitTests/Caching/Base/BaseServiceCollectionExtensionsTests.cs` | modify (line 105-121 holds the same assertion, `AddEncinaCaching_WhenCacheInvalidationEnabled_RegistersInvalidationBehavior`; the base class is inherited by the provider registration test classes, so the assertion moves to the processor descriptor in the same PR or every inheritor fails) |

Tasks:

1. **Create** `public sealed class CacheInvalidationPostCommitProcessor<TRequest, TResponse> : IRequestPostCommitProcessor<TRequest, TResponse>` with `[PipelineStage(PipelineStage.Transaction, Order = -100)]` (the assembly `Encina.Caching` is in `PipelineOrderValidator.BuiltInAssemblies`, so rule R3 requires the declared stage). Constructor `(ICacheProvider cacheProvider, ICacheKeyGenerator keyGenerator, IOptions<CachingOptions> options, DelayedInvalidationScheduler scheduler, ILogger<CacheInvalidationPostCommitProcessor<TRequest, TResponse>> logger, IPubSubProvider? pubSubProvider = null)` (the `= null` default is required: Microsoft DI ignores the `?` annotation and supplies a default value only when the parameter declares one, so with no backplane registered the processor still activates), `ArgumentNullException.ThrowIfNull` on the first five. A `static readonly InvalidatesCacheAttribute[]` per closed generic type (as the behavior does); `Process` returns `Right` immediately when `EnableCacheInvalidation` is off or the array is empty; otherwise, per attribute: generate the pattern with `GeneratePatternFromTemplate`, hand it to the scheduler when `DelayMilliseconds > 0`, else remove locally and broadcast (`BroadcastInvalidation && EnablePubSubInvalidation && pubSubProvider is not null`), exactly as `CacheInvalidationPipelineBehavior.InvalidateCacheAsync` does today. A thrown exception is logged with `ex.ForLogging()` (EventId 302, 303) and counted; with `ThrowOnCacheErrors` it is rethrown. Always `Right` otherwise.
2. **Create** `public sealed class DelayedInvalidationScheduler : IDisposable` (singleton): `Schedule(string pattern, bool broadcast, string correlationId, TimeSpan delay)` creates a `TimeProvider.CreateTimer` whose callback catches every exception (EventId 305) and never throws (AGENTS.md section 3), keeps the timer in a `ConcurrentDictionary<long, ITimer>` that the callback removes it from, and `Dispose` disposes the timers left at container shutdown. It is not a hosted service (`Encina.Caching` has no hosting reference until Phase 4). Constructor `(ICacheProvider cacheProvider, IOptions<CachingOptions> options, TimeProvider timeProvider, ILogger<DelayedInvalidationScheduler> logger, IPubSubProvider? pubSubProvider = null)`: the `= null` default is mandatory for the same reason as in the processor (Hybrid and applications without a backplane register no `IPubSubProvider`), and the scheduler is public only because the public processor constructor takes it.
3. **Modify** `AddEncinaCaching`: replace the `CacheInvalidationPipelineBehavior<,>` registration by `services.TryAddSingleton(TimeProvider.System)` (`AddEncinaCaching` registers no `TimeProvider` today; the old behavior took an optional one), `services.TryAddSingleton<DelayedInvalidationScheduler>()` and `services.AddTransient(typeof(IRequestPostCommitProcessor<,>), typeof(CacheInvalidationPostCommitProcessor<,>))`, both inside `if (options.EnableCacheInvalidation)`. The behavior class stays in the assembly, unregistered, until pull request 3.
4. **Register** the range (task 6 of the table) and the allocation-test mapping.
5. **Tests**: processor evicts and broadcasts after `Right`; does not evict on `Left`; a `PipelineBuilder`-driven test with a stand-in transaction behavior that returns `Left` (rollback) proves "a rolled-back command evicts nothing", and one that returns `Right` proves the eviction ran after the commit step returned; no `InvalidatesCache` attribute is a no-op; delayed eviction on a `FakeTimeProvider` (and after the request token is cancelled); a throwing `ICacheProvider` is logged and does not fail the request, and with `ThrowOnCacheErrors` it throws; a broadcast failure follows the same rule; guard tests for the constructor and `Process`; DI test (in `ServiceCollectionExtensionsTests.cs`, rows 11 and 14) builds the provider with `ValidateOnBuild` and `ValidateScopes`, once with `AddEncinaCaching` plus the Memory pub/sub provider and once with no `IPubSubProvider`, and resolves the processor in both.
6. **Manifest (same PR as the tests)**: targets with one-sentence justifications in `Encina.Caching.json` (Phase 6 table). Measured values replace the estimates before the PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #2408 moves cache eviction after the commit. IRequestPostCommitProcessor
(src/Encina/Abstractions) runs only after the inner pipeline returned Right; PipelineBuilder wraps registered processors.
The old CacheInvalidationPipelineBehavior stays compiled but unregistered in this PR.

TASK:
1. Create CacheInvalidationPostCommitProcessor<TRequest,TResponse> and DelayedInvalidationScheduler as described in the plan.
2. Register both in AddEncinaCaching (inside EnableCacheInvalidation); stop registering the behavior.
3. Add CacheInvalidationLog (ids 300-305) and CacheInvalidationTelemetry; register EventIdRanges.CachingInvalidation (300, 319) and map it in EncinaEventIdAllocationTests.
4. Write the unit, guard and DI tests; ask mechanical-fixer for PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- Declare [PipelineStage(PipelineStage.Transaction, Order = -100)] on the processor (built-in assembly rule R3).
- Never inject a scoped service into the singleton scheduler; timers come from TimeProvider.CreateTimer and their callback never throws.
- Log patterns only through CacheKeyRedactor.Redact and exceptions only through ForLogging(); never EncinaError.Message.
- No tenant is invented: publish exactly the pattern evicted locally.
- ThrowOnCacheErrors keeps its meaning. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs
- src/Encina/Abstractions/IRequestPostCommitProcessor.cs
- src/Encina/Pipeline/Steps/PostCommitProcessorStep.cs
- src/Encina/Pipeline/PipelineBuilder.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Caching/CacheInvalidationPipelineBehaviorTests.cs
```

</details>

### Phase 2: Retarget the tests and benchmarks (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 7 files; the behavior still exists, so each file builds against either class:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.UnitTests/Caching/CacheInvalidationBroadcastTests.cs` | modify (4 references) |
| 2 | `tests/Encina.UnitTests/Caching/CacheLogRedactionTests.cs` | modify (3 references) |
| 3 | `tests/Encina.UnitTests/Caching/CacheTenantFailClosedTests.cs` | modify (2 references) |
| 4 | `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheInvalidationBenchmarkBase.cs` | modify (4 references) |
| 5 | `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheInvalidationBenchmarks.cs` | modify |
| 6 | `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheInvalidationConcurrentBenchmarks.cs` | modify |
| 7 | `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheInvalidationMultipleKeysBenchmarks.cs` | modify |

Tasks: move every test of broadcast, log redaction and tenant fail-closed behavior from `Handle(...)` on the behavior to `Process(...)` on the processor, keeping each assertion; point the four benchmark classes at the processor (`BenchmarkSwitcher`, no `BenchmarkRunner.Run<T>()`). No manifest change: no `src/` file changes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: the processor is the registered invalidation step. These tests and benchmarks still drive the old behavior.

TASK:
Retarget the seven files to CacheInvalidationPostCommitProcessor, keeping every assertion and benchmark scenario.

KEY RULES:
- Tests instantiate the real processor; no reflection-only tests.
- Benchmarks use BenchmarkSwitcher.FromAssembly(...).Run(args, config); verify the filter with --list flat before a run.
- Do not delete the behavior or its own tests (Phase 3).

REFERENCE FILES:
- src/Encina.Caching/Behaviors/CacheInvalidationPostCommitProcessor.cs
- tests/Encina.UnitTests/Caching/CacheInvalidationPostCommitProcessorTests.cs
- tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheInvalidationBenchmarkBase.cs
```

</details>

### Phase 3: Remove the old behavior (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 7 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs` | delete (236 lines) |
| 2 | `src/Encina.Caching/Attributes/InvalidatesCacheAttribute.cs` | modify (documentation names the processor) |
| 3 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (remove the two behavior lines; `mechanical-fixer`) |
| 4 | `tests/Encina.UnitTests/Caching/CacheInvalidationPipelineBehaviorTests.cs` | delete (424 lines, 13 tests; every scenario exists in the processor tests of PR 1) |
| 5 | `tests/Encina.GuardTests/Infrastructure/Caching/CacheInvalidationPipelineBehaviorGuardTests.cs` | delete (180 lines, 7 tests; replaced in PR 1) |
| 6 | `.github/coverage-manifest/Encina.Caching.json` | modify (remove the entry of the deleted file; allowed because the source file is gone; `mechanical-fixer`) |
| 7 | `docs/INVENTORY.md` | modify (`docs-writer`) |

Before deleting, `Select-String` over `src`, `tests`, `docs` and `.github` must show no reference left outside `docs/knowledge` (those records describe history) and outside one accepted historical comment: `.github/workflows/mutation-tests.yml` line 121 names `CacheInvalidationPipelineBehaviorTests` in a comment about an earlier fix (PR #2563). Workflows are an orchestrator hot spot, so the worker neither edits that comment nor counts it in the 7 files; the gate lists it as expected. The registration tests that mentioned the behavior (`ServiceCollectionExtensionsTests.cs` and `Base/BaseServiceCollectionExtensionsTests.cs`) were retargeted in PR 1. The EventIds 3500-3503 disappear with the behavior; the ids 3504-3513 of the other behaviors are untouched.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
The processor replaced the behavior in Phase 1; Phase 2 retargeted the remaining tests and benchmarks.

TASK:
Delete CacheInvalidationPipelineBehavior and its two test files, fix the attribute documentation, remove the behavior lines
from PublicAPI.Unshipped.txt and its manifest entry, update INVENTORY.md.

KEY RULES:
- Grep for the type name first; stop and report if anything outside docs/knowledge still references it.
- Do not lower any other target in the manifest (target-ratchet).
- Zero warnings; no [Obsolete] shim.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs
- src/Encina.Caching/PublicAPI.Unshipped.txt
- .github/coverage-manifest/Encina.Caching.json
```

</details>

### Phase 4: Generic subscriber in Encina.Caching (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 15 files; nothing registers the new subscriber yet except its own extension, and the CDC subscriber is untouched:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Subscribers/CacheInvalidationSubscriberService.cs` | new (internal sealed `IHostedService`) |
| 2 | `src/Encina.Caching/Subscribers/CacheInvalidationSubscriberHealthCheck.cs` | new (internal; the CDC check is the template) |
| 3 | `src/Encina.Caching/Configuration/CacheInvalidationSubscriberOptions.cs` | new |
| 4 | `src/Encina.Caching/Diagnostics/CacheInvalidationLog.cs` | modify (ids 306-314, Phase 5) |
| 5 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (`AddEncinaCacheInvalidationSubscriber`) |
| 6 | `src/Encina.Caching/Encina.Caching.csproj` | modify (`Microsoft.Extensions.Hosting.Abstractions`) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/CacheInvalidationSubscriberServiceTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/CacheInvalidationSubscriberRegistrationTests.cs` | new (DI, `ValidateOnBuild`, `ValidateScopes`, double registration) |
| 10 | `tests/Encina.GuardTests/Infrastructure/Caching/CacheInvalidationSubscriberGuardTests.cs` | new |
| 11 | `tests/Encina.ContractTests/Caching/PubSubInvalidationContractTests.cs` | new (publish then evict through `MemoryPubSubProvider`; `IPubSubProvider` consumers keep the contract) |
| 12 | `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheInvalidationSubscriberIntegrationTests.cs` | new (Redis through the existing shared fixture; second-instance eviction) |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/2408-cache-invalidation-subscriber.added.md` | new (`mechanical-fixer`) |
| 15 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (`ProjectReference` to `Encina.Caching.Memory`; the project references only `Encina.Caching` today) |

Tasks:

1. **Create** `CacheInvalidationSubscriberService : IHostedService` with constructor `(ICacheProvider cacheProvider, IOptions<CacheInvalidationSubscriberOptions> options, TimeProvider timeProvider, ILogger<CacheInvalidationSubscriberService> logger, IPubSubProvider? pubSubProvider = null)` (the optional parameter is last and declares `= null`: Microsoft DI ignores the `?` annotation, so without the default a host with no `IPubSubProvider` would fail at activation with a DI error and never reach the clear `StartAsync` failure of Design Choice 7; `src/Encina.Security.Secrets/ServiceCollectionExtensions.cs` line 285 shows the factory-registration alternative, which is not needed with a default); `StartAsync` fails with an `InvalidOperationException` naming the missing service type when `pubSubProvider` is null (Design Choice 7), then subscribes to every channel in `options.Channels` through `SubscribeAsync(channel, handler, ct)`. The handler validates the message (Design Choice 5), calls `RemoveByPatternAsync`, retries up to `MaxAttempts` with exponential delay on `timeProvider`, and never throws. `StopAsync` disposes the subscriptions.
2. **Create** `CacheInvalidationSubscriberOptions { HashSet<string> Channels (default: the CachingOptions default channel), int MaxAttempts = 3, TimeSpan RetryBaseDelay = 200 ms }` with an options validator (at least one channel, `MaxAttempts >= 1`).
3. **Create** `AddEncinaCacheInvalidationSubscriber(this IServiceCollection, Action<CacheInvalidationSubscriberOptions>? configure = null)`: `TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CacheInvalidationSubscriberService>())`, `TryAddSingleton(TimeProvider.System)`, the options and their validator, and the health check; calling it twice, or once from CDC and once from the application, merges the channels and registers one service. It resolves the channel from `CachingOptions.InvalidationChannel` when the application sets it.
4. **Health check**: `CacheInvalidationSubscriberHealthCheck : EncinaHealthCheck` reports Healthy when the subscription is active; Unhealthy with the exception type only otherwise.
5. **Tests**: valid message evicts; invalid message (empty, 1025 characters, control character) is rejected and never reaches the cache; a failing provider retries `MaxAttempts` times then logs and counts, the handler returns normally; `FakeTimeProvider` drives the delays; missing pub/sub provider fails `StartAsync`, and the registration test (row 9) builds the provider with `ValidateOnBuild` and `ValidateScopes` and no `IPubSubProvider`, proving the service activates and only `StartAsync` fails; the integration test runs two service providers on one Redis container, evicts through the processor of one and asserts the other drops its entry; the contract test publishes through `MemoryPubSubProvider`.
6. **Hybrid, Valkey, Dragonfly, Garnet, KeyDB**: the registration test class (the unit project already references every wrapper; the integration project references only Memory, Redis and Hybrid) has one case per Redis-protocol wrapper that calls the real `AddEncina<Product>Cache` and asserts the `IPubSubProvider` descriptor resolves to `RedisPubSubProvider`, so the subscriber is wired for the four products; the wire behavior is proven once against the Redis container (the products speak the same protocol). Hybrid is asserted to have no `IPubSubProvider` (a documented gap: `StartAsync` fails clearly, Design Choice 7); Memcached has no provider yet.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phase 1 to 3 are merged. The CDC package still has its own internal subscriber; do not touch it here. The message is the raw key pattern
that the processor publishes on CachingOptions.InvalidationChannel.

TASK:
Create the generic subscriber, its options and health check in Encina.Caching, the registration extension, the logs 306-314,
the csproj reference, and the tests listed in the plan.

KEY RULES:
- The handler never throws; failures retry with TimeProvider delays and then log by exception type and count.
- Patterns are logged only through CacheKeyRedactor.Redact; reject empty, over-long or control-character messages.
- StartAsync fails clearly when IPubSubProvider is not registered.
- Registration is idempotent (TryAddEnumerable) and proven by a DI test with ValidateOnBuild and ValidateScopes.
- Integration test uses the shared Redis collection fixture, never a per-class container.

REFERENCE FILES:
- src/Encina.Cdc/Caching/CacheInvalidationSubscriberService.cs
- src/Encina.Cdc/Caching/CacheInvalidationSubscriberHealthCheck.cs
- src/Encina.Security.Secrets/Caching/SecretCachePubSubHostedService.cs
- src/Encina.Caching/Abstractions/IPubSubProvider.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisPubSubProviderIntegrationTests.cs
```

</details>

### Phase 5: CDC reuses the generic subscriber (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

PR 5 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Cdc/Caching/CacheInvalidationSubscriberService.cs` | delete |
| 2 | `src/Encina.Cdc/Caching/CacheInvalidationSubscriberHealthCheck.cs` | delete |
| 3 | `src/Encina.Cdc/Caching/CdcCacheInvalidationLog.cs` | modify (remove ids 4806-4811) |
| 4 | `src/Encina.Cdc/ServiceCollectionExtensions.cs` | modify (`RegisterCacheInvalidationServices` calls `AddEncinaCacheInvalidationSubscriber` with the CDC channel) |
| 5 | `tests/Encina.GuardTests/Cdc/CdcHappyPathAndConstructorGuardTests.cs` | modify (22 references) |
| 6 | `tests/Encina.IntegrationTests/Cdc/Caching/CdcCacheInvalidationIntegrationTests.cs` | modify (4 references) |
| 7 | `tests/Encina.UnitTests/Cdc/CdcCacheInvalidationRegistrationTests.cs` | new (DI test: CDC with `UsePubSubBroadcast` registers one subscriber; with a second `AddEncinaCacheInvalidationSubscriber` still one) |
| 8 | `.github/coverage-manifest/Encina.Cdc.json` | modify (`mechanical-fixer`) |
| 9 | `docs/features/cdc-cache-invalidation.md` | modify (`docs-writer`) |
| 10 | `docs/features/cdc.md` | modify (`docs-writer`) |
| 11 | `src/Encina.Cdc/README.md` | modify (`docs-writer`) |
| 12 | `docs/INVENTORY.md` | modify (`docs-writer`) |
| 13 | `changelog.d/2408-cdc-uses-generic-cache-invalidation-subscriber.changed.md` | new (`mechanical-fixer`) |
| 14 | `src/Encina.Caching/README.md` | new (`docs-writer`; the package has no README today) |

Tasks: delete the two CDC classes; `QueryCacheInvalidationOptions.PubSubChannel` feeds `CacheInvalidationSubscriberOptions.Channels`; the unit test for the CDC registration uses `ValidateOnBuild` and `ValidateScopes`; the Cdc manifest drops the entries of the deleted files (allowed: the sources are gone). EventIds 4806-4811 are freed; the Cdc registry range 4800-4899 keeps its other claims (#2393, #634, #635, #636 use 4854-4891).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Phase 4 is merged: Encina.Caching has AddEncinaCacheInvalidationSubscriber. Encina.Cdc still registers its own internal subscriber.

TASK:
Delete the CDC subscriber and its health check, make RegisterCacheInvalidationServices call the generic extension with the CDC channel,
retarget the tests, update the documentation through docs-writer.

KEY RULES:
- Exactly one subscriber per channel after registration, in any call order.
- The CDC handler (QueryCacheInvalidationCdcHandler) keeps publishing the raw pattern; do not change the wire format.
- Remove the manifest entries of deleted files only; never lower another target.

REFERENCE FILES:
- src/Encina.Cdc/ServiceCollectionExtensions.cs
- src/Encina.Cdc/Caching/QueryCacheInvalidationCdcHandler.cs
- tests/Encina.IntegrationTests/Cdc/Caching/CdcCacheInvalidationIntegrationTests.cs
- docs/features/cdc-cache-invalidation.md
```

</details>

### Phase 6: Observability, logging, testing and documentation

<details>
<summary><strong>Tasks</strong></summary>

1. **Logs** (`src/Encina.Caching/Diagnostics/CacheInvalidationLog.cs`, range `CachingInvalidation` 300-319, ids packed from 300): 300 `Invalidated` (Debug, redacted pattern, correlation id), 301 `Broadcast` (Debug), 302 `InvalidationFailed` (Warning, exception through `ForLogging()`), 303 `BroadcastFailed` (Warning), 304 `DelayedScheduled` (Debug), 305 `DelayedFailed` (Warning), 306 `SubscriberStarting`, 307 `SubscriberStarted`, 308 `SubscriberStopped`, 309 `MessageReceived` (Debug, redacted), 310 `MessageInvalidated` (Debug), 311 `MessageRetrying` (Warning, attempt number), 312 `MessageGaveUp` (Error, exception type), 313 `MessageRejected` (Warning, reason code only), 314 `ProviderMissing` (Error). 315-319 stay free.
2. **Telemetry** (`CacheInvalidationTelemetry`): `ActivitySource "Encina.Caching"`, `Meter "Encina.Caching"` with `Counter<long> encina.cache.invalidations` (tags `source` = `local`, `broadcast`, `subscriber`, `delayed`; `outcome` = `success`, `failed`, `rejected`), subscribed by name from `Encina.OpenTelemetry`'s registration helpers (verify the names in `src/Encina.OpenTelemetry` before the PR). Tenant: the processor adds the tenant tag when multi-tenancy is enabled (ADR-054 D4, from `IRequestContext.TenantId`, hashed as the other packages do if cardinality requires, documented in the observability guide, #906); the subscriber cannot (Design Choice 4) and states it. No cache key, pattern or value is a tag or an attribute (ADR-054 D6 rule 3).
3. **Error categories (ADR-062)**: the plan adds no `EncinaErrors.Create` call: failures are exceptions handled in place, and `ThrowOnCacheErrors` keeps its exception contract. If the maintainer prefers a `Left` from the post-commit step for the throwing case, the code is `cache.invalidation_failed` with `ErrorCategory.UpstreamFailure` through the categorized `EncinaErrors.Create(code, category, message)` overload.
4. **Coverage targets (per flag, new or touched files)**, entered by `mechanical-fixer` in the PR that adds the tests:

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Behaviors/CacheInvalidationPostCommitProcessor.cs` | 100 | 28 | 0 | 0 | Replaces `CacheInvalidationPipelineBehavior.cs`, whose manifest targets are unit 100 and guard 28, so the replacement never lowers them; every branch is driven by the unit tests, guard tests reach the constructor and `Process` checks; no contract or integration flag targets a deterministic in-process step |
| `Behaviors/DelayedInvalidationScheduler.cs` | 95 | 20 | 0 | 0 | A `FakeTimeProvider` reaches every branch; guard reaches the argument checks |
| `Subscribers/CacheInvalidationSubscriberService.cs` | 90 | 25 | 30 | 60 | Unit covers retry, rejection and start failure; the contract test covers the publish-evict path on `MemoryPubSubProvider`; the Redis integration test covers the real subscription |
| `Subscribers/CacheInvalidationSubscriberHealthCheck.cs` | 90 | 20 | 0 | 0 | Two outcomes, driven by unit tests |
| `Configuration/CacheInvalidationSubscriberOptions.cs` | 90 | 30 | 0 | 0 | Defaults and validator driven by unit tests |
| `Diagnostics/CacheInvalidationLog.cs`, `CacheInvalidationTelemetry.cs` | 70 | 0 | 0 | 0 | Generated logger bodies run only when a test enables the level; no parameters guard tests could reach |
| `ServiceCollectionExtensions.cs` (Caching, PR 1 and PR 4) | 90 | 30 | 0 | 0 | The registration paths are covered by the DI tests |
| `Encina.Cdc/ServiceCollectionExtensions.cs` (PR 5) | no explicit target | no explicit target | 0 | 0 | The file has only the default-rule entry in `Encina.Cdc.json`; the change is one call to `AddEncinaCacheInvalidationSubscriber`, measured through the new registration test (the measured figures go in the PR report) |
| `Encina.Cdc/Caching/CdcCacheInvalidationLog.cs` (PR 5) | no explicit target | 0 | 0 | 0 | Only default-rule entry (unit); the PR deletes ids 4806-4811 and adds no executable code |
| `Attributes/InvalidatesCacheAttribute.cs` (PR 3) | no explicit target | no explicit target | 0 | 0 | Default-rule entry; the PR changes only its XML documentation, so the measured coverage must not drop |
| `Encina/Diagnostics/EventIdRanges.cs` (PR 1) | 95 (unchanged) | 0 | 0 | 0 | One new `static readonly` field; the existing unit target of 95 in `Encina.json` is kept and the allocation architecture tests reach the new range |

5. **Property, load and benchmark**: property tests are not required (no non-trivial invariant beyond the validated pattern length); the four existing benchmark classes are retargeted in PR 2 (a benchmark justification is not needed because they exist); a load test is not written (the processor holds no shared mutable state beyond the scheduler's dictionary, covered by a concurrency unit test).
6. **Documentation (`docs-writer`, in PR 5)**: the Encina.Caching package README (no README exists today: create it, with the post-commit contract, the subscriber, the backplane gap table, the telemetry deviation and a trust assumption: a message on the shared channel is evicted as a pattern, so any publisher on that channel can purge any key; this is inherited from today's CDC subscriber and the channel must be restricted to trusted publishers), the `cdc-cache-invalidation.md` and `cdc.md` pages, `INVENTORY.md`; XML documentation with examples on every new public type; changelog fragments as named in the file tables; ROADMAP is unaffected. No ADR: the choices follow #2184 and ADR-050.
7. **Verification**: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the unit, guard and contract projects; the Redis integration test with Docker; per-flag coverage measured with `coverage-report.cs` and `--check-justifications`; CRAP at most 10 for every added method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
All production code of phases 1 to 5 exists. Logs, telemetry, targets and documentation remain per pull request.

TASK:
Add the telemetry and logs named in the plan to the pull request that introduces each caller, measure per-flag coverage of every
touched file, and record targets with one-sentence justifications through mechanical-fixer; ask docs-writer for the README and pages.

KEY RULES:
- EventIds only from range 300-319, packed from 300; the range is registered in EventIdRanges.cs first.
- No cache key, pattern, value or subject in a log, tag or attribute except the redacted pattern in a Debug log.
- The tenant tag appears only when multi-tenancy is enabled; the subscriber omits it.
- Documentation never types coverage figures.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- .github/coverage-manifest/Encina.Caching.json
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Timer callbacks never throw; errors are never swallowed in background infrastructure; registration completeness with a DI test |
| AGENTS.md section 5 (caching) | A feature touching the category is consistent across the 8 providers; the backplane gap is documented |
| ADR-054 D4, D6 | Tenant tag when multi-tenancy is on; no cache keys in telemetry |
| ADR-062 | Every new error code states its `ErrorCategory`; this plan adds none |
| ADR-050 | Module requirements: a module may require another where its guarantees need it |
| #2184 | Post-commit processors and pipeline stages |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `IRequestPostCommitProcessor<,>` and `PostCommitProcessorStep<,>` | `src/Encina/Abstractions`, `src/Encina/Pipeline/Steps` | The post-commit placement |
| `CacheInvalidationPipelineBehavior<,>` | `src/Encina.Caching/Behaviors` | Logic moved, then deleted |
| `CacheInvalidationSubscriberService` and its health check | `src/Encina.Cdc/Caching` | Moved to `Encina.Caching` |
| `IPubSubProvider`, `MemoryPubSubProvider`, `RedisPubSubProvider` | `src/Encina.Caching`, `.Memory`, `.Redis` | The backplane |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration` | Pattern redaction in logs |
| `DefaultCacheKeyGenerator.GeneratePatternFromTemplate` | `src/Encina.Caching/KeyGeneration` | The tenant-qualified pattern |
| `SecretCachePubSubHostedService`, `PolicyCachePubSubHostedService` | `src/Encina.Security.Secrets`, `src/Encina.Security.ABAC` | Existing typed-message hosted subscribers; same shape, not reused (their messages are typed) |
| `EncinaHealthCheck` | `src/Encina.Messaging/Health` | Base of the subscriber check |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingInvalidation`) | 300-319 (from block 300-349) | 300-314 used by this plan, 315-319 free; `EncinaEventIdAllocationTests` maps `Encina.Caching` to `Caching` and `CachingInvalidation` |
| `Encina.Caching` (existing `Caching`) | 3500-3549 | 3500-3503 freed with the behavior (PR 3); 3504-3513 unchanged |
| `Encina.Cdc` | 4800-4899 | 4806-4811 freed (PR 5) |

### Estimated File Count

A file touched by several pull requests is counted once per pull request, because each limit is per PR.

| Category | New Files | Modified Files | Deleted Files | Notes |
|----------|-----------|----------------|---------------|-------|
| Production code (`src/`) | 7 | 8 | 3 | New: processor, scheduler, log, telemetry (PR 1); subscriber, subscriber check, options (PR 4). Modified: Caching DI (PR 1, PR 4), `EventIdRanges.cs`, attribute, Caching log, csproj, Cdc log, Cdc DI. Deleted: behavior, the two Cdc classes |
| Tests | 8 | 13 | 2 | New: 2 in PR 1 (unit, guard), 5 in PR 4 (unit, DI, guard, contract, integration), 1 in PR 5 (CDC DI). Modified: allocation test, `ServiceCollectionExtensionsTests.cs`, `Base/BaseServiceCollectionExtensionsTests.cs` (PR 1); 3 unit and 4 benchmark files (PR 2); contract csproj (PR 4); Cdc guard and integration tests (PR 5). Deleted: the behavior's unit and guard test files |
| Documentation | 1 | 5 | 0 | Caching README (new); `INVENTORY.md` (PR 3 and PR 5), `cdc-cache-invalidation.md`, `cdc.md`, Cdc README |
| Configuration | 3 | 8 | 0 | Three changelog fragments; PublicAPI (Caching in PR 1, PR 3 and PR 4, Encina in PR 1); manifests (Caching in PR 1, PR 3 and PR 4, Cdc in PR 5) |
| **Total** | **19** | **34** | **5** | 58 file operations over 5 pull requests of 15, 7, 7, 15 and 14 files (PR 1 counts the PublicAPI of two projects as two files); each at most 15 files; PR 3 deletes about 840 lines and PR 4 adds about 1100, both under 1500 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #2408: cache eviction must run after the commit, and
a generic pub/sub subscriber in Encina.Caching must consume invalidation messages (CDC reuses it).

IMPLEMENTATION OVERVIEW:
1. CacheInvalidationPostCommitProcessor<,> (IRequestPostCommitProcessor, [PipelineStage(Transaction, -100)]) replaces the behavior;
   DelayedInvalidationScheduler owns delayed evictions with TimeProvider timers.
2. Behavior removal after the tests and benchmarks are retargeted.
3. CacheInvalidationSubscriberService (hosted, bounded retry, health check) and AddEncinaCacheInvalidationSubscriber in Encina.Caching.
4. Encina.Cdc deletes its subscriber and registers the generic one with its channel.
5. EventIds 300-314 in the new range CachingInvalidation (300-319); telemetry without keys or tenant tags on the subscriber.
Five pull requests in the order of the phases.

KEY PATTERNS:
- Post-commit steps run only after the inner pipeline returned Right; ThrowOnCacheErrors keeps its meaning.
- The wire message stays the raw key pattern; the subscriber validates it and never widens it.
- Timer callbacks and subscriber handlers never throw; failures are retried, logged by exception type and counted.
- No scoped service in a singleton; registration is idempotent and proven by DI tests with ValidateOnBuild and ValidateScopes.
- Apply Design Choices 1, 3, 4, 5 and 7 as the maintainer's answers, recorded when work starts, say.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs
- src/Encina/Abstractions/IRequestPostCommitProcessor.cs
- src/Encina.Cdc/Caching/CacheInvalidationSubscriberService.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature is cache invalidation. |
| 2 | OpenTelemetry | ✅ Include | Invalidation counter and a subscriber span; tenant tag on the processor when multi-tenancy is on (ADR-054 D4); the subscriber omits it (Design Choice 4); no cache keys. |
| 3 | Structured Logging | ✅ Include | EventIds 300-314 in the new range 300-319, patterns redacted, exceptions through `ForLogging()`. |
| 4 | Health Checks | ✅ Include | The CDC subscriber health check moves with the subscriber. |
| 5 | Validation | ❌ N/A | No new external input; the channel message is validated as a key pattern by the subscriber (length, characters) without a validation provider. |
| 6 | Resilience | ✅ Include | Bounded retry with exponential delay on a failed removal (Design Choice 5). |
| 7 | Distributed Locks | ❌ N/A | Invalidation is idempotent and needs no lock. |
| 8 | Transactions | ✅ Include | Eviction runs after the commit through a post-commit processor; it is not inside the transaction. |
| 9 | Idempotency | ✅ Include | Removing the same pattern twice has the same result, so the publisher's own echo is harmless. |
| 10 | Multi-Tenancy | ✅ Include | The pattern carries the tenant segment from the key generator; the processor never invents or drops a tenant; no tenant widening. |
| 11 | Module Isolation | ❌ N/A | Not module scoped. |
| 12 | Audit Trail | ❌ N/A | Cache eviction is not an audit event. |

**Deferred items:** none that need an issue. The delayed-invalidation defect (unobserved fire-and-forget on the request token) is fixed here (Design Choice 3), not deferred.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `IRequestPostCommitProcessor` and its pipeline step (#2184) | ✅ Available | Declared stages of the built-in behaviors and the startup validation of placement arrive in later #2184 phases; this plan does not depend on them |
| #2402 Hybrid `RemoveByPatternAsync` removes only tracked keys | ⏭️ Open bug | Post-commit eviction is complete for Hybrid only after it is fixed; the Hybrid acceptance case in the integration tests is added with that fix |
| #1954 type-wide pattern misses per-user entries | ⏭️ Open debt | Defines what a pattern matches; unchanged here |
| Memcached provider (#277) | ⏭️ Later | No pub/sub; documented gap |
| Maintainer answers to Design Choices 1, 3, 4, 5 and 7 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 3, 4, 5, 7) when work starts, records the answers under `## Maintainer Decisions`, then briefs five workers in the phase order.
2. After PR 1 is merged, comment on #2408 with the result. The sibling #2407 (DSR erasure: purge caches, EventIds 320-329) also plans a comment on #2408; the two plans share no EventId.
3. The `Security.Secrets` and `Security.ABAC` pub/sub hosted services share the subscriber shape; whether they adopt the generic subscriber is outside this issue.
