# Implementation Plan: Cache for multi-tenancy connection string resolution

> **Issue**: [#711](https://github.com/dlrivada/Encina/issues/711)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 3 packages touched, 5 pull requests)
> **Estimated Scope**: ~400-600 lines of production code + ~1,800 lines of tests, in five pull requests of 3 to 10 files (see Estimated File Count)

---

## Summary

Reduce the per-request cost of resolving a tenant's connection string in database-per-tenant deployments, and decide whether the pre-built tenant filter clause is worth caching too.

What the code does today (verified):

- Six provider packages each ship a concrete `TenantConnectionFactory` and its own `ITenantConnectionFactory` interface: `Encina.ADO.SqlServer|PostgreSQL|MySQL` and `Encina.Dapper.SqlServer|PostgreSQL|MySQL` (`src/<package>/Tenancy/TenantConnectionFactory.cs`, registered by `TryAddScoped<ITenantConnectionFactory, TenantConnectionFactory>()` in each `TenancyServiceCollectionExtensions.cs`). `GetConnectionStringAsync()` resolves the current tenant and returns `TenantInfo.ConnectionString` for database-per-tenant tenants, the configured default otherwise. The ADO classes read the tenant id from `ITenantProvider` and call `ITenantStore.GetTenantAsync(tenantId)` themselves; the Dapper classes call `ITenantProvider.GetCurrentTenantAsync`, whose `DefaultTenantProvider` (`src/Encina.Tenancy/Core/DefaultTenantProvider.cs`, line 56) calls the same store. An application-supplied `ITenantProvider` that does not go through the store is not helped by a store-level cache. The generic `ITenantConnectionFactory<TConnection>` (`src/Encina.Tenancy/Data/`) has only an abstract base, `TenantConnectionFactoryBase<TConnection>`, with no concrete implementation in `src/`.
- **The lookup that costs is `ITenantStore.GetTenantAsync`**: the shipped `InMemoryTenantStore` is a dictionary, but a production store is a database or a service, and it is called on every scoped connection.
- The tenant filter is `GetTenantFilterClause()` / `GetTenantFilter()` in `TenantAwareFunctionalRepositoryADO` and `TenantAwareFunctionalRepositoryDapper`: an interpolated string over the entity mapping (for example `[TenantId] = @TenantId`), cheap by inspection and still to be measured.
- Entity Framework Core (model-level query filters, built once) and MongoDB have no connection factory.

Two security facts shape the design: `TenantInfo.ConnectionString` is a secret (`AGENTS.md` §3; `TenantInfo` marks it `[property: JsonIgnore]` and masks it in `ToString()` since #3618, PR #3752, so the record no longer prints it), and the factories return the **default** connection string when the tenant is not found or is not database-per-tenant (a fail-open defect tracked as #3617 with the children #3674 ADO, #3675 Dapper, #3676 EF Core and MongoDB and #3686, all open), so a decorator that only sees the returned string cannot tell a resolved tenant from a fallback (Design Choice 3).

Provider category: **Database (10)**, with six packages carrying code today; EF Core x3 and MongoDB are not applicable. SQLite is out of the matrix. Affected packages: `Encina.Tenancy` (new cache component), the six ADO/Dapper packages (registration and tests), possibly none of their factory classes (D3).

---

## Design Choices

<details>
<summary><strong>1. Where resolved connection strings are cached (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICacheProvider` (distributed)** | Resolution done once for the fleet; useful when resolution calls an external service and many instances start together | Stores a secret in plaintext outside the process; needs encryption or access control on the cache (a design of its own); one `ICacheProvider` per application |
| **B) A local in-process cache: a private `MemoryCache` owned by the decorator (never the host's shared `IMemoryCache`, Design Choice 9)** | The secret stays in process memory; no network hop | One resolution per instance and tenant; instances disagree until the TTL expires; adds the `Microsoft.Extensions.Caching.Memory` reference to `Encina.Tenancy` |
| **C) Selectable by option, default local** | Both shapes possible | The distributed shape still needs the secret-protection design before it can ship |

### Chosen Option: **B — local only, a private `MemoryCache`** (recommended, pending the maintainer)

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
| **A) Decorate the six `ITenantConnectionFactory` interfaces (and the generic one) sharing an internal cache component in `Encina.Tenancy`** | Matches the issue's `CachedTenantConnectionFactory`; scope limited to connection strings | Seven thin decorators; the returned string cannot say whether the tenant was resolved or fell back to the default (today's fail-open behavior, tracked as #3617, #3674, #3675, #3676 and #3686), so a tenant created after a "not found" lookup would keep the default database until the TTL expires (cross-tenant data placed in the default database) unless the default string is never cached; the planned fixes of #3674 and #3675 edit the same six factory files |
| **B) Decorate `ITenantStore` once (`CachedTenantStore` in `Encina.Tenancy`), caching only non-null `TenantInfo`** | One class serves all six factories, the generic base and every other store consumer; a missing tenant is never cached; provider coherence by construction (no per-provider code) | Caches the whole `TenantInfo` (the secret is already in the store's memory); the decorator must wrap the application's own store, so registration order matters (Design Choice 10) |
| **C) Cache inside each factory class** | No decorators | Six copies of the same logic in the factories; edits the six classes |

### Chosen Option: **B — a `CachedTenantStore` decorator** (recommended, pending the maintainer)

### Rationale

The store lookup is the cost, and caching there removes the fallback hazard because `null` is never stored. B wins when the factories are the only consumers that matter and the store is the real cost; A wins when the maintainer wants the cache visible at the factory contract named in the issue and accepts the "never cache the default" rule; the issue's acceptance wording ("`CachedTenantConnectionFactory` caches connection strings per tenant") would change to a store-level equivalent. Invalidation is explicit (`ITenantCacheInvalidator.InvalidateAsync(tenantId)`) plus the TTL (default 10 minutes).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Opt-in and secrets</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt-in through `AddEncinaTenantCaching`; the connection string never reaches a log, an `Activity` tag, a metric tag or a health result; the inner store's exceptions propagate unchanged** | Follows the project rules | The caller must register the decorator |
| **B) Always on** | No registration step | Breaks "every feature opt-in" |

### Chosen Option: **A — opt-in, secret never logged**

### Rationale

`ITenantStore.GetTenantAsync` returns `ValueTask<TenantInfo?>` (no `Either`), so "errors" are exceptions: the decorator does not catch the inner store's exceptions, they propagate as today. `TenantHealthCheck` emits `tenant_id` only and is unchanged.

**Class:** Settled

**Settled by:** `AGENTS.md` §1 (every feature opt-in), §3 (secrets never reach logs, tags or plaintext storage; errors not swallowed) and #851.

</details>

<details>
<summary><strong>4b. The cache key: the exact tenant id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key on the exact tenant id, compared ordinally; the decorator never changes the inner store's equality semantics** | A store whose tenant ids are case-sensitive (for example a PostgreSQL table with its default collation can hold `acme` and `ACME` as two tenants) can never have one tenant's `TenantInfo` and connection string returned for the other id | A store that is itself case-insensitive caches `acme` and `ACME` as two entries (a harmless extra miss) |
| **B) Key case-insensitively, "like `InMemoryTenantStore`"** | One entry per case-insensitive id | `InMemoryTenantStore` is the development store (`StringComparer.OrdinalIgnoreCase`); the `ITenantStore` contract says nothing about case, so the cache cannot assume it; for a case-sensitive store the first tenant cached would be served to the other id, a **cross-tenant connection** |

### Chosen Option: **A — exact tenant id, ordinal**

### Rationale

The contract, not the development store, defines equality. The private cache (Design Choice 9) is keyed by the tenant id itself, with no shared prefix to collide on. A unit test uses a case-sensitive inner store holding two tenants that differ only by case and asserts each id returns its own `TenantInfo`.

**Class:** Settled

**Settled by:** `AGENTS.md` §3 (compliance and security gates fail closed; tenant isolation must never be weakened by a cache: a cross-tenant connection is the failure) and the `ITenantStore` contract in `src/Encina.Tenancy/Abstractions/ITenantStore.cs`.

</details>

<details>
<summary><strong>6. Negative caching (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Never cache a `null` result (an unknown tenant)** | A tenant created later is visible at once; no fallback result is ever cached, whichever way the #3617 family is fixed | Repeated lookups of unknown tenants reach the store |
| **B) Cache `null` too, with a short TTL** | Fewer store calls for unknown tenants (for example scans of random tenant ids) | A tenant created later is invisible until the TTL; with today's fail-open factories it pins the default database for that tenant |

### Chosen Option: **A — found tenants only** (recommended, pending the maintainer)

### Rationale

A wins on correctness and on the interaction with #3617; B wins when unknown-tenant traffic is a real load on the store. The rule "null is never cached" does not change when #3674 and #3675 make the factories fail closed.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. A cache failure (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A failure of the decorator's own cache operation is a miss: the inner store answers; the failure is counted as `error` and logged by exception type through `ForLogging()`** | A broken cache costs performance only | Hides a cache fault except through the counter and the log |
| **B) The failure propagates** | A fault is impossible to miss | A cache fault becomes a tenant-resolution outage on every request |

### Chosen Option: **A — a miss, counted and logged** (recommended, pending the maintainer)

### Rationale

With a private in-process `MemoryCache` such a failure is exceptional (for example a disposed cache), so A costs almost nothing; B wins when the maintainer wants such a fault to stop requests. Errors are not swallowed silently: they are counted and logged.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Absolute or sliding expiry (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Absolute expiry: an entry lives `Ttl` from when it was stored** | A rotated connection string is picked up within the TTL whatever the read rate; the staleness contract has one number | A hot tenant is re-read from the store once per TTL |
| **B) Sliding expiry** | A hot tenant is never re-read | A constantly read tenant would never pick up a rotated or revoked connection string, so the staleness window is unbounded |

### Chosen Option: **A — absolute expiry** (recommended, pending the maintainer)

### Rationale

B contradicts the documented staleness contract (a changed tenant connection string is picked up after the TTL or `InvalidateAsync`); A wins unless the maintainer accepts unbounded staleness for hot tenants.

**Class:** Dilemma

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

<details>
<summary><strong>9. The cache engine and its clock (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A private `MemoryCache` owned by the decorator (`SizeLimit = MaxEntries`, each entry of size 1, absolute expiration), with a clock adapter over the injected `TimeProvider` assigned to `MemoryCacheOptions.Clock`; `AddMemoryCache()` is not called and the host's `IMemoryCache` is never used** | Bounded by construction; the entries are not enumerable by other code; expiry follows `TimeProvider`, so `FakeTimeProvider` can drive the TTL tests | The `Clock` type of `MemoryCacheOptions` (the only time hook in `Microsoft.Extensions.Caching.Memory` 10.0.12 verified from its XML documentation; there is no `TimeProvider` property) must be confirmed at work start, including whether it is flagged obsolete |
| **B) A private `MemoryCache` for storage and eviction only; freshness decided by the decorator, which stores the expiry computed from `TimeProvider` beside the `TenantInfo` and treats an expired entry as a miss** | No dependency on the `Clock` type; `TimeProvider` is the only clock in play | A second expiry check in the decorator; the `MemoryCache` expiry is only memory reclamation |
| **C) The host's registered `IMemoryCache` (the earlier draft)** | No new instance | `MaxEntries` is enforced only if the host happened to set a `SizeLimit`, so the bound is false by default; the cache has no `TimeProvider` hook; the secrets sit in a shared cache whose keys are predictable and enumerable |

### Chosen Option: **A — a private `MemoryCache` with a `TimeProvider` clock adapter** (recommended, pending the maintainer)

### Rationale

C is excluded by its hazards (an unbounded, enumerable cache of secrets). A and B both satisfy `AGENTS.md` §3 (time from `TimeProvider`); A keeps one expiry mechanism, B wins if `MemoryCacheOptions.Clock` turns out to be obsolete or awkward. The private cache is created and disposed by the decorator, so no registration of `IMemoryCache` is needed and `AddMemoryCache()` is dropped from Phase 3. Eviction under the size limit is the `MemoryCache` default (compaction); an entry that cannot be stored is a miss and the inner store answers.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Registration order (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaTenantCaching` wraps the `ITenantStore` descriptor registered at call time, and a startup check (an `IValidateOptions<TenantCacheOptions>` that injects `IServiceProvider` and fails when the resolved `ITenantStore` is not the cached decorator, with `ValidateOnStart`) fails loudly when a store registered later removed it** | Small; a misordered application fails at startup instead of silently bypassing the cache | Registration-order dependent: `AddTenantStore<TStore>()` and `AddTenantStore(factory)` call `RemoveAll<ITenantStore>()` (`src/Encina.Tenancy/ServiceCollectionExtensions.cs`, lines 146 and 175), so a store added after `AddEncinaTenantCaching` discards the decorator; needs the log id 1810 |
| **B) Order-independent: `AddEncinaTenantCaching` registers a caching marker, `AddTenantStore<TStore>()` and `AddTenantStore(factory)` wrap their store when the marker is present, and `AddEncinaTenantCaching` wraps a store already registered** | Works in any order, as `AGENTS.md` §3 asks of stores; no startup check, no log id 1810 | Edits the two `AddTenantStore` methods and needs tests for both orders |

### Chosen Option: **B — order-independent registration** (recommended, pending the maintainer)

### Rationale

`AGENTS.md` §3 asks a store to win in any registration order without overriding the application's own registration, which only B delivers; A wins if the maintainer prefers a smaller change and a loud failure. Both are proven by DI tests with `ValidateOnBuild` and `ValidateScopes`. In both, `InMemoryTenantStore` stays resolvable: its registration (`ServiceCollectionExtensions.cs`, lines 97 to 104) does `sp.GetRequiredService<ITenantStore>() as InMemoryTenantStore ?? throw`, which would throw once `ITenantStore` is the decorator, so the decorator exposes its inner store internally and that registration unwraps it. `RegisterTenant` and `RemoveTenant` on the development store bypass invalidation: documented, the application calls `ITenantCacheInvalidator` after programmatic changes.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Bound, invalidation contract and test seams</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MaxEntries` default 10,000 (about 10,000 x 1 KB, roughly 10 MB worst case for `TenantInfo` records with their strings; eviction is the `MemoryCache` compaction); `InvalidateAsync` and `InvalidateAllAsync` return `ValueTask<Either<EncinaError, Unit>>` and, with the local cache, cannot fail, so they return `Right` (if D1 picks a shared cache they gain a `Left` through a `TenancyErrors` code with an `ErrorCategory` per ADR 062, merged in PR #3599); `InvalidateAllAsync` removes every entry (the private cache is cleared)** | The ROP contract is uniform and does not change if the cache becomes shared | A `Left` path that exists only for a future cache |
| **B) `ValueTask` without `Either` because a local invalidation cannot fail** | No unused error path | The contract changes if D1 picks A or C |

### Chosen Option: **A — the values and contract above**

### Rationale

These choices follow D1 and the existing conventions (ROP return types) and change no security property; the 10,000 default is a memory bound, the only consequence of which is more or fewer store lookups.

**Class:** Trivial

</details>

---

## Implementation Phases

### Phase 1: Core — options, validator and invalidation contract (pull request 1)

<details>
<summary>Tasks</summary>

1. `TenantCacheOptions` (public sealed) in `src/Encina.Tenancy/Configuration/`: `Ttl` (default 10 minutes), `MaxEntries` (default 10,000, D11), no secret properties.
2. `TenantCacheOptionsValidator : IValidateOptions<TenantCacheOptions>`: `Ttl` greater than zero, `MaxEntries` greater than zero.
3. `ITenantCacheInvalidator` (public): `ValueTask<Either<EncinaError, Unit>> InvalidateAsync(string tenantId, CancellationToken)` and `InvalidateAllAsync(CancellationToken)` (D11: they cannot fail with the local cache).
4. `Encina.Tenancy.csproj`: add the `Microsoft.Extensions.Caching.Memory` package reference (version 10.0.12, already managed centrally in `Directory.Packages.props`).
5. PublicAPI lines in `src/Encina.Tenancy/PublicAPI.Unshipped.txt`.
6. The manifest rows for the files of this pull request in `.github/coverage-manifest/Encina.Tenancy.json` (Phase 6 table), in the same pull request as their unit and guard tests.

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

### Phase 2: The cached store decorator (pull request 2)

<details>
<summary>Tasks</summary>

1. `CachedTenantStore : ITenantStore, ITenantCacheInvalidator` (internal sealed, or public per the maintainer's D3 answer) in `src/Encina.Tenancy/Stores/`: `GetTenantAsync` reads its **private** `MemoryCache` (D9) keyed by the **exact tenant id, compared ordinally** (Design Choice 4b: never case-insensitive), on a miss calls the inner store and stores a non-null result with an absolute expiration of `Ttl` and entry size 1; `null` is returned and not stored (D6). `GetAllTenantsAsync` and `ExistsAsync` pass through (not hot). The inner store is exposed internally (D10).
2. A failure of the decorator's own cache operation is a miss (D7): the inner store answers; the failure is counted as `error` and logged by exception type. An inner store exception propagates unchanged.
3. `InvalidateAsync` removes the entry; `InvalidateAllAsync` clears the private cache; both return `Right` (D11).
4. The private `MemoryCache` has `SizeLimit = MaxEntries` and the clock adapter over the injected `TimeProvider` (`MemoryCacheOptions.Clock`, confirmed against the package at work start; fallback D9 option B); it is disposed with the decorator. The host's `IMemoryCache` is never used.
5. `TenancyMetrics` and `TenancyLog` extensions of Phase 5 land in the same pull request, with their tests.
6. If the maintainer chooses D3 option A instead: the same cache component is used by seven thin decorators and the default connection string is never cached.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: ITenantStore.GetTenantAsync returns ValueTask<TenantInfo?>; the six factories fall back to the default connection string when it returns null, so null must never be cached (Dilemma D3).
TASK: Implement CachedTenantStore and the invalidation operations as described; keep each method at cyclomatic complexity of 5 or less.
KEY RULES: The key is the exact tenant id, ordinal (never case-insensitive: a case-sensitive store can hold acme and ACME as different tenants); only non-null TenantInfo is cached; the connection string never appears in a log, tag, metric or ToString; a cache failure is a miss (D7); the inner store's exceptions propagate; a private MemoryCache bounded by SizeLimit, never the host's IMemoryCache; absolute expiry from TimeProvider.
REFERENCE FILES: src/Encina.Tenancy/Stores/InMemoryTenantStore.cs, src/Encina.Tenancy/Abstractions/TenantInfo.cs, src/Encina.ADO.SqlServer/Tenancy/TenantConnectionFactory.cs, src/Encina.Caching.Memory/MemoryCacheProvider.cs.
```

</details>

### Phase 3: Configuration, DI & registration (pull request 3)

<details>
<summary>Tasks</summary>

1. `AddEncinaTenantCaching(this IServiceCollection, Action<TenantCacheOptions>? configure = null)` in `src/Encina.Tenancy/ServiceCollectionExtensions.cs`: options and validator with `ValidateOnStart`; **no** `AddMemoryCache()` (the decorator owns a private cache, D9); wraps the `ITenantStore` descriptor registered at call time with a factory that builds the inner store and the decorator; registers `ITenantCacheInvalidator` resolving to the same decorator instance.
2. Registration order (D10): option B registers a caching marker that `AddTenantStore<TStore>()` and `AddTenantStore(factory)` honour (wrapping their store), so any order works; under option A a startup check (`IValidateOptions` injecting `IServiceProvider`) fails when the resolved `ITenantStore` is not the cached decorator.
3. Keep `InMemoryTenantStore` resolvable: the registration at lines 97 to 104 (`sp.GetRequiredService<ITenantStore>() as InMemoryTenantStore ?? throw`) unwraps the decorator's inner store, so consumers of `InMemoryTenantStore` and `RegisterTenant` do not throw at runtime; `ValidateOnBuild` cannot catch a factory registration, so a DI test resolves `InMemoryTenantStore` with caching on.
4. DI tests with `ValidateOnBuild` and `ValidateScopes` for the six `AddEncina*WithTenancy` registrations combined with `AddEncinaTenantCaching`, in both registration orders, with a custom store registered through `AddTenantStore`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The six provider packages register ITenantConnectionFactory with TryAddScoped in their TenancyServiceCollectionExtensions; ITenantStore is registered by AddEncinaTenancy or by the application.
TASK: Add AddEncinaTenantCaching with the descriptor-wrapping registration (D10 as the maintainer answered it, option B until then), keep InMemoryTenantStore resolvable and add the DI tests.
KEY RULES: Registration completeness: every option type and dependency the decorator resolves is registered; no AddMemoryCache(); a store wins or is wrapped in any registration order without discarding the application's own registration; no static state.
REFERENCE FILES: src/Encina.Tenancy/ServiceCollectionExtensions.cs, src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs, src/Encina.Dapper.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs.
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Caching: the feature (local cache, D1). Multi-tenancy: the key is the exact tenant id; an unknown tenant is never cached.
2. Validation: options validated at startup. Observability and logging: Phase 5.
3. Health checks, resilience, locks, transactions, idempotency, module isolation, audit: not applicable (matrix below); `TenantHealthCheck` keeps working because it resolves the same `ITenantStore` and emits only `tenant_id`, unchanged.
4. Related defects, not fixed here and already tracked: the factories fall back to the default connection string for an unknown tenant (fail open), #3617 with the children #3674 (ADO), #3675 (Dapper), #3676 (EF Core and MongoDB) and #3686, all open. The `TenantInfo` connection string printing was fixed by #3618 (PR #3752) and needs no issue.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: See the matrix at the end of this plan.
TASK: Verify TenantHealthCheck and the tenant resolvers still resolve the decorated store; add no other integration.
KEY RULES: Do not change TenantConnectionFactory behavior for unknown tenants in this feature; that is #3617 and its children #3674, #3675, #3676 and #3686.
REFERENCE FILES: src/Encina.Tenancy/Health/TenantHealthCheck.cs, src/Encina.Tenancy/ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Observability (pull request 2)

<details>
<summary>Tasks</summary>

1. Extend `TenancyMetrics` (`src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs`, meter `Encina`) with a counter `encina.tenancy.cache.requests_total` and the tag `outcome` = `hit`/`miss`/`error`; no tenant id or connection string in tags.
2. Extend `TenancyLog` with `[LoggerMessage]` ids packed from 1805 inside the registered range `Tenancy` (1800-1899; 1800-1804 are used; 1805-1810 is the block claimed for #711): 1805 cache hit, 1806 cache miss and store, 1807 cache failure (exception type only), 1808 entry invalidated, 1809 all entries invalidated; 1810 startup check failed, **used only if D10 option A is chosen** (under option B it stays unassigned). The tenant id is logged only if the existing messages already log it; the connection string never.
3. Run the EventId architecture tests; PublicAPI lines if new public members.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: TenancyMetrics and TenancyLog already exist; EventIds 1800-1804 are in use inside the registered range 1800-1899.
TASK: Add the counter, the outcome tag and the log messages 1805 to 1809 (1810 only if D10 option A is chosen).
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

Per-flag targets for the files this work adds or changes. Each row is entered in `.github/coverage-manifest/Encina.Tenancy.json` **in the same pull request as the tests that meet it** (the "PR" column), with a one-sentence justification in the manifest's `justifications`; measured values replace the proposals, and targets never go down (in particular `ServiceCollectionExtensions.cs` keeps its current targets, unit 95 and guard 70 on main, or higher).

| PR | File | unit | guard | contract | property | integration |
|:--:|------|:---:|:---:|:---:|:---:|:---:|
| 1 | `Configuration/TenantCacheOptions.cs` | 100 | 0 | 0 | 0 | 0 |
| 1 | `Configuration/TenantCacheOptionsValidator.cs` | 100 | 40 | 0 | 0 | 0 |
| 1 | `Abstractions/ITenantCacheInvalidator.cs` | 0 | 0 | 0 | 0 | 0 |
| 2 | `Stores/CachedTenantStore.cs` | 95 | 40 | 60 | 60 | 0 |
| 2 | `Diagnostics/TenancyMetrics.cs` (addition) | 90 | 0 | 0 | 0 | 0 |
| 2 | `Diagnostics/TenancyLog.cs` (addition) | 0 | 0 | 0 | 0 | 0 |
| 3 | `ServiceCollectionExtensions.cs` (addition) | 95 (kept) | 70 (kept) | 0 | 0 | 0 |
| 4 | `Stores/CachedTenantStore.cs` (integration added) | unchanged | unchanged | unchanged | unchanged | 70 |

| Row | Justification |
|-----|---------------|
| Options | Two properties with defaults that unit tests read; there are no parameters or branches for guard, contract or property tests to reach. |
| Validator | Both rules are reachable from unit tests, and guard tests reach the null `options` argument check. |
| `ITenantCacheInvalidator.cs` all 0 | An interface has no executable lines; the implementation is covered through `CachedTenantStore`. |
| `CachedTenantStore.cs` unit 95, guard 40, contract 60, property 60 | Unit tests with a fake inner store and `FakeTimeProvider` reach every branch (hit, miss, null, expiry, bound, case-sensitive ids, failure, invalidation); guard tests reach the constructor and argument checks; the contract tests execute the real decorator against `InMemoryTenantStore` for found and missing tenants; the property test drives random get and invalidate sequences. |
| `CachedTenantStore.cs` integration 70 | The six database-backed classes reach the main hit, miss and invalidation paths through the real factories; failure branches stay in unit tests. |
| `TenancyMetrics.cs` | The new counter method is driven for each outcome by unit tests; it has no reference-type parameters for a guard test. |
| `TenancyLog.cs` all 0 | A `[LoggerMessage]` partial has no hand-written coverable lines (the same reason as the `AuthorizationElevatedLog.cs` entry in `Encina.AspNetCore.json`); the messages are asserted through the decorator tests. |
| `ServiceCollectionExtensions.cs` | The new method and the `AddTenantStore` changes are fully reachable from DI tests; the existing targets are kept, never lowered. |

Contract is 0 for every file except the decorator because the contract is the decorator's equivalence to `ITenantStore`; a `.md` justification is never written for contract tests (`AGENTS.md` §9).

1. **Unit** (`tests/Encina.UnitTests/Tenancy/`): hit, miss, null not cached, TTL expiry with `FakeTimeProvider`, invalidation, bound, **a case-sensitive inner store holding two tenants that differ only by case, each id returning its own `TenantInfo`** (the key is the exact id), cache failure is a miss, inner exception propagates, registration in both orders and `InMemoryTenantStore` still resolvable; a test that `ToString()` of options and a captured log contain no connection string, and that the metric tags and the `TenantHealthCheck` result carry no connection string or tenant id (the health check emits only `tenant_id`, unchanged).
2. **Guard**: constructor and public members; a nullable parameter that accepts null gets a valid-result test, not an exception test (#3798).
3. **Contract**: the decorator honours `ITenantStore` exactly like the inner store for found and missing tenants.
4. **Property**: for any sequence of get and invalidate operations, a cached read equals the inner store's current value unless the TTL has not expired and no invalidation occurred; a missing tenant is never returned from the cache; distinct tenant ids never share an entry.
5. **Integration** (pull request 4): the six classes of the table above, `[Collection]` fixtures, `ClearAllDataAsync` in `InitializeAsync`, `[Trait("Category", "Integration")]`, `[Trait("Database", "...")]`. They assert only what the cache changes (a second resolution does not reach the store, a null is not cached, a changed tenant is picked up after `InvalidateAsync`) and never assert the default-database fallback, which #3674 and #3675 change; if those have merged, an unknown-tenant case asserts the fail-closed behavior.
6. **Load**: `.md` justification (the cache is a thin wrapper over a private `MemoryCache`); the benchmark covers the cost.
7. **Benchmark**: cached versus uncached resolution, and the cost of building the tenant filter clause in ADO and Dapper (decides D2), in the existing `Encina.ADO.SqlServer.Benchmarks` and `Encina.Dapper.SqlServer.Benchmarks` projects.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Coverage is per flag against .github/coverage-manifest/Encina.Tenancy.json; targets never go down; database features need real-database integration tests with the shared collection fixtures.
TASK: Write the unit, guard, contract, property, six integration classes, the benchmark additions and the load justification, with the manifest rows and one-sentence justifications in the pull request of each file; measure every flag and the CRAP table.
KEY RULES: Never IClassFixture or new fixtures; never dispose the shared connection; Shouldly; FakeTimeProvider, no Thread.Sleep; no connection string in any assertion message.
REFERENCE FILES: tests/Encina.UnitTests/Tenancy, tests/Encina.IntegrationTests (ADO and Dapper tenancy tests), tests/Encina.BenchmarkTests/Encina.ADO.SqlServer.Benchmarks, docs/testing/integration-tests.md.
```

</details>

### Phase 7: Documentation & Finalization (pull request 5)

<details>
<summary>Tasks</summary>

1. XML docs (in code, in the pull requests that add the members, not a separate file) with an example of `AddEncinaTenantCaching` and the staleness contract (a changed tenant connection string is picked up after the TTL or `InvalidateAsync`; a deactivated or removed tenant keeps resolving for up to the TTL unless invalidated; `RegisterTenant` and `RemoveTenant` on the development store bypass invalidation).
2. `src/Encina.Tenancy/README.md` section and a section of `docs/features/multi-tenancy.md` (via `docs-writer`): when to use it, why the cache is local, the staleness window, how to invalidate.
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
| ADR-021 / `AGENTS.md` §7 | EventIds | 1805-1810 inside the registered `Tenancy` range (1805-1809 used, 1810 only under D10 option A) |
| ADR 062 (merged, PR #3599) | Error category contract | Only if D1 picks a shared cache and invalidation gains a `Left` (D11) |
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
| `Encina.Tenancy` | 1805-1810 inside the registered 1800-1899 (the block claimed for #711 in `eventid-blocks.md`) | 1800-1804 used; 1805-1809 used here, 1810 only under D10 option A; 1811-1899 stay free; packed sequentially |

### Estimated File Count

Every pull request carries its tests, manifest rows (in the same pull request as the tests that meet them) and PublicAPI lines. No pull request uses a type created by a later one. No existing test calls a changed signature (`AddTenantStore` keeps its signature).

| PR | Contents | Files | Lines |
|:--:|----------|------:|------:|
| 1 | `TenantCacheOptions`, `TenantCacheOptionsValidator`, `ITenantCacheInvalidator` (3); `Encina.Tenancy.csproj` (1); PublicAPI (1); manifest rows (1); unit test of options and validator (1); guard test (1) | **8** | about 450 |
| 2 | `CachedTenantStore`, the clock adapter (2); `TenancyMetrics.cs`, `TenancyLog.cs` (2); PublicAPI (1); manifest rows (1); unit tests of the store and of metrics/log (2); guard (1); contract (1); property (1) | **11** | about 1,400 |
| 3 | `ServiceCollectionExtensions.cs` (1); PublicAPI (1); manifest rows (1); DI unit tests (1); guard test (1) | **5** | about 450 |
| 4 | Six integration classes (6); benchmark additions in `Encina.ADO.SqlServer.Benchmarks` and `Encina.Dapper.SqlServer.Benchmarks` (2); manifest rows for the integration flag (1); load `.md` justification (1) | **10** | about 1,300 |
| 5 | README section, `docs/features/multi-tenancy.md` section, changelog fragment | **3** | about 250 |
| **Total** | Five pull requests | **37 file touches, 32 distinct files** (the `Encina.Tenancy` manifest is touched in 4 pull requests and its PublicAPI file in 3) | about 3,850 |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Multi-tenancy lives in Encina.Tenancy; six ADO.NET and Dapper packages (SqlServer, PostgreSQL, MySQL) each ship a TenantConnectionFactory that resolves a tenant's connection string through ITenantStore. EF Core and MongoDB have no factory. Secrets never reach logs or plaintext storage; zero warnings; no backward compatibility.

IMPLEMENTATION OVERVIEW: Five pull requests, each with its tests and manifest rows. 1: options, validator, invalidator contract, package reference; 2: CachedTenantStore (non-null tenants only, key = the exact tenant id ordinal, bounded private MemoryCache with a TimeProvider clock) with the counter and log ids 1805 to 1809; 3: AddEncinaTenantCaching with order-independent descriptor wrapping, InMemoryTenantStore kept resolvable; 4: six real-database integration classes and the benchmark that decides D2; 5: docs. Phase 4 cross-cutting evaluation; the fail-open fallback is tracked by #3617 and its children. Dilemmas D1 (where), D2 (filter half), D3 (placement), D6 (negative caching), D7 (cache failure), D8 (expiry), D9 (engine and clock) and D10 (registration order) are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: a private MemoryCache with SizeLimit and absolute expiration, never the host's IMemoryCache; the key is the exact tenant id, ordinal, never case-insensitive; null never cached; connection string never in logs, tags, metrics, health output or ToString; TimeProvider for clocks; descriptor wrapping in any registration order; shared [Collection] fixtures; Shouldly; CRAP at or below 10 on changed methods.

REFERENCE FILES: src/Encina.Tenancy/Abstractions/ITenantStore.cs, src/Encina.Tenancy/Stores/InMemoryTenantStore.cs, src/Encina.ADO.SqlServer/Tenancy/TenantConnectionFactory.cs, src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs, src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs, tests/Encina.UnitTests/Tenancy.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | The feature: a local cache over the tenant store lookup. |
| 2 | OpenTelemetry | ✅ | One counter with an `outcome` tag on the shared meter; no tenant id or connection string in tags. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 1805-1809 (1810 only under D10 option A) in the registered Tenancy range; never the connection string. |
| 4 | Health Checks | ❌ | `TenantHealthCheck` is unchanged; the cache adds no dependency to check. |
| 5 | Validation | ✅ | `IValidateOptions<TenantCacheOptions>` at startup (and the decorator-outermost check under D10 option A). |
| 6 | Resilience | ❌ | A cache failure falls back to the inner store; no external call is added. |
| 7 | Distributed Locks | ❌ | Concurrent misses may call the store twice, which is harmless. |
| 8 | Transactions | ❌ | No atomic multi-operation guarantee is needed. |
| 9 | Idempotency | ❌ | A lookup is a read. |
| 10 | Multi-Tenancy | ✅ | The key is the exact tenant id (ordinal); an unknown tenant is never cached. |
| 11 | Module Isolation | ❌ | Tenant connections are not module-scoped. |
| 12 | Audit Trail | ❌ | A cached lookup is not an auditable business operation. |

---

## Prerequisites & Dependencies

- **Not blocking**: no other issue. The module and key work of #746 does not touch this feature.
- **Related and already tracked** (verified open on GitHub): #3617 (the tenant connection factories fall back to the default database when the tenant is not found) with the children #3674 (ADO), #3675 (Dapper), #3676 (EF Core and MongoDB) and #3686 (a `DatabasePerTenant` tenant without a connection string). #3674 and #3675 edit the same six factory files that the integration tests of pull request 4 exercise: pull requests 1 to 3 of this plan do not touch those files and can merge in any order with them; pull request 4 should open after #3674 and #3675 are decided, and its tests assert only what the cache changes (Phase 6), not the fallback. The earlier finding that `TenantInfo` printed its connection string is fixed (#3618, PR #3752) and is not repeated.
- **Measurement first**: the benchmark of the filter clause (D2) and of cached versus uncached resolution runs before pull request 4 is marked ready.

---

## Next Steps

1. Ask the maintainer D1, D2, D3, D6, D7, D8, D9 and D10 when work on #711 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1 (8 files): options, validator, invalidator contract, package reference, manifest rows.
3. Pull request 2 (11 files): `CachedTenantStore`, metrics and log ids, unit, guard, contract and property tests, manifest rows.
4. Pull request 3 (5 files): `AddEncinaTenantCaching`, the `AddTenantStore` changes, DI tests.
5. Pull request 4 (10 files): six integration classes, benchmark additions, load justification, manifest rows.
6. Pull request 5 (3 files): README, guide section, changelog fragment.
