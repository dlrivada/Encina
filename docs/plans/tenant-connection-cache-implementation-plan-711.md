# Implementation Plan: Cache for multi-tenancy connection string resolution

> **Issue**: [#711](https://github.com/dlrivada/Encina/issues/711)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 3 packages touched, 3 pull requests)
> **Estimated Scope**: ~400-600 lines of production code + ~1,800 lines of tests

---

## Summary

Reduce the per-request cost of resolving a tenant's connection string in database-per-tenant deployments, and decide whether the pre-built tenant filter clause is worth caching too.

What the code does today (verified):

- Six provider packages each ship a concrete `TenantConnectionFactory` and its own `ITenantConnectionFactory` interface: `Encina.ADO.SqlServer|PostgreSQL|MySQL` and `Encina.Dapper.SqlServer|PostgreSQL|MySQL` (`src/<package>/Tenancy/TenantConnectionFactory.cs`, registered by `TryAddScoped<ITenantConnectionFactory, TenantConnectionFactory>()` in each `TenancyServiceCollectionExtensions.cs`). `GetConnectionStringAsync()` resolves the current tenant and returns `TenantInfo.ConnectionString` for database-per-tenant tenants, the configured default otherwise. The ADO classes read the tenant id from `ITenantProvider` and call `ITenantStore.GetTenantAsync(tenantId)` themselves; the Dapper classes call `ITenantProvider.GetCurrentTenantAsync`, whose `DefaultTenantProvider` (`src/Encina.Tenancy/Core/DefaultTenantProvider.cs`, line 56) calls the same store. An application-supplied `ITenantProvider` that does not go through the store is not helped by a store-level cache. The generic `ITenantConnectionFactory<TConnection>` (`src/Encina.Tenancy/Data/`) has only an abstract base, `TenantConnectionFactoryBase<TConnection>`, with no concrete implementation in `src/`.
- **The lookup that costs is `ITenantStore.GetTenantAsync`**: the shipped `InMemoryTenantStore` is a dictionary, but a production store is a database or a service, and it is called on every scoped connection.
- The tenant filter is `GetTenantFilterClause()` / `GetTenantFilter()` in `TenantAwareFunctionalRepositoryADO` and `TenantAwareFunctionalRepositoryDapper`: an interpolated string over the entity mapping (for example `[TenantId] = @TenantId`), cheap by inspection and still to be measured.
- Entity Framework Core (model-level query filters, built once) and MongoDB have no connection factory.

Two security facts shape the design: `TenantInfo.ConnectionString` is a secret (`AGENTS.md` §3), and the factories return the **default** connection string when the tenant is not found (a defect recorded as a finding) or is not database-per-tenant, so a decorator that only sees the returned string cannot tell a resolved tenant from a fallback (Design Choice 3).

Provider category: **Database (10)**, with six packages carrying code today; EF Core x3 and MongoDB are not applicable. SQLite is out of the matrix. Affected packages: `Encina.Tenancy` (new cache component), the six ADO/Dapper packages (registration and tests), possibly none of their factory classes (D3).

---

## Design Choices

<details>
<summary><strong>1. Where resolved connection strings are cached (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICacheProvider` (distributed)** | Resolution done once for the fleet; useful when resolution calls an external service and many instances start together | Stores a secret in plaintext outside the process; needs encryption or access control on the cache (a design of its own); one `ICacheProvider` per application |
| **B) `IMemoryCache` (local)** | The secret stays in process memory; no network hop | One resolution per instance and tenant; instances disagree until the TTL expires; adds the `Microsoft.Extensions.Caching.Memory` reference to `Encina.Tenancy` |
| **C) Selectable by option, default local** | Both shapes possible | The distributed shape still needs the secret-protection design before it can ship |

### Chosen Option: **B — `IMemoryCache`, local only** (recommended, pending the maintainer)

### Rationale

A secret does not leave the process (`AGENTS.md` §3, #851), and the cost this feature removes is the per-request store lookup, which a per-instance cache removes. A wins when resolution calls an external service and instances start together, and the application accepts encrypting the cached value; C wins when both shapes are real. If A or C is chosen, an ADR on how the secret is protected in the shared cache comes first (ADR number assigned when the ADR pull request opens).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Whether to cache the tenant filter clause (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Build it now: a bounded cache per entity type, with a benchmark proving the build cost** | Wins if the benchmark shows a measurable share of request time | The clause is an interpolated string over mapping fields and the tenant id comes from `ITenantProvider`; a static cache must be bounded, tested for all six providers, and can serve a stale mapping |
| **B) Drop it from this issue; keep a benchmark of the clause build to confirm** | No static state, no six-provider duplication; the connection-string work stands alone | If the benchmark surprises, the work comes back as a new issue |

### Chosen Option: **B — drop the filter cache, measure first** (recommended, pending the maintainer)

### Rationale

B wins if the clause is cheap, which reading `GetTenantFilter()` suggests; A wins only if the measurement shows a share of request time worth a static cache. The benchmark runs in the testing phase for ADO and Dapper (SqlServer is enough to decide), and the result is reported in the pull request. EF Core gains nothing (model-level filters are built once).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where the cache sits: factory decorators or the tenant store (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Decorate the six `ITenantConnectionFactory` interfaces (and the generic one) sharing an internal cache component in `Encina.Tenancy`** | Matches the issue's `CachedTenantConnectionFactory`; scope limited to connection strings | Seven thin decorators; the returned string cannot say whether the tenant was resolved or fell back to the default, so a tenant created after a "not found" lookup would keep the default database until the TTL expires (cross-tenant data placed in the default database) unless the default string is never cached |
| **B) Decorate `ITenantStore` once (`CachedTenantStore` in `Encina.Tenancy`), caching only non-null `TenantInfo`** | One class serves all six factories, the generic base and every other store consumer; a missing tenant is never cached; provider coherence by construction (no per-provider code) | Caches the whole `TenantInfo` (the secret is already in the store's memory); the decorator must wrap the application's own store, so registration order needs a startup check |
| **C) Cache inside each factory class** | No decorators | Six copies of the same logic in the factories; edits the six classes |

### Chosen Option: **B — a `CachedTenantStore` decorator** (recommended, pending the maintainer)

### Rationale

The store lookup is the cost, and caching there removes the fallback hazard because `null` is never stored. B wins when the factories are the only consumers that matter and the store is the real cost; A wins when the maintainer wants the cache visible at the factory contract named in the issue and accepts the "never cache the default" rule; the issue's acceptance wording ("`CachedTenantConnectionFactory` caches connection strings per tenant") would change to a store-level equivalent. Invalidation is explicit (`ITenantCacheInvalidator.InvalidateAsync(tenantId)`) plus the TTL (default 10 minutes).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Errors, secrets and time</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt-in; only a found tenant is cached; a store `Left`-equivalent or exception falls through to the inner store; the connection string never reaches a log, an `Activity` tag, a metric tag or a health result; TTL by the cache entry's absolute expiration with `TimeProvider`** | Follows the project rules; a broken cache costs performance only | Staleness window equals the TTL unless invalidated |
| **B) Always on** | No registration step | Breaks "every feature opt-in" |
| **C) Cache `null` results too (negative caching)** | Fewer store calls for unknown tenants | A tenant created later is invisible until the TTL; the fallback hazard of D3 option A |

### Chosen Option: **A — opt-in, found tenants only, secret never logged**

### Rationale

`ITenantStore.GetTenantAsync` returns `ValueTask<TenantInfo?>` (no `Either`), so "errors" are exceptions: the decorator does not catch the inner store's exceptions (they propagate as today) and treats its own cache failures as misses.

**Class:** Settled

**Settled by:** `AGENTS.md` §1 (every feature opt-in), §3 (secrets never reach logs or plaintext storage; time from `TimeProvider`; errors not swallowed) and #851.

</details>

<details>
<summary><strong>5. Provider scope and tests</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No provider-specific code; integration tests for the six ADO/Dapper packages that use a real database and prove each factory resolves through the cached store; EF Core x3 and MongoDB marked not applicable in the matrix** | The 10-provider rule is satisfied by coverage of the six that have a factory, with a stated reason for the other four | Six integration test classes |
| **B) Six provider-specific decorators and tests** | Per-provider isolation | Duplicated code, the very duplication D3 option B avoids |
| **C) Test one provider only** | Cheap | Violates `AGENTS.md` §9 (all providers of a provider-dependent feature) |

### Chosen Option: **A — one shared cache, six integration test classes**

### Rationale

Tests use the shared collection fixtures (`ADO-SqlServer`, `Dapper-PostgreSQL`, ...) and never create a per-class container.

**Class:** Settled

**Settled by:** `AGENTS.md` §5 (database features implemented and tested consistently across the providers that have the code) and §9 (shared collection fixtures; integration tests of database features are required, never a `.md` justification).

</details>

---

## Implementation Phases

### Phase 1: Core — options, validator and invalidation contract

<details>
<summary>Tasks</summary>

1. `TenantCacheOptions` (public sealed) in `src/Encina.Tenancy/Configuration/`: `Ttl` (default 10 minutes), `MaxEntries` (default 10,000, to bound memory), no secret properties.
2. `TenantCacheOptionsValidator : IValidateOptions<TenantCacheOptions>`: `Ttl` greater than zero, `MaxEntries` greater than zero.
3. `ITenantCacheInvalidator` (public): `ValueTask<Either<EncinaError, Unit>> InvalidateAsync(string tenantId, CancellationToken)` and `InvalidateAllAsync(CancellationToken)`.
4. `Encina.Tenancy.csproj`: add the `Microsoft.Extensions.Caching.Memory` package reference (version already managed centrally).
5. PublicAPI lines in `src/Encina.Tenancy/PublicAPI.Unshipped.txt`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Tenancy (src/Encina.Tenancy) holds ITenantStore, TenantInfo, TenancyOptions and the generic connection factory abstractions. It has no cache today.
TASK: Add TenantCacheOptions, its validator, the ITenantCacheInvalidator contract, the package reference and the PublicAPI lines.
KEY RULES: No secret in options; ROP return types; XML docs on every public member; zero warnings.
REFERENCE FILES: src/Encina.Tenancy/Configuration/TenancyOptions.cs, src/Encina.Tenancy/Data/TenantConnectionOptions.cs, src/Encina.Tenancy/Abstractions/ITenantStore.cs, src/Encina.Tenancy/Encina.Tenancy.csproj.
```

</details>

### Phase 2: The cached store decorator

<details>
<summary>Tasks</summary>

1. `CachedTenantStore : ITenantStore, ITenantCacheInvalidator` (internal sealed or public per the maintainer's D3 answer) in `src/Encina.Tenancy/Stores/`: `GetTenantAsync` reads `IMemoryCache` keyed by `encina:tenant:info:{tenantId}` (case-insensitive like `InMemoryTenantStore`), on a miss calls the inner store and stores a non-null result with `Ttl` and the entry size; `null` is returned and not stored. `GetAllTenantsAsync` and `ExistsAsync` pass through (not hot).
2. A cache failure never fails the call: the inner store answers. An inner store exception propagates unchanged.
3. `InvalidateAsync` removes the entry; `InvalidateAllAsync` clears through a `CancellationChangeToken` shared by all entries.
4. The `IMemoryCache` is the host's registered instance when present, otherwise a private `MemoryCache` with `SizeLimit = MaxEntries` so the decorator never grows without bound.
5. If the maintainer chooses D3 option A instead: the same cache component is used by seven thin decorators and the default connection string is never cached.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: ITenantStore.GetTenantAsync returns ValueTask<TenantInfo?>; the six factories fall back to the default connection string when it returns null, so null must never be cached (Dilemma D3).
TASK: Implement CachedTenantStore and the invalidation operations as described; keep each method at cyclomatic complexity of 5 or less.
KEY RULES: Only non-null TenantInfo is cached; the connection string never appears in a log, tag, metric or ToString; a cache failure is a miss; the inner store's exceptions propagate; bounded memory.
REFERENCE FILES: src/Encina.Tenancy/Stores/InMemoryTenantStore.cs, src/Encina.Tenancy/Abstractions/TenantInfo.cs, src/Encina.ADO.SqlServer/Tenancy/TenantConnectionFactory.cs, src/Encina.Caching.Memory/MemoryCacheProvider.cs.
```

</details>

### Phase 3: Configuration, DI & registration

<details>
<summary>Tasks</summary>

1. `AddEncinaTenantCaching(this IServiceCollection, Action<TenantCacheOptions>? configure = null)` in `src/Encina.Tenancy/ServiceCollectionExtensions.cs`: options and validator with `ValidateOnStart`; `AddMemoryCache()`; replaces the `ITenantStore` descriptor with a factory that builds the inner store from the descriptor registered at call time and wraps it; registers `ITenantCacheInvalidator`.
2. Startup check: a hosted validation (or `IValidateOptions`) that fails when the resolved `ITenantStore` is not the cached decorator, so a store registered after this call cannot silently bypass the cache.
3. DI tests with `ValidateOnBuild` and `ValidateScopes` for the six `AddEncina*WithTenancy` registrations combined with `AddEncinaTenantCaching`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The six provider packages register ITenantConnectionFactory with TryAddScoped in their TenancyServiceCollectionExtensions; ITenantStore is registered by AddEncinaTenancy or by the application.
TASK: Add AddEncinaTenantCaching with the descriptor-wrapping registration, the startup check and the DI tests.
KEY RULES: Registration completeness: every option type and dependency the decorator resolves is registered; the check fails loudly when the decorator is not outermost; no static state.
REFERENCE FILES: src/Encina.Tenancy/ServiceCollectionExtensions.cs, src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs, src/Encina.Dapper.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs.
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Caching: the feature (local cache, D1). Multi-tenancy: the key is per tenant; an unknown tenant is never cached.
2. Validation: options validated at startup. Observability and logging: Phase 5.
3. Health checks, resilience, locks, transactions, idempotency, module isolation, audit: not applicable (matrix below); `TenantHealthCheck` keeps working because it resolves the same `ITenantStore`.
4. Findings recorded as issue files, not fixed here: `TenantInfo` prints its connection string through its record `ToString()`; the factories fall back to the default connection string for an unknown tenant (fail open).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: See the matrix at the end of this plan.
TASK: Verify TenantHealthCheck and the tenant resolvers still resolve the decorated store; add no other integration.
KEY RULES: Do not change TenantConnectionFactory behavior for unknown tenants in this feature; that is a separate issue.
REFERENCE FILES: src/Encina.Tenancy/Health/TenantHealthCheck.cs, src/Encina.Tenancy/ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. Extend `TenancyMetrics` (`src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs`, meter `Encina`) with a counter `encina.tenancy.cache.requests_total` and the tag `outcome` = `hit`/`miss`/`error`; no tenant id or connection string in tags.
2. Extend `TenancyLog` with `[LoggerMessage]` ids 1805 cache hit, 1806 cache miss and store, 1807 cache failure (exception type only), 1808 entry invalidated, 1809 all entries invalidated, 1810 startup check failed; inside the registered range `Tenancy` (1800-1899; 1800-1804 are used). The tenant id is logged only if the existing messages already log it; the connection string never.
3. Run the EventId architecture tests; PublicAPI lines if new public members.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: TenancyMetrics and TenancyLog already exist; EventIds 1800-1804 are in use inside the registered range 1800-1899.
TASK: Add the counter, the outcome tag and the log messages 1805 to 1810.
KEY RULES: Ids packed sequentially; no connection string or secret anywhere; failures logged by exception type through ForLogging(), never the message.
REFERENCE FILES: src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs, src/Encina.Tenancy/Diagnostics/TenancyLog.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

### Phase 6: Testing — 7 test types

<details>
<summary>Tasks</summary>

Provider matrix for tests (`AGENTS.md` §5; EF Core and MongoDB have no factory, so they are not applicable):

| Provider | Integration test | Fixture collection |
|----------|------------------|--------------------|
| ADO SqlServer / PostgreSQL / MySQL | factory resolves through `CachedTenantStore`; second call does not reach the store | `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL` |
| Dapper SqlServer / PostgreSQL / MySQL | same | `Dapper-SqlServer`, `Dapper-PostgreSQL`, `Dapper-MySQL` |
| EF Core x3, MongoDB | not applicable | none |

Proposed per-flag targets for the new files (manifest `.github/coverage-manifest/Encina.Tenancy.json`; measured values replace these):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `Stores/CachedTenantStore.cs` | 95 | 40 | 60 | 60 | 70 |
| `Configuration/TenantCacheOptions.cs` and validator | 100 | 40 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` (addition) | 95 | 45 | 0 | 0 | 0 |

1. **Unit** (`tests/Encina.UnitTests/Tenancy/`): hit, miss, null not cached, TTL expiry with `FakeTimeProvider`, invalidation, bound, cache failure falls through, inner exception propagates, startup check; a test that `ToString()` of options and a captured log contain no connection string.
2. **Guard**: constructor and public members.
3. **Contract**: the decorator honours `ITenantStore` exactly like the inner store for found and missing tenants.
4. **Property**: for any sequence of get and invalidate operations, a cached read equals the inner store's current value unless the TTL has not expired and no invalidation occurred; a missing tenant is never returned from the cache.
5. **Integration**: the six classes of the table above, `[Collection]` fixtures, `ClearAllDataAsync` in `InitializeAsync`, `[Trait("Category", "Integration")]`, `[Trait("Database", "...")]`.
6. **Load**: `.md` justification (the cache is a thin wrapper over `IMemoryCache`); the benchmark covers the cost.
7. **Benchmark**: cached versus uncached resolution, and the cost of building the tenant filter clause in ADO and Dapper (decides D2), in the existing `Encina.ADO.SqlServer.Benchmarks` and `Encina.Dapper.SqlServer.Benchmarks` projects.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Coverage is per flag against .github/coverage-manifest/Encina.Tenancy.json; targets never go down; database features need real-database integration tests with the shared collection fixtures.
TASK: Write the unit, guard, contract, property, six integration classes, the benchmark additions and the load justification; measure every flag and the CRAP table.
KEY RULES: Never IClassFixture or new fixtures; never dispose the shared connection; Shouldly; FakeTimeProvider, no Thread.Sleep; no connection string in any assertion message.
REFERENCE FILES: tests/Encina.UnitTests/Tenancy, tests/Encina.IntegrationTests (ADO and Dapper tenancy tests), tests/Encina.BenchmarkTests/Encina.ADO.SqlServer.Benchmarks, docs/testing/integration-tests.md.
```

</details>

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs with an example of `AddEncinaTenantCaching` and the staleness contract (a changed tenant connection string is picked up after the TTL or `InvalidateAsync`).
2. `src/Encina.Tenancy/README.md` section and a multi-tenancy guide section (via `docs-writer`): when to use it, why the cache is local, how to invalidate.
3. `changelog.d/711-tenant-connection-cache.added.md`.
4. ADR only if D1 chooses a shared cache for secrets (ADR number assigned when the ADR pull request opens).
5. PublicAPI files; zero warnings; all tests green; every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Code and tests exist; docs are English, junior-readable, no hand-typed coverage figures.
TASK: Write the XML docs, README section, guide section and changelog fragment through the specialists that own them.
KEY RULES: Never edit CHANGELOG.md; state the staleness window and the local-only reason plainly; never show a real connection string.
REFERENCE FILES: src/Encina.Tenancy/README.md, changelog.d/README.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` §3 and #851 | Secrets never reach logs or plaintext storage | Drives D1 (local cache) |
| `AGENTS.md` §5 and §9 | Database features: all providers with code, shared collection fixtures | Six packages carry the factory |
| ADR-021 / `AGENTS.md` §7 | EventIds | 1805-1810 inside the registered `Tenancy` range |
| ADR-024 | SQLite is out of the matrix | Not covered |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ITenantStore`, `InMemoryTenantStore`, `TenantInfo` | `src/Encina.Tenancy/Abstractions/`, `src/Encina.Tenancy/Stores/` | The decorated lookup and the cached value |
| `TenantConnectionFactory` x6 and `ITenantConnectionFactory` x6 | `src/Encina.ADO.*/Tenancy/`, `src/Encina.Dapper.*/Tenancy/` | Consumers of the cache; unchanged under D3 option B |
| `TenantConnectionFactoryBase<TConnection>`, `ITenantConnectionFactory<TConnection>` | `src/Encina.Tenancy/Data/` | Also benefit from the store cache |
| `TenancyMetrics`, `TenancyLog`, `TenantHealthCheck` | `src/Encina.Tenancy/Diagnostics/`, `src/Encina.Tenancy/Health/` | Extended / unchanged |
| `TenantAwareFunctionalRepositoryADO`, `TenantAwareFunctionalRepositoryDapper` | `src/Encina.ADO.SqlServer/Tenancy/`, `src/Encina.Dapper.SqlServer/Tenancy/` | Where the filter clause is built (D2 measurement) |
| Shared fixtures `ADO-*`, `Dapper-*` | `tests/Encina.IntegrationTests` | Integration collections |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Tenancy` | 1805-1810 inside the registered 1800-1899 | 1800-1804 used; 1811-1899 stay free |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 5 | options, validator, invalidator contract, `CachedTenantStore`, registration additions |
| Edited production | 4 | csproj, `TenancyMetrics.cs`, `TenancyLog.cs`, PublicAPI |
| Coverage manifest | 1 | `Encina.Tenancy.json` |
| Tests | 15 | unit x3, guard x1, contract x1, property x1, integration x6, benchmark x2, load justification |
| Documentation | 4 | README section, guide section, changelog, XML docs (in code) |
| **Total** | **~29** | Three pull requests: (1) Phases 1 to 5 with unit and guard tests, (2) contract, property, integration, benchmarks, (3) docs |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Multi-tenancy lives in Encina.Tenancy; six ADO.NET and Dapper packages (SqlServer, PostgreSQL, MySQL) each ship a TenantConnectionFactory that resolves a tenant's connection string through ITenantStore. EF Core and MongoDB have no factory. Secrets never reach logs or plaintext storage; zero warnings; no backward compatibility.

IMPLEMENTATION OVERVIEW: Phase 1 options, validator, invalidator contract, package reference; Phase 2 CachedTenantStore (non-null tenants only, bounded, local IMemoryCache); Phase 3 AddEncinaTenantCaching with descriptor wrapping and a startup check; Phase 4 cross-cutting evaluation and the two findings left as issues; Phase 5 counter and log ids 1805 to 1810; Phase 6 tests including six real-database integration classes and the benchmark that decides D2; Phase 7 docs. Dilemmas D1 (where), D2 (filter half) and D3 (placement) are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: IMemoryCache with an absolute expiration and size limit; null never cached; connection string never in logs, tags, metrics or ToString; TimeProvider for clocks; TryAdd and descriptor wrapping with a startup check; shared [Collection] fixtures; Shouldly; CRAP at or below 10 on changed methods.

REFERENCE FILES: src/Encina.Tenancy/Abstractions/ITenantStore.cs, src/Encina.Tenancy/Stores/InMemoryTenantStore.cs, src/Encina.ADO.SqlServer/Tenancy/TenantConnectionFactory.cs, src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs, src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs, tests/Encina.UnitTests/Tenancy.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | The feature: a local cache over the tenant store lookup. |
| 2 | OpenTelemetry | ✅ | One counter with an `outcome` tag on the shared meter; no tenant id or connection string in tags. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 1805-1810 in the registered Tenancy range; never the connection string. |
| 4 | Health Checks | ❌ | `TenantHealthCheck` is unchanged; the cache adds no dependency to check. |
| 5 | Validation | ✅ | `IValidateOptions<TenantCacheOptions>` at startup; the decorator-outermost check. |
| 6 | Resilience | ❌ | A cache failure falls back to the inner store; no external call is added. |
| 7 | Distributed Locks | ❌ | Concurrent misses may call the store twice, which is harmless. |
| 8 | Transactions | ❌ | No atomic multi-operation guarantee is needed. |
| 9 | Idempotency | ❌ | A lookup is a read. |
| 10 | Multi-Tenancy | ✅ | The key is per tenant; an unknown tenant is never cached. |
| 11 | Module Isolation | ❌ | Tenant connections are not module-scoped. |
| 12 | Audit Trail | ❌ | A cached lookup is not an auditable business operation. |

---

## Prerequisites & Dependencies

- **Not blocking**: no other issue. The module and key work of #746 does not touch this feature.
- **Findings recorded as issue files** (see the run report): `TenantInfo` is a record whose synthesized `ToString()` prints `ConnectionString`; the six `TenantConnectionFactory` classes return the default connection string when the tenant is not found (a lookup failure that opens the default database instead of failing closed).
- **Measurement first**: the benchmark of the filter clause (D2) and of cached versus uncached resolution runs before the pull request is marked ready.

---

## Next Steps

1. Ask the maintainer D1, D2 and D3 when work on #711 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: Phases 1 to 5 with unit and guard tests (at most 15 files).
3. Pull request 2: contract and property tests, the six integration classes, benchmarks, manifest entries.
4. Pull request 3: README, guide section, changelog fragment.
