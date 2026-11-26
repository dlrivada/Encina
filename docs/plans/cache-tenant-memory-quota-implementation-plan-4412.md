# Implementation Plan: Per-tenant memory quota (MaxMemoryMb) for cache policies

> **Issue**: [#4412](https://github.com/dlrivada/Encina/issues/4412)
> **Type**: Feature
> **Complexity**: Medium (2 pull requests of 15 and 6 files)
> **Estimated Scope**: about 350 lines of production code, about 700 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #4412 adds the `MaxMemoryMb` member to the per-tenant cache policy of #275 and enforces it from the stored size of each entry. Re-checked against `origin/main` (19b761ffa):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "`CacheTenantPolicy` and the tenant governor of #275 exist" | Not in code yet: `src/Encina.Caching` has no `Tenancy/` folder on main (#275 is a merged plan). This plan starts after #275 PR 2 (usage tracker, governor, behavior hook) and extends its types. |
| "No `ICacheProvider` reports the stored size of an entry" | True: the interface has `GetAsync`, `SetAsync`, `RemoveAsync`, `RemoveByPatternAsync`, `ExistsAsync`, `GetOrSetAsync`, `SetWithSlidingExpirationAsync` and `RefreshAsync` only (`ICacheProvider.cs:52-139`), and `SetAsync` returns `Task`. |
| "After the codec of #274 lands, the codec reports the byte length" | #274 plans `ICacheValueCodec` with `Encode<T>` returning `Either<EncinaError, byte[]>` (#274 PR 1) and registers it from `AddEncinaCaching` (#274 PR 2). The codec runs inside the provider (#274 PR 3), so the behavior never sees the stored bytes: it can only ask the codec for the same encoding. |
| "Memory has no serialization, so a size estimate is needed there" | True: `MemoryCacheProvider.cs:90` stores the object reference with `_cache.Set(key, value, options)` and sets no entry size; `RedisCacheProvider.cs:85-88` stores an STJ JSON string. Hybrid L1 holds objects, L2 is serialized by `HybridCache`. |
| "Behavior identical across the 8 cache providers" | Yes, because accounting is node-local and lives in the behavior, as the entry quota of #275 does. What differs is the meaning of the figure: exact stored length for the Redis family and the planned Memcached once they use the codec, an estimate for Memory and Hybrid L1 (Design Choice 3). |

The plan adds one policy member, byte accounting next to the entry accounting of #275, a small internal `CacheEntrySizer` that asks the codec for the encoded length, two EventIds, and the per-tenant bytes gauge. Nothing is added to the `ICacheProvider` contract.

**Shared pieces and ownership (family cache-patterns-2):** #275 owns `CacheTenantPolicy`, `TenantCacheUsageTracker`, `TenantCacheGovernor`, `ITenantCacheGovernor`, `TenantCacheDecision`, `CacheTenantPolicyLog` (514-519), the `MaxTrackedTenants` bound and `CachePatternMetrics.TenantPolicies.cs`; this plan only adds members to them, so it starts after #275 PR 2. #274 owns `ICacheValueCodec` (PR 1 and PR 2 are the prerequisites). #4413 changes the same #275 files (one more policy member and one more decision field): both are additive, whichever merges second rebases, and the manifest and `PublicAPI.Unshipped.txt` lines are written per pull request. EventIds: this plan's range `CachingTenantMemory` 524-525 in block 500-549 (#4411 520-523, #4413 526-527).

**Affected packages:** `Encina.Caching`, `Encina` (EventId registry). **Provider category:** Caching (8), behavior-level and identical on all 8.

---

## Design Choices

<details>
<summary><strong>1. Where the size of an entry comes from (Dilemma D1, the issue's D1 and D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The behavior asks the public `ICacheValueCodec` (#274) for the encoded length of the `CacheEntry<TResponse>`, only for a tenant whose policy sets `MaxMemoryMb`** | Exact for providers that store the codec's bytes (header and compression included); no provider change; pay-for-what-you-use (no cost without a `MaxMemoryMb` policy) | One extra encode per admitted write of a governed tenant; depends on #274 PR 1 and PR 2 |
| **B) An optional provider capability that reports the stored length from the write** | No double encoding | A new public contract implemented by the Redis family, Hybrid and Memory and forwarded by the decorators (`InstrumentedCacheProvider` in `Encina.OpenTelemetry` today, the recovery decorator of #278 later); Memory still has no stored bytes |
| **C) The behavior serializes with `System.Text.Json` itself** | Works before #274 and without its codec | Approximate once #274 changes the stored encoding (header, compression); the same double encoding as A with no exactness |

### Chosen Option: **A - the codec's length** (recommended, pending the maintainer)

### Rationale

- A matches the issue's primary option and keeps the contract of `ICacheProvider` untouched. Option C wins when the maintainer wants the quota before #274 is merged and accepts an estimate; option B wins when double encoding of large values is the dominant cost and the maintainer accepts a new provider contract.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What happens at the limit (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Deny admission: the entry is not cached (the handler result is still returned), counted with reason `memory`; a single entry larger than the whole quota is always denied** | No I/O on the hot path; the same reaction as the entry quota of #275 | A tenant at the limit stops caching new keys until entries expire |
| **B) Evict the tenant's oldest keys with `ICacheProvider.RemoveAsync` until the new entry fits** | The cache keeps fresh entries | A remove on the write path; with a failing provider the eviction fails too; evicting for a large entry can empty the tenant's cache |

### Chosen Option: **A - deny admission** (recommended, pending the maintainer)

### Rationale

- The two limits of one tenant (entries and bytes) are checked in the same admission call, so the maintainer should answer this together with the same question of #275 (its quota Dilemma): a different answer per limit would be hard to explain. Option B wins for a tenant whose working set changes constantly.
- The limit is per node (documented), exactly like the entry quota; an entry removed by another node's invalidation stays counted until its expiry.
- Class: Dilemma

</details>

<details>
<summary><strong>3. The figure on providers that do not store bytes (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Memory and Hybrid L1 account the codec's encoded length as an estimate of the payload; the documentation says it is not heap usage** | `MaxMemoryMb` works on all 8 providers with one rule | The figure is smaller or larger than the real heap cost of the object graph |
| **B) A policy that sets `MaxMemoryMb` is rejected when the registered provider is Memory or Hybrid** | No estimate is ever presented as a quota | The issue's matrix lists Memory and Hybrid as supported; single-node applications get no memory quota |

### Chosen Option: **A - estimate** (recommended, pending the maintainer)

### Rationale

- The issue's provider matrix asks for the estimate. Option B wins when the application treats the quota as a hard memory bound and wants it refused where it cannot be exact.
- Class: Dilemma

</details>

<details>
<summary><strong>4. An entry whose size cannot be measured is not cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` from the codec (an unsupported type, a payload over the codec's limit) or an exception denies admission for that entry and logs the error code (EventId 525); no tenant is ever left ungoverned** | Missing information never grants the quota; the request is not affected | A type the codec cannot encode is never cached for a tenant that has a memory quota |
| **B) Admit the entry unmeasured** | The cache keeps working | The quota is bypassed exactly when the size is unknown |

### Chosen Option: **A - deny when unmeasurable**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed: when context is missing ... or a lookup fails, the gate denies") and the same fail-closed reading #275 applies to a failed policy lookup. The reaction holds under every option of Design Choice 1 (a failed measurement, whoever performs it, denies). How a denial surfaces to the caller is the cache-bypass behavior of the entry quota; this plan adds no new surface and no error code.
- A cancelled token propagates as `OperationCanceledException` and is never a deny.

</details>

<details>
<summary><strong>5. Units, bounds and keys</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MaxMemoryMb` is an `int?` of mebibytes (1,048,576 bytes), valid from 1 to 1,048,576 (one tebibyte), accounted in `long` bytes; the accounting row of a key is the key `(expiry, bytes)` in the per-tenant map of #275, so a rewrite replaces the previous figure and the key stays the tenant-qualified generated key** | No overflow (`int` times 1,048,576 would overflow, `long` does not); no new table; the keys are as injective as #275's | The bound of one tebibyte is arbitrary but far above any node |
| **B) A `long` number of bytes** | Finer | Not what the issue names (`MaxMemoryMb`) |

### Chosen Option: **A - mebibytes in an `int?`, bytes in a `long`**

### Rationale

- **Class:** Trivial
- **Settled by:** the issue's member name and example (`MaxMemoryMb = 25`), and the validation convention of #275 PR 1 (positive values, a message that names the member and never a tenant id).
- Tenant keys follow #275: the table is keyed by the tenant id verbatim, the implicit single-tenant case uses the reserved segment `default`, and the two modes never share a table; the bound `MaxTrackedTenants` and the lazy purge of idle tenants (which also drops an offboarded tenant's bytes) are #275's and apply unchanged.

</details>

---

## Implementation Phases

### Phase 1: Policy member, accounting and enforcement (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 starts after #275 PR 2 and #274 PR 2 and holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Tenancy/CacheTenantPolicy.cs` | modify (`int? MaxMemoryMb`; `Validate()` rule of Design Choice 5) |
| 2 | `src/Encina.Caching/Tenancy/TenantCacheUsageTracker.cs` | modify (bytes per key, running total per tenant under the tenant's own lock) |
| 3 | `src/Encina.Caching/Tenancy/CacheEntrySizer.cs` | new (internal sealed, over `ICacheValueCodec`) |
| 4 | `src/Encina.Caching/Tenancy/TenantCacheGovernor.cs` | modify (`TryAdmit` takes the size; reports "size required" in the decision) |
| 5 | `src/Encina.Caching/Tenancy/ITenantCacheGovernor.cs` | modify (the `TryAdmit` size parameter and the decision's `RequiresSize` field) |
| 6 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (measure before admission only when `RequiresSize`; one private method) |
| 7 | `src/Encina.Caching/Diagnostics/CacheTenantMemoryLog.cs` | new (EventIds 524-525) |
| 8 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingTenantMemory = (524, 525)`) |
| 9 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (add the range name to the `Encina.Caching` list) |
| 10 | `docs/engineering/eventid-block-registry.md` | modify (row `CachingTenantMemory \| 524-525 \| Caching \| historical`, block row split; read `EventIdRegistryAgreementTests` first) |
| 11 | `tests/Encina.UnitTests/Caching/TenantCacheMemoryQuotaTests.cs` | new |
| 12 | `tests/Encina.UnitTests/Caching/TenantCacheGovernorTests.cs` | modify (the new parameter and decision field) |
| 13 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the policy member, the changed `TryAdmit` line RS0017, the decision field) |
| 14 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the new `EventIdRanges` field) |
| 15 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |

`ITenantCacheGovernor` has one implementer after #275 PR 2 (the internal `TenantCacheGovernor`) and its unit tests; the worker greps `: ITenantCacheGovernor` and `For<ITenantCacheGovernor>` on the then-current `origin/main` and updates every hit in the same pull request (the interface change lands with its implementer, so the build is never broken between pull requests). The size parameter is required, not optional: an optional default of 0 would let a forgotten call site skip the quota.

Tasks:

1. **Modify** `CacheTenantPolicy` and `Validate()`: `MaxMemoryMb` null or 1 to 1,048,576; the message names the member.
2. **Create** `CacheEntrySizer`: `Either<EncinaError, long> Measure<T>(T entry)` calls `ICacheValueCodec.Encode` and returns the array length; it never logs a value or a key.
3. **Modify** the tracker and governor: `TryAdmit(tenantKey, cacheKey, expiry, sizeBytes, policy)` computes `total - previousBytes(cacheKey) + sizeBytes` for a rewrite, denies when it exceeds `MaxMemoryMb * 1,048,576L` (a first entry above the whole quota is denied too), purges expired rows first and subtracts them, and returns the entry-limit and memory-limit denials as distinct reasons (counted with `reason = quota` and `reason = memory`, EventId 524 for memory with the redacted key). The check-and-add stays inside the tenant's lock, so concurrent writes cannot exceed the limit.
4. **Modify** the behavior: when the decision says a size is required, `Measure` the entry first; a `Left` or an exception denies admission and logs EventId 525 by code or exception type (Design Choice 4); otherwise admit with the size.
5. **Tests:** two tenants with different `MaxMemoryMb` never see each other's bytes; a rewrite with a smaller value frees room; expiry frees bytes with `FakeTimeProvider`; the first entry over the quota is denied; a throwing or `Left` codec denies and the handler result is still returned; cancellation propagates; no `MaxMemoryMb` means no encode call (a substitute codec asserts it); the `default` single-tenant mode is governed; the entry quota and the memory quota of one tenant both apply.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
#275 PR 1 and PR 2 are merged (CacheTenantPolicy, TenantCacheUsageTracker, TenantCacheGovernor, ITenantCacheGovernor, the behavior hook) and #274 PR 1 and PR 2
(ICacheValueCodec registered as a singleton). The entry quota counts keys; this issue adds bytes.

TASK:
1. Add MaxMemoryMb to CacheTenantPolicy with its Validate() rule; account bytes per key in TenantCacheUsageTracker and enforce them in TryAdmit.
2. Create CacheEntrySizer over ICacheValueCodec and call it from the behavior only when the tenant's policy has MaxMemoryMb.
3. Create CacheTenantMemoryLog (EventIds 524-525), register CachingTenantMemory = (524, 525) in EventIdRanges.cs, the allocation test and the registry document.
4. Write the unit tests; ask mechanical-fixer for the PublicAPI files and the manifest rows.

KEY RULES:
- Fail closed: an entry whose size cannot be measured is not cached; cancellation propagates, never a deny.
- The check-and-add is atomic per tenant; keys are the tenant-qualified generated keys, never logged (CacheKeyRedactor).
- No cost for tenants without MaxMemoryMb; the size parameter of TryAdmit is required.
- Follow the maintainer's answers to Design Choices 1 to 3 given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Tenancy/TenantCacheGovernor.cs
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
```

</details>

### Phase 2: The bytes gauge, the Redis integration test and documentation (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 6 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.TenantPolicies.cs` | modify (observable gauge `encina.cache.tenant.bytes`; counter reason `memory`) |
| 2 | `tests/Encina.UnitTests/Caching/TenantCacheMemoryMetricsTests.cs` | new (`MeterListener`) |
| 3 | `tests/Encina.IntegrationTests/Infrastructure/Caching/TenantCacheMemoryQuotaIntegrationTests.cs` | new (shared Redis fixture, two tenants) |
| 4 | `docs/features/handler-caching.md` | modify (`docs-writer`: the memory quota in the "Multi-tenant policies" section of #275) |
| 5 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 6 | `changelog.d/4412-cache-tenant-memory-quota.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** the metrics partial: the gauge reports bytes per tenant with `encina.tenant_id` only when `MultiTenancyMarker` is registered (ADR-054 D4), at most `MaxTrackedTenants` series (the bound of #275); no span is added.
2. **Integration test** (`[Trait("Category", "Integration")]`, the shared Redis collection fixture, never its own container): tenant A at its byte quota stops caching while tenant B keeps caching; the denied request still returns the handler's value. With #274 PR 3 merged the test asserts the accounted size equals the stored length of the key read back from Redis; before that it asserts the quota only.
3. **Documentation:** the section names that limits are per node, that the figure is an estimate on Memory and Hybrid L1, and that a type the codec cannot encode is not cached for such a tenant; it cites tests by name and types no figure.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
PR 1 is merged: MaxMemoryMb is enforced and EventIds 524-525 exist.

TASK:
Add the encina.cache.tenant.bytes gauge and the memory reason to the metrics partial, the metrics unit test and the Redis integration test; ask docs-writer for the handler-caching.md addition and mechanical-fixer for the manifest rows and the fragment.

KEY RULES:
- The tenant tag appears only with multi-tenancy on (ADR-054 D4); series are bounded by MaxTrackedTenants.
- The integration test uses the shared Redis collection fixture; no sleeps (FakeTimeProvider).

REFERENCE FILES:
- src/Encina.Caching/Diagnostics/CachePatternMetrics.TenantPolicies.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
- docs/features/handler-caching.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Fail-closed gates; cancellation is never a deny; time from `TimeProvider` |
| AGENTS.md section 5 | Multi-tenancy is ◐ for caching; behavior-level feature identical on all 8 providers |
| ADR-054 D4 | Tenant tag on telemetry only with multi-tenancy on |
| ADR-021 | Range registered before use |
| NCache regions and Hazelcast map size policies (named in the issue) | Comparable per-region or per-partition memory caps |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| Tenant governor, usage tracker and policy of #275 | `src/Encina.Caching/Tenancy/` (after #275 PR 2) | Extended, not redefined |
| `ICacheValueCodec` of #274 | `src/Encina.Caching/Serialization/` (after #274 PR 1) | The encoded length |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | Redacted keys in logs |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared container for the integration test |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs` | New range and assembly map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingTenantMemory`, block 500-549 of the plan batch registry, family cache-patterns-2) | 524-525 | 524 tenant memory quota reached (redacted key), 525 entry size not measurable and not cached (error code or exception type). Verified free: the merged plans use 500-519 in this block and `docs/plans` claims no id in 520-549 (the 520-536 lines in the #274 and #275 plans are superseded by the 30350 and 30400 slots of #276 and #278); #4411 uses 520-523 and #4413 526-527 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 2 | 9 | New: sizer, log; modified: policy, tracker, governor, interface, behavior, metrics partial, `EventIdRanges.cs`, two PublicAPI files |
| Tests | 3 | 3 | New: quota, metrics and integration tests; modified: governor tests, allocation test, registry document |
| Documentation | 0 | 1 | `docs/features/handler-caching.md` |
| Configuration | 1 | 2 | One changelog fragment; the manifest is edited in both pull requests |
| **Total** | **6** | **15** | 2 pull requests of 15 and 6 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4412 adds MaxMemoryMb to the per-tenant cache policy of #275. Accounting is node-local and lives in the
caching pipeline behavior, so the 8 cache providers behave the same; the size comes from the codec of #274.

IMPLEMENTATION OVERVIEW:
1. PR 1: MaxMemoryMb, byte accounting in the tracker, enforcement in TryAdmit, CacheEntrySizer, EventIds 524-525.
2. PR 2: the bytes gauge, the metrics and Redis integration tests, documentation.

KEY PATTERNS:
- Fail closed: an unmeasurable entry is not cached; cancellation propagates; no tenant under multi-tenancy never reaches the governor (existing bypass).
- Check-and-add is atomic per tenant; keys are tenant-qualified and redacted in logs; implicit single tenant uses the reserved segment default.
- Telemetry tags the tenant only with multi-tenancy on (ADR-054 D4); no EncinaError.Message in logs.
- Follow the maintainer's answers to Design Choices 1 to 3 given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Tenancy/TenantCacheGovernor.cs
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends the tenant governor of #275 at the behavior level; no provider change. |
| 2 | OpenTelemetry | ✅ Include | Gauge `encina.cache.tenant.bytes` and the `memory` reason; tenant tag only with multi-tenancy on (ADR-054 D4); spans belong to #723 (open). |
| 3 | Structured Logging | ✅ Include | `CacheTenantMemoryLog`, EventIds 524-525 in `CachingTenantMemory`. |
| 4 | Health Checks | ❌ N/A | In-process accounting has no backend to check. |
| 5 | Validation | ✅ Include | `MaxMemoryMb` from 1 to 1,048,576 in `CacheTenantPolicy.Validate()`, run by the options validator and on every provider answer (#275). |
| 6 | Resilience | ❌ N/A | In-process accounting; a failed measurement denies the entry (fail closed) rather than retrying. |
| 7 | Distributed Locks | ❌ N/A | Node-local state, the same per-node limit as #275. |
| 8 | Transactions | ❌ N/A | Cache operations are independent of units of work. |
| 9 | Idempotency | ❌ N/A | Accounting a key twice replaces its figure; it never doubles it. |
| 10 | Multi-Tenancy | ✅ Include | The feature itself: per-tenant bytes, fail closed for a missing tenant (existing bypass), reserved `default` segment, tag per ADR-054 D4. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped policies follow the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | An operational limit, not a compliance event; policy-change audit is #4414 (batch B). |

**Deferred items:** module-scoped policies (#746, open). Load test and benchmark are justified skips: the accounting adds one lock and one dictionary operation to a path #275 already governs, and #275 tracks the governor load test in its own plan.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #275 PR 1 and PR 2 (policy, tracker, governor, behavior hook; PR 3 only adds duration and rate limiting) | Open | PR 1 of this plan needs them |
| #274 PR 1 and PR 2 (`ICacheValueCodec` and its registration) | Open | PR 1 needs them; PR 2's exact-length assertion needs #274 PR 3 and is conditional |
| Block 500-549 of the EventId registry | ✅ Assigned | 524-525 free |
| Maintainer answers to Design Choices 1 to 3 | Pending | Asked when work starts, together with the quota Dilemma of #275 |

Coverage targets (per flag, set in the pull request that adds each file; every row's test file is in the same pull request):

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Tenancy/CacheEntrySizer.cs` | 100 | 0 | 0 | 0 | Two branches (a length, a `Left`) driven by a substitute codec; internal, no `InternalsVisibleTo` for the guard project |
| `Encina.Caching/Diagnostics/CacheTenantMemoryLog.cs` | 90 | 0 | 0 | 0 | Generated bodies run in the quota tests |
| `Encina.Caching/Tenancy/CacheTenantPolicy.cs`, `TenantCacheUsageTracker.cs` | keep (100 in #275) | 0 | 0 | 0 | The targets #275 sets are kept; the new branches are tested in PR 1 |
| `Encina.Caching/Tenancy/TenantCacheGovernor.cs`, `ITenantCacheGovernor.cs` | keep (95; 40 integration) | 0 | 0 | keep | Kept; the Redis test of PR 2 adds a path to the integration flag without raising its target |
| `Encina.Caching/Diagnostics/CachePatternMetrics.TenantPolicies.cs` | keep (90) | 0 | 0 | 0 | The gauge callback and the reason branch run in the metrics test of PR 2 |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | keep (98 on main today) | keep (20) | 0 | 0 | The existing manifest values are never lowered; the new private method is unit tested in PR 1 |

The worker measures every flag with `coverage-report.cs` per pull request and reports the numbers; no target is lowered (`target-ratchet`).

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 3) when work starts, together with the quota Dilemma of #275, records the answers under `## Maintainer Decisions`, then briefs the two pull requests in order.
2. No issue file is written: the deferred module dimension is #746 (open) and the policy audit is #4414.
