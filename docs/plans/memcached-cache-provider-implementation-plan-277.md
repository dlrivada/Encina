# Implementation Plan: `Encina.Caching.Memcached` — the eighth cache provider

> **Issue**: [#277](https://github.com/dlrivada/Encina/issues/277)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 1 new package, 9 pull requests)
> **Estimated Scope**: ~900-1,200 lines of production code + ~5,800 lines of tests, fixtures, manifests and docs (about 7,000 in total), in nine pull requests of 5 to 13 files each (see Estimated File Count)

---

## Summary

Add the `Encina.Caching.Memcached` package so the caching provider set reaches the eight providers that `AGENTS.md` §5 and SPEC-000 REQ-027 / AC-027 require for 1.0. Encina ships seven today: `Encina.Caching.Memory`, `.Hybrid`, `.Redis`, `.Valkey`, `.Dragonfly`, `.Garnet` and `.KeyDB`; the last four are thin wrappers over `Encina.Caching.Redis`. Memcached is the only provider that cannot reuse the Redis code: it has no key enumeration, no pub/sub and no scripting.

What the plan delivers:

- `MemcachedCacheProvider : ICacheProvider` (get, set with TTL, remove, exists, `GetOrSetAsync`, sliding expiration, refresh, pattern removal per Dilemma D2), `MemcachedCacheOptions` with startup validation, `AddEncinaMemcachedCache`, a `MemcachedHealthCheck`, a `[LoggerMessage]` log class.
- No `IPubSubProvider` and no `IDistributedLockProvider`: Memcached has neither, and the lock set is closed at seven providers (SPEC-000 DEC-009).
- The first shared `ICacheProvider` contract suite that runs against all eight providers (today only `QueryCachingContractTests` exists, and the only cache container fixture on main is `RedisFixture`, in two copies). This plan owns the suite; #746 adds module-scoped key cases to it in its last pull request.
- Pogocache, MemoryPack and NCache stay post-1.0 candidates recorded in the issue; Zaris is evaluated after 1.0 (maintainer comments of 2026-09-21 and later). Nothing of them is planned here.

Provider category: **Caching (8)**, `AGENTS.md` §5. Affected packages: new `Encina.Caching.Memcached`; touched `Encina` (`EventIdRanges.cs`), `Encina.UnitTests` (EventId allocation map), `Encina.TestInfrastructure` (fixtures), `Directory.Packages.props`, `Encina.slnx`. The database-provider rule (10 providers) does not apply.

Size: the whole feature is about 63 distinct files (73 file touches), so it ships as nine pull requests of at most 15 files and 1,500 lines each, each with the tests, coverage-manifest rows and PublicAPI lines that go with its code (see Estimated File Count and Next Steps).

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
| **D) One global epoch per key prefix** (every key embeds the epoch; any pattern removal increments it, so the whole logical cache is dropped) | Correct for every glob; a lost epoch key gets a new value so every entry misses (a lost value fails in the safe direction); no index | Over-invalidates: every removal empties the cache of that prefix, so the hit rate drops for write-heavy workloads; each operation needs the epoch, kept in an in-process window (`EpochRefreshWindow`, default 1 second), so **another instance's pattern removal is served stale for up to that window**: an accepted, bounded consequence of D |

### Chosen Option: **D — global epoch per key prefix** (recommended, pending the maintainer)

### Rationale

Dropping extra entries is always correct for a cache; serving a stale one is not, and D never serves an entry from before a removal **on the instance that removed it**, nor on any instance once its window has passed. D is the only option that honours every glob in use. Semantics fixed by this plan: (1) the rotating instance updates its own in-process epoch immediately after writing the new value, so its next read misses at once; (2) other instances see the new epoch after at most `EpochRefreshWindow` (the staleness window; 1 second recommended, shorter windows cost one extra `get` per window, longer ones widen the staleness); (3) the epoch key is created with `add` (not `set`) so two instances that start together cannot write two different epochs: the loser re-reads the winner's value; (4) rotation uses `set` with a fresh random value, because rotation must overwrite. A wins when invalidation is frequent and precise removal matters more than the stale-read risk after index eviction; B wins when the application invalidates only known prefixes and never uses the EF query cache; C wins when callers use tag or key invalidation only. The documentation states the staleness window and that write-heavy, invalidation-driven caches belong on the Redis family, where `SCAN` gives precise removal.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Capabilities — no pub/sub (and no lock provider)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Register only `ICacheProvider`** | Honest to the backend; `CacheInvalidationPipelineBehavior` already takes `IPubSubProvider?` as optional | Cross-instance invalidation needs a separate backplane |
| **B) Emulate pub/sub with polling keys** | Gives a backplane | Invents a protocol on a store without one; latency and load cost |

### Chosen Option: **A — register only `ICacheProvider`**

### Rationale

`AGENTS.md` §5 asks for a pub/sub backplane "where applicable"; Memcached has no pub/sub, and emulating one with polling would invent a protocol the other backplane providers do not share, so it is judged not applicable. With `CachingOptions.EnablePubSubInvalidation` (default true) and no `IPubSubProvider`, the invalidation behavior already skips the broadcast. A lock provider is not a choice of this plan at all: the lock set is closed at exactly seven providers by SPEC-000 DEC-009 (the `AGENTS.md` §5 distributed-locks row) and Memcached is not one of them, so no `IDistributedLockProvider` is registered and no lock is emulated with `add` semantics.

**Class:** Trivial

</details>

<details>
<summary><strong>5. Serialization (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `System.Text.Json` with the options `RedisCacheProvider` uses (camelCase, not indented), with no new abstraction; a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/caching-serializer-abstraction.md) tracks the abstraction for all providers** | Same wire format as the existing providers; no scope creep | Does not create the serialization abstraction `AGENTS.md` §5 names ("serialization abstraction" among what every cache provider MUST support) in this feature |
| **B) A new serializer abstraction first** | Satisfies the AGENTS wording literally | Scope creep and a change to all seven existing providers: `CachingOptions.SerializerOptions` is copied by `AddEncinaCaching` but read by no provider today |

### Chosen Option: **A — the Redis provider's JSON, abstraction deferred** (recommended, pending the maintainer)

### Rationale

A keeps one wire format across the eight providers until the abstraction exists for all of them; B satisfies the §5 wording directly and wins if the maintainer does not want a provider shipped before it. Because B also satisfies the cited rule, this is not Settled.

**Class:** Dilemma

</details>

<details>
<summary><strong>5b. Failure semantics on reads and writes (Dilemma D5b)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every failure throws: a `false` from a store, a client or epoch lookup failure on a read, and a failed rotation; the pipeline behaviors decide through `CachingOptions.ThrowOnCacheErrors`** | Same as `RedisCacheProvider`, whose `GetAsync` and `SetAsync` do not catch client exceptions; errors are never swallowed (`AGENTS.md` §3) | A Memcached outage surfaces to behaviors on every read until they catch it |
| **B) A read failure returns a miss (`default`)** | A cache outage degrades to uncached | Swallows an error and hides an outage; differs from the other providers |
| **C) Throw on writes, miss on reads** | Reads never fail | The same hidden-outage risk as B, for reads only |

### Chosen Option: **A — every failure throws, with the operation name only** (recommended, pending the maintainer)

### Rationale

Enyim reports failures as a `false` result; the provider converts that to an exception so the pipeline behaviors decide, as with Redis. No `EncinaError.Message` reaches logs: only the operation name and the redacted key (`CacheKeyRedactor.Redact`) are logged, and an exception goes through `ForLogging()`. B and C win if the maintainer wants a cache outage to degrade silently. `GetOrSetAsync` lets an exception from the caller's factory propagate and stores nothing; `RefreshAsync` returns `true` when the entry exists and its window was extended, and `false` when the entry is absent or its absolute limit has passed, as `RedisCacheProvider.RefreshAsync` does.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Where the eight-provider contract suite lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An abstract `CacheProviderContractTests` in `Encina.TestInfrastructure`, one subclass per provider in `Encina.ContractTests/Caching`** | One set of assertions for all eight; message-broker contract tests already run containers from `Encina.ContractTests` | Needs a container fixture for each Redis-protocol provider and for Memcached: only `RedisFixture` exists on main (two copies, in `Encina.TestInfrastructure` and `Encina.IntegrationTests`), so four Redis-protocol fixtures and `MemcachedFixture` are new, and `Encina.ContractTests` references only `Encina.Caching` today, so it needs eight new project references |
| **B) Per-provider integration tests only** | No shared base | Eight copies of the same assertions drift apart; AC-027 asks for a contract that covers the 8 caches |
| **C) Contract tests for in-process providers only** | No Docker | Leaves Memcached and the Redis family unproven |

### Chosen Option: **A — a shared abstract suite with one subclass per provider**

### Rationale

Contract tests must instantiate real implementations (`AGENTS.md` §9). The suite covers get, set with TTL, remove, exists, `GetOrSetAsync`, sliding expiration, refresh and (per D2) pattern removal, plus keys that contain `:`; the module and tenant segment cases come from #746 in its last pull request. The suite and its subclasses are spread over three pull requests (Estimated File Count) so that each stays within 15 files.

**Class:** Trivial

**Settled by:** `AGENTS.md` §9 (contract tests instantiate real implementations; Docker for real infrastructure) and SPEC-000 AC-027.

</details>

<details>
<summary><strong>7. OpenTelemetry for the provider (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to #723 (`[FEATURE] Add OpenTelemetry instrumentation to all Caching providers`, open); the issue's acceptance criterion "OpenTelemetry instrumentation in place" is carried over to #723 explicitly (the pull request that closes #277 states it, and #723 must list Memcached)** | One shared set of names and tags for all eight providers; no provider-local `ActivitySource` to rename later | #277 closes with the criterion not met in its own code; users of the first release see no Memcached telemetry until #723 lands |
| **B) Instrument the provider here with provider-local instruments** | The criterion is met by #277 | Names and tags may diverge from what #723 defines for the other seven providers |

### Chosen Option: **A — defer to #723 with the criterion carried over** (recommended, pending the maintainer)

### Rationale

The provider keeps one logging call per operation in small methods so the shared instrumentation attaches the same way as for Redis. B wins if the maintainer wants #277 closed with its own criteria met. The deferral is recorded here because it has a consequence for users; if #723 lands first, the Memcached instrumentation is added in pull request 3 of this plan.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Resilience around cache calls (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No provider-specific policy: the client owns timeouts and node failure; a cache-wide resilience policy for all eight providers is a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/caching-providers-resilience-policy.md)** | Same as the other seven providers; one decision for all | Memcached calls have no circuit breaker until that issue lands |
| **B) A Polly retry and circuit breaker around the Memcached provider calls** | Resilience for this provider now | Only this provider would have it; inconsistent with the other seven |

### Chosen Option: **A — client-owned timeouts, a follow-up issue for a cache-wide policy** (recommended, pending the maintainer)

### Rationale

`AGENTS.md` §6 requires a Defer to reference an issue: the follow-up draft above is that reference, to be opened before this plan's first pull request merges. B wins if the maintainer wants resilience for the new provider ahead of a cross-provider design.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Key rules and the composed key length (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject a key whose **composed** form `{KeyPrefix}:{epoch}:{key}` is longer than 250 bytes (UTF-8), or that contains whitespace or control characters, with `ArgumentException` before reaching the client** | No silent change of the key; no hash collisions; the limit is checked on what Memcached actually receives, not on the logical key | `RedisCacheProvider` rejects nothing, so a key valid on seven providers can fail on this one |
| **B) Fold an over-long composed key into a fixed-length hash** | Every key works, like the other providers | A hash collision would serve another key's value; the redacted key in logs no longer matches the logical key |

### Chosen Option: **A — reject, on the composed key** (recommended, pending the maintainer)

### Rationale

A makes the Memcached limit explicit instead of hidden behind a hash; B wins if the maintainer wants key-length behavior identical across the eight providers. The metadata entry of sliding expiration uses `{composed key}:meta` (the shape `RedisCacheProvider` uses), so the check also covers the five bytes of that suffix.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Options defaults and the epoch value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DefaultExpiration` 5 minutes (as `RedisCacheOptions.DefaultExpiration`); `EpochRefreshWindow` 1 second (the D2 staleness window); epoch value = 16 lowercase hexadecimal characters from `RandomNumberGenerator` (8 random bytes); a value above the server item limit (1 MB by default) fails the store and so throws like any failed store (D5b option A), the limit being documented in the README** | Same default as the other providers; the random value is unpredictable enough that two rotations never collide in practice and short enough to leave room in the 250-byte key | The 1-second window is the staleness bound of D2 |
| **B) Other values (a `Guid` epoch, a different window or default expiration)** | None that changes behavior | Differs from the other providers or wastes key bytes without a benefit |

### Chosen Option: **A — the values above**

### Rationale

These are tunables and encodings that no caller observes except through the documented D2 window and the D5b failure behavior, and each is changeable without a public API change except the option defaults (documented in the options XML comments).

**Class:** Trivial

</details>

<details>
<summary><strong>11. Exposing the wire protocol in the options (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MemcachedCacheOptions.Protocol` (binary by default, as the issue's example uses), passed to the client; the exact type and default are confirmed against the client package when work starts** | Matches the issue's example; applications can choose the text protocol | A public option tied to the chosen client library (D1) |
| **B) No `Protocol` option; the provider uses the client's default** | Smaller public surface; the client can change without a public change | The issue's example would not compile |

### Chosen Option: **A — expose `Protocol`** (recommended, pending the maintainer)

### Rationale

The issue's example is the only statement of intent on record (D10 lists the other option defaults); B wins if the maintainer prefers to keep the client's types out of the public options.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package Foundation — project, options, validation (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Caching.Memcached/Encina.Caching.Memcached.csproj`: references `Encina.Caching` and `Encina.Messaging` (for `EncinaHealthCheck`, as `Encina.Caching.Redis` does), the Enyim package (version added to `Directory.Packages.props`), `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.GuardTests`, `Encina.PropertyTests`, `Encina.IntegrationTests` and `Encina.ContractTests` (so the internal `KeyEpoch` can be tested; the guard flag can reach internals only through this entry); `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`; `GlobalSuppressions.cs`.
2. `MemcachedCacheOptions` (public sealed): `Servers` (list of `host:port`), `DefaultExpiration` (5 minutes), `KeyPrefix`, `EpochRefreshWindow` (1 second), `Protocol` (D11), `ProviderHealthCheck` (`ProviderHealthCheckOptions`); defaults per D10. Any option that carries credentials (SASL user and password, if the client exposes them) is `[JsonIgnore]` and `ToString()` is overridden (`AGENTS.md` §3).
3. `MemcachedCacheOptionsValidator : IValidateOptions<MemcachedCacheOptions>`: at least one server, each parses as `host:port`, expiration greater than zero.
4. Add the project to `Encina.slnx`; add the package to `Directory.Packages.props`; add the project reference to `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (the test projects reference each cache provider explicitly).
5. The coverage manifest `.github/coverage-manifest/Encina.Caching.Memcached.json`, created here with the rows of the files of this pull request only (options and validator, Phase 6 table), in the same pull request as their unit and guard tests; each later pull request adds the rows of its own files with its own tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14. Caching providers are one package each under src/Encina.Caching.*. This phase creates the empty-but-building package and its options.
TASK: Create src/Encina.Caching.Memcached with the csproj (with the InternalsVisibleTo entries), MemcachedCacheOptions, MemcachedCacheOptionsValidator, GlobalSuppressions.cs and the two PublicAPI files; add it to Encina.slnx and the client package to Directory.Packages.props; add the project references to the unit and guard test projects, the unit and guard tests of the options and validator, and the manifest rows with their justifications.
KEY RULES: Zero warnings; XML docs on every public member; credentials in options are [JsonIgnore] with a ToString() override; TimeProvider by injection only; PublicAPI.Unshipped.txt lists every public symbol.
REFERENCE FILES: src/Encina.Caching.Redis/Encina.Caching.Redis.csproj, src/Encina.Caching.Redis/RedisCacheProvider.cs (RedisCacheOptions at the end), src/Encina.Caching.KeyDB/Encina.Caching.KeyDB.csproj, Directory.Packages.props.
```

</details>

### Phase 2: The Memcached cache provider (pull requests 2 and 3)

<details>
<summary>Tasks</summary>

Pull request 2 holds the log class, the EventId registration (Phase 5) and the internal `KeyEpoch` helper with its tests; pull request 3 holds the provider and its tests (the provider uses both, so neither depends on a later pull request).

1. `MemcachedCacheProvider` (public sealed partial class, `ICacheProvider`) in `src/Encina.Caching.Memcached/`. Constructor: the Enyim `IMemcachedClient`, `IOptions<MemcachedCacheOptions>`, `ILogger<MemcachedCacheProvider>`, `TimeProvider? timeProvider = null`.
2. `GetAsync<T>` / `SetAsync<T>`: JSON with camelCase options (D5); `SetAsync` uses `expiration ?? DefaultExpiration`; every failure throws (D5b): a `false` from the client throws `InvalidOperationException` with the operation name only (no key, no server text in the message); an exception from the client or the epoch lookup on a read propagates.
3. `RemoveAsync`, `ExistsAsync`, `GetOrSetAsync` (factory called on miss, store, return; an exception from the factory propagates and nothing is stored), `SetWithSlidingExpirationAsync` and `RefreshAsync` (a metadata entry at `{composed key}:meta` holding the sliding window and the absolute limit from `TimeProvider`, extended with the client's touch; returns `true` when the entry exists and was extended, `false` when it is absent or past its absolute limit; the same approach as `RedisCacheProvider.SlidingMetadata`).
4. Epoch handling (D2, in pull request 2): an internal `KeyEpoch` helper reads the epoch value of the prefix (key `{KeyPrefix}:epoch`), caches it in process for `EpochRefreshWindow` using `TimeProvider`, and creates a random value (D10) with `add` when absent (the loser of a race re-reads the winner's value). Every logical key is stored as `{KeyPrefix}:{epoch}:{key}`. `RemoveByPatternAsync(pattern)` validates the pattern, replaces the epoch with a new random value (`set`), **updates its own in-process epoch immediately**, and logs the count as unknown (Memcached cannot report it). Other instances see the rotation within `EpochRefreshWindow`. If the maintainer picks another option for D2, only this helper and `RemoveByPatternAsync` change.
5. Keys are redacted in every log call with `CacheKeyRedactor.Redact`; a composed key longer than 250 bytes (UTF-8) or containing whitespace or control characters is rejected with `ArgumentException` before reaching the client (D9 option A).

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

### Phase 3: Configuration, DI & health check (pull request 4)

<details>
<summary>Tasks</summary>

1. `ServiceCollectionExtensions.AddEncinaMemcachedCache(this IServiceCollection, Action<MemcachedCacheOptions>? configure = null)`: registers `TimeProvider.System` (TryAdd), the options and the validator with `ValidateOnStart`, the Enyim client from `Servers`, `ICacheProvider` as `TryAddSingleton<ICacheProvider, MemcachedCacheProvider>()`, and the health check when `ProviderHealthCheck.Enabled`. It never registers `IPubSubProvider` or `IDistributedLockProvider`.
2. `MemcachedHealthCheck : EncinaHealthCheck` with `DefaultName = "encina-memcached"` and tags `["encina", "cache", "memcached", "ready"]`, scoped resolution through `IServiceProvider`, a timeout from `ProviderHealthCheckOptions` and a sentinel round trip. Unhealthy results carry the exception type only, never its message.
3. DI test: build the provider with `ValidateOnBuild` and `ValidateScopes`, resolve `ICacheProvider`, and prove that a database or other `ICacheProvider` registered first is not overridden (`TryAdd`) in either registration order (`AGENTS.md` §3 registration completeness).
4. Fix the stale list in the XML remarks of `AddEncinaCaching` (`src/Encina.Caching/ServiceCollectionExtensions.cs`): it names `AddEncinaNCacheCache`, which does not exist, and does not name Memcached. The edit changes comments only; the existing `Encina.Caching.json` manifest entry of that file keeps its targets (never lowered) and is not otherwise edited.

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
3. OpenTelemetry: deferred to #723 ("Add OpenTelemetry instrumentation to all Caching providers", open) under D7, with the issue's acceptance criterion carried over to #723. The Memcached provider keeps its operations in small methods with one logging call each so the shared instrumentation of #723 attaches the same way as for Redis. When #723 lands first, the Memcached instrumentation is added in pull request 3; otherwise #723 absorbs it.
4. Resilience: deferred under D8. The client has its own timeouts and node failure handling; a Polly policy around cache calls is not what the other seven providers do. The deferral references the follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/caching-providers-resilience-policy.md), a cache-wide policy for all eight providers.
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

### Phase 5: Observability — logging and EventIds (pull request 2)

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

Per-flag targets for the new files. Each row is entered in `.github/coverage-manifest/Encina.Caching.Memcached.json` **in the same pull request as the tests that meet it** (the "PR" column), with the one-sentence justification below; measured values replace the proposals when the code exists, and targets never go down.

| PR | File | unit | guard | contract | property | integration |
|:--:|------|:---:|:---:|:---:|:---:|:---:|
| 1 | `MemcachedCacheOptions.cs` and validator | 100 | 40 | 0 | 0 | 0 |
| 2 | `KeyEpoch.cs` (inside the provider folder) | 95 | 0 | 0 | 0 | 0 |
| 3 | `MemcachedCacheProvider.cs` | 90 | 25 | 0 | 0 | 0 |
| 4 | `ServiceCollectionExtensions.cs` | 95 | 45 | 0 | 0 | 0 |
| 4 | `MemcachedHealthCheck.cs` | 90 | 30 | 0 | 0 | 0 |
| 7 | `MemcachedCacheProvider.cs` (flags added) | 90 | 25 | 60 | 0 | 70 |
| 7 | `KeyEpoch.cs` (flags added) | 95 | 0 | 0 | 0 | 60 |
| 7 | `MemcachedHealthCheck.cs` (flags added) | 90 | 30 | 0 | 0 | 60 |
| 8 | `MemcachedCacheProvider.cs` and `KeyEpoch.cs` (property added) | unchanged | unchanged | unchanged | 40 | unchanged |

| Row | Justification |
|-----|---------------|
| Options and validator | Pure data and rules: every branch is reachable from unit tests and the null and format checks from guard tests. |
| `KeyEpoch.cs` unit 95, guard 0 | The window, creation with `add`, race loss and rotation are driven with `FakeTimeProvider` and a substituted client; the type is internal and has no argument checks of its own (the provider that calls it validates the arguments), so guard tests have nothing to reach there. |
| `MemcachedCacheProvider.cs` unit 90, guard 25 | Every operation, failure and key rule is unit-tested against a substituted client; guard tests reach only the argument checks of each public method and the constructor. |
| `ServiceCollectionExtensions.cs`, `MemcachedHealthCheck.cs` | Registration and the health round trip are fully reachable with a substituted client; guard covers the null arguments. |
| Contract 60, integration 70 or 60 | The contract suite and the real-server tests execute the provider's main paths against a container; error and race branches stay in unit tests. |
| Property 40 | The property tests drive the set/get and rotation paths for generated values; branches keyed by failures are unit-only. |

Contract is 0 for the files outside the provider because the contract suite exercises the provider as a whole. A `.md` justification is never written for contract tests (`AGENTS.md` §9): the suite below is the contract evidence.

1. **Unit** (`tests/Encina.UnitTests/Caching/Memcached/`): provider against a substituted `IMemcachedClient` (NSubstitute): hit, miss, set with default and explicit TTL, failure throws, oversized or invalid composed key rejected, epoch window with `FakeTimeProvider`, `add` race, pattern removal rotates the epoch and updates the local epoch, sliding expiration and refresh; options validator; DI.
2. **Guard** (`tests/Encina.GuardTests/Caching/Memcached/`): every public method and constructor parameter; a nullable `TimeProvider?` gets a valid-result test, not an exception test (#3798).
3. **Contract** (`tests/Encina.ContractTests/Caching/`): abstract `CacheProviderContractTests` in `Encina.TestInfrastructure` plus eight subclasses (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, Memcached). Fixtures: the generic `Testcontainers` package (4.15.0) for Valkey, Dragonfly, Garnet, KeyDB and Memcached, because `Directory.Packages.props` has `Testcontainers.Redis` (used by `RedisFixture`) but no `Testcontainers.Memcached`; four Redis-protocol fixtures and `MemcachedFixture` are new files. `Encina.ContractTests` gets eight project references (it references only `Encina.Caching` today). Spread over pull requests 5, 6 and 7.
4. **Property** (`tests/Encina.PropertyTests/Infrastructure/Caching/`): set then get returns the stored value for any serializable value and key; TTL expiry with a fake clock; epoch rotation never leaves a pre-rotation entry readable (pull request 8).
5. **Integration** (`tests/Encina.IntegrationTests/Infrastructure/Caching/`): `MemcachedFixture` (shared with the contract tests, in `Encina.TestInfrastructure`) and `MemcachedCollection` (in the integration project's fixtures folder) following `RedisFixture`/`RedisCollection`, `[Trait("Category", "Integration")]`, `[Trait("Database", "Memcached")]`; provider, registration and health check against the real server, including the 1 MB value limit (pull request 7).
6. **Load**: `.md` justification (`tests/Encina.LoadTests/...`): the provider has no concurrency logic of its own beyond the client (pull request 8).
7. **Benchmark**: a `tests/Encina.BenchmarkTests/Encina.Caching.Memcached.Benchmarks` project following `Encina.Caching.Redis.Benchmarks` (csproj, `Program.cs` with `BenchmarkSwitcher`, the benchmark class) and its `.github/perf-manifest/Encina.Caching.Memcached.Benchmarks.json` (the Redis benchmarks have `Encina.Caching.Redis.Benchmarks.json`); the comparison table of the eight providers is documented from its output under `artifacts/performance/` (pull request 8).

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

### Phase 7: Documentation & Finalization (pull request 9)

<details>
<summary>Tasks</summary>

1. XML documentation on all public APIs with examples (`AddEncinaMemcachedCache`).
2. `src/Encina.Caching.Memcached/README.md` (options, limits: no pub/sub, no locks, 1 MB values, 250-byte composed keys, the D2 behavior including the cross-instance staleness window).
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

Every pull request carries its tests, coverage-manifest rows (in the same pull request as the tests that meet them), PublicAPI lines and any test-project reference it needs; no pull request uses a type, project or fixture created by a later one.

| PR | Contents | Files | Lines |
|:--:|----------|------:|------:|
| 1 | Package foundation: csproj, `GlobalSuppressions.cs`, 2 PublicAPI files, options, validator (6); `Encina.slnx`, `Directory.Packages.props` (2); manifest (1); `Encina.UnitTests.csproj` and `Encina.GuardTests.csproj` project references (2); unit and guard tests of options and validator (2) | **13** | about 600 |
| 2 | EventIds and epoch: `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs` (3); `MemcachedLogMessages.cs`, `KeyEpoch.cs` (2); Memcached manifest and the `Encina.json` row of `EventIdRanges.cs` if absent (2); `KeyEpochTests.cs` (1) | **8** | about 700 |
| 3 | Provider: `MemcachedCacheProvider.cs`, PublicAPI, manifest, unit tests, guard tests | **5** | about 1,300 |
| 4 | DI and health: `ServiceCollectionExtensions.cs`, `MemcachedHealthCheck.cs`, PublicAPI, manifest, the `AddEncinaCaching` remarks fix in `Encina.Caching` (5); unit tests of DI and health (2); guard test (1) | **8** | about 800 |
| 5 | Contract suite, in-process and Redis: abstract `CacheProviderContractTests` in `Encina.TestInfrastructure` (1); Memory, Hybrid and Redis subclasses (3); `Encina.ContractTests.csproj` references (1) | **5** | about 700 |
| 6 | Redis-protocol fixtures: `ValkeyFixture`, `DragonflyFixture`, `GarnetFixture`, `KeyDBFixture` (4); their collection definitions (1); four subclasses (4); `Encina.ContractTests.csproj` references (1) | **10** | about 700 |
| 7 | Memcached integration and contract: `MemcachedFixture` (1), `MemcachedCollection` (1), integration tests of provider and of registration with health check (2), Memcached contract subclass (1), `Encina.ContractTests.csproj` and `Encina.IntegrationTests.csproj` references (2), manifest rows for the contract and integration flags (1) | **8** | about 800 |
| 8 | Property, benchmark, load: property test and `Encina.PropertyTests.csproj` reference (2); benchmark csproj, `Program.cs`, benchmark class (3); `.github/perf-manifest/Encina.Caching.Memcached.Benchmarks.json` (1); `Encina.slnx` (1); load `.md` justification (1); manifest property rows (1) | **9** | about 700 |
| 9 | Documentation: README, guide, comparison, changelog fragment, ADR, `docs/INVENTORY.md`, `ROADMAP.md` | **7** | about 700 |
| **Total** | Nine pull requests | **73 file touches, 63 distinct files** (the Memcached manifest is touched in 6 pull requests, the Memcached `PublicAPI.Unshipped.txt` in 3, `Encina.ContractTests.csproj` in 3, `Encina.slnx` in 2) | about 7,000 |

Pull request 3 is the largest at about 1,300 lines; if the unit tests of the provider grow beyond the 1,500-line cap they split by operation group (reads and writes, sliding expiration and epoch), which adds one file and no new dependency. Existing test callers of a changed signature: none (the only edit to an existing production file is a comment edit in `AddEncinaCaching` and the registration of the EventId range).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Caching has eight provider packages (AGENTS.md §5); seven exist, and this work adds Encina.Caching.Memcached. No backward compatibility, no [Obsolete], zero warnings, XML docs on all public APIs.

IMPLEMENTATION OVERVIEW: Phase 1 package and options; Phase 2 MemcachedCacheProvider and the epoch helper for pattern removal; Phase 3 AddEncinaMemcachedCache and the health check; Phase 4 cross-cutting evaluation (OpenTelemetry deferred to #723 under D7, resilience under D8); Phase 5 EventIds 6950-6979 and the log class; Phase 6 unit, guard, contract (eight providers), property, integration, benchmark tests; Phase 7 docs. Ship as nine pull requests, each with its tests and manifest rows: (1) package, options, validator, test references; (2) EventIds, log class, KeyEpoch; (3) provider; (4) DI and health check; (5) contract suite with the in-process and Redis subclasses; (6) Valkey, Dragonfly, Garnet, KeyDB fixtures and subclasses; (7) Memcached fixture, integration tests and contract subclass; (8) property tests, benchmark project, load justification; (9) documentation.

KEY PATTERNS: TryAddSingleton for ICacheProvider; IValidateOptions with ValidateOnStart; credentials [JsonIgnore] plus ToString override; failures throw, never return false; no EncinaError.Message or raw key in logs; TimeProvider for every clock read; shared [Collection] fixtures for integration tests; Shouldly assertions; CRAP at or below 10 on changed methods.

REFERENCE FILES: src/Encina.Caching/Abstractions/ICacheProvider.cs, src/Encina.Caching.Redis/RedisCacheProvider.cs, src/Encina.Caching.Redis/ServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs, .github/coverage-manifest/Encina.Caching.Redis.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | This is the caching implementation itself. |
| 2 | OpenTelemetry | ⏭️ | Defer to #723 (instrumentation for all caching providers, open) under D7; the issue's criterion "OpenTelemetry instrumentation in place" is carried over to #723; the provider is shaped so it attaches the same way as for Redis. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` class with EventIds 6950-6958 in the registered range 6950-6979. |
| 4 | Health Checks | ✅ | `MemcachedHealthCheck` in the shape of `RedisHealthCheck`; #754 does not list Memcached. |
| 5 | Validation | ✅ | `IValidateOptions<MemcachedCacheOptions>` with `ValidateOnStart`; no request-level validation. |
| 6 | Resilience | ⏭️ | Defer under D8: the client owns timeouts and node failure, in line with the other seven providers; a cache-wide policy is the follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/caching-providers-resilience-policy.md). |
| 7 | Distributed Locks | ❌ | Memcached is not one of the seven lock providers (SPEC-000 DEC-009). |
| 8 | Transactions | ❌ | Cache operations are independent of database transactions. |
| 9 | Idempotency | ❌ | A cache set is idempotent by nature. |
| 10 | Multi-Tenancy | ❌ | The provider stores opaque keys; tenant scoping is done by the key generators (#746). |
| 11 | Module Isolation | ❌ | Module scoping belongs to key generation (#746). |
| 12 | Audit Trail | ❌ | Infrastructure, not an auditable business operation. |

---

## Prerequisites & Dependencies

- **Not blocking**: #723 (OpenTelemetry for caching) and #754 (health checks for the remaining providers) are open; the plan works with or without them as described in Phase 4.
- **Shared with #746**: this plan owns the `CacheProviderContractTests` suite (pull request 5); #746 adds the module-scoped key cases to it in its own last pull request, which opens only after pull request 5 is on main.
- **Follow-up issues to be opened** (drafts in `artifacts/decisions/plan-batch/followups/`):
  - `CachingOptions.SerializerOptions` is copied by `AddEncinaCaching` but read by no provider, and the serialization abstraction `AGENTS.md` §5 names does not exist: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/caching-serializer-abstraction.md);
  - a cache-wide resilience policy for all eight providers: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/caching-providers-resilience-policy.md);
  - `DefaultCacheKeyGenerator.GeneratePattern` emitting `t:*` for a context without tenant while keys carry no tenant segment is recorded by the #746 plan (follow-up issue to be opened, draft: artifacts/decisions/plan-batch/followups/cache-invalidation-pattern-tenantless-mismatch.md); it is not repeated here.
- **Package version**: the Enyim package version is added to `Directory.Packages.props` at implementation time, with the NuGet data re-checked (D1).

---

## Next Steps

1. Ask the maintainer D1 (client library), D2 (pattern removal), D5, D5b, D7, D8, D9 and D11 when work on #277 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1 (13 files): package, options, validator, test references, manifest rows.
3. Pull request 2 (8 files): EventId range, log class, `KeyEpoch` and its tests.
4. Pull request 3 (5 files): `MemcachedCacheProvider`, its unit and guard tests.
5. Pull request 4 (8 files): `AddEncinaMemcachedCache`, health check, unit and guard tests, `AddEncinaCaching` remarks fix.
6. Pull request 5 (5 files): abstract contract suite with the Memory, Hybrid and Redis subclasses.
7. Pull request 6 (10 files): the Valkey, Dragonfly, Garnet and KeyDB fixtures, collections and subclasses.
8. Pull request 7 (8 files): `MemcachedFixture`, integration tests, the Memcached contract subclass, manifest rows for contract and integration.
9. Pull request 8 (9 files): property tests, benchmark project and perf manifest, load-test justification.
10. Pull request 9 (7 files): README, guide, comparison table, ADR, changelog fragment, inventory and roadmap.
