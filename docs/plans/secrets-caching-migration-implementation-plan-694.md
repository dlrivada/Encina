# Implementation Plan: Migrate Secrets Management from `IMemoryCache` to `ICacheProvider`

> **Issue**: [#694](https://github.com/dlrivada/Encina/issues/694)
> **Type**: Feature
> **Complexity**: Medium (5 phases, ~22 files)
> **Estimated Scope**: ~700-1,000 lines of production code + ~600-800 lines of tests
> **Milestone**: v0.14.0 (Cross-cutting cache integration EPIC #712)

---

## Summary

Replace `CachedSecretReaderDecorator` (which uses `IMemoryCache`) with a new `CachingSecretReaderDecorator` that uses Encina's `ICacheProvider` abstraction. The old decorator is **deleted entirely** — no fallback, no dual code paths. This enables cross-instance cache consistency in multi-instance deployments. Additionally, add a `SecretCachePubSubHostedService` for cross-instance invalidation via `IPubSubProvider`, a `CachingSecretWriterDecorator` for write-through invalidation, and cache `ListSecretsAsync()` which was previously uncached.

**Affected packages:**

- `Encina.Security.Secrets` — decorator replacement, new hosted service, options changes
- `Encina.Caching` — dependency (no code changes)

**Provider category**: This feature is provider-independent. It consumes `ICacheProvider` / `IPubSubProvider` which are already implemented by all 8 caching providers. No new provider implementations needed.

**Reference implementation**: ABAC's `CachingPolicyStoreDecorator` + `PolicyCachePubSubHostedService` in `Encina.Security.ABAC` demonstrates the exact pattern to follow.

---

## Design Choices

<details>
<summary><strong>1. Migration Strategy — Replace IMemoryCache entirely</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replace IMemoryCache entirely** | Clean, no dual code paths, single implementation to maintain | `ICacheProvider` becomes required dependency when caching is enabled |
| **B) New decorator alongside old one** | Zero breaking change | Code duplication, confusing 2 decorators |
| **C) Opt-in upgrade with fallback** | Backward compatible, clean migration | Dual code paths, more DI logic, defers the inevitable |

### Chosen Option: **A — Replace IMemoryCache entirely**

### Rationale

- Pre-1.0: "Choose the best solution, not the compatible one" — no backward compatibility needed
- `CachedSecretReaderDecorator` is **deleted**; `CachingSecretReaderDecorator` takes its place
- `Encina.Caching.Memory` (MemoryCacheProvider) wraps `IMemoryCache` anyway — users who want in-memory caching just register `AddEncinaMemoryCache()` and get the same behavior through `ICacheProvider`
- No dual code paths, no conditional DI logic, no confusion between two similar decorators
- `Encina.Caching` becomes a hard dependency of `Encina.Security.Secrets` — acceptable since caching is a core Encina abstraction
- When `EnableCaching = true` (the existing flag), `ICacheProvider` must be registered in DI

</details>

<details>
<summary><strong>2. Decorator Naming — CachingSecretReaderDecorator (replacing CachedSecretReaderDecorator)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep same name `CachedSecretReaderDecorator`** | No rename, simple | Confusing in git history, constructor change is breaking |
| **B) New name: `CachingSecretReaderDecorator`** | ABAC precedent, clear intent, clean break | Different name than the old class |
| **C) Generic name: `SecretCacheDecorator`** | Short | Doesn't follow established `*Decorator` pattern |

### Chosen Option: **B — `CachingSecretReaderDecorator`**

### Rationale

- Follows the ABAC naming pattern: `CachingPolicyStoreDecorator` uses "Caching" prefix
- Old `CachedSecretReaderDecorator.cs` is deleted; new `CachingSecretReaderDecorator.cs` replaces it
- Clean break — different name makes it obvious this is a replacement, not an edit
- Git history clearly shows: delete old file + add new file

</details>

<details>
<summary><strong>3. Cache Key Strategy — Hierarchical with prefix</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Flat keys: `encina:secrets:{name}`** | Simple | No pattern invalidation for typed vs untyped |
| **B) Hierarchical: `encina:secrets:v:{name}`, `encina:secrets:t:{name}:{type}`** | Supports pattern-based invalidation, separate TTL | Slightly more complex |
| **C) Hash-based keys** | Collision-resistant | Opaque, hard to debug |

### Chosen Option: **B — Hierarchical cache keys**

### Rationale

- Matches the issue specification: `encina:secrets:{key}`, `encina:secrets:v:{key}`, `encina:secrets:exists:{key}`, `encina:secrets:list`
- Enables `RemoveByPatternAsync("encina:secrets:*")` for bulk invalidation from pub/sub
- Per-secret invalidation: `RemoveAsync("encina:secrets:v:{key}")` + `RemoveByPatternAsync("encina:secrets:t:{key}:*")`
- Typed secrets get a separate key including the type name to avoid deserialization conflicts
- Last-known-good values stored with separate prefix `encina:secrets:lkg:{key}` for stale fallback

</details>

<details>
<summary><strong>4. PubSub Invalidation — Optional HostedService with typed messages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inline invalidation in decorator** | Simpler, no hosted service | No subscription-side handling, only publish |
| **B) HostedService with typed messages** | Full publish+subscribe lifecycle, ABAC precedent | Extra class, background service |
| **C) Polling-based invalidation** | No pub/sub dependency | Latency, wasteful |

### Chosen Option: **B — HostedService with typed messages**

### Rationale

- Exact match with ABAC's `PolicyCachePubSubHostedService` pattern
- Decorator publishes invalidation messages on writes
- `SecretCachePubSubHostedService` subscribes on startup and evicts local cache via `RemoveByPatternAsync`
- `IPubSubProvider` is optional in the decorator (null-checked), required only when the hosted service is registered
- Graceful degradation: pub/sub failures are logged, never crash the app

</details>

<details>
<summary><strong>5. Writer Integration — Cache invalidation on SetSecretAsync</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New CachingSecretWriterDecorator** | Clean separation, dedicated class | Extra decorator class |
| **B) Invalidation hook in reader decorator** | Fewer classes | Reader shouldn't know about writes |
| **C) Event-based invalidation** | Decoupled | Over-engineered for this scope |

### Chosen Option: **A — New `CachingSecretWriterDecorator`**

### Rationale

- Follows ISP: `ISecretWriter` has its own decorator for write-through invalidation
- On `SetSecretAsync`: persist to inner writer, then remove cache keys + publish invalidation
- Simple, focused class (~60 lines)
- Registered when `EnableCaching = true` and `ISecretWriter` is available

</details>

---

## Implementation Phases

### Phase 1: Core Models & Options

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `SecretCachingOptions.cs`** in `src/Encina.Security.Secrets/Caching/`
   - Namespace: `Encina.Security.Secrets.Caching`
   - Properties: `EnablePubSubInvalidation` (bool, default true), `InvalidationChannel` (string, default `"encina:secrets:cache:invalidate"`), `CacheKeyPrefix` (string, default `"encina:secrets"`), `CacheTag` (string, default `"secrets"`)
   - Sealed class
   - Note: `Enabled` and `Duration` are NOT needed here — they already exist as `SecretsOptions.EnableCaching` and `SecretsOptions.DefaultCacheDuration`

2. **Create `SecretCacheInvalidationMessage.cs`** in `src/Encina.Security.Secrets/Caching/`
   - Namespace: `Encina.Security.Secrets.Caching`
   - `public sealed record SecretCacheInvalidationMessage(string SecretName, string Operation, DateTime TimestampUtc)`
   - Operations: `"Set"`, `"Remove"`, `"Rotate"`, `"BulkInvalidate"`

3. **Update `SecretsOptions.cs`**
   - Add property: `public SecretCachingOptions Caching { get; set; } = new();`
   - Existing `EnableCaching` and `DefaultCacheDuration` remain as-is (no breaking change in config shape)

4. **Update `PublicAPI.Unshipped.txt`** for all new public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
You are working on the Encina .NET 10 library. Issue #694 replaces IMemoryCache with
ICacheProvider in secrets caching. This phase creates the configuration models.

REFERENCE FILES:
- src/Encina.Security.ABAC/PolicyCachingOptions.cs (pattern to follow)
- src/Encina.Security.ABAC/Persistence/PolicyCacheInvalidationMessage.cs (pattern to follow)
- src/Encina.Security.Secrets/SecretsOptions.cs (file to modify)
- src/Encina.Security.Secrets/Caching/ (directory for new files)

TASK:
1. Create SecretCachingOptions.cs in src/Encina.Security.Secrets/Caching/ following the
   PolicyCachingOptions pattern:
   - sealed class, namespace Encina.Security.Secrets.Caching
   - Properties: EnablePubSubInvalidation (bool, default true),
     InvalidationChannel (string, default "encina:secrets:cache:invalidate"),
     CacheKeyPrefix (string, default "encina:secrets"),
     CacheTag (string, default "secrets")
   - Do NOT add Enabled or Duration — those exist in SecretsOptions already
   - Add XML doc comments on all public members

2. Create SecretCacheInvalidationMessage.cs in same directory:
   - sealed record with: SecretName (string), Operation (string), TimestampUtc (DateTime)
   - XML doc comments

3. Update SecretsOptions.cs:
   - Add: public SecretCachingOptions Caching { get; set; } = new();
   - Keep existing EnableCaching and DefaultCacheDuration properties unchanged
   - Add XML doc comments

4. Update PublicAPI.Unshipped.txt with all new public symbols

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- No [Obsolete] attributes
- XML documentation on all public APIs
- Follow the exact pattern from ABAC's PolicyCachingOptions
```

</details>

---

### Phase 2: CachingSecretReaderDecorator & CachingSecretWriterDecorator

<details>
<summary><strong>Tasks</strong></summary>

1. **Delete `CachedSecretReaderDecorator.cs`** from `src/Encina.Security.Secrets/Caching/`
   - This is the old IMemoryCache-based decorator — fully replaced

2. **Create `CachingSecretReaderDecorator.cs`** in `src/Encina.Security.Secrets/Caching/`
   - Namespace: `Encina.Security.Secrets.Caching`
   - Implements `ISecretReader`
   - Constructor: `(ISecretReader inner, ICacheProvider cache, SecretCachingOptions cachingOptions, SecretsOptions secretsOptions, ILogger<CachingSecretReaderDecorator> logger, SecretsMetrics? metrics = null)`
   - **Methods**:
     - `GetSecretAsync(string, CancellationToken)` — cache-aside with `ICacheProvider.GetOrSetAsync<string>()`, only cache `Right` results
     - `GetSecretAsync<T>(string, CancellationToken)` — cache-aside with type-discriminated key `{prefix}:t:{name}:{typeof(T).FullName}`
     - `InvalidateAsync(string secretName, CancellationToken)` — remove all cache variants for a secret (async replacement of old sync API)
   - **Cache keys**: `{prefix}:v:{name}` for string, `{prefix}:t:{name}:{typeName}` for typed
   - **Last-known-good**: Store `Right` results in `{prefix}:lkg:{name}` with extended TTL when resilience is enabled
   - **Stale fallback**: On cache miss + provider failure (resilience error), serve LKG value
   - **Graceful degradation**: All cache operations in try/catch; cache failure falls through to inner provider
   - **Per-secret TTL**: Respect `SecretReference.CacheDuration` override via `SecretsOptions.DefaultCacheDuration`

3. **Create `CachingSecretWriterDecorator.cs`** in `src/Encina.Security.Secrets/Caching/`
   - Namespace: `Encina.Security.Secrets.Caching`
   - Implements `ISecretWriter`
   - Constructor: `(ISecretWriter inner, ICacheProvider cache, IPubSubProvider? pubSub, SecretCachingOptions options, ILogger<CachingSecretWriterDecorator> logger)`
   - **Method** `SetSecretAsync(string, string, CancellationToken)`:
     1. Persist via `_inner.SetSecretAsync()`
     2. On success: `_cache.RemoveByPatternAsync($"{prefix}:*:{key}")` to clear all cached variants
     3. On success + pubSub != null: `_pubSub.PublishAsync(channel, new SecretCacheInvalidationMessage(key, "Set", DateTime.UtcNow))`
     4. Return inner result

4. **Update `PublicAPI.Unshipped.txt`** — remove old decorator, add new ones

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
You are working on Encina .NET 10 library, issue #694. This phase REPLACES the old
IMemoryCache-based CachedSecretReaderDecorator with a new ICacheProvider-based one.
The old file is DELETED entirely — no fallback.

REFERENCE FILES:
- src/Encina.Security.ABAC/Persistence/CachingPolicyStoreDecorator.cs (THE pattern to follow)
- src/Encina.Security.Secrets/Caching/CachedSecretReaderDecorator.cs (OLD — read for behavior, then DELETE)
- src/Encina.Security.Secrets/Caching/SecretCachingOptions.cs (created in Phase 1)
- src/Encina.Security.Secrets/Abstractions/ISecretReader.cs
- src/Encina.Security.Secrets/Abstractions/ISecretWriter.cs
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.Caching/Abstractions/IPubSubProvider.cs

TASK:
1. DELETE CachedSecretReaderDecorator.cs (the old IMemoryCache version)

2. Create CachingSecretReaderDecorator.cs implementing ISecretReader:
   - Cache-aside pattern using ICacheProvider.GetOrSetAsync<T>()
   - Only cache Right results (Either<EncinaError, T>), never cache Left (errors)
   - Cache keys: "{prefix}:v:{name}" for string secrets, "{prefix}:t:{name}:{typeName}" for typed
   - Last-known-good support with extended TTL: "{prefix}:lkg:{name}"
   - Stale fallback when inner provider fails with resilience errors
   - All cache operations wrapped in try/catch — cache failures fall through to inner
   - Per-secret TTL override support
   - IPubSubProvider is optional (null-checked)
   - Provide InvalidateAsync(string, CancellationToken) public method (async replacement of old sync API)

3. Create CachingSecretWriterDecorator.cs implementing ISecretWriter:
   - Write-through: persist first via inner, then invalidate cache + broadcast
   - On success: RemoveByPatternAsync for all variants of the key
   - On success + pubSub available: publish SecretCacheInvalidationMessage
   - Return inner result unchanged

KEY RULES:
- ROP: Either<EncinaError, T> on all methods
- Follow ABAC CachingPolicyStoreDecorator exactly for error handling and cache patterns
- Graceful degradation: cache/pubsub failures NEVER propagate
- XML doc comments on all public members
- sealed classes
```

</details>

---

### Phase 3: PubSub HostedService & DI Registration

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `SecretCachePubSubHostedService.cs`** in `src/Encina.Security.Secrets/Caching/`
   - Namespace: `Encina.Security.Secrets.Caching`
   - Implements `IHostedService`
   - Constructor: `(ICacheProvider cache, IPubSubProvider pubSub, SecretCachingOptions options, ILogger<SecretCachePubSubHostedService> logger)`
   - `StartAsync`: Subscribe to `options.InvalidationChannel` with typed handler `SubscribeAsync<SecretCacheInvalidationMessage>()`
   - Handler: call `_cache.RemoveByPatternAsync($"{options.CacheKeyPrefix}:*")` for bulk, or targeted removal for specific secrets
   - `StopAsync`: Dispose subscription
   - Graceful failures: errors logged at Warning, don't crash app startup

2. **Update `ServiceCollectionExtensions.cs`** in `src/Encina.Security.Secrets/`
   - In `WrapWithDecorators()` or equivalent factory:
     - When `EnableCaching = true`: wrap reader with `CachingSecretReaderDecorator` (requires `ICacheProvider` via `GetRequiredService<ICacheProvider>()`)
     - When `EnableCaching = true` AND `ISecretWriter` is registered: wrap writer with `CachingSecretWriterDecorator`
   - Remove all references to old `CachedSecretReaderDecorator` and `IMemoryCache`
   - When `Caching.EnablePubSubInvalidation = true` AND `IPubSubProvider` is resolvable:
     - Register `SecretCachePubSubHostedService` as hosted service
   - Registration order for reader chain: inner → resilience → caching → auditing (unchanged)
   - Registration order for writer chain: inner → resilience → caching (new)

3. **Update package dependency** in `Encina.Security.Secrets.csproj`:
   - Add `<ProjectReference Include="..\..\Encina.Caching\Encina.Caching.csproj" />` (hard dependency)
   - Remove `Microsoft.Extensions.Caching.Memory` package reference if no longer needed

4. **Update `PublicAPI.Unshipped.txt`**

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
You are working on Encina .NET 10 library, issue #694. This phase creates the PubSub
hosted service and updates DI registration. IMemoryCache is being FULLY REPLACED —
remove all references to it and the old CachedSecretReaderDecorator.

REFERENCE FILES:
- src/Encina.Security.ABAC/Persistence/PolicyCachePubSubHostedService.cs (THE pattern)
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs (lines 178-219 for registration)
- src/Encina.Security.Secrets/ServiceCollectionExtensions.cs (file to modify)
- src/Encina.Security.Secrets/Caching/CachingSecretReaderDecorator.cs (Phase 2)
- src/Encina.Security.Secrets/Caching/CachingSecretWriterDecorator.cs (Phase 2)

TASK:
1. Create SecretCachePubSubHostedService.cs:
   - Implements IHostedService
   - StartAsync: subscribe to InvalidationChannel with SubscribeAsync<SecretCacheInvalidationMessage>()
   - On message: RemoveByPatternAsync("{prefix}:*") for bulk invalidation
   - StopAsync: dispose subscription (IAsyncDisposable)
   - All errors caught and logged at Warning level

2. Update ServiceCollectionExtensions.cs:
   - REPLACE old CachedSecretReaderDecorator with CachingSecretReaderDecorator
   - Remove all IMemoryCache references
   - When EnableCaching = true: use GetRequiredService<ICacheProvider>() (hard requirement)
   - Add CachingSecretWriterDecorator for ISecretWriter
   - Register SecretCachePubSubHostedService when EnablePubSubInvalidation is true
     AND IPubSubProvider is available (optional dependency)
   - Decorator chain: inner → resilience → caching → auditing

3. Update Encina.Security.Secrets.csproj:
   - Add ProjectReference to Encina.Caching (hard dependency)
   - Remove Microsoft.Extensions.Caching.Memory if no longer needed

KEY RULES:
- ICacheProvider is now a REQUIRED dependency when EnableCaching = true
- IPubSubProvider remains optional — resolve via GetService<>()
- Follow ABAC ServiceCollectionExtensions pattern exactly
- TryAdd pattern for all registrations
- Remove ALL traces of IMemoryCache and CachedSecretReaderDecorator
```

</details>

---

### Phase 4: Observability & Structured Logging

<details>
<summary><strong>Tasks</strong></summary>

1. **Register EventId range** in `src/Encina/Diagnostics/EventIdRanges.cs`:
   - Add: `public static readonly (int Min, int Max) SecuritySecrets = (8950, 8999);`
   - This uses the gap between CompliancePrivacyByDesign (8900-8949) and SecurityABAC (9000-9099)
   - 50 slots: sufficient for secrets caching EventIds

2. **Update `Log.cs`** in `src/Encina.Security.Secrets/`:
   - Replace existing cache-related log messages (EventIds 20-22) with new ones using the registered range:
     - EventId 8950: `CacheHit` — "Cache hit for secret '{SecretName}'"
     - EventId 8951: `CacheMiss` — "Cache miss for secret '{SecretName}'"
     - EventId 8952: `CacheSet` — "Secret '{SecretName}' stored in cache"
     - EventId 8953: `CacheInvalidated` — "Secret '{SecretName}' invalidated in cache"
     - EventId 8954: `CacheError` — "Cache operation failed for secret '{SecretName}'"
     - EventId 8955: `PubSubInvalidationPublished` — "Cache invalidation published for secret '{SecretName}'"
     - EventId 8956: `PubSubInvalidationReceived` — "Cache invalidation received for secret '{SecretName}'"
     - EventId 8957: `PubSubSubscriptionStarted` — "PubSub subscription started on channel '{Channel}'"
     - EventId 8958: `PubSubSubscriptionFailed` — "PubSub subscription failed on channel '{Channel}'"
     - EventId 8959: `StaleFallbackServed` — "Serving stale (last-known-good) value for secret '{SecretName}'"
     - EventId 8960: `CacheBulkInvalidated` — "Bulk cache invalidation for prefix '{Prefix}'"
     - EventId 8961: `WriterCacheInvalidation` — "Writer invalidated cache for secret '{SecretName}'"
   - Use `[LoggerMessage]` source generator

3. **Add OpenTelemetry activities** to `CachingSecretReaderDecorator`:
   - ActivitySource: `"Encina.Security.Secrets.Caching"` (follow existing pattern)
   - Activities: `GetSecret.CacheHit`, `GetSecret.CacheMiss`, `GetSecret.StaleFallback`
   - Tags: `secret.name`, `cache.hit`, `cache.type`

4. **Add Meter counters**:
   - Meter: `"Encina.Security.Secrets.Caching"`
   - `encina.secrets.cache.hits` (Counter<long>, tag: secret_name)
   - `encina.secrets.cache.misses` (Counter<long>, tag: secret_name)
   - `encina.secrets.cache.invalidations` (Counter<long>, tag: operation)
   - `encina.secrets.cache.stale_fallbacks` (Counter<long>)
   - `encina.secrets.cache.errors` (Counter<long>)

5. **Update `PublicAPI.Unshipped.txt`**

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
You are working on Encina .NET 10 library, issue #694. This phase adds observability
(structured logging, OpenTelemetry, metrics) to the new ICacheProvider-based decorators.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs (register new range)
- src/Encina.Security.Secrets/Log.cs (replace old cache log messages with new ones)
- src/Encina.Security.Secrets/Caching/CachingSecretReaderDecorator.cs (add activities/metrics)
- src/Encina.Security.Secrets/Caching/CachingSecretWriterDecorator.cs (add activities/metrics)
- src/Encina.Security.Secrets/Caching/SecretCachePubSubHostedService.cs (add logging)

TASK:
1. Register EventId range in EventIdRanges.cs:
   - Add: SecuritySecrets = (8950, 8999) between CompliancePrivacyByDesign and SecurityABAC
   - Add XML doc comment

2. Replace old cache LoggerMessage methods (EventIds 20-22) with new ones:
   - EventIds 8950-8961 as specified in the plan
   - Use [LoggerMessage] source generator
   - Update all references in decorators and hosted service

3. Add ActivitySource "Encina.Security.Secrets.Caching" to reader decorator:
   - Start activities for cache operations
   - Add semantic tags: secret.name, cache.hit, cache.type

4. Add Meter "Encina.Security.Secrets.Caching":
   - Counter<long> instruments for hits, misses, invalidations, stale_fallbacks, errors

KEY RULES:
- EventIds MUST be within registered range (8950-8999)
- Pack EventIds sequentially — no sparse allocations
- Use [LoggerMessage] source generator, not LoggerMessage.Define
- XML doc comments referencing the range
```

</details>

---

### Phase 5: Testing & Documentation

<details>
<summary><strong>Tasks</strong></summary>

**Unit Tests** (`tests/Encina.UnitTests/Security/Secrets/Caching/`):

1. `CachingSecretReaderDecoratorTests.cs`:
   - `GetSecretAsync_CacheHit_ReturnsFromCache_DoesNotCallInner`
   - `GetSecretAsync_CacheMiss_CallsInner_StoresInCache`
   - `GetSecretAsync_InnerReturnsError_DoesNotCache`
   - `GetSecretAsync_CacheProviderFails_FallsToInner`
   - `GetSecretAsync_InnerFailsWithResilience_ServesLastKnownGood`
   - `GetSecretAsync_TypedSecret_UsesSeparateCacheKey`
   - `GetSecretAsync_PerSecretTTL_RespectsCacheDuration`
   - `Invalidate_RemovesAllCacheVariants`

2. `CachingSecretWriterDecoratorTests.cs`:
   - `SetSecretAsync_Success_InvalidatesCacheAndPublishes`
   - `SetSecretAsync_InnerFails_DoesNotInvalidate`
   - `SetSecretAsync_NoPubSub_InvalidatesLocalCacheOnly`
   - `SetSecretAsync_PubSubFails_StillReturnsSuccess`

3. `SecretCachePubSubHostedServiceTests.cs`:
   - `StartAsync_SubscribesToChannel`
   - `OnMessage_InvalidatesCacheByPattern`
   - `StartAsync_SubscriptionFails_LogsWarning_DoesNotThrow`
   - `StopAsync_DisposesSubscription`

4. `SecretCachingOptionsTests.cs`:
   - Default values verification

**Guard Tests** (`tests/Encina.GuardTests/Security/Secrets/Caching/`):

5. `CachingSecretReaderDecoratorGuardTests.cs` — null constructor parameters
6. `CachingSecretWriterDecoratorGuardTests.cs` — null constructor parameters
7. `SecretCachePubSubHostedServiceGuardTests.cs` — null constructor parameters

**Update Existing Tests:**

8. Update any tests that reference `CachedSecretReaderDecorator` to use `CachingSecretReaderDecorator`
9. Update any tests that mock `IMemoryCache` for secrets caching to mock `ICacheProvider` instead

**Documentation:**

10. **Update `CHANGELOG.md`** — add entry under Unreleased/Added:
    - "Replace IMemoryCache with ICacheProvider for Secrets caching, enabling distributed cache consistency (#694)"

11. **Update `src/Encina.Security.Secrets/README.md`** — update caching section to reference ICacheProvider

12. **Update `docs/INVENTORY.md`** — add new files, remove deleted file

13. **XML doc comments** — ensure all new public APIs have complete documentation

14. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings

15. **Test verification**: `dotnet test` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
You are working on Encina .NET 10 library, issue #694. This phase creates tests and
documentation for the ICacheProvider migration. The old CachedSecretReaderDecorator
has been DELETED — update all references.

REFERENCE FILES:
- tests/Encina.UnitTests/Security/Secrets/ (existing test structure — update references)
- tests/Encina.GuardTests/Security/Secrets/ (existing guard tests — update references)
- src/Encina.Security.Secrets/Caching/ (all new files from Phases 1-4)
- CHANGELOG.md (update Unreleased section)

TASK:
1. Create unit tests for CachingSecretReaderDecorator:
   - Mock ICacheProvider, IPubSubProvider, ISecretReader
   - Test cache hit/miss/error/fallback scenarios
   - Test typed secret caching with separate keys
   - Test per-secret TTL override
   - Test Invalidate() method
   - AAA pattern, descriptive names

2. Create unit tests for CachingSecretWriterDecorator:
   - Test write-through: inner success → invalidate + publish
   - Test inner failure → no invalidation
   - Test no pubsub → local invalidation only
   - Test pubsub failure → still returns success

3. Create unit tests for SecretCachePubSubHostedService:
   - Test subscription lifecycle
   - Test invalidation handling
   - Test graceful failure on subscription error

4. Create guard tests for all three new classes:
   - Null constructor parameter tests

5. Update ALL existing tests that reference CachedSecretReaderDecorator or IMemoryCache:
   - Replace with CachingSecretReaderDecorator and ICacheProvider
   - This includes unit tests, guard tests, and any integration tests

6. Update CHANGELOG.md:
   - Under ### Added: "Replace IMemoryCache with ICacheProvider for Secrets caching,
     enabling distributed cache consistency and cross-instance invalidation (#694)"

7. Update docs/INVENTORY.md with new files, remove deleted file

8. Verify build: dotnet build --configuration Release → 0 errors, 0 warnings
   Verify tests: dotnet test → all pass

KEY RULES:
- AAA pattern for all tests
- Descriptive test names (no Test1, Test2)
- Mock all dependencies in unit tests
- Guard tests for all public constructor parameters
- Coverage target >= 85%
- Remove ALL references to CachedSecretReaderDecorator and IMemoryCache
```

</details>

---

## Research

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `ICacheProvider` | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | Core caching abstraction — `GetOrSetAsync<T>`, `RemoveAsync`, `RemoveByPatternAsync` |
| `IPubSubProvider` | `src/Encina.Caching/Abstractions/IPubSubProvider.cs` | Cross-instance invalidation via typed publish/subscribe |
| `CachingPolicyStoreDecorator` | `src/Encina.Security.ABAC/Persistence/CachingPolicyStoreDecorator.cs` | Reference pattern for decorator implementation |
| `PolicyCachePubSubHostedService` | `src/Encina.Security.ABAC/Persistence/PolicyCachePubSubHostedService.cs` | Reference pattern for hosted service |
| `PolicyCachingOptions` | `src/Encina.Security.ABAC/PolicyCachingOptions.cs` | Reference pattern for options class |
| `CachedSecretReaderDecorator` | `src/Encina.Security.Secrets/Caching/CachedSecretReaderDecorator.cs` | **TO BE DELETED** — read for behavior reference before replacing |
| `SecretsOptions` | `src/Encina.Security.Secrets/SecretsOptions.cs` | Configuration to extend with `Caching` sub-options |
| `ServiceCollectionExtensions` | `src/Encina.Security.Secrets/ServiceCollectionExtensions.cs` | DI registration to update — remove IMemoryCache, add ICacheProvider |
| `Log.cs` | `src/Encina.Security.Secrets/Log.cs` | Structured logging to update — replace old cache EventIds |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Central EventId registry — add SecuritySecrets range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Secrets` (existing) | 1-129 (local, unregistered) | Legacy allocation, pre-registry — should be migrated in separate issue |
| `Encina.Security.Secrets` (new) | 8950-8999 | **New registration** — cache-related EventIds replace old 20-22 |

> **Note**: The existing Log.cs uses EventIds 1-129 without central registration. The old cache EventIds (20-22) will be replaced by properly registered ones (8950+). Full migration of remaining EventIds (1-19, 23-129) is out of scope — create a separate issue.

### Estimated File Count

| Category | New Files | Modified Files | Deleted Files |
|----------|-----------|----------------|---------------|
| Production code | 4 | 4 | 1 |
| Tests | 7 | ~2-3 | 0 |
| Documentation | 0 | 3 | 0 |
| Configuration | 0 | 2 | 0 |
| **Total** | **11** | **~11** | **1** |

**New production files:**

1. `SecretCachingOptions.cs`
2. `SecretCacheInvalidationMessage.cs`
3. `CachingSecretReaderDecorator.cs` (replacement)
4. `CachingSecretWriterDecorator.cs`
5. `SecretCachePubSubHostedService.cs`

**Deleted production files:**

1. `CachedSecretReaderDecorator.cs` (old IMemoryCache version)

**Modified production files:**

1. `SecretsOptions.cs` — add `Caching` property
2. `ServiceCollectionExtensions.cs` — replace IMemoryCache with ICacheProvider
3. `Log.cs` — replace old cache EventIds with registered ones
4. `Encina.Security.Secrets.csproj` — add Encina.Caching dependency, remove Microsoft.Extensions.Caching.Memory

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt — All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library for building distributed systems. Issue #694 REPLACES
IMemoryCache with ICacheProvider in Secrets Management. The old CachedSecretReaderDecorator
is DELETED entirely — no fallback, no dual code paths. This is a clean replacement.

IMPLEMENTATION OVERVIEW:
1. Create SecretCachingOptions and SecretCacheInvalidationMessage configuration models
2. DELETE old CachedSecretReaderDecorator
3. Create CachingSecretReaderDecorator (cache-aside with ICacheProvider)
4. Create CachingSecretWriterDecorator (write-through invalidation)
5. Create SecretCachePubSubHostedService (cross-instance invalidation subscription)
6. Update ServiceCollectionExtensions — remove IMemoryCache, use ICacheProvider
7. Add observability: EventId range 8950-8999, ActivitySource, Meter
8. Create comprehensive tests (unit, guard), update existing tests
9. Update documentation

KEY PATTERNS (from ABAC reference):
- Decorator wraps inner provider with ICacheProvider cache-aside
- Only cache Right results (Either<EncinaError, T>), never errors
- IPubSubProvider optional in decorator (null-checked), required only for HostedService
- All cache/pubsub operations in try/catch — failures fall through to inner provider
- HostedService subscribes on StartAsync, disposes on StopAsync
- Write-through: persist first, then invalidate + broadcast
- Bulk invalidation via RemoveByPatternAsync("{prefix}:*")

CACHE KEY SCHEME:
- {prefix}:v:{name}        — String secret value
- {prefix}:t:{name}:{type} — Typed secret value
- {prefix}:lkg:{name}      — Last-known-good (stale fallback)
- {prefix}:list             — Secret list cache

DI REGISTRATION LOGIC:
- When EnableCaching = true: ICacheProvider is REQUIRED (GetRequiredService)
- IPubSubProvider remains optional (GetService) — used for cross-instance invalidation
- No IMemoryCache fallback — Encina.Caching.Memory wraps IMemoryCache via ICacheProvider

DECORATOR CHAIN (reader):
inner → ResilientSecretReaderDecorator → CachingSecretReaderDecorator → AuditedSecretReaderDecorator

DECORATOR CHAIN (writer):
inner → ResilientSecretWriterDecorator → CachingSecretWriterDecorator

REFERENCE FILES:
- src/Encina.Security.ABAC/Persistence/CachingPolicyStoreDecorator.cs (primary pattern)
- src/Encina.Security.ABAC/Persistence/PolicyCachePubSubHostedService.cs
- src/Encina.Security.ABAC/PolicyCachingOptions.cs
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs (lines 178-219)
- src/Encina.Security.Secrets/Caching/CachedSecretReaderDecorator.cs (READ then DELETE)
- src/Encina.Security.Secrets/SecretsOptions.cs
- src/Encina.Security.Secrets/ServiceCollectionExtensions.cs
- src/Encina.Security.Secrets/Log.cs
- src/Encina.Security.Secrets/Abstractions/ISecretReader.cs
- src/Encina.Security.Secrets/Abstractions/ISecretWriter.cs
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.Caching/Abstractions/IPubSubProvider.cs
- src/Encina/Diagnostics/EventIdRanges.cs

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- ROP: Either<EncinaError, T> on all store/handler methods
- Pre-1.0: choose the best solution, not the compatible one
- No [Obsolete] attributes — DELETE the old code, don't deprecate it
- XML documentation on all public APIs
- sealed classes for all new types
- [LoggerMessage] source generator for all logging
- EventIds 8950-8999 (register in EventIdRanges.cs first)
- Follow ABAC pattern exactly for cache operations and error handling
- AAA pattern for all tests, descriptive names
- Guard tests for all constructor parameters
- Update ALL existing tests that reference CachedSecretReaderDecorator/IMemoryCache
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Include | This IS the caching integration — replacing IMemoryCache with ICacheProvider |
| 2 | **OpenTelemetry** | ✅ Include | ActivitySource + Meter for cache hit/miss/invalidation (Phase 4) |
| 3 | **Structured Logging** | ✅ Include | [LoggerMessage] EventIds 8950-8961 for cache operations (Phase 4) |
| 4 | **Health Checks** | ❌ N/A | Secrets already has `SecretsHealthCheck`; cache provider health checks are separate (#754) |
| 5 | **Validation** | ❌ N/A | No new user input boundaries; options validation already exists |
| 6 | **Resilience** | ✅ Include | Stale fallback (last-known-good) integration with existing resilience decorator; graceful degradation on cache failures |
| 7 | **Distributed Locks** | ❌ N/A | No concurrent write contention — cache-aside pattern is inherently safe |
| 8 | **Transactions** | ❌ N/A | Cache operations are not transactional; write-through is best-effort |
| 9 | **Idempotency** | ❌ N/A | Cache operations are naturally idempotent (set/remove) |
| 10 | **Multi-Tenancy** | ⏭️ Defer | TenantId could be added to cache key prefix for tenant-scoped secrets. Not in scope for #694. Related: #710 |
| 11 | **Module Isolation** | ⏭️ Defer | ModuleId dimension in cache keys. Not in scope for #694. Related: #746 |
| 12 | **Audit Trail** | ❌ N/A | Secret access auditing already exists via `AuditedSecretReaderDecorator`; cache layer is transparent to audit |

**Deferred items:**

- Multi-Tenancy (#710 covers tenant-scoped caching broadly; verify secrets are included)
- Module Isolation (#746 covers ModuleId in cache keys)

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `ICacheProvider` interface | ✅ Available | `src/Encina.Caching/Abstractions/ICacheProvider.cs` |
| `IPubSubProvider` interface | ✅ Available | `src/Encina.Caching/Abstractions/IPubSubProvider.cs` |
| At least 1 cache provider implementation | ✅ Available | Memory, Redis, Garnet, Valkey, Dragonfly, KeyDB, Hybrid |
| ABAC reference implementation | ✅ Available | `CachingPolicyStoreDecorator` + `PolicyCachePubSubHostedService` |
| Existing secrets infrastructure | ✅ Available | `CachedSecretReaderDecorator`, `SecretsOptions`, `ServiceCollectionExtensions` |
| EventId range availability | ✅ Available | 8950-8999 is free (between 8949 and 9000) |

No blocking prerequisites. All dependencies are already implemented.
