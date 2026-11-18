# Implementation Plan: Enhanced tag-based cache invalidation

> **Issue**: [#271](https://github.com/dlrivada/Encina/issues/271)
> **Type**: Feature
> **Complexity**: High (6 pull requests of 11, 7, 6, 15, 15 and 14 files)
> **Estimated Scope**: about 900 lines of production code, about 1,800 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #271 asks for tags on cache entries: `[Cache(Tags = [...])]`, dynamic `[CacheTag("product:{Response.Id}")]`, `[InvalidatesCache(Tags = [...])]`, programmatic `SetWithTagsAsync`, `RemoveByTagAsync`, `RemoveByTagsAsync`, tag hierarchies with pattern removal (`RemoveByTagPatternAsync`), Redis tag sets and a memory implementation. Re-checked against `origin/main` (f484cdf4f):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "HybridCache provider has basic tag support" | True and larger than "basic": `HybridCacheProvider` has public `RemoveByTagAsync` (line 172), a `GetOrSetAsync` overload with tags (line 247) and its own `_tagToKeys` and `_keyToTags` tracking, but none of it is on `ICacheProvider`. |
| `ICacheProvider` can be extended | It has 5 source implementers (`MemoryCacheProvider`, `RedisCacheProvider`, `HybridCacheProvider`, `InstrumentedCacheProvider` in `Encina.OpenTelemetry`, `FakeCacheProvider` in `Encina.Testing.Fakes`) and 5 test doubles: `NoOpCacheProvider` (`tests/Encina.ContractTests/Cdc/Caching/QueryCacheInvalidationCdcHandlerContractTests.cs`), `FakeCache` (`QueryCachingPipelineBehaviorBoundaryGuardTests.cs`), `SharedCache` (`DSRTenantScopingIntegrationTests.cs`), `FailingCacheProvider` (`CacheLogRedactionTests.cs`) and `RecordingCacheProvider` (`QueryCachingVaryByUserTests.cs`). Valkey, Dragonfly, Garnet and KeyDB reuse `RedisCacheProvider`; Memcached (#277) is not on `main`. |
| The issue's `ICacheProvider` listing has three tag methods | The acceptance criteria also need pattern removal over tags (`RemoveByTagPatternAsync` appears in the issue's hierarchy example): four methods. |
| `[InvalidatesCache]` invalidates by glob only | True: `KeyPattern`, `BroadcastInvalidation`, `DelayMilliseconds`; `CacheInvalidationPipelineBehavior` removes by pattern and publishes the pattern on `CachingOptions.InvalidationChannel`; no subscriber of that channel is in `src` today. |
| `tenant:{TenantId}:products` as a tag template embeds the tenant by hand | A hand-written tenant segment is neither escaped nor enforced, so an invalidation without it clears every tenant's entries or none (Design Choice 2). |
| Redis tag sets (`SADD tag:products key`, `SMEMBERS`, `DEL`) | `RedisCacheProvider` already runs scripts (`ScriptEvaluateAsync`) and scans keys with the server of the first endpoint (`GetServer`, cluster mode is not supported today). |

The plan adds four tag methods to `ICacheProvider` and implements them on every provider first (as public methods), then adds them to the interface together with the decorator, the fake and the test doubles, and finally wires `[Cache(Tags)]`, `[CacheTag]` and `[InvalidatesCache(Tags)]` into the two pipeline behaviors.

**Shared pieces (family cache-patterns-1):** `CachePatternLog`, `CachePatternMetrics` (partial classes) and the `CachingPatterns` range (450-499) belong to #266; this plan owns behavior-level EventIds 478-484. Provider-level tag logs use the provider ranges (the mapped assembly emits its own ids). Nothing here touches `CacheEntry<T>`.

**Affected packages:** `Encina.Caching`, `Encina.Caching.Memory`, `Encina.Caching.Redis`, `Encina.Caching.Hybrid`, `Encina.OpenTelemetry`, `Encina.Testing.Fakes`. **Provider category:** Caching (8): Memory, Hybrid and Redis implement; Valkey, Dragonfly, Garnet and KeyDB inherit the Redis implementation; Memcached follows in the later-merging PR between this plan and #277.

---

## Design Choices

<details>
<summary><strong>1. Where the tag API lives (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Four new members on `ICacheProvider`: `SetWithTagsAsync<T>(key, value, expiration, IReadOnlyCollection<string> tags, ct)`, `RemoveByTagAsync`, `RemoveByTagsAsync`, `RemoveByTagPatternAsync`** | One abstraction; every provider must implement it (AGENTS.md section 5: a caching feature is consistent across all providers); matches the issue | A breaking interface change: 5 implementers and 5 test doubles change (pre-1.0, acceptable) |
| **B) A separate `ITaggedCacheProvider : ICacheProvider` capability interface** | No change for existing implementers | Consumers must test for the capability; a provider may silently lack tags, against section 5 |

### Chosen Option: **A - extend `ICacheProvider`** (recommended, pending the maintainer)

### Rationale

- The issue's `string[]` parameter becomes `IReadOnlyCollection<string>`, the type `HybridCacheProvider.GetOrSetAsync` already uses for tags (line 253).
- Option B wins if the maintainer wants providers that cannot support tags to remain valid implementers.
- Order of change (no broken build between pull requests): PR 1 to PR 3 add the four methods as public methods on each provider; PR 4 adds them to the interface together with the decorator, the fake and the five doubles.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Tenant qualification of tags (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The framework qualifies every tag: `t:{tenant}:{name}` for an entry that varies by tenant and has a tenant, `g:{name}` otherwise; the tenant segment is percent-encoded (`%`, `:` and the glob characters `* ? [ ] \`), so two (tenant, name) pairs never give one qualified tag and a tenant id never acts as a pattern; a tag pattern is qualified the same way** | `[InvalidatesCache(Tags = ["products"])]` clears only the caller's tenant; injective; no pattern widening | Provider tag keys are not the raw names |
| **B) Tags are literal; applications put the tenant into the tag by template (the issue's example)** | Simplest | Forgetting the segment clears or shares every tenant's entries; a tenant id with `:` or `*` collides or widens a pattern |

### Chosen Option: **A - qualify in the framework** (recommended, pending the maintainer)

### Rationale

- The existing `CacheKeyRedactor.EscapeSegment` escapes `%` and `:` only; tags add the glob characters because `RemoveByTagPatternAsync` takes a glob. A tenant named `default` is an ordinary tenant (`t:default:...` and `g:...` differ).
- Option B wins when tags are shared across tenants on purpose; that case is expressed here by `VaryByTenant = false`, which yields `g:`.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Invalidation with no tenant under multi-tenancy (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Invalidate the tag in every tenant (a tag pattern `t:*:{name}`) and log it (EventId 484)** | Never leaves stale data; over-invalidation costs only cache misses | One caller with no tenant clears all tenants' entries of that tag |
| **B) Skip the invalidation and log it** | Never clears another tenant's data | Leaves stale entries the command meant to remove |

### Chosen Option: **A - invalidate all tenants** (recommended, pending the maintainer)

### Rationale

- The existing glob invalidation already does this: `DefaultCacheKeyGenerator.GeneratePattern` uses `t:*` when no tenant exists (`DefaultCacheKeyGenerator.cs`, lines 69-76). Reads fail closed (the cache is bypassed with no tenant); an invalidation is not a read gate and under-invalidating is the unsafe side.
- Option B wins where a tenant-less command in a multi-tenant host is a programming error that must not touch other tenants.
- Class: Dilemma

</details>

<details>
<summary><strong>4. How Redis-protocol providers store tags (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One set per tag (`SADD`), written with the entry in a `MULTI`/`EXEC` transaction; the set's TTL is extended to the longest member TTL with `PEXPIRE key ttl GT` (an atomic "only when larger", Redis 7.0 and later; a read-then-write `PEXPIRE` inside `MULTI` would race and could shorten the set below a member's TTL, so the entry would survive an invalidation); invalidation renames the set to a draining key (`RENAME`), reads its members (`SSCAN`), deletes them (`UNLINK`) and the draining key; pattern removal enumerates tag-set keys with the existing server scan** | The issue's design; no Lua; the rename isolates concurrent adds; works for the five Redis-protocol providers if each supports the commands | Stale members stay until the set expires (a delete of an absent key is harmless); no cluster support (as today) |
| **B) A version counter per tag embedded in each entry; invalidation is one `INCR` and every read compares versions** | O(1) invalidation; works for providers without sets (Memcached) | Every read costs an extra read of the tag versions; the read path of the behavior and of each provider changes |

### Chosen Option: **A - tag sets** (recommended, pending the maintainer)

### Rationale

- The worker verifies that Valkey, Dragonfly, Garnet and KeyDB each support `MULTI`/`EXEC`, `SADD`, `PEXPIRE ... GT`, `RENAME`, `SSCAN` and `UNLINK` against their documentation (and, where a container image is available, against the running server) before the PR and records the result; an unsupported command, notably `GT` on an older server, is a finding for a follow-up issue file, not a silent weaker fallback.
- Option B wins if Memcached (#277) must be served by the same technique or tag fan-out is very large. Expected case under option A: KeyDB is based on Redis 6, which has no `PEXPIRE ... GT`, so KeyDB is the provider the verification is expected to flag (AGENTS.md section 5 asks for consistency across all 8 providers, so a flagged provider gets a follow-up issue file and the maintainer decides between option B for it and a minimum KeyDB version).
- Class: Dilemma

</details>

<details>
<summary><strong>5. Dynamic tags that cannot be resolved, limits and unsupported combinations (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A tag that cannot be built (a `{Response.X}` or `{X}` placeholder with no property or a null value), a name that violates the tag rules, or more than `MaxTagsPerEntry` (default 16) tags: the entry is not cached, EventId 478 or 479 is logged (placeholder text only, never a value), the request still returns its response; a tags-with-sliding-expiration configuration throws `InvalidOperationException` at resolution** | An entry that cannot be invalidated is never stored | A typo disables caching for that request type (visible in logs and a counter) |
| **B) Cache the entry without the missing tag** | The request stays cached | The entry survives invalidations meant for it: stale data |

### Chosen Option: **A - do not cache** (recommended, pending the maintainer)

### Rationale

- The cache is an optimization and stale data is the failure to avoid; option B wins when availability of caching matters more than strict invalidation.
- Sliding expiration has no tag-aware write on `ICacheProvider` (only `SetWithSlidingExpirationAsync`), so the combination is rejected (the failure mode follows the maintainer's answer to #266 Design Choice 7).
- Class: Dilemma

</details>

<details>
<summary><strong>6. Broadcasting tag invalidations (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate channel option `TagInvalidationChannel` (default `sm:cache:invalidate:tags`); the message is the qualified tag (or tag pattern) with a one-character kind prefix** | A subscriber of the pattern channel never mistakes a tag for a glob | One more option; the moved subscriber of #2408 must subscribe to both |
| **B) The same channel with a `tag:` prefix inside the message** | One channel | A subscriber that treats every message as a glob would run `tag:...` as a pattern (a harmless no-op today because no subscriber exists, but a trap) |

### Chosen Option: **A - separate channel** (recommended, pending the maintainer)

### Rationale

- Distributed caches (the Redis family) share one store, so the broadcast matters for local L1 caches (Memory, Hybrid); the subscriber side is #2408's generic subscriber (open); a comment on #2408 asks it to handle the tag channel.
- Option B wins to keep the option surface minimal.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Tag name rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A name is non-empty, at most `MaxTagLength` (128) characters, has no control character and none of `* ? [ ] \`; `:` is allowed (the hierarchy separator)** | A pattern never matches a literal name by accident; bounded keys | Names with glob characters are rejected |
| **B) Any string** | No rule | `a*` as a name equals the pattern `a*`; unbounded key sizes |

### Chosen Option: **A - validated names**

### Rationale

- **Class:** Trivial
- **Settled by:** the glob semantics already in the providers (`MemoryCacheProvider.GlobToRegex` treats `* ? [ ]` as metacharacters, `MemoryCacheProvider.cs` lines 232-240) and AGENTS.md section 3 (no widening of a namespace). Violations throw `ArgumentException` on the public provider methods (what happens to an attribute tag is Design Choice 5). This holds under every option of Design Choices 1 to 6 and 9.

</details>

<details>
<summary><strong>8. EventIds and the emitting assembly</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Behavior-level ids 478-484 in `CachingPatterns` (emitted by `Encina.Caching`); provider-level ids inside each provider's own range (Memory 3565-3567, Redis 3616-3618, Hybrid 3657-3659, each verified free by a grep of the whole assembly: Memory uses 3550-3564 and Redis 3600-3615 because the lock and pub/sub providers of the same assemblies log there)** | Every id is emitted only by the assembly its range maps to (`EncinaEventIdAllocationTests`) | Two places |
| **B) All ids in `CachingPatterns`** | One place | The provider assemblies are not mapped to that range; the architecture test fails |

### Chosen Option: **A - each assembly its own range**

### Rationale

- **Class:** Trivial
- **Settled by:** ADR-021 and AGENTS.md section 7 (never assign an EventId outside the package's range); `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` maps `Encina.Caching.Memory` to `CachingMemory`, `Encina.Caching.Redis` to `CachingRedis` and `Encina.Caching.Hybrid` to `CachingHybrid`. This is the rule only (each assembly emits ids of its own range); the concrete ids are in the Event ID table and are provisional where a Dilemma decides whether the log exists (478 and 479 depend on Design Choice 5, 484 on Design Choice 3). Holds under every option of Design Choices 1 to 6 and 9.

</details>

<details>
<summary><strong>9. `[InvalidatesCache]` with `KeyPattern`, `Tags` and `TagPatterns` (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `KeyPattern` keeps its type and its `"*"` default; an internal flag records whether it was set explicitly (set by its `init` accessor): nothing set means `"*"` as today; only tags set means tags only; an explicit pattern means the pattern, plus the tags when given** | No public signature change; a bare `[InvalidatesCache]` behaves as today; a tag-only attribute never clears everything | An explicit `KeyPattern = "*"` is detectable only through the flag; behavior depends on a hidden flag |
| **B) `KeyPattern` becomes `string?` with default null; null and no tags means `"*"`** | The state is visible in the type | A public API change for every reader of `KeyPattern`, and `[InvalidatesCache(KeyPattern = null)]` becomes legal |
| **C) A separate `[InvalidatesCacheTags]` attribute** | `InvalidatesCache` is untouched | Two attributes for one concept; both can be applied to one request |

### Chosen Option: **A - flag behind the existing property** (recommended, pending the maintainer)

### Rationale

- The issue shows `[InvalidatesCache(Tags = ["products"])]` on one attribute, and the default `"*"` of today's attribute cannot be told from an explicit `"*"` without the flag.
- Option B wins when public visibility of the state matters; option C wins when the maintainer wants no change to `InvalidatesCacheAttribute`.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Tag helper and the Memory provider (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 11 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Tags/CacheTag.cs` | new (public static: programmatic callers need `Qualify` to remove behavior-set tags per tenant) |
| 2 | `src/Encina.Caching.Memory/MemoryCacheProvider.cs` | modify (tag index, four public methods, logs 3565-3567) |
| 3 | `src/Encina.Caching.Memory/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 4 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: `CacheTag`) |
| 5 | `tests/Encina.UnitTests/Caching/CacheTagTests.cs` | new |
| 6 | `tests/Encina.UnitTests/Caching/Memory/MemoryCacheProviderTagTests.cs` | new |
| 7 | `tests/Encina.GuardTests/Infrastructure/Caching/MemoryCacheProviderTagGuardTests.cs` | new |
| 8 | `tests/Encina.GuardTests/Infrastructure/Caching/CacheTagGuardTests.cs` | new (public `CacheTag` methods) |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 10 | `.github/coverage-manifest/Encina.Caching.Memory.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/271-cache-tags-memory.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `public static class CacheTag` (XML docs; because it is public, `Encina.GuardTests` reaches it and no `InternalsVisibleTo` entry is needed for the providers): `Validate(string name)` (Design Choice 7), `Qualify(string name, string? tenantId, bool varyByTenant)` (`t:{encoded}:{name}` or `g:{name}`, Design Choice 2), `QualifyPattern(string pattern, string? tenantId, bool varyByTenant)` and `AllTenantsPattern(string name)` (`t:*:{name}`), `EncodeTenant(string tenantId)` (percent-encodes `% : * ? [ ] \`). Providers never qualify: they validate tag names and take tags as given; the callers (the two behaviors, applications) qualify with `CacheTag`.
2. **Modify** `MemoryCacheProvider`: `ConcurrentDictionary<string, HashSet<string>>` tag to keys and key to tags, updated under one lock per set; `SetWithTagsAsync` stores the value (as `SetAsync` does, with the same post-eviction callback also removing the key from both maps); `RemoveByTagAsync` removes the tag's keys from the cache and the maps; `RemoveByTagsAsync` loops; `RemoveByTagPatternAsync` matches tag names with a case-sensitive glob (the existing `GlobToRegex` is case-insensitive and is not reused for tags). `RemoveAsync` and `RemoveByPatternAsync` clean the maps. Each public method checks its arguments (`ArgumentNullException`, `ArgumentException` for an invalid tag, pre-cancelled token) as the neighbours do. Logs 3565 tags set, 3566 removed by tag, 3567 removed by tag pattern (counts only; the tag text is never logged because a qualified tag carries the tenant).
3. **Tests (unit):** set with two tags then remove by each; remove by tags; pattern `products:*` and `t:a:*`; expiry removes the key from the maps (a `FakeTimeProvider`-driven `MemoryCache` clock); case sensitivity; concurrent set and remove (`Parallel.For`, no exception, no orphan); `CacheTag` encodes `a:b`, `a*`, `%3A` distinctly and two (tenant, name) pairs never give one result (a table of hostile ids). **Guard tests:** null key, null tags, null tag, empty and whitespace tag, tag with a glob character, tag longer than 128, pre-cancelled token (Memory file); null and invalid arguments of `CacheTag.Validate`, `Qualify`, `QualifyPattern` and `AllTenantsPattern` (`CacheTagGuardTests`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #271 adds tags to the cache providers. MemoryCacheProvider
(src/Encina.Caching.Memory) tracks keys in _keyTracker and cleans it in a post-eviction callback; its GlobToRegex is case-insensitive.

TASK:
Add CacheTag (a PUBLIC static class, src/Encina.Caching/Tags) and the four tag methods as PUBLIC methods of MemoryCacheProvider (not on ICacheProvider yet);
write the unit and guard tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, both manifests and the changelog fragment.

KEY RULES:
- Tags are qualified by tenant (t:{encoded}:{name} / g:{name}); names are validated; the tenant segment encodes % : * ? [ ] \.
- Eviction and removal keep both maps consistent; no leak after expiry.
- Logs carry counts only; EventIds 3565-3567 (Memory range, verified free); time from TimeProvider where used.
- Follow the maintainer's answers to Design Choices 1, 2 and 4, given in your brief; the name rules (Design Choice 7) apply in every case.

REFERENCE FILES:
- src/Encina.Caching.Memory/MemoryCacheProvider.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- src/Encina.Caching.Hybrid/HybridCacheProvider.cs
- tests/Encina.GuardTests/Infrastructure/Caching/MemoryCacheProviderGuardTests.cs
```

</details>

### Phase 2: The Redis-protocol providers (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 7 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching.Redis/RedisCacheProvider.cs` | modify (four public methods, logs 3616-3618) |
| 2 | `src/Encina.Caching.Redis/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 3 | `tests/Encina.UnitTests/Caching/Redis/RedisCacheProviderTagTests.cs` | new |
| 4 | `tests/Encina.GuardTests/Infrastructure/Caching/RedisCacheProviderTagGuardTests.cs` | new |
| 5 | `tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderTagIntegrationTests.cs` | new |
| 6 | `.github/coverage-manifest/Encina.Caching.Redis.json` | modify (`mechanical-fixer`) |
| 7 | `changelog.d/271-cache-tags-redis.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** `RedisCacheProvider` (Design Choice 4 option A): `SetWithTagsAsync` writes the value with its TTL and, for each qualified tag, `SADD {prefix}:tag:{qualified} key` and `PEXPIRE {set} {entry ttl} GT` (the set TTL only grows, atomically; Redis 7.0 and later, verified per provider as Design Choice 4 says), all in one `ITransaction`; `RemoveByTagAsync` renames the set to `{prefix}:tag-drain:{qualified}:{guid}`, enumerates it with `SetScanAsync`, `KeyDeleteAsync` with `CommandFlags.FireAndForget` replaced by awaited `UNLINK` batches, then deletes the draining key; `RemoveByTagsAsync` loops; `RemoveByTagPatternAsync` enumerates tag-set keys with the existing server scan (`GetServer`, `KeysAsync(pattern)` under `{prefix}:tag:`) and removes each. A tag set stores prefixed keys, never raw tag names or tenant ids in logs; logs 3616-3618 (tags set, removed by tag, removed by tag pattern) carry counts. The key prefix of `RedisCacheOptions` applies to the tag keys.
2. **Tests (unit, mocked `IConnectionMultiplexer`/`IDatabase` as `RedisCacheProviderTests` does):** the commands issued per call; tag-set TTL extension; pattern enumeration. **Guard tests:** as PR 1. **Integration (shared `RedisCollection`):** set with tags, remove by tag removes the keys and the set, a key added during a removal survives or is removed consistently (the rename isolates it), a pattern removal over two tenants clears only the matching tenant, TTL expiry of the set. The Redis family (Valkey, Dragonfly, Garnet, KeyDB) shares this provider; the worker records the command-support check of Design Choice 4 for each in the PR description.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged (CacheTag, Memory). RedisCacheProvider (src/Encina.Caching.Redis) uses StackExchange.Redis, a key prefix and GetServer for scans.

TASK:
Add SetWithTagsAsync, RemoveByTagAsync, RemoveByTagsAsync and RemoveByTagPatternAsync as PUBLIC methods of RedisCacheProvider with tag sets
(SADD in a transaction, RENAME to a draining key, SSCAN, UNLINK); write unit, guard and Redis integration tests; ask mechanical-fixer for
PublicAPI, the manifest and the changelog fragment.

KEY RULES:
- Use only commands every Redis-protocol provider supports (PEXPIRE with GT needs Redis 7.0 or later); verify and report the check for Valkey, Dragonfly, Garnet, KeyDB; an unsupported command is a finding, never a silent fallback.
- Database calls are asynchronous with a CancellationToken; never log tag names, keys or tenants unredacted.
- EventIds 3616-3618 (Redis range, verified free: 3600-3615 are used). Cluster mode stays unsupported (as today); say so in the XML docs.

REFERENCE FILES:
- src/Encina.Caching.Redis/RedisCacheProvider.cs
- tests/Encina.UnitTests/Caching/Redis/RedisCacheProviderTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 3: The Hybrid provider (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 6 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching.Hybrid/HybridCacheProvider.cs` | modify (`SetWithTagsAsync`, `RemoveByTagsAsync`, `RemoveByTagPatternAsync`; `RemoveByTagAsync` validates the name; the existing `LogCacheRemoveByTag` (EventId 3654) is redacted; new logs 3657-3659) |
| 2 | `src/Encina.Caching.Hybrid/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 3 | `tests/Encina.UnitTests/Caching/Hybrid/HybridCacheProviderTaggingTests.cs` | modify |
| 4 | `tests/Encina.GuardTests/Infrastructure/Caching/HybridCacheProviderGuardTests.cs` | modify |
| 5 | `.github/coverage-manifest/Encina.Caching.Hybrid.json` | modify (`mechanical-fixer`) |
| 6 | `changelog.d/271-cache-tags-hybrid.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** `HybridCacheProvider`: `SetWithTagsAsync` calls `HybridCache.SetAsync(key, value, options, tags: qualifiedTags, ...)` (native tags) and updates the existing `_tagToKeys` and `_keyToTags` maps; `RemoveByTagAsync` (existing, line 172) now validates the tag name but does not qualify it (providers never qualify; callers do, as PR 1 states), and its log `LogCacheRemoveByTag` (line 198 today logs the raw `{Tag}`, which after tenant qualification would put the tenant id into logs) passes the tag through `CacheKeyRedactor.Redact` (it masks the `t:` segment) or logs a count only; `RemoveByTagsAsync` loops; `RemoveByTagPatternAsync` matches `_tagToKeys` names with a case-sensitive glob and calls `RemoveByTagAsync` per match (HybridCache has no pattern removal); the existing `IsTagPattern` convention of `RemoveByPatternAsync` is left unchanged. Native tags invalidate every node's L2 entries; L1 on other nodes follows the HybridCache backplane behavior, which the XML docs state.
2. **Tests:** the existing tagging tests keep passing (a test asserts the redacted log carries no tenant id); new cases for pattern removal, tags set through the new method, and the maps after removal; guard tests as PR 1 for the three new methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged. HybridCacheProvider already exposes RemoveByTagAsync and a GetOrSetAsync overload with tags.

TASK:
Add SetWithTagsAsync, RemoveByTagsAsync and RemoveByTagPatternAsync as public methods, make RemoveByTagAsync validate the name (never qualify) and redact its log, update the tests; ask mechanical-fixer for PublicAPI, the manifest and the changelog.

KEY RULES:
- Reuse the native HybridCache tag support; the maps only serve pattern removal and cleanup.
- EventIds 3657-3659 (Hybrid range); never log raw tags or tenants.

REFERENCE FILES:
- src/Encina.Caching.Hybrid/HybridCacheProvider.cs
- tests/Encina.UnitTests/Caching/Hybrid/HybridCacheProviderTaggingTests.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
```

</details>

### Phase 4: The interface, the decorator, the fake and the doubles (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | modify (four members) |
| 2 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 3 | `src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs` | modify (four decorated methods with spans) |
| 4 | `src/Encina.Testing.Fakes/Providers/FakeCacheProvider.cs` | modify (in-memory tags) |
| 5 | `src/Encina.Testing.Fakes/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.ContractTests/Cdc/Caching/QueryCacheInvalidationCdcHandlerContractTests.cs` | modify (`NoOpCacheProvider`) |
| 7 | `tests/Encina.GuardTests/Infrastructure/Caching/QueryCachingPipelineBehaviorBoundaryGuardTests.cs` | modify (`FakeCache`) |
| 8 | `tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRTenantScopingIntegrationTests.cs` | modify (`SharedCache`) |
| 9 | `tests/Encina.UnitTests/Caching/CacheLogRedactionTests.cs` | modify (`FailingCacheProvider`) |
| 10 | `tests/Encina.UnitTests/Caching/QueryCachingVaryByUserTests.cs` | modify (`RecordingCacheProvider`) |
| 11 | `tests/Encina.UnitTests/OpenTelemetry/QueryCache/InstrumentedCacheProviderTests.cs` | modify |
| 12 | `tests/Encina.UnitTests/Testing/Fakes/FakeCacheProviderTagTests.cs` | new (the fake's tag semantics; the folder is the one of the existing Fakes unit tests, located with `Get-ChildItem tests -Recurse -Filter *Fake*`) |
| 13 | `tests/Encina.GuardTests/Testing/Fakes/FakeCacheProviderTagGuardTests.cs` | new (null key, null tags, invalid tag, pre-cancelled token on the four new public methods of the public fake) |
| 14 | `.github/coverage-manifest/Encina.Testing.Fakes.json` | modify (`mechanical-fixer`: unit 95 and guard 60 for the new members of `FakeCacheProvider.cs`, existing targets kept) |
| 15 | `changelog.d/271-cache-tags-interface.changed.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** `ICacheProvider` with the four members and their XML docs (the contract: tag names are validated by the provider, qualification is the caller's job through `CacheTag` and never the provider's, an unknown tag is a no-op, never an exception). `MemoryCacheProvider`, `RedisCacheProvider` and `HybridCacheProvider` already have them, so they implement the interface with no further change; the `PublicAPI.Unshipped.txt` lines of PR 1 to PR 3 already cover them.
2. **Modify** `InstrumentedCacheProvider` (spans per method with the existing `Start...`/`Complete`/`Failed` helpers, no tag names or tenant in attributes), `FakeCacheProvider` (a dictionary-based implementation sharing semantics with the Memory provider) and the five test doubles (a minimal implementation each: the recording and failing doubles record or throw like their existing methods).
3. **Tests:** the decorator forwards each call and records a failed span on an exception (file 11); the fake's behavior is asserted against the Memory provider by the contract test of PR 6, which instantiates `FakeCacheProvider`. The decorator forwards each call and lets the inner provider validate the arguments, so it adds no new argument check; `InstrumentedCacheProvider.cs` keeps its existing targets and PR 4 edits no OpenTelemetry manifest. **Guard tests:** the public `FakeCacheProvider` validates its new method arguments, so files 12 and 13 test it (unit and guard); both files are in this PR so its targets are reachable here.
4. **Memcached (#277):** Memcached has no tag primitive, so its implementation uses the technique the maintainer chooses in Design Choice 4 (option B fits it). Whichever of this PR 4 and #277 merges second implements the four members for Memcached: the interface makes the build fail otherwise. The orchestrator comments on #277 when this PR merges.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1 to 3 are merged: Memory, Redis and Hybrid providers have the four tag methods as public methods.

TASK:
Add the four members to ICacheProvider, implement them in InstrumentedCacheProvider and FakeCacheProvider and update the five test doubles
(NoOpCacheProvider, FakeCache, SharedCache, FailingCacheProvider, RecordingCacheProvider); update the decorator and fake tests; ask
mechanical-fixer for both PublicAPI files, the manifests and the changelog fragment. Run grep for any other implementer first.

KEY RULES:
- No build may break: grep ': ICacheProvider' in src and tests before the change and list every implementer.
- The decorator never puts tag names, keys or tenants into span attributes.

REFERENCE FILES:
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs
- src/Encina.Testing.Fakes/Providers/FakeCacheProvider.cs
```

</details>

### Phase 5: Attributes, tag resolution and the store path (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

PR 5 holds exactly these 15 files (it needs #266 PR 3 for `CachePatternLog` and #266 PR 4 for `CachePatternMetrics`, which is created there; it edits `QueryCachingPipelineBehavior.cs`, `CacheAttribute.cs` and `CachingOptions.cs` like #266, #267 and #268, and merges last in that order, after #268 PR 2, because the store path composes with fail-safe):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`Tags`) |
| 2 | `src/Encina.Caching/Attributes/CacheTagAttribute.cs` | new |
| 3 | `src/Encina.Caching/Tags/CacheTagResolver.cs` | new (internal) |
| 4 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (resolve tags, `SetWithTagsAsync`) |
| 5 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`MaxTagsPerEntry`, `MaxTagLength`, `TagInvalidationChannel`) |
| 6 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy the three options) |
| 7 | `src/Encina.Caching/Diagnostics/CachePatternLog.Tags.cs` | new (ids 478-484) |
| 8 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.Tags.cs` | new (partial class of #266) |
| 9 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 10 | `tests/Encina.UnitTests/Caching/CacheTagResolverTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Caching/QueryCachingTagTests.cs` | new |
| 12 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify |
| 12a | `tests/Encina.GuardTests/Infrastructure/Caching/CachingOptionsTagGuardTests.cs` | new (the three option setters reject out-of-range values) |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/271-cache-tags-attributes.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Attributes:** `CacheAttribute.Tags` (`string[]`, default empty) and `[CacheTag(template)]` (`AllowMultiple = true`, class target; the constructor does `ArgumentException.ThrowIfNullOrWhiteSpace(template)`, so a guard test for null, empty and whitespace is owed and lives in the options guard file of this PR; the template accepts `{Name}` for a request property and `{Response.Name}` for a property of the response value). `CachingOptions`: `MaxTagsPerEntry` 16 (setter rejects below 1), `MaxTagLength` 128 (rejects below 1), `TagInvalidationChannel` `"sm:cache:invalidate:tags"`; all three copied in `AddEncinaCaching`.
2. **`CacheTagResolver`:** per closed generic, a static table of compiled property accessors built once; `Resolve(request, response, tenantId, varyByTenant)` returns the qualified tags or a failure reason (unresolved placeholder, invalid name, too many) without any value in the reason; static tags are validated at the first use per type (Design Choice 5).
3. **Behavior:** after a `Right`, resolve the tags (the response exists only now, so dynamic tags need the response); an empty tag list uses `SetAsync` exactly as today; a failure reason logs 478 or 479 (placeholder or limit only), counts it and returns the response uncached; otherwise `SetWithTagsAsync` with the same TTL logic (jitter and fail-safe from #266 and #268 compose by taking the same effective duration). The tenant used is the one already used for the key; a bypassed request never reaches the tag path.
4. **Tests (unit):** static tags stored qualified by tenant; two tenants never share a tag; `VaryByTenant = false` gives `g:`; dynamic tags from the request and the response; a null or missing placeholder leaves the entry uncached and the response returned; more than `MaxTagsPerEntry`; a tag with a glob character; `SlidingExpiration` with tags throws at resolution; no tags keeps the old path (all existing behavior tests stay green); options defaults and DI copy. **Guard tests:** `CachingOptionsTagGuardTests` (file 12a): `MaxTagsPerEntry` and `MaxTagLength` below 1 throw `ArgumentOutOfRangeException`, valid values are stored, `TagInvalidationChannel` rejects null or whitespace; the attribute properties have no validation, and `CacheTagResolver` is internal without `InternalsVisibleTo` for `Encina.GuardTests`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Phase 4 is merged: ICacheProvider has the four tag methods on every provider. #266 PR 3 registered CachePatternLog and the 450-499 range.

TASK:
Add CacheAttribute.Tags, CacheTagAttribute, CacheTagResolver (compiled accessors), the three CachingOptions members and their copy lines, the
store path in QueryCachingPipelineBehavior, CachePatternLog.Tags.cs (478-484), the metrics file and the unit tests; ask mechanical-fixer for
PublicAPI, the manifest and the changelog.

KEY RULES:
- Qualify every tag with CacheTag.Qualify; an entry whose tags cannot be built is NOT cached (as the maintainer answered Design Choice 5, given in your brief).
- Never log a tag value, a tenant or a response value; placeholder text and counts only.
- A bypassed request (no tenant under multi-tenancy, VaryByUser without a user) never reaches the tag path.
- Keep Handle at CRAP 10 or below: put tag resolution in private methods.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Tags/CacheTag.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
```

</details>

### Phase 6: Tag invalidation, contract test, integration and docs (pull request 6)

<details>
<summary><strong>Tasks</strong></summary>

PR 6 holds exactly these 14 files (the contract test lands here, so the Memory and Hybrid manifests with the contract targets are edited here):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Attributes/InvalidatesCacheAttribute.cs` | modify (`Tags`, `TagPatterns`) |
| 2 | `src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs` | modify (tag invalidation and broadcast) |
| 3 | `tests/Encina.UnitTests/Caching/CacheInvalidationPipelineBehaviorTests.cs` | modify (regression: `KeyPattern` behavior unchanged; ids 480-484 are already declared by PR 5) |
| 4 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/Caching/CacheInvalidationTagTests.cs` | new |
| 6 | `tests/Encina.ContractTests/Caching/CacheProviderTagContractTests.cs` | new |
| 7 | `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheTagInvalidationIntegrationTests.cs` | new |
| 8 | `docs/features/query-caching.md` | modify (`docs-writer`) |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/271-cache-tag-invalidation.added.md` | new (`mechanical-fixer`) |
| 11 | `tests/Encina.UnitTests/Caching/InvalidatesCacheAttributeTests.cs` | modify |
| 12 | `.github/coverage-manifest/Encina.Caching.Memory.json` | modify (`mechanical-fixer`: contract 60) |
| 13 | `.github/coverage-manifest/Encina.Caching.Hybrid.json` | modify (`mechanical-fixer`: contract 60) |
| 14 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (ProjectReference to `Encina.Caching.Memory` and `Encina.Caching.Hybrid`; today it references `Encina.Caching` and `Encina.Testing.Fakes` only) |

Tasks:

1. **Attribute and behavior:** `InvalidatesCache.Tags` (exact tags) and `TagPatterns` (globs over names); how `KeyPattern`, `Tags` and `TagPatterns` combine is Design Choice 9 (recommended: `KeyPattern` keeps its public type and its `"*"` meaning when nothing else is given, and is ignored when only tags are given; an explicit `KeyPattern` is detected with an internal flag set by the `init` accessor; a worker test proves bare, pattern-only, tag-only and combined attributes). After a successful command, for each tag: qualify with the context tenant (`CacheTag.Qualify`) and call `RemoveByTagAsync`/`RemoveByTagPatternAsync`; with no tenant under multi-tenancy follow Design Choice 3 (EventId 484); broadcast on `TagInvalidationChannel` when `BroadcastInvalidation` and pub/sub are on (480 applied, 481 failed, 482 broadcast, 483 broadcast failed); failures follow `ThrowOnCacheErrors` exactly as the pattern path does. Tag values are redacted in every log.
2. **Contract test** (`Encina.ContractTests`, instantiates the real `MemoryCacheProvider`, `HybridCacheProvider` (built through DI) and `FakeCacheProvider`): the same sequence gives the same result on each: set with tags, remove by tag, by tags, by pattern, unknown tag no-op, key removal cleans tag membership. Redis runs the same sequence in the integration test.
3. **Integration (shared `RedisCollection`):** a real `IEncina` with both behaviors: a query cached with static and dynamic tags, a command invalidating by tag clears it for the caller's tenant and not for another tenant; the pattern form; the broadcast message appears on the tag channel. The Redis family shares the provider; Memory and Hybrid are covered by the contract test; Memcached by its own PR (Phase 4 task 4).
4. **Docs:** `docs-writer` adds a "Tags" section to `docs/features/query-caching.md`: tags, tenant qualification, hierarchies, the not-cached rule, citing tests by name.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
Phase 5 is merged: tags are resolved and stored with entries.

TASK:
Add Tags and TagPatterns to InvalidatesCacheAttribute and tag invalidation plus broadcast to CacheInvalidationPipelineBehavior; write the unit
tests, the contract test over Memory, Hybrid and Fake providers and the Redis integration test; ask mechanical-fixer for PublicAPI, the manifest
and the changelog and docs-writer for the docs section.

KEY RULES:
- Qualify every tag with the context tenant; no tenant under multi-tenancy invalidates all tenants (as the maintainer answered Design Choice 3, given in your brief) and logs 484; the KeyPattern and tag combination follows Design Choice 9.
- Failures follow ThrowOnCacheErrors like the pattern path; logs carry redacted values and counts only.
- The default KeyPattern "*" must not clear everything when only tags are given.
- The contract test instantiates real providers; no reflection-only tests.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs
- src/Encina.Caching/Attributes/InvalidatesCacheAttribute.cs
- tests/Encina.UnitTests/Caching/CacheInvalidationPipelineBehaviorTests.cs
```

</details>

### Phase 7: Coverage targets and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **No new error code**: nothing returns a `Left` created here (invalid input on the public provider methods is `ArgumentException`; attribute errors are `InvalidOperationException`), so there is no ADR-062 category to name. **EventIds:** behavior 478-484 (`CachingPatterns`), providers 3565-3567 (Memory), 3616-3618 (Redis) and 3657-3659 (Hybrid), in their own ranges, already registered; 3556-3558 and 3606-3608 belong to the lock providers of the same assemblies and are not used.
2. **Coverage targets (per flag).** Each row is entered in the manifest of the PR named in its justification, never later (#1762); existing targets are never lowered:

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Tags/CacheTagResolver.cs` | 95 | 0 | 0 | 0 | Every failure reason and the accessor cache are unit tested; internal |
| `Encina.Caching.Memory/MemoryCacheProvider.cs` | keeps 95 | keeps 20, set in PR 1 | 60 (set in PR 6) | 0 | PR 1 adds the unit and guard tests of the new methods and measures the guard value (the manifest guard value is raised only to what PR 1 measures); the contract flag is set by PR 6, which carries the contract test and the Memory manifest edit |
| `Encina.Caching.Redis/RedisCacheProvider.cs` | keeps 95 | keeps 24, raise to what PR 2 measures | 0 | 50 | Commands are unit tested with mocks; the integration test of PR 2 runs the real server, so the integration target is set in PR 2 |
| `Encina.Caching.Hybrid/HybridCacheProvider.cs` | keeps 98 | keeps 95 | 60 (set in PR 6) | 0 | Existing targets kept by PR 3's tagging and guard tests; the contract flag is set by PR 6 with the Hybrid manifest edit |
| `Encina.Caching/Tags/CacheTag.cs` | 100 | 90 | 0 | 0 | Set in PR 1: public pure helpers driven by a hostile-id table and by `CacheTagGuardTests` |
| `Encina.Caching/Configuration/CachingOptions.cs` | keeps | keeps 85, raise to 90 | 0 | 0 | Set in PR 5 with `CachingOptionsTagGuardTests` (three validated setters) |
| `Encina.Caching/ServiceCollectionExtensions.cs` | 90 | 30 | 0 | 0 | The manifest has no target today (the first is set by #266 and kept here); the DI tests of PR 5 run the three copy lines and the guard flag reaches the null-services check |
| `Encina.Caching/Attributes/CacheAttribute.cs`, `CacheTagAttribute.cs`, `InvalidatesCacheAttribute.cs` | 100 | 0 | 0 | 0 | Auto-properties and the `KeyPattern` accessor (PR 6) are executed by the attribute and behavior tests; no validated parameter |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | keeps 98 | keeps 20 | 0 | 0 | Never lowered; the tag path is unit tested |
| `Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs` | keeps 100 | keeps 28 | 0 | 40 | Tag invalidation unit tested; the integration flag runs the end-to-end sequence |
| `Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs` | keep | keep | 0 | 0 | Four forwarding methods covered by the decorator tests of PR 4; no new argument check |
| `Encina.Testing.Fakes/Providers/FakeCacheProvider.cs` | set to the number PR 4 measures (expected near 25) | set to the number PR 4 measures (expected near 10) | 0 | 0 | The manifest has no target for the file today and targets are per whole file (about 422 lines and 20 public members); the PR 4 tests cover the four new members only, so the worker records the measured values with this reason and the later issue that tests the fake's other members raises them (an issue file is written at that point if none exists); the PR 6 contract test raises the unit flag further |
| `Abstractions/ICacheProvider.cs`, attributes, `CachePatternLog.Tags.cs`, `CachePatternMetrics.Tags.cs` | 0 or 90 | 0 | 0 | 0 | Interface and attributes have no executable lines; generated log bodies run in the behavior tests |

   The worker measures each flag with `coverage-report.cs` and records the numbers in each PR description.
3. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching, OpenTelemetry), `Encina.GuardTests`, `Encina.ContractTests` and, for PR 2 and PR 6, `Encina.IntegrationTests` (Redis collection); the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
The six code PRs are merged; each carried its own per-flag targets.

TASK:
Verify, with coverage-report.cs, that each PR's measured numbers reach the targets above; fix any gap through mechanical-fixer in a small PR; run
--check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet); no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- .github/coverage-manifest/Encina.Caching.Redis.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| `Microsoft.Extensions.Caching.Hybrid` tag API (`SetAsync` with tags, `RemoveByTagAsync`) | The native Hybrid tag support the Hybrid provider reuses |
| Redis set commands (`SADD`, `RENAME`, `SSCAN`, `UNLINK`) | The tag-set storage of Design Choice 4 |
| AGENTS.md section 5 (caching) | Tag invalidation is consistent across all 8 providers |
| AGENTS.md sections 3 and 7, ADR-021 | Fail closed on context; EventIds only in the emitting assembly's range |
| ADR-054 D4 | Tenant tag on telemetry only with multi-tenancy on |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `ICacheProvider` and its 5 implementers | `src/Encina.Caching`, `src/Encina.Caching.{Memory,Redis,Hybrid}`, `src/Encina.OpenTelemetry/QueryCache`, `src/Encina.Testing.Fakes/Providers` | Interface change and implementations |
| `HybridCacheProvider` tag tracking | `src/Encina.Caching.Hybrid/HybridCacheProvider.cs` | Native tags and maps already present |
| `CacheKeyRedactor.EscapeSegment` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | Base of the tenant encoding |
| `CacheInvalidationPipelineBehavior` | `src/Encina.Caching/Behaviors/` | Pattern invalidation path to extend |
| `RedisCollection`, `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/` | Shared Redis container |
| `CachePatternLog`, `CachePatternMetrics` | `src/Encina.Caching` (from #266) | Partial log and metrics files |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingPatterns`, registered by #266) | 478-484 | 478 tags unresolved (entry not cached), 479 tag limit or rule violated (entry not cached), 480 tag invalidation applied, 481 tag invalidation failed, 482 tag invalidation broadcast, 483 broadcast failed, 484 invalidation with no tenant applied to all tenants |
| `Encina.Caching.Memory` (`CachingMemory`) | 3565-3567 | tags set, removed by tag, removed by tag pattern (3550-3564 are used by the cache, lock and pub/sub providers of the assembly; checked with a grep of `src/Encina.Caching.Memory`) |
| `Encina.Caching.Redis` (`CachingRedis`) | 3616-3618 | same three (3600-3615 are used; checked with a grep of `src/Encina.Caching.Redis`) |
| `Encina.Caching.Hybrid` (`CachingHybrid`) | 3657-3659 | same three |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 6 | 12 | New: `CacheTag`, `CacheTagResolver`, `CacheTagAttribute`, two partial files; modified: three providers, interface, decorator, fake, two attributes, two behaviors, options, DI; the only csproj edit is the contract test project's references (PR 6) |
| Tests | 10 | 12 | New: helper, resolver, provider, guard, contract, integration and behavior tests; modified: five doubles, hybrid, decorator, fake, options, attribute tests |
| Documentation | 0 | 1 | `docs/features/query-caching.md` |
| Configuration | 6 | 12 | Six changelog fragments; manifests of Caching, Memory, Redis, Hybrid, OpenTelemetry, Fakes; five `PublicAPI.Unshipped.txt` files |
| **Total** | **22** | **37** | 6 pull requests: 11, 7, 6, 15, 15 and 14 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #271 adds tag-based invalidation to the cache. #266 owns the CachePatternLog/Metrics
partial classes and the CachingPatterns range (450-499); this plan uses 478-484 there and its own provider ranges for provider logs.

IMPLEMENTATION OVERVIEW:
1. PR 1: CacheTag helper and Memory tags (public methods).  2. PR 2: Redis tag sets (SADD, RENAME, SSCAN, UNLINK).  3. PR 3: Hybrid.
4. PR 4: the four members on ICacheProvider, decorator, fake, five test doubles.  5. PR 5: [Cache(Tags)], [CacheTag], resolver, store path.
6. PR 6: [InvalidatesCache(Tags, TagPatterns)], broadcast, contract and integration tests, docs.

KEY PATTERNS:
- Tags qualified per tenant (t:{encoded}:{name} / g:{name}); names validated; an entry whose tags cannot be built is not cached.
- Providers first, interface last, so no build breaks; grep every implementer.
- No tag value, tenant or key unredacted in logs or spans; EventIds only in the emitting assembly's range.
- Follow the maintainer's answers to Design Choices 1 to 6 and 9, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends `ICacheProvider` and both caching behaviors across all 8 providers. |
| 2 | OpenTelemetry | ✅ Include | `InstrumentedCacheProvider` spans for the new methods; counter on the `Encina.Caching` meter of #266 for tag outcomes; tenant tag per ADR-054 D4; meter registration is #723. |
| 3 | Structured Logging | ✅ Include | EventIds 478-484 plus provider-range ids. |
| 4 | Health Checks | ❌ N/A | Tag operations are cache writes with no backend of their own; provider health is #754. |
| 5 | Validation | ✅ Include | Tag names and limits validated (Design Choice 7). |
| 6 | Resilience | ❌ N/A | Tag operations are simple cache calls; failures follow `ThrowOnCacheErrors` like the pattern path. |
| 7 | Distributed Locks | ❌ N/A | Redis tag removal isolates concurrent adds with `RENAME`, not a lock. |
| 8 | Transactions | ❌ N/A | Cache operations are independent; the Redis `MULTI`/`EXEC` is internal to one write. |
| 9 | Idempotency | ❌ N/A | Tag invalidation is idempotent by nature. |
| 10 | Multi-Tenancy | ✅ Include | Framework-qualified injective tags and the all-tenants rule for a tenant-less invalidation (Design Choices 2 and 3). |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped tag namespaces follow #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure with no compliance event; erasure-driven purges are #2407. |

**Deferred items:** module dimension (#746, open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #266 PR 3 (`CachePatternLog`, range 450-499) and PR 4 (`CachePatternMetrics`, created there) | Pending | Needed by PR 5 only; PR 1 to PR 4 have no dependency |
| #277 (Memcached provider) | Pending | The later-merging PR of #271 PR 4 and #277 implements Memcached tags (Phase 4 task 4) |
| #2408 (generic invalidation subscriber) | Open | Must also subscribe to `TagInvalidationChannel` |
| Maintainer answers to Design Choices 1 to 6 and 9 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 6 and 9) when work starts, records the answers under `## Maintainer Decisions`, then briefs six workers in PR order.
2. After PR 4 merges, comment on #277 that its provider must implement the four tag members; after PR 6 merges, comment on #2408 about the tag channel.
