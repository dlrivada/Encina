# Implementation Plan: `Encina.Caching` idempotency store abstraction under the existing idempotency behavior

> **Issue**: [#334](https://github.com/dlrivada/Encina/issues/334)
> **Type**: Feature
> **Verified against**: origin/main `ec129cae5` (2026-11-17)
> **Complexity**: Medium (5 phases, no database provider code, ~38 files in 5 pull requests)
> **Estimated Scope**: ~650-800 lines of production code + ~1,000-1,300 lines of tests

---

## Summary

Issue #334 asks for "a pipeline behavior that provides automatic idempotency checking at the mediator level, complementing the Inbox for simpler use cases", with an `IIdempotencyStore`, in-memory, Redis and database backends, attribute configuration, a TTL and duplicate-rate metrics. Most of the behavior already exists, so the issue text ("No simple check and skip if duplicate behavior") is stale. This plan records what exists today and plans only the gap.

**What the code does today (verified on `origin/main` `ec129cae5`)**:

- `IRequestContext.IdempotencyKey` (`src/Encina/Abstractions/IRequestContext.cs:116`) carries the key. The ASP.NET Core host fills it from the header `EncinaAspNetCoreOptions.IdempotencyKeyHeader` (default `X-Idempotency-Key`, `src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs:50`) through `HttpContextInboundRequestExtensions.cs:100`, bounded to 255 characters by `InboundRequestInfo.MaxIdempotencyKeyLength`.
- `DistributedIdempotencyPipelineBehavior<TRequest, TResponse>` (`src/Encina.Caching/Behaviors/DistributedIdempotencyPipelineBehavior.cs`) is the lightweight behavior of the issue: it applies to requests marked `IDistributedIdempotentRequest`, keeps its state in `ICacheProvider` under `{IdempotencyKeyPrefix}:{TenantId}:{key}` (`CachingOptions.IdempotencyKeyPrefix = "sm:idempotency"`, `IdempotencyTtl = 24 h`, `EnableDistributedIdempotency = true`) and is registered by `AddEncinaCaching` (`ServiceCollectionExtensions.cs:88-90`).
- `InboxPipelineBehavior<,>` with the marker `IIdempotentRequest` (`src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs`) is the durable, database-backed sibling on the 10 providers.
- The cache backends are the cache providers: `MemoryCacheProvider`, `HybridCacheProvider` and `RedisCacheProvider` (Valkey, Dragonfly, Garnet and KeyDB register the Redis provider), so "in-memory" and "Redis" backends exist.
- Not present: an `IIdempotencyStore` abstraction (the behavior talks to `ICacheProvider` and an `internal sealed class IdempotencyEntry<TResponse>`), any metric or activity tag for duplicate detection (`Encina.Caching` has no metrics class), and a way for a host to learn that an answer was a replay (needed by [#355](https://github.com/dlrivada/Encina/issues/355), the Stripe-style HTTP face of the same feature).

**Defects of the existing behavior are separate bugs and are not planned here**: [#3439](https://github.com/dlrivada/Encina/issues/3439) (a stuck in-progress entry after an exception, transient failures cached for 24 hours, a non-atomic get-then-set claim, fail-open on cache errors), [#3438](https://github.com/dlrivada/Encina/issues/3438) (key without request type, principal or payload fingerprint; a missing key answers 500; open `needs-decision`) and [#2452](https://github.com/dlrivada/Encina/issues/2452) (a tenant-less caller shares the `prefix::key` entry). This plan gives those fixes a seam (the store contract has the three operations they need: claim, complete, release) and does not edit the behavior's logic beyond moving it onto that seam.

What it implements:

- `IIdempotencyStore` (`Encina.Caching`): an atomic `TryBeginAsync` (claim with a lease, or the existing completed record, or in-progress, or a fingerprint mismatch), `CompleteAsync` and `ReleaseAsync`, all returning `Either<EncinaError, T>` (ADR-001, ADR-006), plus `CacheIdempotencyStore` over `ICacheProvider` (so every cache provider is a backend) and the record and claim types.
- `DistributedIdempotencyPipelineBehavior` rewritten onto the store (same class and marker names; no second behavior beside it, AGENTS.md section 3).
- `IdempotencyOutcome` and `IIdempotencyOutcomeAccessor` in the core `Encina` package: the behavior records `Executed`, `Replayed` or `InProgress` for the current scope, so [#355](https://github.com/dlrivada/Encina/issues/355) can set `Idempotent-Replayed` and a later work can reuse it for the inbox behavior.
- Observability: a counter of idempotency decisions (duplicate-detection rate), an activity tag and `[LoggerMessage]` entries (Phase 4).

Standards and specifications: ADR-001 and ADR-006 (Railway Oriented Programming); ADR-062 (error category set where the error is created); ADR-054 D3 and D4 (package-named meter without `_total`; tenant tag when tenancy is on); ADR-050 (Caching row: tenant in the key when multi-tenant); the IETF "Idempotency-Key HTTP header field" draft (an in-flight duplicate is answered 409; a reused key with another payload is rejected); Stripe idempotent requests (named comparable product); AGENTS.md section 3 (a store `Left` fails the operation; `TimeProvider` for time; `EncinaError.Message` never reaches logs).

Estimated scope: about 38 distinct files (10 new and 5 modified production files, 7 metadata files, 15 test files, 1 documentation file), shipped as five pull requests (A, B, B2, C, D; see "Pull request split").

Affected packages: `Encina` (outcome types, error codes, `EventIdRanges.cs`), `Encina.Caching` (store, behavior, metrics, log, DI). No change to `Encina.AspNetCore` here (that is [#355](https://github.com/dlrivada/Encina/issues/355)).

Provider category: Caching (8), consumed through `ICacheProvider` (the three implementations `MemoryCacheProvider`, `HybridCacheProvider`, `RedisCacheProvider`, the last one serving Redis, Valkey, Dragonfly, Garnet and KeyDB). Memcached is planned and not built, so it is outside this plan. The Database category is not applicable here: the durable path is the Inbox (Design Choice 4).

---

## Design Choices

<details>
<summary><strong>1. Store abstraction — a new <code>IIdempotencyStore</code>, or the behavior keeps talking to <code>ICacheProvider</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the behavior over `ICacheProvider`; add metrics and the outcome accessor only** | Smallest diff; no new public abstraction | The claim, lease, replay and fingerprint logic stays welded to a cache API with no atomic operation; the issue's `IIdempotencyStore` acceptance item is not met; [#355](https://github.com/dlrivada/Encina/issues/355) needs the same operations and would duplicate them |
| **B) `IIdempotencyStore` in `Encina.Caching` with `CacheIdempotencyStore` over `ICacheProvider`; the behavior depends only on the store** | One seam where [#3439](https://github.com/dlrivada/Encina/issues/3439) puts the atomic claim, the lease and the release; the behavior is testable with a fake store; another backend becomes possible without touching the behavior | One more public abstraction and one adapter |
| **C) `IIdempotencyStore` in core `Encina`, with a built-in in-memory store** | Usable without `Encina.Caching` | A second in-memory implementation next to `MemoryCacheProvider`; core grows for one backend that already exists |

### Chosen Option: **B — store in `Encina.Caching` over `ICacheProvider`** (recommended, pending the maintainer)

### Rationale

- B wins when the behavior's logic must be tested and fixed independently of a cache API, which is the case once [#3439](https://github.com/dlrivada/Encina/issues/3439), [#3438](https://github.com/dlrivada/Encina/issues/3438) and [#2452](https://github.com/dlrivada/Encina/issues/2452) change it. A wins when the maintainer wants no new public type before 1.0. C wins only if a non-cache application wants in-process idempotency without referencing `Encina.Caching`, which `MemoryCacheProvider` already serves.
- Only the outcome types go to core (Design Choice 6) because the host must read them.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. The atomic claim — owned by #3439, adopted here</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A set-if-absent primitive on `ICacheProvider` (Redis `SET NX`, memory `TryAdd`, Hybrid per its L2)** | One round trip; the lease is the entry's TTL; no extra dependency | Changes the interface that has 3 production implementers, `InstrumentedCacheProvider`, `FakeCacheProvider` and 5 test doubles (counted by grep on `origin/main`) |
| **B) `IDistributedLockProvider` around the claim (`Encina.Caching` already references `Encina.DistributedLock`)** | No change to `ICacheProvider` | Needs a lock provider on every idempotency user; the in-memory lock is not valid on several instances (ADR-050, Distributed locks row); lock expiry can let two callers in |
| **C) Fix it only per store: `CacheIdempotencyStore` documents that the claim is best effort** | No interface change | Keeps the defect [#3439](https://github.com/dlrivada/Encina/issues/3439) reports |

### Chosen Option: **A — a set-if-absent primitive on `ICacheProvider`, delivered by #3439** (recommended, pending the maintainer)

### Rationale

- The mechanism belongs to [#3439](https://github.com/dlrivada/Encina/issues/3439) (its expected behavior: "the claim should be atomic"); this plan opens no second decision on it. The store contract of Phase 2 (`TryBeginAsync`) is written so that any of A or B satisfies it, and Phase 2 starts only after #3439 chose.
- A wins when the cache providers can offer the primitive on all 8 backends; B wins when the maintainer does not want to widen `ICacheProvider`.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Key shape and payload fingerprint — owned by #3438, adopted here</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The behavior passes the store an opaque, already composed key and a fingerprint string (possibly empty); the composition rule is #3438's** | Key policy decided once, in one place; the store stays generic | The behavior depends on the outcome of an open decision |
| **B) Fix the shape here (`prefix:tenant:requestType:key`, no fingerprint)** | Independent of #3438 | Pre-empts #3438's open decision (principal, payload hash) and would be rewritten |
| **C) The store composes the key from tenant, request type and fingerprint** | The store guarantees isolation for every user | The store needs request-type and principal knowledge it should not have |

### Chosen Option: **A — opaque key and fingerprint from the behavior, rule owned by #3438** (recommended, pending the maintainer)

### Rationale

- [#3438](https://github.com/dlrivada/Encina/issues/3438) is `needs-decision` and decides what makes two requests "the same"; this plan only guarantees that the store treats the key as opaque and injective and carries the fingerprint to the mismatch outcome. The tenant segment follows [#2452](https://github.com/dlrivada/Encina/issues/2452): with multi-tenancy on and no tenant the behavior fails closed (no claim, no replay), and with tenancy off the reserved segment `default` is used, so no two tenant and key pairs map to one id.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Durable backend — reuse the Inbox, or an <code>IIdempotencyStore</code> on the 10 database providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No database implementation of `IIdempotencyStore`; the durable path is the Inbox (`IIdempotentRequest`, atomic claim of [#2401](https://github.com/dlrivada/Encina/issues/2401))** | No new schema on 10 providers; one durable mechanism; the issue's "Database (existing stores)" is the Inbox's stores | An application that wants durable HTTP idempotency must enable the Inbox |
| **B) `IIdempotencyStore` on ADO.NET, Dapper, EF Core (SqlServer, PostgreSQL, MySQL) and MongoDB** | Durable without the Inbox | Duplicates the Inbox on 10 providers (AGENTS.md section 5): about 35 files, a second table with the same lifecycle |
| **C) An `InboxIdempotencyStore` adapter implementing `IIdempotencyStore` over `IInboxStore`** | One abstraction over both | The Inbox stores a cached response and retry state with different semantics (handler exceptions count retries, [#2084](https://github.com/dlrivada/Encina/issues/2084)); the adapter would fight them |

### Chosen Option: **A — the Inbox is the durable backend; no new database stores** (recommended, pending the maintainer)

### Rationale

- A wins for the lightweight use case the issue itself names ("Lightweight cache" versus "Full message store" in its comparison table); B wins when an application needs durable idempotency but cannot run the Inbox, which the issue does not claim.
- [#355](https://github.com/dlrivada/Encina/issues/355) asks for a SQL Server store with the same reasoning; its plan refers here so the maintainer answers once.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Request-derived keys and per-request TTL — left to #319</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[Idempotent(KeyExpression = "...")]` and a per-type TTL here** | Matches the issue's example | A string expression needs runtime compilation (AOT); it duplicates the key generator that [#319](https://github.com/dlrivada/Encina/issues/319) designs (`IIdempotencyKeyGenerator`, `[IdempotencyKey]`) |
| **B) This plan consumes `IRequestContext.IdempotencyKey` only; key derivation from request properties is #319's scope** | No duplicate; the behavior needs no knowledge of how the key was made | The issue's attribute item is met by #319, not here |

### Chosen Option: **B — consume the context key; derivation stays with #319**

### Rationale

- Both behaviors of today already read only `context.IdempotencyKey` (`InboxPipelineBehavior.cs`, `DistributedIdempotencyPipelineBehavior.cs:88`), so B changes nothing in how a key reaches the pipeline.
- The global TTL (`CachingOptions.IdempotencyTtl`) stays; a per-type TTL attribute is the same scope as #319's attribute.
- **Class:** Settled
- **Settled by:** [#319](https://github.com/dlrivada/Encina/issues/319) (the Idempotency Key Generator issue owns key derivation from request properties and the declarative key attribute) and AGENTS.md section 3 (no second mechanism beside an existing one)

</details>

<details>
<summary><strong>6. Where the replay outcome lives — core types and a scoped accessor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IdempotencyOutcome` and `IIdempotencyOutcomeAccessor` in core `Encina`, written by the behavior, read by the host** | Neutral package: `Encina.Caching` references `Encina.Messaging` (not the reverse), and `Encina.AspNetCore` references both, so only core is below all three; the replay header needs it | Hidden per-scope state |
| **B) The behavior returns a wrapper type** | Explicit | Changes `TResponse` for every handler; breaks the pipeline contract |
| **C) No outcome; the host cannot tell a replay** | No new type | The `Idempotent-Replayed` header of [#355](https://github.com/dlrivada/Encina/issues/355) cannot be produced |

### Chosen Option: **A — core outcome types and a scoped accessor**

### Rationale

- The pattern is the one already used for the ambient request context (`IRequestContextAccessor`, `src/Encina/Abstractions/IRequestContextAccessor.cs`): a scoped holder set by the pipeline and read after the dispatch.
- **Class:** Trivial
- **Settled by:** `src/Encina/Abstractions/IRequestContextAccessor.cs` and `src/Encina/Abstractions/IRequestContext.cs:116` (the idempotency key abstractions already live in core)

</details>

---

## Implementation Phases

### Phase 1: Core Models — outcome types and error codes

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Idempotency/IdempotencyOutcome.cs`** (new), namespace `Encina.Idempotency`: `public enum IdempotencyOutcome { None, Executed, Replayed, InProgress }` with XML docs (`None` = no idempotency decision was taken for the scope).
2. **`src/Encina/Idempotency/IIdempotencyOutcomeAccessor.cs`** (new): `IdempotencyOutcome Outcome { get; }` and `void Record(IdempotencyOutcome outcome)`; and **`IdempotencyOutcomeAccessor.cs`** (new, public sealed, scoped): the first non-`None` recording of a scope wins, so a nested dispatch cannot overwrite the entry point's outcome.
3. **`src/Encina/Idempotency/IdempotencyErrorCodes.cs`** (new): `MissingKey = "idempotency.missing_key"`, `InProgress = "idempotency.in_progress"` (the two codes the behavior already uses as literals), and `StoreFailed = "idempotency.store_failed"`. Categories per ADR-062, each asserted by a unit test through `GetCategory()`: `MissingKey` = `InvalidRequest` (the `ErrorCategory.InvalidRequest` remark names "an idempotency key"), `InProgress` = `Conflict` (decided in ADR-062), `StoreFailed` = `Unavailable`.
4. Register the accessor as scoped in the core registration (`TryAddScoped`), in the file that already registers `IRequestContextAccessor`.
5. `PublicAPI.Unshipped.txt` of `Encina` (via `mechanical-fixer` at implementation).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility, ROP everywhere
(Either<EncinaError, T>). Issue #334 puts an idempotency store under the existing
DistributedIdempotencyPipelineBehavior. Phase 1 adds only the core types the host needs.
TASK: Create IdempotencyOutcome, IIdempotencyOutcomeAccessor, IdempotencyOutcomeAccessor (scoped, first non-None
recording wins) and IdempotencyErrorCodes in src/Encina/Idempotency; create each error with its ADR-062 category;
register the accessor with TryAddScoped next to IRequestContextAccessor.
KEY RULES: XML docs on every public symbol; no [Obsolete]; no personal data in error messages; PublicAPI.Unshipped.txt;
zero warnings; unit tests assert GetCategory for each code.
REFERENCE FILES: src/Encina/Abstractions/IRequestContextAccessor.cs, src/Encina/Errors/ErrorCategory.cs,
docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 2: Core Interfaces & Default Implementation — the store and its cache adapter

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Caching/Abstractions/IIdempotencyStore.cs`** (new), namespace `Encina.Caching`: `Task<Either<EncinaError, IdempotencyClaim>> TryBeginAsync(string key, string fingerprint, TimeSpan lease, CancellationToken cancellationToken)`, `Task<Either<EncinaError, Unit>> CompleteAsync(string key, IdempotencyRecord record, TimeSpan ttl, CancellationToken cancellationToken)`, `Task<Either<EncinaError, Unit>> ReleaseAsync(string key, CancellationToken cancellationToken)`.
2. **`IdempotencyRecord.cs`** (new, sealed record): `ResponseType`, `ResponseJson`, `IsSuccess`, `ErrorCode`, `Fingerprint`, `CompletedAtUtc` (the `AtUtc` suffix, from `TimeProvider`). **`IdempotencyClaim.cs`** (new, sealed): `Kind` (`Acquired`, `Completed`, `InProgress`, `FingerprintMismatch`), `Record` for `Completed`, `RetryAfter` (`TimeSpan?`) for `InProgress`; static factories, no public constructor with free state.
3. **`CacheIdempotencyStore.cs`** (new, sealed): constructor `(ICacheProvider cacheProvider, IOptions<CachingOptions> options, TimeProvider? timeProvider = null)`; the claim uses the atomic primitive chosen in Design Choice 2; every cache exception becomes `Left(idempotency.store_failed)` (category `Unavailable`), never a swallowed success (fail closed, #3439).
4. Registration: `TryAddSingleton<IIdempotencyStore, CacheIdempotencyStore>` in `Encina.Caching` `ServiceCollectionExtensions.cs` (the store is stateless over the cache provider, which is registered as a singleton by the caching providers; the plan's DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`).
5. `PublicAPI.Unshipped.txt` of `Encina.Caching`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina.Caching, issue #334. The atomic claim primitive is delivered by issue #3439 (Design Choice 2);
start this phase after it merged and read what it added to ICacheProvider (or the lock-based claim).
TASK: Add IIdempotencyStore, IdempotencyRecord, IdempotencyClaim and CacheIdempotencyStore as described in the plan.
KEY RULES: Either<EncinaError,T> on every member; TimeProvider for every time value (no DateTime.UtcNow); a cache
exception is Left(idempotency.store_failed), never a success; the key is opaque and never logged; XML docs; guard
clauses on every public constructor and method parameter.
REFERENCE FILES: src/Encina.Caching/Abstractions/ICacheProvider.cs,
src/Encina.Caching/Behaviors/DistributedIdempotencyPipelineBehavior.cs, src/Encina.Messaging/Inbox/IInboxStore.cs
```

</details>

### Phase 3: Pipeline Behavior — move the behavior onto the store

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Caching/Behaviors/DistributedIdempotencyPipelineBehavior.cs`** (modify): replace the `ICacheProvider` and `IdempotencyEntry<T>` plumbing with `IIdempotencyStore`; constructor `(IIdempotencyStore store, IOptions<CachingOptions> options, ILogger<...> logger, IIdempotencyOutcomeAccessor? outcome = null, TimeProvider? timeProvider = null)`. The flow: marker and enable flag checks (unchanged), the nested-dispatch rule (unchanged), the key from `context.IdempotencyKey`, `TryBeginAsync`, then by claim kind: `Completed` returns the stored response and records `Replayed`; `InProgress` returns `Left(idempotency.in_progress)` with the remaining lease under `EncinaErrorMetadataKeys.RetryAfter` and records `InProgress`; `FingerprintMismatch` returns the error code owned by #3438; `Acquired` runs `nextStep` and calls `CompleteAsync` (or `ReleaseAsync` when the policy of #3439 says the result must not be cached) and records `Executed`.
2. The in-process `IdempotencyEntry<TResponse>` class is deleted (no compatibility layer).
3. CRAP: the claim switch is its own method; the behavior's `Handle` stays under complexity 10.
4. `Encina.Caching` `PublicAPI.Unshipped.txt` and the manifest entry of the behavior (the "Per-file coverage targets" table in Research).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: issue #334, Phase 3. The store and the outcome accessor exist (Phases 1 and 2). The behavior keeps its class
name, its marker IDistributedIdempotentRequest and its nested-dispatch rule.
TASK: Rewrite DistributedIdempotencyPipelineBehavior over IIdempotencyStore as described; record the outcome in the
accessor; keep Handle small by moving the claim handling into one private method per claim kind.
KEY RULES: a store Left fails the request (never reported as success); Left error codes carry their ADR-062 category;
the key and EncinaError.Message never reach logs or tags; ConfigureAwait(false); no new behavior beside the old one.
REFERENCE FILES: src/Encina.Caching/Behaviors/DistributedIdempotencyPipelineBehavior.cs,
src/Encina/Core/RequestContextDispatchExtensions.cs, tests/Encina.UnitTests/Caching/DistributedIdempotencyPipelineBehaviorTests.cs
```

</details>

### Phase 4: Observability — counter, activity tag and log entries

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Caching/Diagnostics/IdempotencyMetrics.cs`** (new, internal static): `Meter` named `Encina.Caching` (ADR-054 D3, no `_total`), counter `encina.caching.idempotency.requests` with tags `outcome` (`claimed`, `replayed`, `in_progress`, `mismatch`, `store_error`) and `tenant` only when the `MultiTenancyMarker` is registered (ADR-054 D4); the duplicate-detection rate is `replayed / (claimed + replayed)` computed by the metrics backend. Bounded dimensions: no key, no request type with an open set, no error text.
2. The current activity gets the tag `encina.idempotency.outcome` (read from the accessor value just recorded).
3. **`src/Encina.Caching/Diagnostics/IdempotencyLog.cs`** (new, `[LoggerMessage]`, internal static partial): EventIds 7800 (`DuplicateReplayed`), 7801 (`InProgress`), 7802 (`StoreFailed`, exception through `ForLogging()`), 7803 (`ClaimReleased`), registered first as `CachingIdempotency = (7800, 7819)` in `src/Encina/Diagnostics/EventIdRanges.cs`, mapped to `Encina.Caching` in the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. The four ids `3504` to `3507` of the old behavior stay in the `Caching` range with their messages (missing key, duplicate, stored, cache error); the new log entries replace none of them.
4. Register `Encina.Caching` as a meter name with `Encina.OpenTelemetry` only if the package does not already add every `Encina.*` meter (check at implementation; recorded in the Phase 5 task list when a change is needed).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: issue #334, Phase 4. ADR-054 D3/D4 govern meters and tenant tags; ADR-021 governs EventIds.
TASK: Add IdempotencyMetrics, the activity tag and IdempotencyLog (EventIds 7800-7803) and register the range
CachingIdempotency (7800, 7819) in EventIdRanges.cs before using any id; update the allocation test map.
KEY RULES: [LoggerMessage] source generator; ids packed from 7800 without gaps; never log the key, the payload or
EncinaError.Message; exceptions through ForLogging(); bounded metric dimensions; tenant tag only when tenancy is on.
REFERENCE FILES: src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs, src/Encina/Diagnostics/EventIdRanges.cs,
tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 5: Testing and Documentation

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: `CacheIdempotencyStoreTests` (claim, completed, in-progress with remaining lease, mismatch, release, cache exception becomes `Left`), `IdempotencyOutcomeAccessorTests` (first non-`None` wins), the existing behavior tests (`tests/Encina.UnitTests/Caching/DistributedIdempotencyPipelineBehaviorTests.cs`, `...PathsTests.cs`, `Base/DistributedIdempotencyPipelineBehaviorTests.cs`) rewritten over a fake store, and a two-tenant case (same key, two tenants, two entries) plus a tenant-less case under multi-tenancy (fails closed).
2. Guard: `CacheIdempotencyStoreGuardTests`, `IdempotencyOutcomeAccessorGuardTests`, behavior constructor and `Handle` null checks (`Encina.GuardTests`).
3. Contract: `IIdempotencyStoreContractTests` run against `CacheIdempotencyStore` over `MemoryCacheProvider` (real implementation).
4. Integration: the store against the Redis provider through the Testcontainers `[Collection]` fixture (the Valkey, Dragonfly, Garnet and KeyDB packages register the same `RedisCacheProvider`).
5. DI: the existing `ServiceCollectionExtensionsTests` extended: provider built with `ValidateOnBuild` and `ValidateScopes` resolves the store and the behavior.
6. Load and benchmark: justification files (a cache round trip is the cost; no new concurrency primitive beyond #3439's).
7. Documentation (through `docs-writer` and `mechanical-fixer` at implementation): XML docs, changelog fragment `changelog.d/334-idempotency-store.added.md`, the `Encina.Caching` README section, `docs/INVENTORY.md`, a "Choosing the Inbox or the cache store" section in the caching guide.
8. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test`, per-flag coverage against the table below, `crap-gate` table for the changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: issue #334. Tests must execute real package code (CacheIdempotencyStore over MemoryCacheProvider), not
reflection. Coverage is per flag; targets are in the plan's table.
TASK: Write the unit, guard, contract, integration and DI tests listed in the plan, the load and benchmark
justification files, and hand the documentation to docs-writer.
KEY RULES: Shouldly through Encina.Testing.Shouldly; FakeTimeProvider for time; no Thread.Sleep; integration tests
use a shared [Collection] fixture; builders for test data.
REFERENCE FILES: tests/Encina.UnitTests/Caching/DistributedIdempotencyPipelineBehaviorTests.cs,
.github/coverage-manifest/Encina.Caching.json, docs/en/guides/TESTING.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| ADR-001, ADR-006 | Operations return `Either<EncinaError, T>` | The store and the behavior return `Either`; a store failure is `Left` |
| ADR-062 | The producer sets the error category | Codes of Phase 1 carry `InvalidRequest`, `Conflict`, `Unavailable` |
| ADR-054 D3, D4 | Package-named meter, no `_total`; tenant tag when tenancy is on | `IdempotencyMetrics` (Phase 4) |
| ADR-050 (Caching row) | Tenant in the key when multi-tenant; no query caching without a tenant | The tenant segment and the fail-closed case of Design Choice 3 |
| ADR-021 | EventId ranges are registered before use | `CachingIdempotency` 7800-7819 |
| IETF Idempotency-Key header draft | In-flight duplicate answered 409; reused key with another payload rejected | The `InProgress` claim and the `FingerprintMismatch` claim; the HTTP status is [#355](https://github.com/dlrivada/Encina/issues/355) |
| Stripe idempotent requests | Same key returns the first result | The `Completed` claim replays the stored response |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DistributedIdempotencyPipelineBehavior` | `src/Encina.Caching/Behaviors/DistributedIdempotencyPipelineBehavior.cs` | Rewritten onto the store; keeps its name, marker and nested-dispatch rule |
| `CachingOptions` | `src/Encina.Caching/Configuration/CachingOptions.cs` | `EnableDistributedIdempotency`, `IdempotencyKeyPrefix`, `IdempotencyTtl`, `ThrowOnCacheErrors` (the last one's fate belongs to #3439) |
| `ICacheProvider` | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | The backend of `CacheIdempotencyStore` |
| `IRequestContext.IdempotencyKey`, `RequestContextDispatchExtensions.IsNestedDispatch` | `src/Encina/Abstractions/IRequestContext.cs:116`, `src/Encina/Core/RequestContextDispatchExtensions.cs` | The key source and the nested rule |
| `MessagePurgeMetrics` | `src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs` | Pattern for the counter class |
| `EventIdRanges` and the allocation test | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Range registration |
| `InboxPipelineBehavior` | `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs` | The durable alternative (Design Choice 4) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` | **7800-7803** used of the new `CachingIdempotency` = (7800, 7819) | From the block 7800-7849 assigned to this family; 7804-7819 stay free for the follow-ups of the store; the family's other ranges are 7830-7839 (#733) and 7840-7849 (#735); the existing ids 3504-3507 are not moved |

### Per-file coverage targets

| File | Unit | Guard | Contract | Integration | Justification |
|------|-----:|------:|---------:|------------:|---------------|
| `Idempotency/IdempotencyOutcomeAccessor.cs` (Encina) | 95 | 70 | 0 | 0 | Tiny state holder; guard covers its argument checks; the contract flag has no contract to run (a sealed class with no abstraction) |
| `Idempotency/IdempotencyErrorCodes.cs` (Encina) | 0 | 0 | 0 | 0 | Constants only, no executable code; the categories are asserted in the accessor and behavior unit tests |
| `Abstractions/IIdempotencyStore.cs`, `IdempotencyRecord.cs` | 0 | 0 | 0 | 0 | Interface and record without logic; covered through the store and the contract suite |
| `Abstractions/IdempotencyClaim.cs` (Encina.Caching) | 90 | 60 | 0 | 0 | Factories and the kind checks, all reached by the store tests |
| `CacheIdempotencyStore.cs` | 92 | 70 | 85 | 80 | Every claim kind and the exception path are unit-testable with a fake cache; the contract suite runs it over `MemoryCacheProvider`; Redis integration proves the atomic claim |
| `Behaviors/DistributedIdempotencyPipelineBehavior.cs` | 93 | 80 | 60 | 0 | Mockable next delegate and fake store; guard tests cover constructor and `Handle` arguments |
| `Diagnostics/IdempotencyMetrics.cs`, `IdempotencyLog.cs` | 85 | 0 | 0 | 0 | Recorded through a `MeterListener` and a fake logger; no parameters worth a guard test (internal static, no InternalsVisibleTo for `Encina.GuardTests`) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production source, new | 10 | `IdempotencyOutcome`, accessor interface, accessor, `IdempotencyErrorCodes` (core); `IIdempotencyStore`, `IdempotencyRecord`, `IdempotencyClaim`, `CacheIdempotencyStore`, `IdempotencyMetrics`, `IdempotencyLog` (`Encina.Caching`) |
| Production source, modified | 5 | behavior, `ServiceCollectionExtensions.cs` (Caching), the core registration file, `EventIdRanges.cs`, `CachingOptions.cs` only if #3439 does not add the lease option |
| Metadata | 7 | `PublicAPI.Unshipped.txt` (Encina, Encina.Caching), manifests `Encina.json`, `Encina.Caching.json`, changelog fragment, `docs/INVENTORY.md`, README of `Encina.Caching` |
| Tests | 15 | unit (store, accessor and codes, metrics and log), the three existing behavior test files rewritten, `NestedDispatchContextTests`, guard (store, accessor, behavior), contract, integration, DI test extended, EventId allocation map, 2 justification files |
| Documentation | 1 | caching guide section |
| **Total** | **~38 distinct** | Five pull requests (the table below sums to 46 because the `PublicAPI` and manifest files of `Encina.Caching` and `Encina` are touched in several) |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phase 1: 4 outcome and error-code files, core registration, `PublicAPI` and manifest of `Encina`, accessor unit and guard tests, error-category unit test | 10 |
| B | Phase 2: store interface, record, claim, `CacheIdempotencyStore`, `PublicAPI` and manifest of `Encina.Caching`, store unit, guard and contract tests; starts after #3439 chose the claim primitive | 9 |
| B2 | Phase 4 classes: `IdempotencyMetrics`, `IdempotencyLog`, `EventIdRanges.cs`, allocation test map, `PublicAPI` of `Encina` and `Encina.Caching`, manifests `Encina.json` and `Encina.Caching.json`, metrics and log unit test | 9 |
| C | Phase 3: the rewritten behavior, DI registration, the three existing behavior test files rewritten, `NestedDispatchContextTests`, DI test extended, behavior guard test, `PublicAPI` and manifest of `Encina.Caching`; the activity tag | 11 |
| D | Redis integration test, load and benchmark justification files, changelog fragment, README, guide section, `docs/INVENTORY.md` | 7 |

Order: A, then B (after #3439), then B2, then C, then D. Each pull request builds alone: A adds types nothing references yet; B adds the store beside the old behavior; B2 adds the unused telemetry classes; C switches the behavior and removes its internal entry class in the same pull request; D adds only tests and documentation.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility (no [Obsolete], no aliases).
Issue #334 puts an idempotency store (IIdempotencyStore) under the existing DistributedIdempotencyPipelineBehavior in
Encina.Caching and adds core outcome types so a host can tell a replay (needed by #355). Prerequisites: #3439 (atomic
claim, release on failure, fail closed) merged; #3438 (key shape, fingerprint) and #2452 (tenant) decided or merged.

IMPLEMENTATION OVERVIEW:
1. Encina core: IdempotencyOutcome, IIdempotencyOutcomeAccessor (scoped, first non-None wins), IdempotencyErrorCodes
   (missing_key InvalidRequest, in_progress Conflict, store_failed Unavailable).
2. Encina.Caching: IIdempotencyStore (TryBeginAsync, CompleteAsync, ReleaseAsync), IdempotencyRecord, IdempotencyClaim,
   CacheIdempotencyStore over ICacheProvider; the behavior rewritten onto the store (same class and marker names).
3. Observability: IdempotencyMetrics (meter Encina.Caching, counter encina.caching.idempotency.requests, tenant tag only
   when tenancy is on), activity tag encina.idempotency.outcome, IdempotencyLog EventIds 7800-7803 in the new range
   CachingIdempotency (7800, 7819) registered first in EventIdRanges.cs.
4. Tests: unit, guard, contract (store over MemoryCacheProvider), integration (Redis), DI with ValidateOnBuild and
   ValidateScopes; per-file targets from the plan.
5. Docs: XML docs, changelog fragment, README, guide, INVENTORY, PublicAPI files.

KEY PATTERNS:
- ROP: a store failure is Left; nothing is swallowed; the request fails visibly.
- TimeProvider for time; no DateTime.UtcNow. The key and EncinaError.Message never reach logs or tags.
- Bounded metric dimensions; [LoggerMessage] source generator; EventIds packed in a registered range.
- Never inject a scoped service into a singleton: the store is a singleton over singleton providers; the accessor is
  scoped and the behavior is transient.
- CRAP <= 10 on every changed method; targets never go down; one pull request per phase group.

REFERENCE FILES:
docs/plans/idempotency-store-implementation-plan-334.md, src/Encina.Caching/Behaviors/DistributedIdempotencyPipelineBehavior.cs,
src/Encina.Caching/Abstractions/ICacheProvider.cs, src/Encina/Abstractions/IRequestContextAccessor.cs,
src/Encina/Diagnostics/EventIdRanges.cs, docs/architecture/adr/062-error-category-contract.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | The store is backed by `ICacheProvider` (the 8 cache providers) and the idempotency entry lives in the cache with a TTL |
| 2 | OpenTelemetry | ✅ | Counter `encina.caching.idempotency.requests` with tags outcome and tenant (tenant only when multi-tenancy is on) and tag `encina.idempotency.outcome` on the current activity |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 7800-7819 in the new range `CachingIdempotency`, logging request type and outcome only, never the key |
| 4 | Health Checks | ❌ | The availability of the store is the availability of the cache provider, which has its own health check |
| 5 | Validation | ❌ | No new input: the key is bounded to 255 characters by the existing inbound normalizer, and the format policy is [#355](https://github.com/dlrivada/Encina/issues/355) |
| 6 | Resilience | ❌ | A store failure fails the request closed; retries of the cache call belong to the cache provider |
| 7 | Distributed Locks | ❌ | The claim is the lease of the store; whether its atomic primitive is a cache set-if-absent or a lock is decided by [#3439](https://github.com/dlrivada/Encina/issues/3439) (Design Choice 2) |
| 8 | Transactions | ❌ | A cache entry cannot join a database transaction, so the window between handler success and the stored result stays at-least-once; the Inbox is the transactional path (ADR-048) |
| 9 | Idempotency | ✅ | This is the idempotency feature itself |
| 10 | Multi-Tenancy | ✅ | The tenant is a segment of the store key, the reserved segment `default` is used when tenancy is off and a missing tenant fails closed when it is on ([#2452](https://github.com/dlrivada/Encina/issues/2452)) |
| 11 | Module Isolation | ❌ | Keys are caller scoped and a module can use its own `IdempotencyKeyPrefix` |
| 12 | Audit Trail | ❌ | A replay is a flow-control signal covered by telemetry, not a state change or a compliance event |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#3439](https://github.com/dlrivada/Encina/issues/3439) | Blocks PR B | Atomic claim, release on exception, transient failures not cached, fail closed; chooses the primitive of Design Choice 2 |
| [#3438](https://github.com/dlrivada/Encina/issues/3438) | Blocks PR C for the key shape (`needs-decision`) | Key composition and fingerprint; this plan stays key-agnostic |
| [#2452](https://github.com/dlrivada/Encina/issues/2452) | Shares the behavior | Tenant-less key; the fail-closed rule is adopted here |
| [#319](https://github.com/dlrivada/Encina/issues/319) | Sibling (not planned yet) | Owns key derivation and the key attribute; no overlap |
| [#355](https://github.com/dlrivada/Encina/issues/355) | Consumer | Reads `IdempotencyOutcome` and answers 409 and the replay header |
| [#2401](https://github.com/dlrivada/Encina/issues/2401) | Related | The durable Inbox path (Design Choice 4) |

---

## Next Steps

1. Wait for [#3439](https://github.com/dlrivada/Encina/issues/3439) before opening PR B; re-read its primitive and adapt Phase 2.
2. At pick-up: re-check the Design Choices against today's code, ask the maintainer Dilemmas 1, 2, 3 and 4 (store abstraction, claim primitive (asked once, with #3439), key shape (asked once, with #3438), durable backend (asked once, with #355)) and record the answers in `## Maintainer Decisions`; then run `check-plan.ps1` without `-Draft`.
3. Confirm the `CachingIdempotency` range (7800-7819) is still free when PR B starts.
4. Measure per-flag coverage and the CRAP table for the touched files before each pull request.
