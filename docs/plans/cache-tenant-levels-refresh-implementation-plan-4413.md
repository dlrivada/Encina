# Implementation Plan: Per-tenant cache levels and eager-refresh opt-out in cache policies

> **Issue**: [#4413](https://github.com/dlrivada/Encina/issues/4413)
> **Type**: Feature
> **Complexity**: Medium (3 pull requests of 15, 15 and 12 files)
> **Estimated Scope**: about 450 lines of production code, about 900 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #4413 adds `AllowedCacheLevels` and `DisableEagerRefresh` to the per-tenant cache policy of #275: free tenants served from L2 only and excluded from background refresh, premium tenants from L1 and L2. Re-checked against `origin/main` (19b761ffa):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "The policy of #275 exists" | Not in code yet: `src/Encina.Caching` has no `Tenancy/` folder on main (#275 is a merged plan). The levels capability of PR 1 here is independent of it; PR 2 and PR 3 start after #275 PR 2. |
| "Cache levels apply only to the Hybrid provider" | True. `HybridCacheProvider` builds a `HybridCacheEntryOptions` per call with `Expiration` (L2) and `LocalCacheExpiration` (L1) (`HybridCacheProvider.cs:98-103`) and already uses `HybridCacheEntryFlags` (line 78, `DisableLocalCacheWrite \| DisableDistributedCacheWrite`). Memory, the Redis family (Redis, Valkey, Dragonfly, Garnet, KeyDB) and the planned Memcached (#277) have one level. |
| "A level flag on the provider write path" | `ICacheProvider` has no level or options parameter (`ICacheProvider.cs:52-139`). It has five implementers or decorators on main: `HybridCacheProvider`, `MemoryCacheProvider`, `RedisCacheProvider`, the internal `InstrumentedCacheProvider` decorator (`src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs:22`) and `FakeCacheProvider` (`src/Encina.Testing.Fakes/Providers/FakeCacheProvider.cs:25`); the recovery decorator of #278 will be a sixth. |
| "`DisableEagerRefresh` is read by the refresh decision once #267 is merged" | Eager refresh is a merged plan, not code: the trigger sits on the hit path of `QueryCachingPipelineBehavior` in #267 PR 3 (`EagerRefreshPolicy.ShouldRefresh`, then `IBackgroundCacheRefresher`). Nothing can be skipped until that exists. |
| `DisableEagerRefresh` is a per-tenant setting | The tenant is the `IRequestContext.TenantId` the behavior already reads; #275's governor resolves the policy once per request, so the opt-out is one more field of its decision. |

The plan adds a flags enum `CacheLevels`, an optional capability `ILeveledCacheProvider` implemented by Hybrid and forwarded by the decorators, the two policy members, the decision fields the behavior reads, and two EventIds. The 8 cache providers stay consistent: the 7 single-level ones never receive a level call, and a policy that asks for one is rejected or denied explicitly (Design Choice 1).

**Shared pieces and ownership (family cache-patterns-2):** #275 owns `CacheTenantPolicy`, the resolver, `TenantCacheGovernor`, `ITenantCacheGovernor` and `TenantCacheDecision`; #267 owns `EagerRefreshPolicy` and the refresher; #278 owns the recovery decorator `ResilientCacheProvider` (it must forward `ILeveledCacheProvider`, a change made by whichever of the two plans lands second, see PR 1). #4412 changes the same #275 files (a policy member and a decision field): both additive, whichever merges second rebases. EventIds: this plan's range `CachingTenantLevels` 526-527 in block 500-549 (#4411 520-523, #4412 524-525; 528-549 free).

**Affected packages:** `Encina.Caching`, `Encina.Caching.Hybrid`, `Encina.OpenTelemetry` (one decorator), `Encina` (EventId registry). **Provider category:** Caching (8): levels are Hybrid-only by nature, the opt-out is behavior-level on all 8.

---

## Design Choices

<details>
<summary><strong>1. A policy that sets levels on a provider with one level (Dilemma D1, the issue's D1 and D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject: a configured policy that sets `AllowedCacheLevels` fails host start when the registered provider cannot honor levels; a policy returned later by a custom `ITenantCachePolicyProvider` is denied for that tenant (the cache is bypassed, EventId 527), never silently widened** | Explicit; no setting that does nothing; fail closed | An application that runs Hybrid in production and Memory in tests must not set levels in the shared configuration |
| **B) Advisory: levels are applied where the provider has them and ignored (logged once per tenant, EventId 527) elsewhere** | No startup failure in mixed deployments | A tenant policy that silently does nothing on seven of eight providers |

### Chosen Option: **A - reject** (recommended, pending the maintainer)

### Rationale

- A single-provider application gets an immediate error; option B wins for mixed deployments (Memory in development, Hybrid in production) that share one configuration.
- The check asks the registered provider after decoration: `provider is ILeveledCacheProvider { SupportsLevels: true }` (Design Choice 2), so a Hybrid wrapped by `InstrumentedCacheProvider` still qualifies, and a decorator over a single-level provider does not.
- Class: Dilemma

</details>

<details>
<summary><strong>2. How the level reaches the provider (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An optional public capability `ILeveledCacheProvider` (get, set and sliding set that take a `CacheLevels` argument, plus `SupportsLevels`); Hybrid implements it; `InstrumentedCacheProvider` and the #278 decorator forward it** | `ICacheProvider` and its five implementers stay unchanged; levels cost nothing where they do not exist | Every decorator must forward it; a forgotten decorator turns levels off, which Design Choice 1 makes visible (reject or deny), never silent |
| **B) Level arguments on the `ICacheProvider` members** | One contract; no discovery | Every implementer, decorator and test double (five today, 6 with #278) must state what a level means, seven of eight providers with a meaningless argument |

### Chosen Option: **A - capability interface** (recommended, pending the maintainer)

### Rationale

- Pre-1.0 allows B, but a parameter that is meaningless for seven of eight providers is the wrong contract. Option B wins if the maintainer expects a second leveled provider soon.
- Removal, pattern removal and tags stay level-agnostic on both options (Design Choice 5).
- Class: Dilemma

</details>

<details>
<summary><strong>3. Which level combinations a policy may name (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `L1`, `L2` or both (`CacheLevels` flags, `None` invalid)** | Complete; a tenant can opt out of the shared level | An L1-only tenant's entries live on one node only, so two nodes can serve different values for the same key until expiry |
| **B) `L2` or both only; `L1` alone is invalid in `Validate()`** | Every governed tenant keeps the cross-node level; no node-divergent entries; the issue's use cases (free tenants L2, premium both) are covered | `L1` as a flag exists but is rejected until a use case appears |

### Chosen Option: **B - `L2` or both** (recommended, pending the maintainer)

### Rationale

- The issue names two tenant kinds, neither is L1-only. Option A wins for computed values that may safely differ per node.
- `AllowedCacheLevels` null means all levels (today's behavior); the enum is a flags enum so an L1-only mode is additive later.
- Class: Dilemma

</details>

<details>
<summary><strong>4. What `DisableEagerRefresh` does</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A tenant with the flag set is skipped by the on-access refresh trigger of #267: its entries are served until they expire, then recomputed inline on the next miss; the skip is counted and logged** | Delivers the issue's acceptance criterion; no extra state | The tenant's hot keys see the miss latency |
| **B) Raise the tenant's refresh threshold instead of skipping** | Reuses the threshold | Not what the issue says; "never refresh" becomes a magic threshold |

### Chosen Option: **A - skip the trigger**

### Rationale

- **Class:** Trivial
- **Settled by:** the issue's acceptance criterion ("A tenant with `DisableEagerRefresh` is skipped by background refresh") and the trigger design of #267 (an on-access decision on the hit path, with no timer), so the policy is one more input of that decision. It holds under every option of Design Choices 1 to 3.
- The decision comes from the governor's per-request resolution, so a failed lookup or a missing policy already bypasses the cache (the #275 fail-closed rule) before any refresh is considered; a request with `VaryByUser` is not eligible for refresh in #267 at all.

</details>

<details>
<summary><strong>5. Removal and invalidation clear every level</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RemoveAsync`, `RemoveByPatternAsync` and tag removal stay level-agnostic: they clear L1 and L2** | An invalidation can never leave a stale level, whichever levels the tenant uses | None |
| **B) A removal that follows the tenant's levels** | Symmetric with reads and writes | A tenant that changed from both levels to L2 would leave stale L1 entries after an invalidation |

### Chosen Option: **A - level-agnostic removal**

### Rationale

- **Class:** Settled
- **Settled by:** `ICacheProvider.RemoveAsync(string key, CancellationToken)` (`ICacheProvider.cs:71`) has no level parameter and `HybridCacheProvider.RemoveAsync` (line 109) removes through `HybridCache`, which clears both levels; `[InvalidatesCache]` promises the key is gone. Only option A satisfies that promise.
- Consequence for a tenant whose policy changes from both levels to L2: older L1 entries are never read (an L2-only read disables the local read) and expire by `LocalCacheExpiration`; the documentation says so.

</details>

---

## Implementation Phases

### Phase 1: The levels capability on Hybrid (pull request 1, independent of #275)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Configuration/CacheLevels.cs` | new (public flags enum: `L1 = 1`, `L2 = 2`, `Both = 3`) |
| 2 | `src/Encina.Caching/Abstractions/ILeveledCacheProvider.cs` | new (public: `SupportsLevels`, leveled `GetAsync`, `SetAsync`, `SetWithSlidingExpirationAsync`) |
| 3 | `src/Encina.Caching.Hybrid/HybridCacheProvider.cs` | modify (implements it; maps levels to `HybridCacheEntryFlags`) |
| 4 | `src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs` | modify (forwards the three methods; `SupportsLevels` is the inner provider's) |
| 5 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `src/Encina.Caching.Hybrid/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/Hybrid/HybridCacheProviderLevelsTests.cs` | new |
| 8 | `tests/Encina.UnitTests/OpenTelemetry/QueryCache/InstrumentedCacheProviderTests.cs` | modify (forwarding and `SupportsLevels` over a leveled and a single-level inner) |
| 9 | `tests/Encina.ContractTests/Caching/LeveledCacheProviderContractTests.cs` | new (real `HybridCacheProvider`: `Both` equals the unleveled behavior, `L2` never reads or writes L1) |
| 10 | `tests/Encina.GuardTests/Infrastructure/Caching/HybridCacheProviderGuardTests.cs` | modify (null key of the three new methods; valid-result tests for a nullable expiration) |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/Caching/HybridCacheLevelsIntegrationTests.cs` | new (Hybrid with a Redis L2 from the shared fixture) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `.github/coverage-manifest/Encina.Caching.Hybrid.json` | modify (`mechanical-fixer`) |
| 14 | `.github/coverage-manifest/Encina.OpenTelemetry.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/4413-cache-levels-capability.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `CacheLevels` and `ILeveledCacheProvider` (members mirror `GetAsync<T>(key, ct)`, `SetAsync<T>(key, value, expiration, ct)` and `SetWithSlidingExpirationAsync<T>(key, value, sliding, absolute, ct)` with a `CacheLevels` argument after the existing parameters, XML docs with an `<example>`).
2. **Modify** `HybridCacheProvider`: `SupportsLevels` is true; `L2` reads add `DisableLocalCacheRead | DisableLocalCacheWrite` and L2 writes add `DisableLocalCacheWrite` to the `Flags` of the options built for the call (the existing probe in `GetAsync` already sets write flags, so the read variant extends it); `Both` is today's behavior; `L1` alone throws `ArgumentOutOfRangeException` under Design Choice 3 option B (it is allowed under option A); cancellation is checked first, as the existing methods do.
3. **Modify** `InstrumentedCacheProvider`: forwarding with the same spans and tags as the unleveled calls; when the inner provider is not leveled `SupportsLevels` is false and the leveled methods throw `NotSupportedException` (the behavior never calls them in that case).
4. **Tests:** every flag combination reaches the options; the unit tests use a counting `IDistributedCache` to prove an `L2` read reaches L2 every time while a `Both` read is served from L1 after the first; the integration test repeats it with the real Redis L2; the decorator test covers both inner kinds; guard tests cover the three new members (the key is validated, the expiration is nullable and its valid result is tested).
5. **#278 coordination:** when #278 PR 2 (the recovery decorator) is already merged, the worker adds its forwarding to this pull request (one file, one test change, a manifest row); when it is not, the #278 worker adds it. The brief says which, after `git grep -n ": ICacheProvider" src`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4413 adds per-tenant cache levels. Only HybridCacheProvider has two levels (HybridCacheEntryFlags in
src/Encina.Caching.Hybrid/HybridCacheProvider.cs). ICacheProvider has no level parameter and is wrapped by InstrumentedCacheProvider in Encina.OpenTelemetry.

TASK:
1. Create CacheLevels (flags) and the optional public capability ILeveledCacheProvider; implement it in HybridCacheProvider and forward it in InstrumentedCacheProvider.
2. Write the unit, contract, guard and Hybrid-with-Redis integration tests; ask mechanical-fixer for the PublicAPI files, the three manifest rows and the fragment.

KEY RULES:
- An L2-only read and write never touch L1; Both behaves exactly as today; removal stays level-agnostic.
- A decorator over a single-level provider reports SupportsLevels false; never silently ignore a level.
- Integration test uses the shared Redis collection fixture; the guard targets are sized to the argument checks.
- Follow the maintainer's answers to Design Choices 2 and 3 given in your brief.

REFERENCE FILES:
- src/Encina.Caching.Hybrid/HybridCacheProvider.cs
- src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs
- tests/Encina.UnitTests/Caching/Hybrid/HybridCacheProviderTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
```

</details>

### Phase 2: The `AllowedCacheLevels` policy member (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 starts after #275 PR 2 and PR 1 of this plan, and holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Tenancy/CacheTenantPolicy.cs` | modify (`CacheLevels? AllowedCacheLevels`; `Validate()` rejects `None`, undefined bits and, under Design Choice 3 option B, `L1`) |
| 2 | `src/Encina.Caching/Tenancy/CacheTenantPolicyOptions.cs` | modify (the nested options validator also rejects levels when the registered provider cannot honor them, Design Choice 1) |
| 3 | `src/Encina.Caching/Tenancy/TenantCachePolicyResolver.cs` | modify (a policy with levels on a non-leveled provider is `Deny(InvalidPolicy)`) |
| 4 | `src/Encina.Caching/Tenancy/TenantCacheGovernor.cs` | modify (the decision carries `Levels`) |
| 5 | `src/Encina.Caching/Tenancy/ITenantCacheGovernor.cs` | modify (`TenantCacheDecision.Levels`) |
| 6 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (read and write through `ILeveledCacheProvider` when the decision restricts levels; one private method) |
| 7 | `src/Encina.Caching/Diagnostics/CacheTenantLevelsLog.cs` | new (EventId 527 here; 526 arrives in PR 3) |
| 8 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingTenantLevels = (526, 527)`) |
| 9 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (add the range name to the `Encina.Caching` list) |
| 10 | `docs/engineering/eventid-block-registry.md` | modify (row `CachingTenantLevels \| 526-527 \| Caching \| historical`, block row split; read `EventIdRegistryAgreementTests` first) |
| 11 | `tests/Encina.UnitTests/Caching/TenantCacheLevelsTests.cs` | new (policy rules, startup validator, governor decision, behavior with a substitute leveled provider) |
| 12 | `tests/Encina.UnitTests/Caching/TenantCachePolicyResolverTests.cs` | modify (capability deny) |
| 13 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the policy member, the decision field) |
| 14 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the new `EventIdRanges` field) |
| 15 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |

PR 2 has no changelog fragment of its own: the fragment of PR 1 names the levels, and PR 3's fragment names the policy members. The worker greps `: ITenantCacheGovernor` and `For<ITenantCacheGovernor>` on the then-current `origin/main` (one implementer after #275 PR 2) and updates every hit in this pull request.

Tasks:

1. **Modify** the policy and its validation; the message names the member, never a tenant id.
2. **Modify** the resolver and the options validator: the capability is read once from the registered `ICacheProvider` (a singleton, so the singleton resolver may hold it) as `is ILeveledCacheProvider { SupportsLevels: true }`; a failure is `Deny(InvalidPolicy)` for runtime policies and an `OptionsValidationException` at start for configured ones.
3. **Modify** the behavior: when `decision.Levels` is `L2`, reads and writes use the leveled methods (sliding writes included); with `null` or `Both` the unleveled path is untouched. A request with `[Cache(VaryByTenant = false)]` is a global entry and is not governed, as in #275.
4. **Tests:** an `L2` tenant never calls the unleveled read or write (substitute provider with call counts); two tenants (one `L2`, one `Both`) on the real `HybridCacheProvider` show the L1 isolation; a custom provider answering levels for a non-leveled provider bypasses the cache and logs 527; a missing tenant under multi-tenancy still bypasses before any level logic (existing behavior); the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
PR 1 is merged (CacheLevels, ILeveledCacheProvider, Hybrid and the decorator) and #275 PR 1 and PR 2 are merged (policy, resolver, governor, behavior hook).

TASK:
1. Add AllowedCacheLevels to CacheTenantPolicy and its validation; make the resolver and the options validator check the provider capability.
2. Carry the levels in TenantCacheDecision and use ILeveledCacheProvider in the behavior when they restrict levels.
3. Create CacheTenantLevelsLog (EventId 527), register CachingTenantLevels = (526, 527) in EventIdRanges.cs, the allocation test and the registry document.
4. Write the unit tests; ask mechanical-fixer for the PublicAPI files and manifest rows.

KEY RULES:
- Fail closed: an unsupported level setting rejects at start or bypasses the cache; it never widens caching silently.
- Cancellation propagates, never a deny. No tenant under multi-tenancy never reaches the governor (existing bypass); the implicit single tenant uses the reserved segment default.
- Keep QueryCachingPipelineBehavior.Handle at CRAP 10 or below: the new logic is a private method.
- Follow the maintainer's answers to Design Choices 1 and 3 given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Tenancy/TenantCacheGovernor.cs
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 3: The eager-refresh opt-out and documentation (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 starts after #267 PR 3 (the hit-path trigger) and PR 2 of this plan, and holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Tenancy/CacheTenantPolicy.cs` | modify (`bool DisableEagerRefresh`) |
| 2 | `src/Encina.Caching/Tenancy/ITenantCacheGovernor.cs` | modify (`TenantCacheDecision.AllowEagerRefresh`) |
| 3 | `src/Encina.Caching/Tenancy/TenantCacheGovernor.cs` | modify |
| 4 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (the trigger of #267 asks the decision first; a skip is counted and logged) |
| 5 | `src/Encina.Caching/Diagnostics/CacheTenantLevelsLog.cs` | modify (EventId 526, Debug, request type only) |
| 6 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.TenantPolicies.cs` | modify (counter `encina.cache.refresh.skipped_by_policy`, tag `encina.tenant_id` only with multi-tenancy on) |
| 7 | `tests/Encina.UnitTests/Caching/TenantCacheEagerRefreshOptOutTests.cs` | new (real refresher of #267, two tenants, `FakeTimeProvider`, `MeterListener`) |
| 8 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/4413-cache-tenant-levels-refresh.added.md` | new (`mechanical-fixer`) |
| 11 | `docs/features/handler-caching.md` | modify (`docs-writer`: levels and the refresh opt-out in the "Multi-tenant policies" section of #275) |
| 12 | `tests/Encina.UnitTests/Caching/CacheTenantPolicyTests.cs` | modify (the new member's default and its round trip through the provider contract) |

Tasks:

1. **Modify** the governor and decision: `AllowEagerRefresh` is false when the resolved policy sets `DisableEagerRefresh`; with no tenant policies enabled the governor does not exist and the trigger of #267 is unchanged.
2. **Modify** the behavior: the `EagerRefreshPolicy.ShouldRefresh` call of #267 runs only when the decision allows; otherwise the entry is served without scheduling, the counter increments and EventId 526 is logged once per request.
3. **Tests:** at the threshold a `DisableEagerRefresh` tenant schedules nothing while another tenant schedules one refresh; the served value and its expiry are unchanged; the counter carries the tenant tag only with multi-tenancy on; a request with `VaryByUser` never reaches the check (#267 excludes it).
4. **Documentation:** the section states that levels need a leveled provider (Hybrid), that older L1 entries of a tenant moved to L2 expire on their own, and that the opt-out only affects background refresh; it cites tests by name and types no figure. XML docs with `<example>` on every new public member.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 2 is merged (levels in the policy and the decision) and #267 PR 3 is merged (the hit-path refresh trigger with EagerRefreshPolicy and IBackgroundCacheRefresher).

TASK:
Add DisableEagerRefresh to the policy and AllowEagerRefresh to the decision; make the trigger check it before scheduling; add EventId 526 and the skipped_by_policy counter; write the tests; ask docs-writer for the handler-caching.md addition and mechanical-fixer for PublicAPI, manifest rows and the fragment.

KEY RULES:
- A skipped tenant is still served from the cache until expiry; nothing else changes for it.
- The tenant tag appears only with multi-tenancy on (ADR-054 D4); logs carry the request type only.
- Keep QueryCachingPipelineBehavior.Handle at CRAP 10 or below.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Diagnostics/CachePatternMetrics.TenantPolicies.cs
- docs/features/handler-caching.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Fail-closed gates; cancellation is never a deny; time from `TimeProvider` |
| AGENTS.md section 5 | Caching category: a feature touching providers is consistent across the 8; levels exist for one provider, so the other seven are rejected explicitly |
| ADR-054 D4 | Tenant tag on telemetry only with multi-tenancy on |
| ADR-021 | Range registered before use |
| NCache and Hazelcast tiered or near caches (named in the issue's family) | Comparable per-tenant tier control |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `HybridCacheProvider` and its `HybridCacheEntryOptions` | `src/Encina.Caching.Hybrid/HybridCacheProvider.cs` | The only leveled provider |
| `InstrumentedCacheProvider` | `src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs` | Decorator that must forward the capability |
| Tenant policy, resolver, governor of #275 | `src/Encina.Caching/Tenancy/` (after #275 PR 2) | Extended, not redefined |
| `EagerRefreshPolicy` and the refresher of #267 | `src/Encina.Caching/Patterns/` (after #267 PR 3) | The refresh decision the opt-out feeds |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared container for the Hybrid integration test |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs` | New range and assembly map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingTenantLevels`, block 500-549 of the plan batch registry, family cache-patterns-2) | 526-527 | 526 eager refresh skipped by tenant policy (request type only), 527 tenant policy names levels the registered provider cannot honor (the tenant bypasses the cache). Verified free: the merged plans use 500-519 in this block and `docs/plans` claims no id in 520-549 (the 520-536 lines in the #274 and #275 plans are superseded by the 30350 and 30400 slots of #276 and #278); #4411 uses 520-523 and #4412 524-525 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 4 | 14 | New: enum, capability interface, log; modified: Hybrid provider, decorator, policy (twice), options, resolver, governor and interface (twice), behavior (twice), metrics partial, `EventIdRanges.cs`, PublicAPI files |
| Tests | 6 | 6 | New: Hybrid levels, contract, integration, levels, opt-out tests; modified: decorator, guard, resolver, policy tests, allocation test |
| Documentation | 0 | 2 | `docs/features/handler-caching.md`, the registry document |
| Configuration | 2 | 5 | Two changelog fragments; manifests edited per pull request |
| **Total** | **12** | **27** | 3 pull requests of 15, 15 and 12 files (a file edited in two pull requests counts in each), each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4413 adds AllowedCacheLevels and DisableEagerRefresh to the per-tenant cache policy of #275. Levels exist
only in HybridCacheProvider; the other seven cache providers have one level. The eager-refresh opt-out feeds the on-access trigger of #267.

IMPLEMENTATION OVERVIEW:
1. PR 1: CacheLevels, the optional ILeveledCacheProvider capability, Hybrid implementation, decorator forwarding, tests (independent of #275).
2. PR 2: AllowedCacheLevels in the policy, capability check in the resolver and the options validator, levels in the decision, the behavior's leveled calls, EventId 527.
3. PR 3: DisableEagerRefresh, the decision field, the trigger check, EventId 526, the skipped_by_policy counter, documentation.

KEY PATTERNS:
- Fail closed: an unsupported level setting rejects at host start or bypasses the cache for that tenant; it never widens caching.
- Removal and invalidation stay level-agnostic; an L2-only tenant never reads or writes L1.
- No tenant under multi-tenancy bypasses the cache (existing); the implicit tenant uses the reserved segment default; telemetry tags the tenant only with multi-tenancy on.
- Follow the maintainer's answers to Design Choices 1 to 3 given in your brief.

REFERENCE FILES:
- src/Encina.Caching.Hybrid/HybridCacheProvider.cs
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends the tenant policy of #275; levels are a Hybrid capability, the opt-out is behavior-level on all 8 providers. |
| 2 | OpenTelemetry | ✅ Include | Counter `encina.cache.refresh.skipped_by_policy`; tenant tag only with multi-tenancy on (ADR-054 D4); spans belong to #723 (open). |
| 3 | Structured Logging | ✅ Include | `CacheTenantLevelsLog`, EventIds 526-527 in `CachingTenantLevels`. |
| 4 | Health Checks | ❌ N/A | No backend of its own; provider health is #754. |
| 5 | Validation | ✅ Include | `CacheTenantPolicy.Validate()` for the level combination, plus the provider capability check at start and at resolution. |
| 6 | Resilience | ❌ N/A | Configuration and an in-process decision; the recovery decorator of #278 only has to forward the capability. |
| 7 | Distributed Locks | ❌ N/A | No shared resource is coordinated. |
| 8 | Transactions | ❌ N/A | Cache operations are independent of units of work. |
| 9 | Idempotency | ❌ N/A | Applying a policy is idempotent. |
| 10 | Multi-Tenancy | ✅ Include | The feature itself: per-tenant levels and opt-out, fail closed for a missing tenant (existing bypass), reserved `default` segment, tag per ADR-054 D4; the decision is stateless per request, so no per-tenant table exists and none can grow with offboarded tenants. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped policies follow the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | An operational setting, not a compliance event; policy-change audit is #4414 (batch B). |

**Deferred items:** module-scoped policies (#746, open). Load test and benchmark are justified skips: the change adds one flag mapping to calls the provider already makes and one boolean to a decision #275 already computes.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `HybridCacheProvider`, `InstrumentedCacheProvider`, `RedisFixture` | ✅ Available | On `origin/main`; PR 1 needs nothing else |
| #275 PR 1 and PR 2 | Open | PR 2 and PR 3 of this plan need them |
| #267 PR 3 (hit-path trigger) | Open | PR 3 of this plan needs it |
| #278 recovery decorator | Open | Must forward `ILeveledCacheProvider` (PR 1, task 5) |
| Block 500-549 of the EventId registry | ✅ Assigned | 526-527 free |
| Maintainer answers to Design Choices 1 to 3 | Pending | Asked when work starts |

Coverage targets (per flag, set in the pull request that adds or changes each file; every row's test file is in the same pull request):

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching.Hybrid/HybridCacheProvider.cs` | keep (98) | keep (95) | 20 | 20 | The manifest has unit 98 and guard 95 today and they are kept; the contract and integration tests of PR 1 execute only the three leveled methods, about a fifth of the file |
| `Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs` | keep (65) | keep (77) | 0 | 0 | The manifest has unit 65 and guard 77 today and they are kept; the new forwarders are covered by the unit tests of PR 1 |
| `Encina.Caching/Configuration/CacheLevels.cs`, `Abstractions/ILeveledCacheProvider.cs` | 0 | 0 | 0 | 0 | An enum and an interface have no executable lines |
| `Encina.Caching/Diagnostics/CacheTenantLevelsLog.cs` | 90 | 0 | 0 | 0 | Generated bodies run in the level and opt-out tests |
| `Encina.Caching/Tenancy/CacheTenantPolicy.cs`, `TenantCachePolicyResolver.cs`, `TenantCacheGovernor.cs`, `CacheTenantPolicyOptions.cs`, `ITenantCacheGovernor.cs` | keep (the #275 values) | keep | 0 | keep | #275's targets are never lowered; the new branches are tested in PR 2 and PR 3 |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | keep (98 on main today) | keep (20) | 0 | 0 | The existing manifest values are never lowered; the new private methods are unit tested |
| `Encina.Caching/Diagnostics/CachePatternMetrics.TenantPolicies.cs` | keep (90) | 0 | 0 | 0 | The counter runs in the opt-out test of PR 3 |

The worker measures every flag with `coverage-report.cs` per pull request and reports the numbers; no target is lowered (`target-ratchet`).

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 3) when work starts, records the answers under `## Maintainer Decisions`, then briefs PR 1 (which can start at once), PR 2 after #275 PR 2 and PR 3 after #267 PR 3.
2. No issue file is written: module scoping is #746 (open) and policy audit is #4414.
