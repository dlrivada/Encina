# Implementation Plan: Cache diagnostics and debugging tools

> **Issue**: [#276](https://github.com/dlrivada/Encina/issues/276)
> **Type**: Feature
> **Complexity**: Medium (3 code phases, 3 code pull requests of 15, 9 and 15 files, and a documentation and integration pull request of 4 files)
> **Estimated Scope**: about 700 lines of production code, about 1,500 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #276 asks for development-time diagnostics of the query cache: response headers (`X-Cache-Status`, `X-Cache-Key`, `X-Cache-Age`, `X-Cache-TTL`, `X-Cache-Level`, `X-Cache-Stale`), diagnostic endpoints (stats, key search, key details, key deletion) mapped by `MapEncinaCacheDiagnostics`, an `ICacheDiagnostics` inspection API, authorization on the endpoints and a switch to disable them. Re-checked against `origin/main` (ce352001f):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| `ICacheDiagnostics` can list keys, report TTL, size and tags for any provider | No provider API supports it: `ICacheProvider` (`src/Encina.Caching/Abstractions/ICacheProvider.cs`) has get, set, remove, pattern removal, exists and refresh only. `MemoryCacheProvider` tracks keys per process (`_keyTracker`), `RedisCacheProvider` could `SCAN`, `HybridCacheProvider` tracks only its own keys (#2402) and Memcached (#277) cannot enumerate keys. |
| The endpoint is mapped bare and secured by chaining `.RequireAuthorization("CacheAdmin")` | The repository's precedent is stricter: `MapDPIAEndpoints` (`src/Encina.AspNetCore/DPIAEndpointExtensions.cs`) requires the policy name as a parameter, applies it to every endpoint and throws at startup when it is blank or unregistered. A bare mapping is open when the caller forgets the chain. |
| `X-Cache-Key` shows the key | Keys carry the tenant and, with `VaryByUser`, the user segment; logs redact them with `CacheKeyRedactor` (`src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs`). A response header is read by the caller and by every proxy in between. |
| The details endpoint returns the deserialized value | Cached values can hold personal data (the reference application caches health-related query results); nothing in the issue restricts who may read them beyond the endpoint policy. |
| `X-Cache-Level` (L1, L2), `X-Cache-Stale` | Only `HybridCacheProvider` has levels and does not report which one served a read; stale reads are #268 (open). `CacheEntry<T>` has `CachedAtUtc` only today; the expiry instant is #266 (open). |
| `ThrowOnCacheErrors`-style graceful degradation also covers diagnostics | Not applicable: diagnostics are read-only, never part of a request's critical path. |

The plan builds diagnostics on what every one of the 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277) can offer identically: a bounded, node-local registry of the keys that `QueryCachingPipelineBehavior` itself wrote, with Encina metadata (request type, cached-at, duration, tenant), an outcome carrier for the headers and fail-closed HTTP endpoints modeled on `MapDPIAEndpoints`.

**Shared pieces and ownership (family cache-patterns-1 and -2):** the behavior hook points are small private methods added next to those of #266, #267, #268, #272 and #275; this plan reads the `CacheEntry<T>` fields #266 owns (it works with `CachedAtUtc` only until `ExpiresAtUtc` exists). EventIds, in the 10000+ zone (AGENTS.md section 7): `CachingDiagnostics` 30050-30051 (emitted by `Encina.Caching`, slot 30050-30099 of the Caching block 30000-30999, packed after #278's slot 30000-30049) and `AspNetCoreCachingDiagnostics` 60100-60102 (emitted by `Encina.AspNetCore`, slot 60100-60149 of the Observability and platform block 60000-60999, where ADR-021's area table puts ASP.NET Core; the open plans of #182 (60000-60049) and #180 (60050-60099) hold the two slots before it, and the free slot is re-checked against the registry, `docs/plans` on main and the open `docs/plan-*` branches right before the code PR).

**Affected packages:** `Encina.Caching` (registry, inspection API, outcome carrier), `Encina.AspNetCore` (headers and endpoints), `Encina` (EventId registry). **Provider category:** Caching (8): behavior-level, identical on all 8.

---

## Design Choices

<details>
<summary><strong>1. Where the diagnostic data comes from (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A bounded in-process registry filled by `QueryCachingPipelineBehavior` (`CacheKeyRegistry`): key, request type, cached-at, duration, tenant; search, details and delete work on it** | Identical on all 8 providers; carries Encina-specific data the issue asks for (request type); no provider change; bounded by `MaxTrackedKeys` (default 10,000, oldest evicted, EventId 30051) | Node-local: it lists only the entries this node wrote since it started, not what other nodes or other code wrote to the provider |
| **B) Provider-native enumeration through an optional `ICacheDiagnostics` per provider (Redis `SCAN` and `TTL`, Memory `_keyTracker`); providers that cannot (Hybrid, Memcached) answer "unsupported"** | Sees the real content of a shared Redis | Eight implementations with different capabilities (AGENTS.md section 5 asks consistency); Dragonfly, Garnet and KeyDB differ in `SCAN` and `MEMORY` support; no request-type metadata |
| **C) A as the base, B added later for providers that can** | Incremental | Two sources of truth to explain |

### Chosen Option: **A - behavior registry** (recommended, pending the maintainer)

### Rationale

- The motivation of the issue is Encina-specific information ("query handlers and tags") that external tools cannot show; a registry carries it. Option B wins for an operator who needs to see everything in one shared Redis; it can be added behind the same `ICacheDiagnostics` interface later (option C) without changing this plan's API.
- The statistics report counters of this node (hits, misses, bypasses, writes, entries tracked) and the provider type name. Under multi-tenancy the counters are kept and reported per tenant: `ICacheKeyRecorder.RecordOutcome(string? tenantId, status)` increments a per-tenant counter row (keyed by the tenant string verbatim, bounded by `MaxTrackedKeys`; a tenant beyond the bound is not counted, never folded into another tenant's row), and `GetStatsAsync(tenantId)` returns only that tenant's row, so tenant A never sees tenant B's activity; without multi-tenancy the single row is keyed by the reserved segment `default` and is the node-wide figure. The provider type name is not tenant data; the `l1`, `l2` and `backplane` sections of the issue's example are not reported because no provider exposes them uniformly.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Value inspection (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Metadata only: the details endpoint never returns the cached value in this version** | A diagnostic endpoint can never leak cached personal data; nothing to configure wrongly | The issue's "deserialized value if enabled" is not delivered |
| **B) A `Diagnostics.ShowValues` option (default false) and a second policy name required to read values** | Delivers the issue's feature for development | The registry would have to hold or re-read values (a re-read goes through the provider and the codec); values of other tenants must be filtered; a new data-exposure surface |

### Chosen Option: **A - metadata only** (recommended, pending the maintainer)

### Rationale

- The reference application handles health data; AGENTS.md section 3 requires gates to fail closed, and "no value endpoint" is the closed state. Option B wins in a development-only deployment where seeing the value is the point; it is an issue file (`artifacts/issues/cache-diagnostics-value-inspection.md`).
- Class: Dilemma

</details>

<details>
<summary><strong>3. Endpoint security and the default state</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MapEncinaCacheDiagnostics(string authorizationPolicy, string prefix = "/cache")` requires the policy name, applies it to every endpoint and throws at startup when the name is blank or the policy is not registered; nothing is mapped or enabled unless the application calls `AddEncinaCacheDiagnostics` and the mapping method (there is no other switch)** | Fail closed, as `MapDPIAEndpoints` does; "disabled in production" is the absence of the calls | Applications must define a policy |
| **B) The issue's bare `MapEncinaCacheDiagnostics("/cache")` with `.RequireAuthorization(...)` chained by the caller** | Matches the issue text | Open by default when the chain is forgotten |

### Chosen Option: **A - policy name required**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed ... the only opt-out is explicit and logged") and the existing precedent `DPIAEndpointExtensions.MapDPIAEndpoints` (policy required, validated at startup, no anonymous mode); AGENTS.md section 1 (pay-for-what-you-use).
- Tenant scoping of every diagnostic answer, statistics included, is settled by AGENTS.md section 3 (a gate that lacks tenant context denies) and holds under either answer to Design Choice 1: whichever source supplies the data, the counters, search, details and both deletes are per tenant, and `GetStatsAsync(null)` with multi-tenancy on returns `cache.diagnostics_tenant_required`.
- Under multi-tenancy (`MultiTenancyMarker` registered) every endpoint serves only the caller's tenant (`IRequestContext.TenantId` bound by `UseEncinaContext()`); a request with no tenant is denied with `cache.diagnostics_tenant_required` (ADR-062 `ErrorCategory.InvalidRequest`, the category `tenancy.tenant_required` uses) and logged (EventId 60100); there is no all-tenants mode in this version.

</details>

<details>
<summary><strong>4. What delete means (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete removes only registry keys that belong to the caller's tenant, one by one with `ICacheProvider.RemoveAsync`, at most `MaxDeleteBatch` (default 1,000) per call; the pattern is a glob over the registry** | A pattern cannot reach another tenant's entries | Entries written by other nodes are not removed |
| **B) The pattern goes to `ICacheProvider.RemoveByPatternAsync`** | Cluster-wide, includes entries of other nodes | A pattern such as `*` removes every tenant's entries; pattern syntax differs per provider (Hybrid removes tracked keys only, #2402) |

### Chosen Option: **A - registry and tenant scoped** (recommended, pending the maintainer)

### Rationale

- Option B wins for single-tenant operations that must clean a shared cache from one node. The glob is implemented with `*` and `?` only, by an iterative matcher (no regular expression, so no backtracking cost), with a pattern length limit of 200 and a result limit of 1,000; a longer pattern returns `cache.diagnostics_invalid_pattern` (`ErrorCategory.Validation`).
- Each executed delete is logged with the count only (EventId 60101); the audit trail of administrative deletes is deferred (`artifacts/issues/cache-diagnostics-delete-audit.md`).
- Class: Dilemma

</details>

<details>
<summary><strong>5. Header content (Dilemma D5, the issue's header list)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `X-Cache-Status` (`HIT`, `MISS`, `BYPASS`), `X-Cache-Age` (from `CachedAtUtc`) and `X-Cache-TTL` (when an expiry is known, which needs #266's `ExpiresAtUtc`, so the header is omitted until then); `X-Cache-Key` only when `IncludeKey` is on and then redacted with `CacheKeyRedactor`; `X-Cache-Level` and `X-Cache-Stale` are not emitted (no provider reports a level; stale reads are #268, open); a request that runs several cached requests reports `HIT` only when all hit, `MISS` when any missed and `BYPASS` when none was cached** | Safe by default and truthful; a proxy never sees tenant or user segments | Less detail than the issue's example; the aggregation is one of two readings |
| **B) Every header of the issue; the raw key when `IncludeKey` is on; `X-Cache-Level` and `X-Cache-Stale` emitted when a provider or #268 reports them; one `X-Cache-Status` per cached request (suffixed by index)** | Matches the issue text and distinguishes each cached request | Leaks tenant and user segments with the raw key; two headers have no source today |

### Chosen Option: **A - safe subset, redacted key, aggregated status** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 3 settles redaction for logs and telemetry; whether a development header may carry the raw key, and whether status is per request or aggregated, is a design choice. Option B wins in a development-only deployment where the exact key is the point.
- Class: Dilemma

</details>

<details>
<summary><strong>7. How the outcome reaches the response</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A singleton `CacheOutcomeAccessor` with an `AsyncLocal` holder started by the middleware; the behavior receives it as an optional trailing constructor parameter (an existing constructor), so with diagnostics off no holder exists and the cost is one null check** | Flows into `Send` in the same async flow; no scoped service in a singleton | The holder aggregates across the HTTP request only: it starts and ends with the middleware and is not passed to background work |
| **B) A scoped service** | Familiar | The pipeline does not share the HTTP request's scope, so the behavior would see another instance |

### Chosen Option: **A - `AsyncLocal` holder**

### Rationale

- **Class:** Trivial
- **Settled by:** the precedent `src/Encina/Core/RequestContextAccessor.cs` (an `AsyncLocal` holder, the `HttpContextAccessor` pattern, flows into `Send`) and the no-scoped-into-singleton rule; the holder's thread safety (an `Interlocked` packed counter and a `CompareExchange` first-key slot) holds under either answer to Design Choice 5; if option B of Design Choice 5 is chosen (one status per cached request), the packed counter becomes a bounded list guarded by the same rule (parallel `Send` calls never lose an outcome) and the carrier choice itself is unchanged.

</details>

<details>
<summary><strong>8. Statistics content (Dilemma D8, the issue's `l1`, `l2` and `backplane` sections)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Node counters only (hits, misses, bypasses, writes, entries tracked) and the provider type name** | Identical on all 8 providers | The `l1`, `l2` and `backplane` sections of the issue are not delivered |
| **B) A as the base, plus provider-native figures where a provider offers them (Redis `INFO` hit and miss counts, Hybrid levels once reported) behind an optional `ICacheStatsSource`** | Closer to the issue | A per-provider capability (AGENTS.md section 5 asks consistency); Dragonfly, Garnet and KeyDB differ on `INFO`; Memcached has its own stats |

### Chosen Option: **A - node counters** (recommended, pending the maintainer)

### Rationale

- Option B wins for an operator who needs the shared server's hit rate in the same response; it can be added behind the same interface later.
- Class: Dilemma

</details>

<details>
<summary><strong>9. Integration tests (Dilemma D9, the issue's "unit and integration tests")</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unit tests with a test server in every PR, plus one integration test in PR 3 that runs the endpoints and headers over the real behavior and a real Redis provider (shared Redis fixture)** | Meets the criterion; proves that keys, tenant scoping and headers hold with a real serializing provider | Needs the Redis container (the integration job has it) |
| **B) Unit tests with a test server only** | No container | The criterion "integration tests" is not met; the behavior-level code has no provider-specific path |

### Chosen Option: **A - one Redis integration test** (recommended, pending the maintainer)

### Rationale

- Option B wins when the maintainer treats the test-server tests as sufficient for behavior-level code.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Package split, EventIds and error categories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registry, `ICacheDiagnostics`, the holder and options in `Encina.Caching`; the middleware and endpoints in `Encina.AspNetCore` (already references `Encina.Caching`); two EventId ranges, one per emitting assembly** | Non-HTTP callers can inspect the cache; each assembly logs only from its own range | Two small ranges |
| **B) Everything in `Encina.AspNetCore`** | One package | The behavior cannot reference `Encina.AspNetCore` (the reference runs the other way) |

### Chosen Option: **A - split by dependency direction**

### Rationale

- **Class:** Trivial
- **Settled by:** `Encina.AspNetCore.csproj` (`ProjectReference` to `Encina.Caching`) and ADR-021 / AGENTS.md section 7 (an EventId is emitted only by its mapped assembly; `EncinaEventIdAllocationTests.AssemblyRanges`).
- Error codes follow ADR-062 and use constant messages; they are returned as `Either` by `ICacheDiagnostics` (AGENTS.md section 3, ROP) and mapped to the HTTP response by the endpoints through the existing `ProblemDetailsExtensions`. The codes that exist whatever the answers: `cache.diagnostics_tenant_required` `InvalidRequest` and `cache.diagnostics_key_not_found` `NotFound`; `cache.diagnostics_invalid_pattern` (`Validation`) and the pattern and result limits exist only under Design Choice 4 option A.

</details>

---

## Implementation Phases

### Phase 1: Registry, outcome carrier and behavior hooks (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 15 files (the registry logs its own eviction, so the log class, the range, its registration and its row in `docs/engineering/eventid-block-registry.md` belong here; row 4e below):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Diagnostics/CacheKeyRegistry.cs` | new (internal sealed, bounded, instance-level; implements `Record`, `RecordOutcome` and the per-tenant statistics rows) |
| 2 | `src/Encina.Caching/Diagnostics/CacheOutcomeAccessor.cs` | new (public sealed: the `AsyncLocal` holder, `CacheOutcome` record, and the public interface `ICacheKeyRecorder` with `Record` and `RecordOutcome(string? tenantId, CacheOutcomeStatus status)` that the internal registry implements) |
| 3 | `src/Encina.Caching/Configuration/CacheDiagnosticsOptions.cs` | new (public: `MaxTrackedKeys`, `MaxDeleteBatch`, `IncludeKey`, `HeaderPrefix`; it is the only options source: `AddEncinaCacheDiagnostics(Action<CacheDiagnosticsOptions>)` registers `IOptions<CacheDiagnosticsOptions>` and the registry and middleware read that, so nothing is added to `CachingOptions` and the hand-written copy list of `AddEncinaCaching` is not involved; the diagnostics switch is calling `AddEncinaCacheDiagnostics` itself) |
| 4 | `src/Encina.Caching/Diagnostics/CacheDiagnosticsLog.cs` | new (EventIds 30050-30051; 30051 eviction is emitted by the registry of this PR, 30050 by `CacheDiagnostics` in PR 2) |
| 4b | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingDiagnostics = (30050, 30051)`) |
| 4c | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Caching`) |
| 4d | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: one line, `static readonly Encina.Diagnostics.EventIdRanges.CachingDiagnostics -> (int Min, int Max)`, for the new field) |
| 4e | `docs/engineering/eventid-block-registry.md` | modify (a row `CachingDiagnostics \| 30050-30051 \| Caching \| zone` in the "Registered ranges" table and, in the "Blocks" table, the `reserved` row of the slot is replaced by a `registered` row with the exact bounds of the range, `30050-30051 \| Caching \| #276 cache diagnostics \| registered` (rule 2 of `EventIdRegistryAgreementTests` requires a `registered` block to match a range of `EventIdRanges.cs` with the same bounds, and rule 3 forbids a `reserved` row overlapping a registered range); the rest of the slot, 30052-30099, is left unlisted or as a `free` row (not checked); read the test and the document first) |
| 5 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (optional trailing `ICacheKeyRecorder?` and `CacheOutcomeAccessor?`, both public types because the behavior's constructor is public and an internal parameter type would not compile; record on hit, miss, bypass and write) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/CacheKeyRegistryTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/QueryCachingDiagnosticsTests.cs` | new |
| 9 | `tests/Encina.GuardTests/Caching/CacheDiagnosticsGuardTests.cs` | new |
| 10 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/276-cache-diagnostics-registry.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `CacheKeyRegistry`: a `ConcurrentDictionary<string, Entry>` plus an insertion-order queue bounded by `MaxTrackedKeys`; `Record(key, requestType, cachedAtUtc, duration, tenantId)`, `Remove(key)`, `Snapshot(tenantId)`, and the per-tenant statistics table behind `ICacheKeyRecorder.RecordOutcome(string? tenantId, CacheOutcomeStatus status)` (one row of hit, miss, bypass and write counters per tenant string, bounded by `MaxTrackedKeys`; a tenant beyond the bound is not counted, EventId 30051 is logged once per eviction or refusal, and it is never folded into another tenant's row); instance-level state only (a registry per `AddEncinaCaching`, never a static field of a generic class); the stored tenant is the segment used by the key generator, so a tenant filter compares equal strings, never substrings.
2. **Create** `CacheOutcomeAccessor` (singleton): `Begin()` returns a disposable holder pushed on an `AsyncLocal`, `Record(CacheOutcome)` is a no-op without a holder; `CacheOutcome` carries the status, `CachedAtUtc`, the duration and, only when `IncludeKey` is on, the key redacted by `CacheKeyRedactor` (a nullable `RedactedKey` field), never the value or the raw key. Selection rule for `X-Cache-Key` when one HTTP request runs several cached requests: the header carries the redacted key of the first outcome recorded with a key (completion order), independent of the aggregated status rule; the other keys are not exposed. The holder keeps the status counts as an `Interlocked` packed counter, and the first redacted key in a separate reference slot set once with `Interlocked.CompareExchange`, so parallel `Send` calls of one HTTP request neither lose a count nor replace the first key.
3. **Modify** the behavior: the existing paths already know hit, miss, bypass and write; each hook is a one-line call to a private method so `Handle` keeps CRAP at 10 or below. With both optional parameters null (diagnostics off) behavior is unchanged.
4. **Tests (unit):** bound and eviction order, tenant filter is equality, a re-write refreshes an entry, no holder means no work, outcomes for hit, miss, bypass (the three existing bypass reasons) and write, the registry never holds a value. The two new optional parameters of the behavior's public constructor accept null (diagnostics off): `CacheDiagnosticsGuardTests.cs` has a valid-result test for each (the behavior behaves as today), not an exception test. The `AsyncLocal` holder is thread-safe: one HTTP request can run several `Send` calls in parallel, so the holder aggregates with `Interlocked` operations on a packed state (hit, miss, bypass counts), never a plain list. Guard tests: `CacheDiagnosticsOptions` setters reject invalid ranges (`MaxTrackedKeys` at least 1); `ICacheKeyRecorder.RecordOutcome(string? tenantId, CacheOutcomeStatus status)` accepts a null tenant (the single-tenant case, counted in the `default` row) and gets a valid-result test, and its enum `status` has a test with every defined value (an undefined value is ignored, never thrown); unit tests cover per-tenant rows, a row never containing another tenant's counts, and the bound with the not-counted-beyond-bound behavior; `ICacheKeyRecorder.Record(string key, ...)` throws `ArgumentNullException` for a null or whitespace key (a key is never recorded blank); `CacheOutcomeAccessor.Record(CacheOutcome)` takes a value-type record, so a default value is a valid input and gets a valid-result test (it records nothing without a holder); `Begin()` has no parameter.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #276 adds cache diagnostics. QueryCachingPipelineBehavior
(src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs) knows every hit, miss, bypass and write. RequestContextAccessor
(src/Encina/Core/RequestContextAccessor.cs) is the precedent for an AsyncLocal holder.

TASK:
1. Create CacheKeyRegistry (with its log, EventIds 30050-30051, the CachingDiagnostics range registration and the per-tenant statistics rows behind ICacheKeyRecorder.RecordOutcome(tenantId, status)), CacheOutcomeAccessor (with CacheOutcome, an Interlocked packed counter plus a CompareExchange first-key slot, and ICacheKeyRecorder) and CacheDiagnosticsOptions; nothing is added to CachingOptions.
2. Hook the behavior through optional trailing constructor parameters; keep Handle at CRAP 10 or below.
3. Write the unit and guard tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest rows and the changelog fragment.

KEY RULES:
- With diagnostics off the behavior is unchanged and allocates nothing extra.
- The registry stores no value and no raw key outside itself; logs and headers use CacheKeyRedactor.
- State is instance-level and bounded; time from TimeProvider; the tenant filter is string equality.
- Follow the maintainer's answer to Design Choice 1, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Core/RequestContextAccessor.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- tests/Encina.UnitTests/Caching/QueryCachingPipelineBehaviorTests.cs
```

</details>

### Phase 2: The inspection API and its registration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 9 files (the log class and the range came with PR 1):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/ICacheDiagnostics.cs` | new (public interface and records `CacheStats`, `CacheKeyInfo`, `CacheKeyDetails`) |
| 2 | `src/Encina.Caching/Diagnostics/CacheDiagnostics.cs` | new (internal sealed implementation over the registry and `ICacheProvider`; emits EventId 30050) |
| 3 | `src/Encina.Caching/CacheDiagnosticsServiceCollectionExtensions.cs` | new (public `AddEncinaCacheDiagnostics`) |
| 4 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/Caching/CacheDiagnosticsTests.cs` | new (includes the registration test with `ValidateOnBuild` and `ValidateScopes`) |
| 6 | `tests/Encina.GuardTests/Caching/CacheDiagnosticsServiceCollectionExtensionsGuardTests.cs` | new |
| 7 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 8 | `changelog.d/276-cache-diagnostics-api.added.md` | new (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Caching/CacheKeyRegistryTests.cs` | modify (the tenant-filtered snapshot used by search) |

Tasks:

1. **Create** `ICacheDiagnostics`: `ValueTask<Either<EncinaError, CacheStats>> GetStatsAsync(string? tenantId, CancellationToken)`, `ValueTask<Either<EncinaError, IReadOnlyList<CacheKeyInfo>>> SearchKeysAsync(string? tenantId, string pattern, int limit, CancellationToken)`, `ValueTask<Either<EncinaError, CacheKeyDetails>> GetKeyDetailsAsync(string? tenantId, string id, CancellationToken)` `ValueTask<Either<EncinaError, int>> DeleteKeysAsync(string? tenantId, string pattern, CancellationToken)` and the exact operation `ValueTask<Either<EncinaError, Unit>> DeleteKeyAsync(string? tenantId, string id, CancellationToken)` (`DELETE {prefix}/keys/{id}` calls it; the id must belong to the caller's tenant, otherwise `cache.diagnostics_key_not_found`, so an id of another tenant is indistinguishable from an absent one); entries are addressed by an opaque `id` (a hash of the key) and shown with the redacted key, so a caller never receives a raw key. The tenant is a parameter, not an ambient read: the endpoint passes the request's tenant.
2. **Create** `CacheDiagnostics`: glob matcher with `*` and `?` and a 200-character limit, results capped at 1,000, a missing entry returns `cache.diagnostics_key_not_found`, a longer pattern `cache.diagnostics_invalid_pattern`; delete follows Design Choice 4 and logs the count only (EventId 30050 keys deleted); the registry logs 30051 when it evicts at the bound. Cancellation propagates: a cancelled token throws `OperationCanceledException` and is never turned into a `Left`. A delete removes keys with an unconditional `RemoveAsync`, so a concurrent newer write of the same key can be removed; that costs one miss and is accepted for a diagnostic tool.
3. **Create** `AddEncinaCacheDiagnostics(this IServiceCollection, Action<CacheDiagnosticsOptions>? = null)` (public, `ArgumentNullException.ThrowIfNull(services)`): registers `IOptions<CacheDiagnosticsOptions>` (the only options source), the registry (an `ILogger` and `MaxTrackedKeys` from those options), the accessor and `ICacheDiagnostics` as singletons with `TryAdd`; without this call nothing is registered (pay-for-what-you-use).
4. **Tests:** statistics rows per tenant (tenant A's `GetStatsAsync` never contains tenant B's counts, whatever the order of the recorded outcomes), `GetStatsAsync(null)` with multi-tenancy on returns `cache.diagnostics_tenant_required` (ADR-062 `ErrorCategory.InvalidRequest`, constant message; fail closed for non-HTTP callers too, and the same rule for every operation) while without multi-tenancy it returns the `default` row, tenant isolation of search, details, delete by pattern and delete by id (tenant A never sees or removes tenant B's id), a single-key delete logs EventId 30050 with count 1, the matcher with `*` and `?` and an adversarial long pattern, the limit, the not-found code, DI built with `ValidateOnBuild` and `ValidateScopes`, guard test for `AddEncinaCacheDiagnostics` (null services; a null configure delegate is valid and gets a valid-result test).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: CacheKeyRegistry, CacheOutcomeAccessor and CacheDiagnosticsOptions exist and the behavior records into them.

TASK:
1. Create ICacheDiagnostics (GetStatsAsync, SearchKeysAsync, GetKeyDetailsAsync, DeleteKeysAsync and the exact DeleteKeyAsync; + records), the internal implementation and AddEncinaCacheDiagnostics; the log class and the CachingDiagnostics range registration were PR 1's.
2. Emit EventId 30050 for every delete (count 1 for DeleteKeyAsync); GetStatsAsync(null) with multi-tenancy on returns cache.diagnostics_tenant_required.
3. Write the unit, DI and guard tests; ask mechanical-fixer for the PublicAPI files, the manifest rows and the changelog fragment.

KEY RULES:
- Tenant isolation: every operation takes the tenant and compares it by equality; a tenant never sees or deletes another's entries.
- Entries are addressed by an opaque id and shown redacted; no raw key, no value is ever returned.
- Error codes with constant messages and named ADR-062 categories (InvalidRequest, NotFound, Validation), created with EncinaErrors.Create.
- The glob matcher is iterative (no regular expression); input length and result size are bounded.

REFERENCE FILES:
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- src/Encina/Results/EncinaErrors.cs
- src/Encina/Errors/ErrorCategory.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 3: Headers middleware and endpoints in `Encina.AspNetCore` (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 15 files (the integration test of Design Choice 9 moved to PR 4 to make room for the registry row):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.AspNetCore/CacheDiagnosticHeadersMiddleware.cs` | new (internal sealed) |
| 1b | `src/Encina.AspNetCore/ApplicationBuilderExtensions.cs` | modify (`UseEncinaCacheDiagnosticHeaders`) |
| 1c | `tests/Encina.UnitTests/AspNetCore/ApplicationBuilderExtensionsTests.cs` | modify |
| 1d | `docs/engineering/eventid-block-registry.md` | modify (a row `AspNetCoreCachingDiagnostics \| 60100-60102 \| Observability and platform \| zone` in the "Registered ranges" table and, in the "Blocks" table, the `reserved` row of the slot replaced by `60100-60102 \| Observability and platform \| #276 cache diagnostics (AspNetCore slice) \| registered` with the exact bounds, the rest of the slot (60103-60149) unlisted or `free`; `EventIdRegistryAgreementTests` rules 2 and 3 fail otherwise) |
| 2 | `src/Encina.AspNetCore/CacheDiagnosticsEndpointExtensions.cs` | new (public `MapEncinaCacheDiagnostics`) |
| 3 | `src/Encina.AspNetCore/CacheDiagnosticsAspNetLog.cs` | new (EventIds 60100-60102) |
| 4 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`AspNetCoreCachingDiagnostics = (60100, 60102)`) |
| 5 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.AspNetCore`) |
| 6 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: one line, `static readonly Encina.Diagnostics.EventIdRanges.AspNetCoreCachingDiagnostics -> (int Min, int Max)`, for the new field) |
| 7 | `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/AspNetCore/CacheDiagnosticHeadersMiddlewareTests.cs` | new |
| 9 | `tests/Encina.UnitTests/AspNetCore/CacheDiagnosticsEndpointTests.cs` | new (test server) |
| 10 | `tests/Encina.GuardTests/AspNetCore/CacheDiagnosticsEndpointGuardTests.cs` | new |
| 11 | `.github/coverage-manifest/Encina.AspNetCore.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/276-cache-diagnostics-endpoints.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `MapEncinaCacheDiagnostics(this IEndpointRouteBuilder, string authorizationPolicy, string prefix = "/cache")` returning a `RouteGroupBuilder`, modeled on `MapDPIAEndpoints`: `ArgumentException.ThrowIfNullOrWhiteSpace(authorizationPolicy)`, `RequireAuthorization(policy)` on the group, startup validation that the policy is registered (throws `InvalidOperationException`) and that `AddEncinaCacheDiagnostics` was called; routes `GET {prefix}/stats`, `GET {prefix}/keys?pattern=&limit=`, `GET {prefix}/keys/{id}`, `DELETE {prefix}/keys/{id}` and `DELETE {prefix}/keys?pattern=`. Each handler reads the tenant from the `IRequestContext` of `UseEncinaContext()`; under multi-tenancy a missing tenant is denied before any registry access (EventId 60100).
2. **Create** the middleware: starts a holder (`CacheOutcomeAccessor.Begin`), runs the rest of the pipeline, and writes the headers in `Response.OnStarting` (so they are set before the body) with the prefix from the options; `X-Cache-Key` only with `IncludeKey` and redacted. It is added by `app.UseEncinaCacheDiagnosticHeaders()`, which does nothing when diagnostics are disabled (no `AddEncinaCacheDiagnostics`).
3. **EventIds:** 60100 request denied (no tenant), 60101 keys deleted (count), 60102 diagnostics endpoints mapped (prefix, once at startup); the policy name is never logged.
4. **Tests:** an unauthenticated request is rejected (401), an authenticated one without the policy is rejected (403), the mapping throws on a blank or unregistered policy, tenant isolation over HTTP, the header set for hit, miss and bypass, a response with several cached requests aggregates as specified, the raw key never appears in a header. Guard tests: null builder, blank policy, blank prefix.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: the registry, the outcome accessor and ICacheDiagnostics exist. Encina.AspNetCore references Encina.Caching.
MapDPIAEndpoints (src/Encina.AspNetCore/DPIAEndpointExtensions.cs) is the precedent for fail-closed endpoint mapping.

TASK:
1. Create MapEncinaCacheDiagnostics and the headers middleware with UseEncinaCacheDiagnosticHeaders.
2. Create CacheDiagnosticsAspNetLog (60100-60102) and register AspNetCoreCachingDiagnostics = (60100, 60102) in EventIdRanges.cs and the assembly map.
3. Write the unit, test-server and guard tests; ask mechanical-fixer for the PublicAPI files, the manifest rows and the changelog fragment.

KEY RULES:
- Fail closed: the policy name is required and validated at startup; no tenant under multi-tenancy is denied before any data access.
- Headers never carry a raw key or a value; X-Cache-Key only when IncludeKey is on, redacted.
- Map ICacheDiagnostics errors to ProblemDetails through the existing extensions; never put EncinaError.Message in a log.
- Follow the maintainer's answers to Design Choices 1, 2 and 4, given in your brief.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.AspNetCore/ApplicationBuilderExtensions.cs
- src/Encina.AspNetCore/ProblemDetailsExtensions.cs
- tests/Encina.UnitTests/AspNetCore/ApplicationBuilderExtensionsTests.cs
```

</details>

### Phase 4: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** 30050 keys deleted (count) and 30051 registry eviction in `CachingDiagnostics`; 60100 request denied (no tenant), 60101 diagnostic delete executed, 60102 endpoints mapped in `AspNetCoreCachingDiagnostics`. To keep each id in the assembly that emits it: 30050 and 30051 are emitted by `CacheDiagnostics` and `CacheKeyRegistry` (`Encina.Caching`); 60100-60102 by the endpoints and middleware (`Encina.AspNetCore`). Both ranges are in the 10000+ zone, so `EventIdRegistryAgreementTests` rule 5 holds. Verified free by a repo-wide grep of both assemblies (`Encina.Caching` uses 3500-3513, `Encina.AspNetCore` uses 200-207).
2. **Telemetry:** none by design (the issue marks OpenTelemetry not applicable: diagnostics are a development tool); node-local counters are exposed through `GetStatsAsync`, and exporting them as metrics belongs to #273 and #723 (open).
3. **Coverage targets (per flag, set in the PR that adds each file):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Diagnostics/CacheKeyRegistry.cs` | 100 | 0 | 0 | 0 | Pure bounded structure driven by unit tests; internal, no `InternalsVisibleTo` for the guard project |
| `Encina.Caching/Diagnostics/CacheOutcomeAccessor.cs` | 100 | 50 | 0 | 0 | Small; guard tests reach the public argument checks only |
| `Encina.Caching/Diagnostics/CacheDiagnostics.cs` | 95 | 0 | 0 | 0 | Every operation and error code is unit tested; internal |
| `Encina.Caching/CacheDiagnosticsServiceCollectionExtensions.cs` | 100 | 100 | 0 | 0 | One null guard and the registrations, covered by guard and DI tests |
| `Encina.Caching/Configuration/CacheDiagnosticsOptions.cs` | 100 | 80 | 0 | 0 | Setters with range guards |
| `Encina.Caching/Abstractions/ICacheDiagnostics.cs` | 0 | 0 | 0 | 0 | An interface and records without executable validation |
| `Encina.AspNetCore/CacheDiagnosticsEndpointExtensions.cs` | 90 | 70 | 0 | 0 | Route handlers run on a test server; the startup guards run in guard tests |
| `Encina.AspNetCore/ApplicationBuilderExtensions.cs` | keep | keep | 0 | 0 | An existing file touched by PR 3 (`UseEncinaCacheDiagnosticHeaders`): its targets are never lowered and the new method's null guard and registration are covered by `ApplicationBuilderExtensionsTests.cs` and the guard file |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` (the two new optional constructor parameters) | keep | keep | 0 | 0 | The valid-result guard tests for null `ICacheKeyRecorder?` and `CacheOutcomeAccessor?` (#3798) are in `CacheDiagnosticsGuardTests.cs` of PR 1 |
| `Encina.AspNetCore/CacheDiagnosticHeadersMiddleware.cs` | 100 | 0 | 0 | 0 | Internal; driven through the test server |
| `Encina.Caching/Diagnostics/CacheDiagnosticsLog.cs`, `Encina.AspNetCore/CacheDiagnosticsAspNetLog.cs` | 90 | 0 | 0 | 0 | Generated bodies run in the tests above; internal |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`, `Configuration/CachingOptions.cs` | keep | keep | 0 | 0 | Existing targets are never lowered; the new hooks are tested in PR 1 |

   The worker measures every flag per PR with `coverage-report.cs`; no target is lowered (`target-ratchet`). No contract test is added (`ICacheDiagnostics` has one implementation), no property test, no load or benchmark test (a development tool off the request path).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The three code PRs are merged. Verification remains.

TASK:
Verify each PR carried its per-flag targets and the EventId registrations; fix omissions in a small PR through mechanical-fixer; run
--check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet).
- Add no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- .github/coverage-manifest/Encina.AspNetCore.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 5: Documentation and finalization (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation with `<example>` on `ICacheDiagnostics`, `AddEncinaCacheDiagnostics`, `MapEncinaCacheDiagnostics` and `UseEncinaCacheDiagnosticHeaders`; the remarks state that data is node-local, that no value is returned and that production use needs a policy.
2. PR 4 (four files with `docs/INVENTORY.md`) also carries the integration test of Design Choice 9, `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheDiagnosticsIntegrationTests.cs` (shared Redis fixture, test server: headers, tenant scoping and delete over the real behavior and provider). `docs-writer` (PR 4, two files): `docs/features/cache-diagnostics.md` (new) and a pointer from `docs/features/handler-caching.md` (the handler-caching page owned by the #266 family; `query-caching.md` is the EF Core page and is not touched), with the header table, the endpoint table and the security model, citing tests by name and no typed figure.
3. `PublicAPI.Unshipped.txt` lines are written by `mechanical-fixer` per PR; `docs/INVENTORY.md` is updated by `docs-writer` in PR 4 for the new files (PR 4 is then four files: the integration test, `cache-diagnostics.md`, the `handler-caching.md` pointer and `INVENTORY.md`); no ROADMAP or ADR change (ADR number assigned when the ADR pull request opens, if the maintainer asks for one).
4. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching and AspNetCore namespaces) and `Encina.GuardTests`; for PR 4 `dotnet test tests\Encina.IntegrationTests --filter "FullyQualifiedName~CacheDiagnosticsIntegrationTests"` (Redis container) and, before the PR is marked ready, `dotnet test Encina.slnx --configuration Release` with every test passing; per-flag coverage and the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
All code PRs are merged. Documentation remains.

TASK:
Confirm the XML docs and the three changelog fragments; ask docs-writer for the two documentation pages and the docs/INVENTORY.md update.

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
| AGENTS.md section 3 | Fail-closed gates; keys never raw in logs; ROP for `ICacheDiagnostics` |
| AGENTS.md section 5 | Caching feature identical across the 8 providers: behavior-level registry |
| ADR-062 | Categories of the three diagnostics error codes |
| ADR-021 | EventId ranges registered before use and emitted by the mapped assembly |
| OWASP API Security Top 10 (API1 broken object level authorization, API5 broken function level authorization) | Tenant-scoped ids and a required policy on administrative endpoints |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | Source of every outcome and write |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | Redacted keys in headers and responses |
| `MapDPIAEndpoints` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` | Precedent for fail-closed mapping |
| `RequestContextAccessor` | `src/Encina/Core/RequestContextAccessor.cs` | Precedent for an `AsyncLocal` holder |
| `ProblemDetailsExtensions` | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` | Error to HTTP mapping |
| `QueryCacheHealthCheck` | `src/Encina.Caching/Health/QueryCacheHealthCheck.cs` | Existing health surface, unchanged |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs` | New ranges and assembly maps |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingDiagnostics`, slot 30050-30099 of the Caching block 30000-30999) | 30050-30051 | 30050 diagnostic keys deleted (count), 30051 registry eviction at the bound; 30052-30099 stay free in the slot |
| `Encina.AspNetCore` (new range `AspNetCoreCachingDiagnostics`, slot 60100-60149 of the Observability and platform block 60000-60999; the open plans #182 (60000-60049) and #180 (60050-60099) hold the earlier slots) | 60100-60102 | 60100 request denied (no tenant), 60101 diagnostic delete executed over HTTP, 60102 endpoints mapped. #278's slot is 30000-30049 just before this plan's Caching slot. The existing `Caching` range 3500-3549 and `AspNetCore` range 200-249 are untouched |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 10 | 6 | New: registry, accessor, options, inspection API, implementation, registration, two log classes, middleware, endpoints; modified: behavior, options, `ApplicationBuilderExtensions.cs`, `EventIdRanges.cs`, two PublicAPI |
| Tests | 7 | 3 | Unit (5), guard (3 across PRs); modified: allocation test (twice) |
| Documentation | 1 | 2 | `docs/features/cache-diagnostics.md` (new), `docs/features/handler-caching.md` (owned by the #266 family) and `docs/INVENTORY.md` modified |
| Configuration | 3 | 3 | Three changelog fragments; manifests edited per PR |
| **Total** | **21** | **13** | 4 pull requests of 15, 9, 15 and 4 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #276 adds cache diagnostics: a node-local registry of the keys the query-caching
behavior wrote, an inspection API, response headers and fail-closed diagnostic endpoints. All 8 cache providers behave the same because
the data comes from the behavior, not from the provider.

IMPLEMENTATION OVERVIEW:
1. PR 1: CacheKeyRegistry, CacheOutcomeAccessor, CacheDiagnosticsOptions, behavior hooks.
2. PR 2: ICacheDiagnostics, its implementation, AddEncinaCacheDiagnostics, EventIds 30050-30051.
3. PR 3: headers middleware and MapEncinaCacheDiagnostics in Encina.AspNetCore, EventIds 60100-60102.
4. PR 4: documentation.

KEY PATTERNS:
- Fail closed: the authorization policy is required and validated at startup; no tenant under multi-tenancy is denied.
- No raw key and no value leave the process: ids are opaque, keys are redacted, values are never returned; statistics, search, details and both deletes (DeleteKeysAsync by pattern, DeleteKeyAsync by id) are scoped to the caller's tenant.
- Everything off unless AddEncinaCacheDiagnostics and the mapping are called; state is bounded and instance-level.
- Every new public symbol has PublicAPI lines and guard tests; error codes have named ADR-062 categories and constant messages.
- Follow the maintainer's answers to Design Choices 1, 2 and 4, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends the query-caching behavior; identical on all 8 providers. |
| 2 | OpenTelemetry | ⏭️ Defer | Node-local counters are returned by `GetStatsAsync`; exporting them as metrics is #273 and #723 (open). |
| 3 | Structured Logging | ✅ Include | `CacheDiagnosticsLog` (30050-30051) and `CacheDiagnosticsAspNetLog` (60100-60102). |
| 4 | Health Checks | ❌ N/A | A development tool, not a dependency; provider health is #754. |
| 5 | Validation | ✅ Include | Pattern length, result limit and option ranges are validated. |
| 6 | Resilience | ❌ N/A | Read-only tooling off the request path; a failing delete returns its `Left`. |
| 7 | Distributed Locks | ❌ N/A | Node-local read-only data. |
| 8 | Transactions | ❌ N/A | No unit of work. |
| 9 | Idempotency | ❌ N/A | Reads and idempotent deletes. |
| 10 | Multi-Tenancy | ✅ Include | Every operation is scoped to the caller's tenant by string equality; no tenant under multi-tenancy is denied. |
| 11 | Module Isolation | ⏭️ Defer | Filtering by module follows the module dimension of #746 (open). |
| 12 | Audit Trail | ⏭️ Defer | Administrative deletes deserve an audit event: `artifacts/issues/cache-diagnostics-delete-audit.md`, to be opened by the orchestrator. |

**Deferred items:** value inspection (`artifacts/issues/cache-diagnostics-value-inspection.md`), delete audit (`artifacts/issues/cache-diagnostics-delete-audit.md`), metrics export (#273, #723, open), module filter (#746, open), `X-Cache-Level` and `X-Cache-Stale` (when #268 and a level report exist).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `QueryCachingPipelineBehavior`, `CacheKeyRedactor`, `MapDPIAEndpoints` precedent, `Encina.AspNetCore` reference to `Encina.Caching` | ✅ Available | On `origin/main` |
| #266 PR 1 (`ExpiresAtUtc`) | Open | Only for `X-Cache-TTL`; without it the header is omitted |
| Slots 30050-30099 (Caching) and 60100-60149 (Observability and platform) of the EventId registry | Reserved by this plan; the 60100 slot is re-checked against the registry, `docs/plans` on main and the open `docs/plan-*` branches at code-PR time | Rows with status `reserved` in the "Blocks" table, added when the plan merges or by the first code PR of each range |
| Maintainer answers to Design Choices 1, 2, 4, 5, 8 and 9 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 2, 4, 5, 8 and 9) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers in PR order and `docs-writer` for PR 4.
2. Open the two follow-up issue files.
