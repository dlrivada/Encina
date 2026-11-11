# Implementation Plan: `Encina.Caching.Memcached` — the eighth cache provider

> **Issue**: [#277](https://github.com/dlrivada/Encina/issues/277)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 1 new package, 5 pull requests)
> **Estimated Scope**: ~900-1,200 lines of production code + ~2,500 lines of tests and docs

---

## Summary

Add the `Encina.Caching.Memcached` package so the caching provider set reaches the eight providers that `AGENTS.md` §5 and SPEC-000 REQ-027 / AC-027 require for 1.0. Encina ships seven today: `Encina.Caching.Memory`, `.Hybrid`, `.Redis`, `.Valkey`, `.Dragonfly`, `.Garnet` and `.KeyDB`; the last four are thin wrappers over `Encina.Caching.Redis`. Memcached is the only provider that cannot reuse the Redis code: it has no key enumeration, no pub/sub and no scripting.

What the plan delivers:

- `MemcachedCacheProvider : ICacheProvider` (get, set with TTL, remove, exists, `GetOrSetAsync`, sliding expiration, refresh, pattern removal per Dilemma D2), `MemcachedCacheOptions` with startup validation, `AddEncinaMemcachedCache`, a `MemcachedHealthCheck`, a `[LoggerMessage]` log class.
- No `IPubSubProvider` and no `IDistributedLockProvider`: Memcached has neither, and the lock set is closed at seven providers (SPEC-000 DEC-009).
- The first shared `ICacheProvider` contract suite that runs against all eight providers (today only `QueryCachingContractTests` exists). This plan owns the suite; #746 adds module-scoped key cases to it.
- Pogocache, MemoryPack and NCache stay post-1.0 candidates recorded in the issue; Zaris is evaluated after 1.0 (maintainer comments of 2026-09-21 and later). Nothing of them is planned here.

Provider category: **Caching (8)**, `AGENTS.md` §5. Affected packages: new `Encina.Caching.Memcached`; touched `Encina` (`EventIdRanges.cs`), `Encina.UnitTests` (EventId allocation map), `Encina.TestInfrastructure` (fixtures), `Directory.Packages.props`, `Encina.slnx`. The database-provider rule (10 providers) does not apply.

Size: the whole feature is about 45 files, so it ships as five pull requests of at most 15 files and 1,500 lines each (see Next Steps).

---

## Design Choices

<details>
<summary><strong>1. Package shape — a standalone <code>Encina.Caching.Memcached</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New standalone package `Encina.Caching.Memcached`** | Same shape as `Encina.Caching.Memory`/`Redis`; own dependencies; opt-in | One more package to maintain |
| **B) Add a Memcached mode to `Encina.Caching.Redis`** | No new package | The Redis package would depend on a Memcached client; Redis-only features (SCAN, pub/sub, locks) leak into a provider without them |
| **C) A wrapper over a generic cache library (FusionCache, CacheManager)** | Many backends at once | Hides provider behavior behind another abstraction; contradicts one-package-per-provider |

### Chosen Option: **A — a standalone `Encina.Caching.Memcached` package**

### Rationale

Every provider is its own package that implements `ICacheProvider` and registers itself with `TryAddSingleton` (see `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs`, `src/Encina.Caching.Memory/ServiceCollectionExtensions.cs`). Valkey, KeyDB, Garnet and Dragonfly wrap Redis because they speak its protocol; Memcached does not, so the wrapper pattern does not apply.

**Class:** Settled

**Settled by:** `AGENTS.md` §5 (Caching row: eight providers, one per backend), SPEC-000 REQ-027 / AC-027, and the maintainer comment of 2026-09-21 on #277 (only Memcached enters 1.0).

</details>

<details>
<summary><strong>2. Client library — which Memcached client the provider wraps (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) EnyimMemcachedCore** | The most used .NET client (about 11.8 million downloads on NuGet), Apache-2.0, targets `net10.0`, version 3.5.2 published on 2026-10-07 (active), built-in node location and `IMemcachedClient` with async calls | Adds a third-party dependency to the provider; its API returns `false` on failure instead of throwing, so the provider must translate it |
| **B) Another maintained client chosen after an evaluation** | Could fit better if Enyim's API is found lacking | Costs an evaluation; the alternatives found on NuGet today are Enyim forks or wrappers (`bcuff.EnyimMemcachedCore`, `EasyCaching.Memcached`) |
| **C) A thin in-house client over the Memcached meta protocol** | Owns the code, no dependency, meta protocol is the forward path | Encina would own protocol code, node selection and connection pooling, which is outside its scope |

### Chosen Option: **A — EnyimMemcachedCore** (recommended, pending the maintainer)

### Rationale

Option A wins when the team wants the least code and the widest compatibility, and the NuGet data above shows the maintenance and licence bar of the issue (open licence, sustained activity, adoption) is met today. B wins only if the implementer finds a blocking defect (for example no cancellation support); C wins only if no maintained client exists, which is not the case. The provider hides the client behind its own options, so changing the library later does not change the public surface. The choice is re-checked when work starts, with the NuGet data refreshed.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Pattern removal on a store that cannot list keys (Dilemma D2)</strong></summary>

### Options Considered

The pattern shapes in use today decide this choice. `QueryCacheInterceptor` removes `"{KeyPrefix}:*:{entityType}:*"` after every `SaveChanges` that touches an entity (`src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs`, lines 542-590), and `CacheInvalidationPipelineBehavior` removes the pattern of `[InvalidatesCache]` built by `GeneratePatternFromTemplate`. Both are globs with a wildcard in the middle, so any option must honour arbitrary globs or declare them unsupported.

| Option | Pros | Cons |
|--------|------|------|
| **A) Key index** (a per-prefix set of keys stored in Memcached, matched against the glob at removal time) | Precise: removes exactly the matching keys; honours any glob | Every set adds a write; the index is itself an evictable value, so an evicted index leaves stale entries that survive an invalidation (a stale read, the unsafe direction) |
| **B) Namespace versioning** (a version number inside the key prefix; removal increments it) | O(1) removal, no index, fails safe (a lost version means a new version) | Only works for patterns that are a known prefix; cannot honour `sm:qc:*:Order:*` or the `t:*` / `m:*` shapes, which would break EF query-cache invalidation |
| **C) Throw `NotSupportedException` from `RemoveByPatternAsync`** | Simplest and honest | Breaks the shared `ICacheProvider` contract (§5: every provider supports the same operations); EF query caching and `[InvalidatesCache]` would fail at runtime |
| **D) One global epoch per key prefix** (every key embeds the epoch; any pattern removal increments it, so the whole logical cache is dropped) | Correct for every glob; fails safe (a lost epoch key gets a new value, all entries miss); no index | Over-invalidates: every removal empties the cache of that prefix, so the hit rate drops for write-heavy workloads; each operation needs the epoch (kept in a short in-process window, default 1 second, which bounds cross-instance staleness) |

### Chosen Option: **D — global epoch per key prefix** (recommended, pending the maintainer)

### Rationale

Dropping extra entries is always correct for a cache; serving a stale one is not. D is the only option that honours every glob in use and fails in the safe direction. A wins when invalidation is frequent and precise removal matters more than the stale-read risk after index eviction; B wins when the application invalidates only known prefixes and never uses the EF query cache; C wins when callers use tag or key invalidation only. The documentation states that write-heavy, invalidation-driven caches belong on the Redis family, where `SCAN` gives precise removal.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Capabilities — no pub/sub and no distributed lock</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Register only `ICacheProvider`** | Honest to the backend; `CacheInvalidationPipelineBehavior` already takes `IPubSubProvider?` as optional | Cross-instance invalidation needs a separate backplane |
| **B) Emulate pub/sub with polling keys** | Gives a backplane | Invents a protocol on a store without one; latency and load cost |
| **C) Emulate locks with `add` semantics** | A lock provider for free | The lock set is closed at seven providers; an unreviewed lock is a correctness risk |

### Chosen Option: **A — register only `ICacheProvider`**

### Rationale

`AGENTS.md` §5 asks for a pub/sub backplane "where applicable"; it is not applicable here. The lock list is fixed by SPEC-000 DEC-009. With `CachingOptions.EnablePubSubInvalidation` (default true) and no `IPubSubProvider`, the invalidation behavior already skips the broadcast.

**Class:** Settled

**Settled by:** `AGENTS.md` §5 (Caching row, "pub/sub backplane where applicable"; distributed-locks row, exactly seven providers), SPEC-000 DEC-009.

</details>

<details>
<summary><strong>5. Serialization and error semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `System.Text.Json` with the options `RedisCacheProvider` uses (camelCase, not indented); a failed store throws, honouring `CachingOptions.ThrowOnCacheErrors` in the behaviors** | Same wire format and failure behavior as the existing providers | Does not create the serialization abstraction `AGENTS.md` §5 mentions (see Prerequisites) |
| **B) A new serializer abstraction first** | Matches the AGENTS wording | Scope creep: `CachingOptions.SerializerOptions` has no consumer in any provider today |
| **C) Return `false` on failure like the client does** | Matches the client | Swallows the error; `ICacheProvider` methods return `Task`, so the caller could not tell |

### Chosen Option: **A — the Redis provider's JSON and exception behavior**

### Rationale

Enyim reports failures as a `false` result; the provider converts that to an exception so the pipeline behaviors decide, as with Redis. No `EncinaError.Message` reaches logs: only the operation name and the redacted key (`CacheKeyRedactor.Redact`) are logged.

**Class:** Trivial

**Settled by:** `AGENTS.md` §3 (errors are never swallowed; `EncinaError.Message` never reaches logs) and the reference implementation `src/Encina.Caching.Redis/RedisCacheProvider.cs`.

</details>

<details>
<summary><strong>6. Where the eight-provider contract suite lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An abstract `CacheProviderContractTests` in `Encina.TestInfrastructure`, one subclass per provider in `Encina.ContractTests/Caching`** | One set of assertions for all eight; message-broker contract tests already run containers from `Encina.ContractTests` | Needs a container image for each Redis-protocol provider |
| **B) Per-provider integration tests only** | No shared base | Eight copies of the same assertions drift apart; AC-027 asks for a contract that covers the 8 caches |
| **C) Contract tests for in-process providers only** | No Docker | Leaves Memcached and the Redis family unproven |

### Chosen Option: **A — a shared abstract suite with one subclass per provider**

### Rationale

Contract tests must instantiate real implementations (`AGENTS.md` §9). The suite covers get, set with TTL, remove, exists, `GetOrSetAsync`, sliding expiration, refresh and (per D2) pattern removal, plus keys that contain `:` and the module and tenant segments #746 adds.

**Class:** Trivial

**Settled by:** `AGENTS.md` §9 (contract tests instantiate real implementations; Docker for real infrastructure) and SPEC-000 AC-027.

</details>

---

## Implementation Phases

### Phase 1: Package Foundation — project, options, validation

<details>
<summary>Tasks</summary>

1. `src/Encina.Caching.Memcached/Encina.Caching.Memcached.csproj`: references `Encina.Caching` and `Encina.Messaging` (for `EncinaHealthCheck`, as `Encina.Caching.Redis` does), the Enyim package (version added to `Directory.Packages.props`), `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`; `GlobalSuppressions.cs`.
2. `MemcachedCacheOptions` (public sealed): `Servers` (list of `host:port`), `DefaultExpiration` (5 minutes), `KeyPrefix`, `EpochRefreshWindow` (1 second, D2), `ProviderHealthCheck` (`ProviderHealthCheckOptions`). Any option that carries credentials (SASL user and password, if the client exposes them) is `[JsonIgnore]` and `ToString()` is overridden (`AGENTS.md` §3).
3. `MemcachedCacheOptionsValidator : IValidateOptions<MemcachedCacheOptions>`: at least one server, each parses as `host:port`, expiration greater than zero.
4. Add the project to `Encina.slnx`; add the package to `Directory.Packages.props`.
5. Register the EventId range (Phase 5) and a coverage manifest `.github/coverage-manifest/Encina.Caching.Memcached.json` with per-file targets and justifications (`AGENTS.md` §9).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14. Caching providers are one package each under src/Encina.Caching.*. This phase creates the empty-but-building package and its options.
TASK: Create src/Encina.Caching.Memcached with the csproj, MemcachedCacheOptions, MemcachedCacheOptionsValidator, GlobalSuppressions.cs and the two PublicAPI files; add it to Encina.slnx and the client package to Directory.Packages.props.
KEY RULES: Zero warnings; XML docs on every public member; credentials in options are [JsonIgnore] with a ToString() override; TimeProvider by injection only; PublicAPI.Unshipped.txt lists every public symbol.
REFERENCE FILES: src/Encina.Caching.Redis/Encina.Caching.Redis.csproj, src/Encina.Caching.Redis/RedisCacheProvider.cs (RedisCacheOptions at the end), src/Encina.Caching.KeyDB/Encina.Caching.KeyDB.csproj, Directory.Packages.props.
```

</details>

### Phase 2: The Memcached cache provider

<details>
<summary>Tasks</summary>

1. `MemcachedCacheProvider` (public sealed partial class, `ICacheProvider`) in `src/Encina.Caching.Memcached/`. Constructor: the Enyim `IMemcachedClient`, `IOptions<MemcachedCacheOptions>`, `ILogger<MemcachedCacheProvider>`, `TimeProvider? timeProvider = null`.
2. `GetAsync<T>` / `SetAsync<T>`: JSON with camelCase options; `SetAsync` uses `expiration ?? DefaultExpiration`; a `false` from the client throws `InvalidOperationException` with the operation name only (no key, no server text in the message).
3. `RemoveAsync`, `ExistsAsync`, `GetOrSetAsync` (factory called on miss, store, return), `SetWithSlidingExpirationAsync` and `RefreshAsync` (a metadata entry holding the sliding window and the absolute limit from `TimeProvider`, extended with the client's touch; the same approach as `RedisCacheProvider.SlidingMetadata`).
4. Epoch handling (D2): an internal `KeyEpoch` helper reads the epoch value of the prefix (key `{KeyPrefix}:epoch`), caches it in process for `EpochRefreshWindow` using `TimeProvider`, and creates a random value when absent. Every logical key is stored as `{KeyPrefix}:{epoch}:{key}`. `RemoveByPatternAsync(pattern)` validates the pattern, replaces the epoch with a new random value and logs the count as unknown (Memcached cannot report it). If the maintainer picks another option for D2, only this helper and `RemoveByPatternAsync` change.
5. Keys are redacted in every log call with `CacheKeyRedactor.Redact`; keys longer than the protocol limit (250 bytes) or containing whitespace or control characters are rejected with `ArgumentException` before reaching the client (Memcached key rules).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 created the package. ICacheProvider (src/Encina.Caching/Abstractions/ICacheProvider.cs) has GetAsync, SetAsync, RemoveAsync, RemoveByPatternAsync, ExistsAsync, GetOrSetAsync, SetWithSlidingExpirationAsync and RefreshAsync. Memcached cannot enumerate keys, so pattern removal uses a global epoch per key prefix (Dilemma D2, or the option the maintainer chose).
TASK: Implement MemcachedCacheProvider and the KeyEpoch helper as described in the Tasks block; keep every method at cyclomatic complexity of 5 or less so CRAP stays at or below 10 with the planned tests.
KEY RULES: Never swallow a failure (throw, do not return false); never log EncinaError.Message or the raw key; TimeProvider for every time read; cancellation honoured with CancellationToken on every client call; no synchronous client calls.
REFERENCE FILES: src/Encina.Caching.Redis/RedisCacheProvider.cs, src/Encina.Caching.Memory/MemoryCacheProvider.cs, src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs, src/Encina.Caching/Abstractions/ICacheProvider.cs.
```

</details>

### Phase 3: Configuration, DI & health check

<details>
<summary>Tasks</summary>

1. `ServiceCollectionExtensions.AddEncinaMemcachedCache(this IServiceCollection, Action<MemcachedCacheOptions>? configure = null)`: registers `TimeProvider.System` (TryAdd), the options and the validator with `ValidateOnStart`, the Enyim client from `Servers`, `ICacheProvider` as `TryAddSingleton<ICacheProvider, MemcachedCacheProvider>()`, and the health check when `ProviderHealthCheck.Enabled`. It never registers `IPubSubProvider` or `IDistributedLockProvider`.
2. `MemcachedHealthCheck : EncinaHealthCheck` with `DefaultName = "encina-memcached"` and tags `["encina", "cache", "memcached", "ready"]`, scoped resolution through `IServiceProvider`, a timeout from `ProviderHealthCheckOptions` and a sentinel round trip. Unhealthy results carry the exception type only, never its message.
3. DI test: build the provider with `ValidateOnBuild` and `ValidateScopes`, resolve `ICacheProvider`, and prove that a database or other `ICacheProvider` registered first is not overridden (`TryAdd`) in either registration order (`AGENTS.md` §3 registration completeness).
4. Fix the stale list in the XML remarks of `AddEncinaCaching` (`src/Encina.Caching/ServiceCollectionExtensions.cs`): it names `AddEncinaNCacheCache`, which does not exist, and does not name Memcached.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phase 2 produced MemcachedCacheProvider. Registration style, options validation and the health-check base class are fixed by the Redis package.
TASK: Add AddEncinaMemcachedCache, MemcachedHealthCheck and the DI tests; correct the XML remarks of AddEncinaCaching.
KEY RULES: TryAdd* for ICacheProvider so the application's own registration wins in any order; options validated at startup (ValidateOnStart); health check resolves dependencies through IServiceProvider scope; the Unhealthy result never contains an exception message.
REFERENCE FILES: src/Encina.Caching.Redis/ServiceCollectionExtensions.cs, src/Encina.Caching.Redis/Health/RedisHealthCheck.cs, src/Encina.Messaging/Health (EncinaHealthCheck, ProviderHealthCheckOptions), tests/Encina.UnitTests/Caching/Redis/ServiceCollectionExtensionsTests.cs.
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Structured logging: the Phase 5 log class (included).
2. Health check: Phase 3 (included). #754 covers "6 remaining" providers and does not list Memcached; the Memcached check follows the same shape so #754's pattern applies unchanged.
3. OpenTelemetry: deferred to #723 ("Add OpenTelemetry instrumentation to all Caching providers"). The Memcached provider keeps its operations in small methods with one logging call each so the shared instrumentation of #723 attaches the same way as for Redis. When #723 lands first, Phase 5 adds the Memcached instrumentation in this feature's PR 2; otherwise #723 absorbs it.
4. Resilience: deferred. The client has its own timeouts and node failure handling; a Polly policy around cache calls is not what the other seven providers do. This plan adds nothing provider-specific, so it opens no issue; a cache-wide resilience policy, if the maintainer wants one, would apply to all eight providers at once.
5. Multi-tenancy and module isolation: not applicable to the provider (opaque keys); the key generators own both (#746).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: AGENTS.md §6 requires every feature to be evaluated against the 12 transversal functions; the matrix at the end of this plan holds the result for Memcached.
TASK: Confirm that logging and the health check from Phases 3 and 5 are in place; leave a one-line code comment pointing at #723 where the instrumentation attaches; do not add a resilience wrapper.
KEY RULES: No ActivitySource or Meter is created by this provider before #723 defines the shared names; no tag or log field carries a key, a value or an error message.
REFERENCE FILES: src/Encina.Caching.Redis/RedisCacheProvider.cs (log call shape), docs/plans of #723 when it exists, AGENTS.md §6.
```

</details>

### Phase 5: Observability — logging and EventIds

<details>
<summary>Tasks</summary>

1. Register `public static readonly (int Min, int Max) CachingMemcached = (6950, 6979);` in `src/Encina/Diagnostics/EventIdRanges.cs` with the XML summary "Encina.Caching.Memcached — Memcached cache provider" (the family block for this run is 6950-6999; 6980-6999 stay free).
2. Add `["Encina.Caching.Memcached"] = [nameof(EventIdRanges.CachingMemcached)]` to the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; add the field to `src/Encina/PublicAPI.Unshipped.txt`.
3. `Diagnostics/MemcachedLogMessages.cs` with `[LoggerMessage]` methods, ids packed from 6950: 6950 cache hit, 6951 cache miss, 6952 cache set, 6953 cache remove, 6954 epoch rotated (pattern removal), 6955 epoch created, 6956 operation failed (error type only), 6957 key rejected, 6958 health check failed (exception type only). Ids 6959-6979 are reserved for the #723 additions. XML docs name the range.
4. Run the architecture tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: ADR-021 / AGENTS.md §7: every EventId range is registered in EventIdRanges.cs before use and ids are packed with no gaps.
TASK: Register CachingMemcached = (6950, 6979), map the assembly in EncinaEventIdAllocationTests, add the log class with ids 6950 to 6958, update the PublicAPI file of Encina, run the architecture tests.
KEY RULES: [LoggerMessage] source generator, not LoggerMessage.Define; ids packed sequentially; an exception is logged through ForLogging() (type and stack trace), never its message; keys go through CacheKeyRedactor.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Caching.Redis/RedisCacheProvider.cs (log messages at the end).
```

</details>

### Phase 6: Testing — 7 test types

<details>
<summary>Tasks</summary>

Per-flag targets for the new files (to be written into the manifest with one-sentence justifications; measured values replace the proposals when the code exists):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `MemcachedCacheProvider.cs` | 90 | 25 | 60 | 40 | 70 |
| `KeyEpoch.cs` (inside the provider folder) | 95 | 20 | 0 | 40 | 60 |
| `MemcachedCacheOptions.cs` and validator | 100 | 40 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 95 | 45 | 0 | 0 | 0 |
| `MemcachedHealthCheck.cs` | 90 | 30 | 0 | 0 | 60 |

1. **Unit** (`tests/Encina.UnitTests/Caching/Memcached/`): provider against a substituted `IMemcachedClient` (NSubstitute): hit, miss, set with default and explicit TTL, failure throws, oversized or invalid key rejected, epoch window with `FakeTimeProvider`, pattern removal rotates the epoch, sliding expiration and refresh; options validator; DI.
2. **Guard** (`tests/Encina.GuardTests/Caching/Memcached/`): every public method and constructor parameter.
3. **Contract** (`tests/Encina.ContractTests/Caching/`): abstract `CacheProviderContractTests` in `Encina.TestInfrastructure` plus eight subclasses (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, Memcached); containers through the generic `Testcontainers` package (4.15.0) for the Redis-protocol servers and Memcached (there is no `Testcontainers.Memcached` entry in `Directory.Packages.props`).
4. **Property** (`tests/Encina.PropertyTests/Infrastructure/Caching/`): set then get returns the stored value for any serializable value and key; TTL expiry with a fake clock; epoch rotation never leaves a pre-rotation entry readable.
5. **Integration** (`tests/Encina.IntegrationTests/Infrastructure/Caching/`): a `MemcachedFixture` and `MemcachedCollection` following `RedisFixture`/`RedisCollection`, `[Trait("Category", "Integration")]`, `[Trait("Database", "Memcached")]`; provider, registration and health check against the real server, including the 1 MB value limit.
6. **Load**: `.md` justification (`tests/Encina.LoadTests/...`): the provider has no concurrency logic of its own beyond the client.
7. **Benchmark**: a `tests/Encina.BenchmarkTests/Encina.Caching.Memcached.Benchmarks` project following `Encina.Caching.Redis.Benchmarks` (uses `BenchmarkSwitcher`); the comparison table of the eight providers is documented from its output under `artifacts/performance/`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Coverage is measured per flag (unit, guard, contract, property, integration) against .github/coverage-manifest/Encina.Caching.Memcached.json. Tests must execute real package code; assertions use Shouldly through Encina.Testing.Shouldly.
TASK: Write the unit, guard, contract, property, integration, benchmark tests and the load-test justification described in the Tasks block; measure each flag with dotnet test --collect "XPlat Code Coverage" and the coverage-report.cs script; keep CRAP at or below 10 on changed methods.
KEY RULES: Integration tests use the shared [Collection] fixture, never IClassFixture or new(); call ClearAllDataAsync-style cleanup in InitializeAsync; no Thread.Sleep (use FakeTimeProvider); no FluentAssertions.
REFERENCE FILES: tests/Encina.UnitTests/Caching/Redis/RedisCacheProviderTests.cs, tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs, tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs, tests/Encina.ContractTests/Database/Caching/QueryCachingContractTests.cs.
```

</details>

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML documentation on all public APIs with examples (`AddEncinaMemcachedCache`).
2. `src/Encina.Caching.Memcached/README.md` (options, limits: no pub/sub, no locks, 1 MB values, 250-byte keys, the D2 behavior).
3. Configuration guide and the cache provider comparison under `docs/` (via `docs-writer`): eight providers by pattern removal, pub/sub, locks, maximum value, licence (BSD for Memcached; Apache-2.0 for the client).
4. `changelog.d/277-memcached-cache-provider.added.md`.
5. ADR for the client library and the pattern-removal strategy, written when the maintainer answers D1 and D2 (ADR number assigned when the ADR pull request opens).
6. `docs/INVENTORY.md` and `ROADMAP.md` entries; PublicAPI files; `dotnet build --configuration Release` with zero warnings; all tests green; every coverage flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package, tests and EventIds exist. Documentation is English, junior-readable, with figures cited by covref markers rather than typed by hand.
TASK: Write the README, the guide section and comparison table, the changelog fragment, the ADR and the inventory entries listed in the Tasks block, through the specialists that own them.
KEY RULES: Never edit CHANGELOG.md; never type coverage numbers by hand; name the limits of Memcached plainly; cite DEC-009 for the closed lock set.
REFERENCE FILES: src/Encina.Caching.Redis/README.md, changelog.d/README.md, docs/architecture/adr/index.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| SPEC-000 REQ-027, DEC-003, AC-027 | Eight cache providers in 1.0; contract tests cover all eight | Memcached is the only one missing |
| SPEC-000 DEC-009 | Closed lock set | Memcached is not a lock provider |
| ADR-021 / `AGENTS.md` §7 | EventId registration | Range 6950-6979 |
| Memcached protocol limits | 250-byte keys, 1 MB default value, no key listing | Drives D2 and the key validation |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ICacheProvider` | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | The contract implemented |
| `RedisCacheProvider`, `RedisCacheOptions` | `src/Encina.Caching.Redis/RedisCacheProvider.cs` | JSON options, sliding-expiration metadata, log shape |
| `AddEncinaRedisCache` | `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` | Registration pattern (`TryAddSingleton`, health check) |
| `RedisHealthCheck`, `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Caching.Redis/Health/`, `src/Encina.Messaging/Health` | Health check shape |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | Redacted keys in logs |
| `CacheInvalidationPipelineBehavior`, `QueryCacheInterceptor` | `src/Encina.Caching/Behaviors/`, `src/Encina.EntityFrameworkCore/Caching/` | Consumers of `RemoveByPatternAsync` that decide D2 |
| `RedisFixture`, `RedisCollection` | `tests/Encina.TestInfrastructure/Fixtures/`, `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures` | Template for `MemcachedFixture` |
| `Encina.Caching.Redis.Benchmarks` | `tests/Encina.BenchmarkTests/` | Template for the benchmark project |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching.Memcached` | 6950-6979 (new, from the family block 6950-6999) | Ids 6950-6958 used by this plan; 6959-6979 reserved for #723 |
| Free in the family block | 6980-6999 | Unassigned |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Package source | 8 | csproj, provider, key epoch, options, validator, DI, health check, log class |
| Package housekeeping | 3 | `GlobalSuppressions.cs`, two PublicAPI files |
| Repository wiring | 6 | `Encina.slnx`, `Directory.Packages.props`, `EventIdRanges.cs`, Encina `PublicAPI.Unshipped.txt`, EventId allocation test, coverage manifest |
| Unit and guard tests | 6 | provider, options, DI, health check, guard x2 |
| Contract, property, integration tests | 14 | abstract suite, 8 subclasses, fixture, collection, property, integration x2 |
| Benchmark and load | 4 | 3 benchmark files, 1 justification |
| Documentation | 6 | README, guide, comparison, changelog, ADR, inventory |
| **Total** | **~47** | Five pull requests |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Caching has eight provider packages (AGENTS.md §5); seven exist, and this work adds Encina.Caching.Memcached. No backward compatibility, no [Obsolete], zero warnings, XML docs on all public APIs.

IMPLEMENTATION OVERVIEW: Phase 1 package and options; Phase 2 MemcachedCacheProvider and the epoch helper for pattern removal; Phase 3 AddEncinaMemcachedCache and the health check; Phase 4 cross-cutting evaluation (OpenTelemetry deferred to #723); Phase 5 EventIds 6950-6979 and the log class; Phase 6 unit, guard, contract (eight providers), property, integration, benchmark tests; Phase 7 docs. Ship as five pull requests: (1) package, options, provider, log class, wiring; (2) DI, health check, their tests; (3) contract suite and fixtures; (4) property, benchmark and load justification; (5) documentation.

KEY PATTERNS: TryAddSingleton for ICacheProvider; IValidateOptions with ValidateOnStart; credentials [JsonIgnore] plus ToString override; failures throw, never return false; no EncinaError.Message or raw key in logs; TimeProvider for every clock read; shared [Collection] fixtures for integration tests; Shouldly assertions; CRAP at or below 10 on changed methods.

REFERENCE FILES: src/Encina.Caching/Abstractions/ICacheProvider.cs, src/Encina.Caching.Redis/RedisCacheProvider.cs, src/Encina.Caching.Redis/ServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs, .github/coverage-manifest/Encina.Caching.Redis.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | This is the caching implementation itself. |
| 2 | OpenTelemetry | ⏭️ | Deferred to #723 (instrumentation for all caching providers); the provider is shaped so it attaches the same way as for Redis. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` class with EventIds 6950-6958 in the registered range 6950-6979. |
| 4 | Health Checks | ✅ | `MemcachedHealthCheck` in the shape of `RedisHealthCheck`; #754 does not list Memcached. |
| 5 | Validation | ✅ | `IValidateOptions<MemcachedCacheOptions>` with `ValidateOnStart`; no request-level validation. |
| 6 | Resilience | ⏭️ | The client owns timeouts and node failure; no provider-specific policy, in line with the other seven providers. |
| 7 | Distributed Locks | ❌ | Memcached is not one of the seven lock providers (SPEC-000 DEC-009). |
| 8 | Transactions | ❌ | Cache operations are independent of database transactions. |
| 9 | Idempotency | ❌ | A cache set is idempotent by nature. |
| 10 | Multi-Tenancy | ❌ | The provider stores opaque keys; tenant scoping is done by the key generators (#746). |
| 11 | Module Isolation | ❌ | Module scoping belongs to key generation (#746). |
| 12 | Audit Trail | ❌ | Infrastructure, not an auditable business operation. |

---

## Prerequisites & Dependencies

- **Not blocking**: #723 (OpenTelemetry for caching) and #754 (health checks for the remaining providers) are open; the plan works with or without them as described in Phase 4.
- **Shared with #746**: this plan owns the `CacheProviderContractTests` suite; #746 adds the module-scoped key cases to it and, if it merges first, leaves that one test step to this feature.
- **Follow-up findings** (written as issue files, see the run report): `CachingOptions.SerializerOptions` is copied by `AddEncinaCaching` but read by no provider (the serialization abstraction `AGENTS.md` §5 names does not exist yet), and `DefaultCacheKeyGenerator.GeneratePattern` emits `t:*` for a context without tenant while keys then carry no tenant segment.
- **Package version**: the Enyim package version is added to `Directory.Packages.props` at implementation time, with the NuGet data re-checked (D1).

---

## Next Steps

1. Ask the maintainer D1 (client library) and D2 (pattern removal) when work on #277 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package, options, provider, log class, EventId range, wiring (about 15 files).
3. Pull request 2: `AddEncinaMemcachedCache`, health check, unit and guard tests, `AddEncinaCaching` remarks fix.
4. Pull request 3: abstract contract suite, eight subclasses, `MemcachedFixture` and the Redis-protocol fixtures.
5. Pull request 4: property tests, benchmark project, load-test justification.
6. Pull request 5: README, guide, comparison table, ADR, changelog fragment, inventory.
