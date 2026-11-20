# Implementation Plan: Cache strategies (read-through, write-through, write-behind)

> **Issue**: [#272](https://github.com/dlrivada/Encina/issues/272)
> **Type**: Feature
> **Complexity**: High (4 code phases, 4 pull requests of 13, 12, 15 and 15 files)
> **Estimated Scope**: about 900 lines of production code, about 1,900 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #272 asks for a `CacheStrategy` enum selectable per request on `[Cache]` (cache-aside, read-through, write-through, write-behind), the three new strategies in "`CachePipelineBehavior`", a write-behind background worker with configurable flush interval and batch size, and durability options for the write-behind queue. Re-checked against `origin/main` (e31de98cb):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "Encina implements the cache-aside pattern exclusively: the application checks, queries and populates the cache" | True for direct `ICacheProvider` use, false for the pipeline. `QueryCachingPipelineBehavior` (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`) reads the entry, runs `nextStep()` on a miss and stores the `Right` result itself: that is read-through with the handler as loader. Every provider also has `GetOrSetAsync` (provider-level read-through) that the behavior does not call. |
| "Implement ... in `CachePipelineBehavior`" | No such type. The behaviors are `QueryCachingPipelineBehavior` and `CacheInvalidationPipelineBehavior` (registered by `AddEncinaCaching`, `ServiceCollectionExtensions.cs`). |
| `[Cache(Strategy = ..., FlushIntervalSeconds, BatchSize)]` on a command | `CacheAttribute` (`Attributes/CacheAttribute.cs`) has no such properties. A command with `[Cache]` is cached today under its own request-type key like a query, which is never what an application wants for a command. |
| Write-through "updates the cache after the handler succeeds" | The pipeline has a post-commit stage: `IRequestPostCommitProcessor<TRequest, TResponse>` (`src/Encina/Abstractions/IRequestPostCommitProcessor.cs`) runs after the inner pipeline, including the commit, returned `Right` (`PipelineBuilder.WrapProcessors`, `src/Encina/Pipeline/PipelineBuilder.cs`, wraps it in `PostCommitProcessorStep`). The XML remark "Not yet invoked" on the interface is stale against that code. The behavior-level write in `QueryCachingPipelineBehavior` runs wherever DI order puts it, possibly before the commit. |
| Write-behind has a "background worker" and "durability options" | Nothing exists. `Encina.Caching` already references `Encina.Messaging` (`Encina.Caching.csproj`), whose `IDeadLetterStore` (`src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`) is the existing home of failed messages. |

The plan keeps everything at the behavior level, so the 8 cache providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277) behave the same with no provider change: `ReadThrough` is the existing miss path (named, validated and documented), `WriteThrough` is a post-commit processor that writes the command's response under the cached query's key, and `WriteBehind` is an opt-in behavior that queues the request and a hosted service that flushes batches through a user-supplied batch handler.

**Shared pieces and ownership (family cache-patterns-1 and cache-patterns-2):** #266 owns `CachePatternSettings` (the per-request-type resolved settings), the `CacheEntry<T>` envelope with `ExpiresAtUtc`, `CachePatternMetrics` and the request-flow contract; this plan extends `CachePatternSettings` and adds a partial file of `CachePatternMetrics`, so its pull requests start after #266 PR 1 and PR 4 are merged. The EventIds are this plan's own range `CachingStrategies` 500-507 (block 500-549 of the plan batch registry, shared with #274, #275, #276 and #278; each of those plans registers its own sub-range).

**Affected packages:** `Encina.Caching` (core), `Encina` (EventId registry only). **Provider category:** Caching (8), behavior-level and identical on all 8. The database provider matrix does not apply.

---

## Design Choices

<details>
<summary><strong>1. The strategy vocabulary and what `ReadThrough` adds (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CacheStrategy { ReadThrough, WriteThrough, WriteBehind }`, `ReadThrough` is the default; cache-aside stays "direct `ICacheProvider` use" and is documented as such** | No two names for the same behavior; the default keeps today's pipeline behavior exactly | Differs from the issue's four-value enum |
| **B) The issue's four values; `CacheAside` is the default and `ReadThrough` is accepted as an explicit synonym for the same code path** | Matches the issue text and its acceptance criterion "all four strategies"; a handler author can state intent | Two values with one behavior is confusing, and a test cannot tell them apart |
| **C) The issue's four values and make `ReadThrough` differ: `CacheAside` bypasses the loader (read-only cache, the application populates it)** | Four distinct behaviors | `CacheAside` on an attribute that makes the behavior stop populating the cache is a new semantic nobody asked for, and the application has no way to populate the key the behavior reads |

### Chosen Option: **A - three strategies, `ReadThrough` default** (recommended, pending the maintainer)

### Rationale

- The pipeline behavior already is read-through; the only honest new work for reads is naming and validating it. Cache-aside exists where the application calls `ICacheProvider` itself.
- Option B wins if the maintainer wants the issue's public vocabulary and its literal acceptance criterion unchanged.
- Class: Dilemma

</details>

<details>
<summary><strong>2. How write-through finds the key and the value type (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[Cache(Strategy = WriteThrough, WriteThroughQuery = typeof(GetProductQuery))]`: the named query must carry `[Cache]` with a `KeyTemplate` and implement `IRequest<TResponse>` of the same `TResponse` as the command; the key is built from the query's template with the command's properties** | One source of truth for the template; the response type is verified when the processor is built | The command's assembly must reference the query's type, which a module boundary may forbid |
| **B) The command repeats the query's `KeyTemplate` (`[Cache(Strategy = WriteThrough, KeyTemplate = "product:{Id}")]`) and nothing is verified** | No type reference across modules | A typo or a differing placeholder writes an entry nobody reads, silently; a response type mismatch fails only at read time |
| **C) A registered `ICacheWriteKeyResolver<TCommand>` per command** | Full control | One more public abstraction and a class per command for what a template already says |

### Chosen Option: **A - typed query reference** (recommended, pending the maintainer)

### Rationale

- The issue's example has no key at all (`[Cache(Strategy = WriteThrough)]` on `UpdateProductCommand`), so a mechanism is required. Option A fails at startup when the pair does not match; option B wins for a modular monolith where the command's module cannot reference the query's.
- Mechanism: the key is built by the existing `ICacheKeyGenerator.GeneratePatternFromTemplate(template, command, context)`, which gives `{prefix}:t:{tenant}:{template with the command's properties}`, the exact string `GenerateKey` gives the query when the query has `VaryByTenant = true` and `VaryByUser = false`. Those two flags are therefore validated on the named query at resolution (a query varying by user or not by tenant is rejected), the placeholders must exist as properties of the command, and a unit test asserts the two generators agree. No interface change and no test double changes. A custom `ICacheKeyGenerator` must keep that agreement; the documentation says so.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Where the write-through runs: after the commit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `CacheWriteThroughPostCommitProcessor<TRequest, TResponse>` implementing `IRequestPostCommitProcessor<,>`** | Runs only after the inner pipeline, including the commit, returned `Right`, and never after a rollback, whatever the registration order | Opening the generic adds one step to the pipeline of every request type, so it is registered only when `CachingOptions.EnableWriteThrough` is on |
| **B) Inside `QueryCachingPipelineBehavior`, after `nextStep()`** | One class | Runs before the commit when the behavior is registered inside the transaction behavior: a cached value that the rollback later makes untrue |

### Chosen Option: **A - post-commit processor**

### Rationale

- **Class:** Trivial
- **Settled by:** the interface's own contract (`IRequestPostCommitProcessor.cs`: runs after the transaction committed and the request succeeded, never when the inner pipeline returned `Left`) and `PipelineBuilder.WrapProcessors` (`src/Encina/Pipeline/PipelineBuilder.cs`, which resolves the registered processors); AGENTS.md section 3 (a cache write that precedes the commit would be untrue after a rollback).
- The processor does not claim atomicity: the orchestrator never calls `SaveChangesAsync` (the handler or the transaction behavior owns the commit). A failure to write the cache cannot fail the request (a `Left` from a post-commit processor fails a request that already committed), so the processor returns `Right` always, logs the exception type (EventId 501) and, when `CachingOptions.ThrowOnCacheErrors` is on, throws as the existing behavior does. An `OperationCanceledException` from the caller's token always propagates (never a `Right`, never a `Left`). Because the failure is swallowed when `ThrowOnCacheErrors` is off, a `Right` from `Send` does not confirm that the cache was written; the documentation says so. After a failed set it tries `RemoveAsync` of the key, so a stale value does not outlive the update; that removal is unconditional, so a concurrent newer write of the same key can be removed too, which costs one miss.

</details>

<details>
<summary><strong>4. Opt-in registration of the strategies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CachingOptions.EnableWriteThrough` (default false) registers the processor; `AddEncinaWriteBehind()` registers the write-behind behavior, queue and hosted service; `ReadThrough` needs nothing** | Pay-for-what-you-use: no extra pipeline step and no hosted service unless asked | A request with `[Cache(Strategy = WriteThrough)]` and the option off is a configuration error that must be reported |
| **B) Register everything from `AddEncinaCaching`** | Nothing to remember | The open generic processor adds a pipeline step to every request type; a hosted service starts for applications that never use it |

### Chosen Option: **A - opt-in**

### Rationale

- **Class:** Settled (that each strategy is opt-in and off by default); Trivial (the two shapes, an option flag for write-through and a registration method for write-behind)
- **Settled by:** AGENTS.md section 3 ("Every messaging pattern ... is optional and disabled by default") and the pay-for-what-you-use principle of section 1; `PipelineBuilder.WrapProcessors` allocates a step per registered processor type for every request. The shapes follow existing precedent: boolean `Enable*` options of `CachingOptions` that `AddEncinaCaching` turns into behavior registrations (`EnableQueryCaching`, `EnableCacheInvalidation`), and a separate extension that registers a hosted service and its options (`ShardingCacheServiceCollectionExtensions.cs`, `TopologyRefreshHostedService`).
- The misconfiguration (an attribute on a type while the feature is not registered) is reported when the type's settings are resolved, in whatever form the maintainer chooses for invalid configuration.

</details>

<details>
<summary><strong>5. What write-behind caches and what the batch contract is (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The queued unit is the request; a public `IWriteBehindBatchHandler<TRequest>.HandleBatchAsync(IReadOnlyList<TRequest>, WriteBehindBatchContext, CancellationToken)` returning `Either<EncinaError, Unit>` persists a batch; no cache value is written by the library (the command returns `Unit`); an optional `WriteBehindQueueDepth` metric is the visible effect** | Honest about what a generic library can know: it cannot derive a cache value from a command, and the issue's example returns `Unit` | "Write to cache immediately" of the issue is not delivered by the library |
| **B) A + an optional `IWriteBehindCacheProjection<TRequest>` that returns the key and value to write immediately** | Delivers the issue's "cache first" for applications that supply the projection | One more public abstraction; the projection runs in the request path and its value type has the same ambiguity as Design Choice 2 |
| **C) The handler runs once per request in the background (no batch handler)** | No new abstraction | Defeats batching, which the issue lists as the point of write-behind |

### Chosen Option: **B - batch handler plus optional projection** (recommended, pending the maintainer)

### Rationale

- The issue states both halves ("write to cache immediately", "background worker persists in batches"); B delivers both and A is B without the optional part. Option A wins if the maintainer wants the smaller first version (the projection can be added later without a break).
- The batch is grouped by tenant: one batch never mixes tenants, and the context handed to the batch handler carries only the tenant id and the batch id; no user identity is persisted, because the queue holds the request and authorization happened at enqueue time.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Durability of the write-behind queue (Dilemma D6, the issue's "durability options")</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A bounded in-memory queue (`System.Threading.Channels`), drained for `ShutdownDrainTimeout` on graceful shutdown; a process crash loses queued items, which the documentation and the option name state; a follow-up issue tracks a durable queue** | No new dependency or store; honest, small | The issue's acceptance criterion of durability options is met only by a documented limit |
| **B) A durable queue now, through `Encina.Messaging` scheduled messages or the outbox** | At-least-once persistence | The 10-provider database matrix of AGENTS.md section 5 then applies; scheduling carries domain messages, not batches of arbitrary requests; this plan would triple in size |
| **C) An `IWriteBehindQueue` abstraction with the in-memory default now and durable implementations as later packages** | The seam exists from the start | A public abstraction with one implementation; premature |

### Chosen Option: **A - in-memory with a follow-up issue** (recommended, pending the maintainer)

### Rationale

- A durable write-behind is a separate design (a store per provider, ordering, idempotent replay) and is recorded as its own issue file (`artifacts/issues/durable-write-behind-queue.md`). Option C wins if the maintainer wants the seam first; option B wins when losing the queue on a crash is unacceptable for the use case.
- A write-behind failure is never silent: retries, then the dead-letter sink (Design Choice 7); the loss window is limited to a process crash or a forced stop.
- Class: Dilemma

</details>

<details>
<summary><strong>7. A failed flush: retry, then dead-letter (Dilemma D7 on the destination)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Bounded retries with a fixed backoff, then an `IWriteBehindDeadLetterSink`; the default sink logs the batch size and the error code (EventId 504) and drops** | No new storage; the sink is the extension point | The default loses the batch after the retries (visibly) |
| **B) The default sink writes each request to `IDeadLetterStore` of `Encina.Messaging` when one is registered, else behaves as A** | Failed batches are recoverable with the existing dead-letter tooling | `IDeadLetterMessage` is shaped for messages (request type, serialized content): needs a mapping and a serializer, and the store needs the 10-provider registrations the application already chose |

### Chosen Option: **A - sink interface, logging default** (recommended, pending the maintainer)

### Rationale

- The rule itself is fixed by AGENTS.md section 3 ("Errors are NEVER swallowed in background infrastructure: a `Left` ... fails that operation (retry, dead-letter or exception per its semantics), never reports success"): a `Left` from the batch handler is retried and then dead-lettered, never counted as flushed. Only the destination is a choice. Option B wins for an application that already runs the messaging dead-letter store and wants one place to look.
- The flush loop catches per iteration and never lets an exception escape (AGENTS.md section 3, background loops); a `PeriodicTimer` loop ends only on cancellation of the stopping token (EventId 506 for a faulted iteration).
- Class: Dilemma

</details>

<details>
<summary><strong>8. Full queue (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject: the enqueue returns `Left` `cache.write_behind_queue_full` (ADR-062 `ErrorCategory.Unavailable`, constant message), the handler does not run** | Memory stays bounded; the caller learns the truth (fail closed) | A burst of low-priority writes becomes errors |
| **B) Fall back to running the handler synchronously for that request** | The write happens, only slower | A full queue usually means the persistence is failing, so the synchronous call fails too, now in the request path |
| **C) Drop the oldest queued item** | Never errors | Silent data loss |

### Chosen Option: **A - reject** (recommended, pending the maintainer)

### Rationale

- Option C is excluded by AGENTS.md section 3 (no silent loss). Option B wins for counters where a slower request beats an error.
- The error is raised on the request path and is logged by code (EventId 502), never with a message; `ErrorCategory.Unavailable` follows the ADR-062 table (the capacity is exhausted, not the request invalid).
- Class: Dilemma

</details>

<details>
<summary><strong>9. Invalid strategy configuration (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `InvalidOperationException` when the behavior or processor is built for the request type (first request of that type), with a message naming the request type and the rule** | A configuration error is loud and deterministic, not a business `Left`; no error code to maintain | Seen at the first request of the type, not at startup |
| **B) A `Left` with an error code (ADR-062 `ErrorCategory.Internal`)** | Fits the ROP convention | Turns a developer mistake into a per-request error code that callers then handle |

### Chosen Option: **A - throw at resolution** (recommended, pending the maintainer)

### Rationale

- The rules to enforce: `WriteThrough` needs `WriteThroughQuery` (D2 option A) and `CachingOptions.EnableWriteThrough`; `WriteThrough` and `WriteBehind` reject `VaryByUser` (a per-user entry cannot be kept consistent by one update), `SlidingExpiration` and `[InvalidatesCache]` on the same type; `WriteBehind` needs `TResponse` of `Unit`, `BatchSize` 1 to 10,000, `FlushIntervalSeconds` 1 to 3,600 and a registered `IWriteBehindBatchHandler<TRequest>`; the write-behind properties are rejected on other strategies.
- Option B wins if the maintainer wants no exception from the pipeline.
- Class: Dilemma

</details>

<details>
<summary><strong>10. Time, randomness and optional constructor dependencies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeProvider` for every timestamp and delay (the flush loop uses `new PeriodicTimer(period, timeProvider)`); every parameter of a NEW class is required, and a dependency added to an EXISTING constructor is an optional trailing parameter** | Deterministic tests; the existing `new QueryCachingPipelineBehavior<` call sites keep compiling; a new class has no call sites, so an optional parameter there would only hide a missing registration (an absent `MultiTenancyMarker` would read as single-tenant) | None |
| **B) Optional parameters everywhere** | Uniform | A forgotten registration silently degrades to a default (fail open for tenancy) |
| **C) Required parameters and `Task.Delay`** | Explicit | Breaks the existing call sites; real time in tests |

### Chosen Option: **A - `TimeProvider` and optional parameters**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (time comes from `TimeProvider`) and the precedent in `QueryCachingPipelineBehavior`'s constructor (`TimeProvider? timeProvider = null`, kept because that constructor exists).

</details>

<details>
<summary><strong>11. Integration tests (Dilemma D11, the issue's "unit and integration tests")</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unit and contract tests in every PR, plus one Redis integration test file in PR 4 (shared Redis fixture) that runs write-through and a write-behind flush against a real provider** | Meets the acceptance criterion; proves the serialization round trip of a written-through `CacheEntry` and a real flush | Needs the Redis container on CI (the integration job already runs it) |
| **B) Unit and contract tests only** | No container | The criterion "integration tests" is not met; the behavior-level code has no provider-specific path, which is the argument for skipping |

### Chosen Option: **A - one Redis integration test file** (recommended, pending the maintainer)

### Rationale

- A written-through value must deserialize through a real provider into the type the query reads, which no unit test with a mocked provider shows. Option B wins when the maintainer treats the contract tests as sufficient for behavior-level code.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: The strategy enum, the attribute and read-through (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Configuration/CacheStrategy.cs` | new (public enum: `ReadThrough`, `WriteThrough`, `WriteBehind`) |
| 2 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`Strategy`, `WriteThroughQuery`, `FlushIntervalSeconds`, `BatchSize`) |
| 3 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | modify (strategy members and all validation rules, including the options-dependent ones; owned by #266 PR 1) |
| 4 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (a request whose strategy is not `ReadThrough` skips the read-through path) |
| 5 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`EnableWriteThrough`, default false) |
| 6 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (the hand-written copy line `opt.EnableWriteThrough = options.EnableWriteThrough;`) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/CacheStrategySettingsTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/CacheAttributeTests.cs` | modify |
| 10 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify (default false) |
| 11 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (the option reaches `IOptions<CachingOptions>`: a missing copy line fails this test) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/272-cache-strategy-attribute.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `public enum CacheStrategy` with XML docs and a strategy comparison table in the remarks (consistency, latency, complexity, from the issue).
2. **Modify** `CacheAttribute`: `public CacheStrategy Strategy { get; init; } = CacheStrategy.ReadThrough;`, `public Type? WriteThroughQuery { get; init; }`, `public int FlushIntervalSeconds { get; init; } = 30;`, `public int BatchSize { get; init; } = 100;` (the defaults of the issue's example).
3. **Modify** `CachePatternSettings`: carry `Strategy`, `WriteThroughQuery`, `FlushInterval`, `BatchSize`; `Resolve` applies every Design Choice 9 rule that PR 1 can check: the attribute-only rules, `WriteThrough` while `EnableWriteThrough` is off, and the query validation of Design Choice 2 (the named query must carry `[Cache]` with a `KeyTemplate`, `VaryByUser = false` and `VaryByTenant = true`, because the write-through key is built by `ICacheKeyGenerator.GeneratePatternFromTemplate`, which always adds the tenant segment and never a user segment; a query with other flags would read a key the command never writes). The write-behind option rules arrive with their options in PR 3. A request type without `[Cache]` resolves nothing and throws nothing.
4. **Modify** `QueryCachingPipelineBehavior`: `ShouldUseCache` returns false when the static attribute's strategy is `WriteThrough` or `WriteBehind` (a command is never cached under its own key); `ReadThrough` is unchanged.
5. **Modify** `CachingOptions` (`public bool EnableWriteThrough { get; set; }`, default false) and add the copy line in `AddEncinaCaching` (the option list is hand-written: a missing line silently drops the option); `ServiceCollectionExtensions.cs` keeps at least the unit target it has on main.
6. **Tests (unit):** every rule of Design Choice 9 that PR 1 can check, the attribute defaults, an equality test that `DefaultCacheKeyGenerator.GenerateKey` of the query and `GeneratePatternFromTemplate` of the command give the same string for the same properties and tenant, and "a `WriteThrough` command goes straight to `nextStep()` with no cache read or write". No guard test: the settings type is internal (no `InternalsVisibleTo` for `Encina.GuardTests` in `Encina.Caching.csproj`) and the attribute has no constructor parameters.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #272 adds cache strategies to the [Cache] attribute. QueryCachingPipelineBehavior
(src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs) already reads the entry and runs the handler on a miss: that is
read-through. CachePatternSettings (src/Encina.Caching/Patterns/CachePatternSettings.cs) comes from issue #266 PR 1 and is on main.

TASK:
1. Add the public enum CacheStrategy (ReadThrough, WriteThrough, WriteBehind) and the four CacheAttribute properties of the plan.
2. Extend CachePatternSettings with the strategy members and the validation of Design Choice 9 that PR 1 can check (the maintainer's answer to it is in your brief).
3. Make QueryCachingPipelineBehavior skip the cache for WriteThrough and WriteBehind request types; add EnableWriteThrough with its copy line.
4. Write the unit tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest entry and the changelog fragment.

KEY RULES:
- ReadThrough (default) changes nothing for existing requests.
- Invalid configuration fails per Design Choice 9 and the message names the request type, never a request value.
- Zero warnings; CRAP of every added or changed method at most 10.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Attributes/CacheAttribute.cs
- src/Encina.Caching/Patterns/CachePatternSettings.cs
- tests/Encina.UnitTests/Caching/QueryCachingPipelineBehaviorTests.cs
```

</details>

### Phase 2: Write-through (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Patterns/CacheWriteThroughPostCommitProcessor.cs` | new (internal sealed) |
| 2 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (register the open generic processor when `EnableWriteThrough` is on) |
| 3 | `src/Encina.Caching/Diagnostics/CacheStrategyLog.cs` | new (EventIds 500, 501 and 507 here; 502 in PR 3, 503-506 in PR 4) |
| 4 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.Strategies.cs` | new (partial of #266's class; the `encina.cache.write_through` counter, emitted by the processor of this PR) |
| 5 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingStrategies = (500, 507)`) |
| 6 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (`Encina.Caching` maps to `Caching` and `CachingStrategies`) |
| 7 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the new `EventIdRanges` field) |
| 8 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Caching/CacheWriteThroughPostCommitProcessorTests.cs` | new (includes the `MeterListener` test of the counter) |
| 10 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (DI test with `ValidateOnBuild` and `ValidateScopes`; the processor is registered only when the option is on) |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/272-cache-write-through.added.md` | new (`mechanical-fixer`) |

PR 2 needs #266 PR 4 (`CachePatternMetrics.cs`) merged first.

Tasks:

1. **Create** `internal sealed partial class CacheWriteThroughPostCommitProcessor<TRequest, TResponse> : IRequestPostCommitProcessor<TRequest, TResponse> where TRequest : IRequest<TResponse>` with a constructor whose parameters are all required, because a new class has no call sites to keep compiling and an optional parameter would hide a missing registration: `(ICacheProvider, ICacheKeyGenerator, IOptions<CachingOptions>, ILogger<...>, TimeProvider, IEnumerable<MultiTenancyMarker>)` (an empty sequence means multi-tenancy is off, which DI supplies explicitly) and `ValueTask<Either<EncinaError, Unit>> Process(TRequest, IRequestContext, TResponse, CancellationToken)`.
2. Flow of `Process`: no `[Cache(Strategy = WriteThrough)]` on the type, or `EnableQueryCaching` off: return `Right`; with multi-tenancy on and no tenant on the context: return `Right` without writing (fail closed for the cache, as `QueryCachingPipelineBehavior` does with EventId 3513; log Debug with EventId 507; an existing entry of the key is not touched, so it stays until it expires, and the documentation says so); build the key with `ICacheKeyGenerator.GeneratePatternFromTemplate(queryTemplate, command, context)` (Design Choice 2); write `CacheEntry<TResponse>` (with `CachedAtUtc` and, from #266's envelope, `ExpiresAtUtc`) with `SetAsync` and the attribute duration; on any exception log EventId 501 with `ex.ForLogging()`, try `RemoveAsync`, rethrow only when `ThrowOnCacheErrors` is on; always return `Right`.
3. The key is tenant-qualified by the same generator as the query's key, so a write for tenant A can never be read by tenant B; the processor never logs the raw key (it goes through `CacheKeyRedactor`).
4. **Modify** `AddEncinaCaching`: register `services.AddTransient(typeof(IRequestPostCommitProcessor<,>), typeof(CacheWriteThroughPostCommitProcessor<,>))` only when `EnableWriteThrough` is on (the option and its copy line came with PR 1).
5. **Register** the range in `EventIdRanges.cs` and the assembly map; EventIds: 500 write-through applied (Debug), 501 write-through failed (Warning), 507 write-through skipped for no tenant (Debug); the counter `encina.cache.write_through` (tag `outcome`: applied, failed, skipped) is emitted by the processor, so the metrics partial is created here. `[LoggerMessage]` source generator, XML docs naming the range.
6. **Tests (unit):** applied on `Right` for the matching key, tenant-qualified key for two tenants, no write without tenant under multi-tenancy, set failure returns `Right` and removes the key, `ThrowOnCacheErrors` rethrows, a `Left` handler result never reaches the processor (pipeline-level test with `PipelineBuilder`), the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`. Targets in Research.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: CacheStrategy and the CacheAttribute properties exist. IRequestPostCommitProcessor<TRequest,TResponse>
(src/Encina/Abstractions/IRequestPostCommitProcessor.cs) runs after the commit; PipelineBuilder wraps registered processors.

TASK:
1. Create CacheWriteThroughPostCommitProcessor (required constructor parameters) and the opt-in registration; EnableWriteThrough exists from PR 1.
2. Create CacheStrategyLog (EventIds 500, 501 and 507) and the CachePatternMetrics.Strategies.cs partial, register CachingStrategies = (500, 507) in EventIdRanges.cs and the assembly map.
3. Write the unit tests and the DI test; ask mechanical-fixer for the two PublicAPI files, the manifest rows and the changelog fragment.

KEY RULES:
- The processor never returns Left (the commit already happened); failures are logged by exception type and ForLogging().
- The key is built by the same generator as the query's key (tenant-qualified, injective); no tenant under multi-tenancy writes nothing.
- Never log the raw key or EncinaError.Message; time from TimeProvider.
- Registered only when EnableWriteThrough is on (pay-for-what-you-use).

REFERENCE FILES:
- src/Encina/Abstractions/IRequestPostCommitProcessor.cs
- src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 3: The write-behind queue, behavior and contracts (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 15 files: the 13 rows below and the two settings files named after the table. It carries no changelog fragment: a queue and a behavior nothing registers have no user-visible effect, and PR 4's fragment covers write-behind.

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/WriteBehindAbstractions.cs` | new (public `IWriteBehindBatchHandler<TRequest>`, `WriteBehindBatchContext`, optional `IWriteBehindCacheProjection<TRequest>`, `IWriteBehindDeadLetterSink`: one file for the four small contracts) |
| 2 | `src/Encina.Caching/Patterns/WriteBehindQueue.cs` | new (internal sealed, bounded `Channel`) |
| 3 | `src/Encina.Caching/Behaviors/WriteBehindPipelineBehavior.cs` | new (internal sealed) |
| 4 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`WriteBehind` sub-options: `QueueCapacity`, `FlushRetryCount`, `FlushRetryDelay`, `ShutdownDrainTimeout`) |
| 5 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (the hand-written copy line for the `WriteBehind` sub-options object) |
| 6 | `src/Encina.Caching/Diagnostics/CacheStrategyLog.cs` | modify (EventId 502, emitted by the behavior) |
| 7 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.Strategies.cs` | modify (the `encina.cache.write_behind.enqueued` counter, emitted by the behavior) |
| 8 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Caching/WriteBehindQueueTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Caching/WriteBehindPipelineBehaviorTests.cs` | new (includes the `MeterListener` test of the counter) |
| 11 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify (defaults and the copy line through `AddEncinaCaching`; the rule tests are in `CacheStrategySettingsTests.cs`) |
| 12 | `tests/Encina.ContractTests/Caching/WriteBehindContractTests.cs` | new (executes the real queue and behavior) |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |

`src/Encina.Caching/Patterns/CachePatternSettings.cs` and `tests/Encina.UnitTests/Caching/CacheStrategySettingsTests.cs` also change here (the write-behind option rules of Design Choice 9).

Tasks:

1. **Create** `IWriteBehindBatchHandler<TRequest>` (`ValueTask<Either<EncinaError, Unit>> HandleBatchAsync(IReadOnlyList<TRequest> batch, WriteBehindBatchContext context, CancellationToken cancellationToken)`; `WriteBehindBatchContext` is a public record with `BatchId` (Guid, so a handler can deduplicate a retry) and `TenantId`), `IWriteBehindCacheProjection<TRequest>` (`(string Key, object Value)?` from a request, null when nothing to cache), and `IWriteBehindDeadLetterSink` (`ValueTask WriteAsync(IReadOnlyList<object> batch, WriteBehindBatchContext context, string errorCode, CancellationToken cancellationToken)`).
2. **Create** `WriteBehindQueue` (internal): `Channel.CreateBounded` of capacity `QueueCapacity` (default 10,000), `TryEnqueue` (never blocks), per-tenant grouping in `DequeueBatchAsync`, depth property for the metric.
3. **Create** `WriteBehindPipelineBehavior<TRequest, TResponse>`: for a `[Cache(Strategy = WriteBehind)]` request, after the bypass rules (caching off, multi-tenancy with no tenant: run `nextStep()` synchronously, the cache is never involved), enqueue and return `Right(default)` (the response is `Unit`, Design Choice 9); when the queue is full return `EncinaErrors.Create` with `ErrorCategory.Unavailable` and code `cache.write_behind_queue_full`, constant message "The write-behind queue is full." and log EventId 502 with the request type only; call the optional projection and write the entry (a failure there is logged, never fails the request).
4. **Tests (unit and contract):** FIFO per tenant, a batch never mixes tenants, capacity rejection, a full queue returns the `Left`, the projection runs, no-tenant bypass. The contract test builds the real queue and behavior and asserts the handler never runs in the request path.
5. **Guard tests:** none for the internal types; the public interfaces and the record have no executable validated parameters. The manifest reasons say so; the first public registration method arrives in PR 4 with its guard test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged. Write-behind returns to the caller as soon as the request is queued; a hosted service (Phase 4) flushes
batches through IWriteBehindBatchHandler<TRequest>.

TASK:
1. Create the public abstractions file, WriteBehindQueue and WriteBehindPipelineBehavior (required constructor parameters); add the WriteBehind sub-options to CachingOptions with their copy line in AddEncinaCaching and their rules in CachePatternSettings.
2. Add EventId 502 to CacheStrategyLog (range 500-507 already registered) and the enqueued counter to the metrics partial.
3. Write the unit and contract tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest rows and the changelog fragment.

KEY RULES:
- Fail closed: no tenant under multi-tenancy runs the handler synchronously and never queues.
- A batch never mixes tenants; no user identity is stored in the queue.
- A full queue returns a Left with ErrorCategory.Unavailable and a constant message; never drop silently.
- Nothing is registered publicly in this PR (AddEncinaWriteBehind arrives in Phase 4).

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Results/EncinaErrors.cs
- src/Encina/Errors/ErrorCategory.cs
- tests/Encina.ContractTests/Database/Caching/QueryCachingContractTests.cs
```

</details>

### Phase 4: The flush service, registration, health and metrics (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Services/WriteBehindFlushService.cs` | new (internal sealed `BackgroundService`) |
| 2 | `src/Encina.Caching/Services/LoggingWriteBehindDeadLetterSink.cs` | new (internal, default sink) |
| 3 | `src/Encina.Caching/Health/WriteBehindHealthCheck.cs` | new (`EncinaHealthCheck`) |
| 4 | `src/Encina.Caching/WriteBehindServiceCollectionExtensions.cs` | new (public `AddEncinaWriteBehind`) |
| 5 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.Strategies.cs` | modify (flushed, dead-lettered and queue-depth instruments, emitted by the flush service) |
| 6 | `src/Encina.Caching/Diagnostics/CacheStrategyLog.cs` | modify (EventIds 503-506, emitted by the flush service) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/WriteBehindFlushServiceTests.cs` | new (`FakeTimeProvider`) |
| 9 | `tests/Encina.UnitTests/Caching/WriteBehindHealthCheckTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Caching/WriteBehindRegistrationTests.cs` | new (`ValidateOnBuild`, `ValidateScopes`) |
| 11 | `tests/Encina.GuardTests/Caching/WriteBehindServiceCollectionExtensionsGuardTests.cs` | new |
| 12 | `tests/Encina.UnitTests/Caching/CachePatternMetricsStrategiesTests.cs` | new (`MeterListener`: flush instruments) |
| 13 | `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheStrategiesIntegrationTests.cs` | new (shared Redis fixture: write-through and a write-behind flush against a real provider, Design Choice 11) |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/272-cache-write-behind.added.md` | new (`mechanical-fixer`: covers PR 3 and PR 4) |

Tasks:

1. **Create** `WriteBehindFlushService : BackgroundService`: a `PeriodicTimer` at the smallest `FlushIntervalSeconds` of the registered request types (per-type intervals are honored by a per-type due time), flush also when a type reaches its `BatchSize`; each iteration catches its failures (never lets an exception escape; log EventId 506 by exception type, keep running); a `Left` or exception from the batch handler is retried `FlushRetryCount` times with `FlushRetryDelay` and then handed to `IWriteBehindDeadLetterSink` (EventIds 503 retry, 504 dead-lettered); on `StopAsync` it drains for `ShutdownDrainTimeout` and logs EventId 505 with the number of undrained items. The service starts no work from an ambient request: it is created at host start with no `RequestContextAccessor` holder, and each batch runs in a new `IServiceScopeFactory` scope whose context carries only the batch's tenant id, so no request identity (an `AsyncLocal` flows into any work started from a request) can leak into the flush. Stopping cancels the loop with `OperationCanceledException`, which is not an error and not a dead-letter cause.
2. **Create** `AddEncinaWriteBehind(this IServiceCollection)` (public, `ArgumentNullException.ThrowIfNull`): registers the queue, the behavior as an open generic `IPipelineBehavior<,>`, the flush service, the default sink with `TryAdd` and the health check; it is the only way to switch write-behind on.
3. **Create** `WriteBehindHealthCheck` (DefaultName const, static tags): Degraded at 80 percent of `QueueCapacity`, Unhealthy while the flush service is faulted; `Healthy` otherwise. Provider health of the cache itself stays with #754.
4. **Modify** `CachePatternMetrics.Strategies.cs` (created in PR 2, extended in PR 3): the counters `.flushed` and `.dead_lettered` and an observable gauge `encina.cache.write_behind.queue_depth`; the tag `encina.tenant_id` only when multi-tenancy is on (ADR-054 D4).
5. **Tests:** retry then dead-letter with `FakeTimeProvider`, a throwing handler does not stop the loop, shutdown drain, a tenant-split batch, health thresholds, DI built with `ValidateOnBuild` and `ValidateScopes` (no scoped service injected into a singleton: the flush service resolves the handler from an `IServiceScopeFactory` scope per batch), the guard test for `AddEncinaWriteBehind` (null services).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1-3 are merged: the queue, the behavior and the abstractions exist but nothing flushes. #266 PR 4 created CachePatternMetrics.cs.

TASK:
1. Create WriteBehindFlushService, the default dead-letter sink, WriteBehindHealthCheck and AddEncinaWriteBehind.
2. Add EventIds 503-506 to CacheStrategyLog and the flush instruments to the metrics partial CachePatternMetrics.Strategies.cs; add the Redis integration test.
3. Write the unit, DI and guard tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest rows and the changelog fragment.

KEY RULES:
- The loop catches per iteration and never lets an exception escape; a Left from the batch handler is retried then dead-lettered, never counted as flushed.
- Resolve the batch handler from IServiceScopeFactory per batch; never inject a scoped service into the singleton service.
- Time from TimeProvider; logs carry the request type and the error code, never EncinaError.Message or request content.
- Nothing starts unless AddEncinaWriteBehind was called.

REFERENCE FILES:
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- src/Encina.Caching/Health/QueryCacheHealthCheck.cs
- src/Encina.Caching/Diagnostics/CachePatternMetrics.cs
```

</details>

### Phase 5: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** `CachingStrategies` 500-507 only (500 write-through applied, 501 failed, 502 queue full, 503 flush retry, 504 batch dead-lettered, 505 stopped with undrained items, 506 flush iteration faulted, 507 write-through skipped for no tenant); verified free by a repo-wide grep of `src/Encina.Caching` (its used range is 3500-3513) and registered in `EventIdRanges.cs` and the `AssemblyRanges` map in PR 2; emitted only by `Encina.Caching`.
2. **Error codes:** one, `cache.write_behind_queue_full`, ADR-062 `ErrorCategory.Unavailable`, constant message; no code is created for the post-commit write (it never returns `Left`).
3. **Coverage targets (per flag, per PR, set in the PR that adds the file):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Patterns/CacheWriteThroughPostCommitProcessor.cs` | 95 | 0 | 0 | 0 | Every branch is driven with a mocked provider; internal, no `InternalsVisibleTo` for the guard project |
| `Encina.Caching/Patterns/WriteBehindQueue.cs` | 100 | 0 | 80 | 0 | Pure channel logic driven by unit tests and by the contract test; internal |
| `Encina.Caching/Behaviors/WriteBehindPipelineBehavior.cs` | 95 | 0 | 70 | 0 | The contract test runs the real behavior and queue; internal |
| `Encina.Caching/Services/WriteBehindFlushService.cs` | 90 | 0 | 0 | 0 | Driven with `FakeTimeProvider`; the process-stop path is the only uncovered part; internal |
| `Encina.Caching/Services/LoggingWriteBehindDeadLetterSink.cs` | 100 | 0 | 0 | 0 | One log call |
| `Encina.Caching/Health/WriteBehindHealthCheck.cs` | 100 | 0 | 0 | 0 | Three thresholds |
| `Encina.Caching/WriteBehindServiceCollectionExtensions.cs` | 100 | 100 | 0 | 0 | One null guard and the registrations, covered by the guard and DI tests |
| `Encina.Caching/Diagnostics/CacheStrategyLog.cs`, `CachePatternMetrics.Strategies.cs` | 90 | 0 | 0 | 0 | Generated bodies and instrument calls run in the behavior tests |
| `Encina.Caching/Abstractions/IWriteBehind*.cs`, `Configuration/CacheStrategy.cs` | 0 | 0 | 0 | 0 | Interfaces, a record and an enum: no executable lines |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`, `Attributes/CacheAttribute.cs`, `Configuration/CachingOptions.cs`, `ServiceCollectionExtensions.cs` | keep | keep | 0 | 0 | Existing targets are never lowered; the new branches are tested in the same PRs |

   The worker measures every flag with `coverage-report.cs` per PR and records the numbers in the PR description; no target is lowered (`target-ratchet`).
4. **Property, load, benchmark:** no property test (no invariant beyond the unit grid); no load test (the queue is a bounded channel covered by gated unit tests); a benchmark of the enqueue path is a follow-up inside the existing caching benchmarks, not a PR of this plan.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The four code PRs are merged. Verification and finalization remain.

TASK:
Verify that each PR carried its own per-flag targets (table above) and the EventId registration; if one was missed add it in a small PR
with one-sentence justifications through mechanical-fixer; run --check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet).
- Add no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 6: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation with `<example>` on `CacheStrategy`, the three abstractions, `AddEncinaWriteBehind` and the new `CacheAttribute` and `CachingOptions` members; the remarks state the loss window of write-behind and that a bypassed request is never queued.
2. `docs-writer`: a "Cache strategies" section in `docs/features/handler-caching.md` (the handler-caching page owned by the #266 family; `docs/features/query-caching.md` is the EF Core query-cache page and is not touched; the #84 overview links to the handler page) with the strategy comparison and use-case guidance (the issue's acceptance criterion), citing tests by name and no typed figure.
3. `PublicAPI.Unshipped.txt` lines are written by `mechanical-fixer` per PR.
4. No ROADMAP, INVENTORY or ADR change: no architectural decision beyond this plan; ADR number assigned when the ADR pull request opens, if the maintainer asks for one.
5. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests` and `Encina.ContractTests`; per-flag coverage and the CRAP table of the changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
All code PRs are merged. Documentation remains.

TASK:
Confirm the XML docs and the four changelog fragments; ask docs-writer for the handler-caching.md section.

KEY RULES:
- No hand-typed figures; cite tests by name.
- Fragment section is added; never edit CHANGELOG.md.

REFERENCE FILES:
- docs/features/handler-caching.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 5 (caching category) | A caching feature is consistent across all 8 providers: behavior-level here |
| AGENTS.md section 3 | Opt-in patterns; errors never swallowed in background infrastructure; background loops never throw; time from `TimeProvider` |
| ADR-054 D4 | Tenant tag on metrics only with multi-tenancy on |
| ADR-021 | EventId range registered before use |
| ADR-062 | `cache.write_behind_queue_full` is `Unavailable` |
| Read-through, write-through and write-behind (Hazelcast, Caffeine `CacheWriter`, NCache named as comparable products) | Vocabulary and the loss window of write-behind |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | Read-through path; skips write strategies |
| `IRequestPostCommitProcessor<,>` and `PostCommitProcessorStep` | `src/Encina/Abstractions/IRequestPostCommitProcessor.cs`, `src/Encina/Pipeline/Steps/PostCommitProcessorStep.cs` | Write-through stage |
| `DefaultCacheKeyGenerator` and `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/` | Tenant-qualified keys and redacted logs |
| `CachePatternSettings`, `CachePatternMetrics` (#266) | `src/Encina.Caching/Patterns/`, `Diagnostics/` | Extended, not redefined |
| `EncinaHealthCheck` | `src/Encina.Messaging/Health/EncinaHealthCheck.cs` | Health check base |
| `TopologyRefreshHostedService` | `src/Encina.Caching/Sharding/Services/` | Precedent for a `PeriodicTimer` loop in this package |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared container of the integration test of PR 4 (Design Choice 11) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingStrategies`, block 500-549 of the plan batch registry) | 500-507 | This plan: 500 write-through applied, 501 failed, 502 queue full, 503 flush retry, 504 batch dead-lettered, 505 stopped with undrained items, 506 flush iteration faulted, 507 write-through skipped for no tenant. Family: #274 508-513, #275 514-519, #278 520-531, #276 532-536 (each registers its own range); 537-549 free. The existing `Caching` range 3500-3549 is untouched |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 12 | 8 | New: enum, processor, three abstractions, queue, behavior, flush service, sink, health check, registration, log, metrics partial; modified: attribute, options, settings, behavior, DI, `EventIdRanges.cs`, two PublicAPI files |
| Tests | 10 | 5 | New: unit (7), guard (1), contract (1), plus the metrics test; modified: attribute, options, DI, allocation test |
| Documentation | 0 | 1 | `docs/features/handler-caching.md` |
| Configuration | 4 | 4 | Four changelog fragments; manifest edited in each PR |
| **Total** | **26** | **18** | 4 pull requests: 13, 12, 15 and 15 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #272 adds cache strategies to [Cache]. The pipeline
behavior QueryCachingPipelineBehavior already is read-through; this plan names and validates it, adds write-through as a post-commit
processor and write-behind as an opt-in behavior with a bounded queue and a flush hosted service. All 8 cache providers behave the same
because everything lives at the behavior level.

IMPLEMENTATION OVERVIEW:
1. PR 1: CacheStrategy, attribute properties, settings validation, write strategies skip the read-through path.
2. PR 2: CacheWriteThroughPostCommitProcessor, EnableWriteThrough, EventIds 500-501, CachingStrategies range.
3. PR 3: write-behind abstractions, WriteBehindQueue, WriteBehindPipelineBehavior, EventId 502.
4. PR 4: WriteBehindFlushService, dead-letter sink, health check, AddEncinaWriteBehind, metrics, EventIds 503-507.

KEY PATTERNS:
- Fail closed: no tenant under multi-tenancy never reaches the cache or the queue.
- Post-commit writes never return Left; background Left is retried then dead-lettered, never reported as success.
- Time from TimeProvider; logs carry redacted keys and error codes only; tenant metric tag only with multi-tenancy on.
- Every new public symbol has PublicAPI.Unshipped.txt lines and, where it validates parameters, guard tests.
- Follow the maintainer's answers to Design Choices 1, 2, 5, 6, 7, 8 and 9, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Abstractions/IRequestPostCommitProcessor.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature itself; behavior-level on all 8 providers. |
| 2 | OpenTelemetry | ✅ Include | Counters and a queue-depth gauge in `CachePatternMetrics.Strategies.cs`; tenant tag only with multi-tenancy on (ADR-054 D4); spans belong to #723 (open). |
| 3 | Structured Logging | ✅ Include | `CacheStrategyLog`, EventIds 500-507 in `CachingStrategies`. |
| 4 | Health Checks | ✅ Include | `WriteBehindHealthCheck` (queue depth, faulted service); provider health stays with #754. |
| 5 | Validation | ✅ Include | Attribute and option rules validated at resolution (Design Choice 9). |
| 6 | Resilience | ✅ Include | Bounded retry then dead-letter for a failed flush; no Polly dependency is added to `Encina.Caching`. |
| 7 | Distributed Locks | ❌ N/A | Each node's queue is node-local and its items are not shared; cross-node stampede on a miss belongs to #266. |
| 8 | Transactions | ✅ Include | Write-through runs in the post-commit stage; the orchestrator never calls `SaveChangesAsync` and the plan claims no atomicity. |
| 9 | Idempotency | ✅ Include | The batch carries a `BatchId` so a handler can deduplicate a retried batch; the delivery is at least once. |
| 10 | Multi-Tenancy | ✅ Include | Tenant-qualified keys from the existing generator; no tenant under multi-tenancy bypasses cache and queue (fail closed); batches never mix tenants; tenant quotas are #275. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped keys follow the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | A cache and persistence-latency pattern; a dead-lettered batch is an operational event logged by code, not a compliance event. |

**Deferred items:** module-scoped keys (#746, open); durable write-behind queue (`artifacts/issues/durable-write-behind-queue.md`, to be opened by the orchestrator).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `QueryCachingPipelineBehavior`, `IRequestPostCommitProcessor`, `IDeadLetterStore` | ✅ Available | On `origin/main` |
| #266 PR 1 (`CachePatternSettings`, envelope) and PR 4 (`CachePatternMetrics.cs`) | Open | PR 1 and PR 2 of this plan need PR 1; PR 4 needs #266 PR 4 |
| Block 500-549 of the EventId registry | ✅ Assigned | Plan batch registry |
| Maintainer answers to Design Choices 1, 2, 5, 6, 7, 8 and 9 | Pending | Asked when work starts |

#277 (Memcached) needs nothing from this plan.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 2, 5, 6, 7, 8 and 9) when work starts, records the answers under `## Maintainer Decisions`, then briefs four workers in the PR order.
2. Open the follow-up issue file for the durable write-behind queue.
