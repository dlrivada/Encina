# Implementation Plan: ModuleId dimension in cache key generation

> **Issue**: [#746](https://github.com/dlrivada/Encina/issues/746)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 6 packages touched, 3 pull requests plus a fourth that waits for the #277 contract suite)
> **Estimated Scope**: ~250-350 lines of production code + ~900 lines of tests, split into pull requests of 9, 13, 15 and 1 files (see Estimated File Count)

---

## Summary

Cache keys carry the tenant (`t:{tenant}`) and optionally the user (`u:{user}`) but not the module. In a modular monolith, module A's cached entry for `GetOrderQuery` can be served to module B when the request type name and content are equal. This plan adds a `m:{module}` segment to every key and invalidation pattern that Encina generates for request and query caching, so cached entries are isolated per module (`AGENTS.md` §6, function 11).

Where keys are built today (all verified in the code):

| Site | File | Key shape today |
|------|------|-----------------|
| `DefaultCacheKeyGenerator` (key, template key, `GeneratePattern`, `GeneratePatternFromTemplate`) | `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` | `{prefix}:t:{tenant}:u:{user}:{RequestType}:{hash}` |
| `CacheConfiguration<TRequest>.GenerateKey` | `src/Encina.Caching/Attributes/CacheableQueryAttribute.cs` | `t:`/`u:` parts, type name, hash; a public type created with `new` by `AddCacheConfiguration` (a singleton with no dependency injection) and with no caller of `GenerateKey` in `src/` today |
| `DefaultQueryCacheKeyGenerator` (EF Core query cache) | `src/Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs` | `{prefix}:{tenant}:{entity}:{hash}` (no `t:` marker, tenant not escaped) |
| `ShardCacheKeyGenerator` | `src/Encina.Caching/Sharding/ShardCacheKeyGenerator.cs` | infrastructure keys (directory, topology), not request data |

The eight cache providers need no change: `ICacheProvider` takes the key as an opaque string. Provider coherence (`AGENTS.md` §5) is proven by module-scoped cases in a shared `ICacheProvider` contract suite; that suite (`CacheProviderContractTests`) does **not exist on main today** (no file with that name under `tests/`) and is owned by the Memcached plan (#277), so those cases go in a last, gated pull request (Design Choice 14, Prerequisites). The database provider matrix does not apply: only key generation changes and no store, SQL or connection type is touched.

Three facts shape the design (details in Design Choices):

1. The module is known in two places: the ambient `IModuleExecutionContext.CurrentModule` (an `AsyncLocal`, set by `ModuleExecutionContextBehavior` in `Encina.EntityFrameworkCore` from `IModuleHandlerRegistry`) and the request-context metadata read by `IRequestContext.GetModuleName()` (`src/Encina/Modules/RequestContextModuleExtensions.cs`), which no framework component sets. The issue text says the context has no module property; the metadata accessor exists but is never populated.
2. `IModuleExecutionContext` is registered `TryAddScoped` in the ADO.SqlServer, Dapper.SqlServer, EF Core and MongoDB packages (the PostgreSQL and MySQL ADO and Dapper packages register nothing yet inject it in their `ModuleAwareConnectionFactory` and `SchemaValidatingCommand`), while `DefaultCacheKeyGenerator` is a singleton (`TryAddSingleton` in `AddEncinaCaching`); injecting it as is would fail `ValidateScopes`.
3. Invalidation patterns already use a wildcard in the middle (`{prefix}:*:{entityType}:*` in `QueryCacheInterceptor`, lines 542-590), which keeps matching keys that gain a module segment.

Provider category: **Caching (8)**, key generation only. Affected packages: `Encina.Caching`, `Encina.EntityFrameworkCore`, and the registration of `IModuleExecutionContext` in `Encina.ADO.SqlServer`, `Encina.Dapper.SqlServer` and `Encina.MongoDB` if D1 keeps the ambient source.

---

## Design Choices

<details>
<summary><strong>1. Where the generators read the module from (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The request context**: read `IRequestContext.GetModuleName()` (existing metadata accessor, or a new `ModuleId` property) and have the dispatcher stamp it | Generators stay pure on their inputs; the value travels with the request | Nothing stamps it today; `RequestHandlerCallback` takes no context, so a behavior cannot change the context for later steps: stamping must happen at dispatch, a core change; the EF interceptor reads the ambient accessor context, which would also need it |
| **B) The ambient `IModuleExecutionContext.CurrentModule`**, injected as an optional dependency | Needs no core abstraction change; it is already set while the handler runs (the EF interceptor and `QueryCachingPipelineBehavior` both run inside that scope when the module behavior is outer); both request-level and SQL-level caching see the same value | Generators depend on async-local state; `IModuleExecutionContext` must be registered as a singleton (its state is a static `AsyncLocal`, so lifetime is irrelevant) in four packages; the only producer today is the EF package's `ModuleExecutionContextBehavior`; the module behavior must be registered before the caching behavior |
| **C) A new `ICacheScopeProvider` yielding all scoping dimensions (tenant, module, future ones)** | One place for scoping; singleton-safe by design | One more concept and a public API for two dimensions |

### Chosen Option: **B — the ambient `IModuleExecutionContext`** (recommended, pending the maintainer)

### Rationale

B reuses the producer that already exists and works for both cache levels without touching the dispatcher. A wins when the module is known at pipeline entry and the dispatcher is willing to stamp it (it would also serve ABAC and retention, which already read `GetModuleName()`); C wins when more dimensions than tenant and module are expected. The cost of B is limited by three guards in this plan: a singleton registration change, a pipeline-order test, and a follow-up issue for the missing producer outside the EF package (see Prerequisites).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Behavior when no module is executing (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Omit the `m:` segment** (non-modular applications unaffected) | Same rule the tenant segment follows in the generator; keys of non-modular apps do not change | If module resolution fails for a module's handler, its entries land in the shared module-less scope. Today the only producer of `CurrentModule` is the EF package, so an application using ADO.NET, Dapper or MongoDB gets module-less keys **silently** for every handler: the cache is not isolated per module there, with no error and no log |
| **B) Bypass the cache when module isolation is enabled but no module is known** (fail closed, like `TenantAllowsCache` does for tenants when multi-tenancy is on) | No shared module-less entries when isolation is on; the ADO.NET, Dapper and MongoDB consequence becomes visible (no caching) instead of silent | Needs a marker for "module isolation enabled"; handlers outside any module (shared code) would lose caching; until the producer exists outside EF, caching is off for those packages when isolation is on |
| **C) Use a fixed placeholder such as `m:none`** | Every key has the segment, patterns are uniform | Changes all keys of non-modular apps; adds a segment with no information |

### Chosen Option: **A — omit the segment** (recommended, pending the maintainer)

### Rationale

The issue listed A as settled, but `AGENTS.md` §3 (compliance and security gates fail closed when context is missing) has real weight here: `QueryCachingPipelineBehavior.TenantAllowsCache` bypasses the cache for a missing tenant because a tenant-less key would be shared by every tenant, and the module analogue is B. A is recommended because the module is a scoping dimension rather than an access gate, a module-less entry cannot be read by a module-scoped lookup (the key shapes differ), and non-modular applications must keep working (pay for what you use); B wins in deployments that treat module isolation as a security boundary and cannot tolerate a shared module-less scope, and it wins outright if the maintainer considers the silent ADO.NET, Dapper and MongoDB behavior unacceptable. The missing producer outside the EF package is tracked by a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/module-execution-context-producer-outside-ef.md), to be opened before the first pull request of this plan merges. Because the tenant code has a fail-closed branch and B also satisfies the cited rule, the choice is a Dilemma.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Invalidation across modules (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A pattern carries the executing module** (`m:{current}`), so a command in module A invalidates only A's entries | Strict isolation, consistent with the keys | A write in module A leaves stale entries for the same data cached under module B |
| **B) A pattern spans all modules** (`m:*` in the module position, the shape the issue states for the case where no module is known, here used always) | Removal is always safe for module-scoped entries: a write never leaves a stale entry in another module; the EF pattern `{prefix}:*:{entityType}:*` already behaves this way | Invalidation reaches into other modules' entries (it only deletes, no module reads another's data). A glob cannot match both `...:t:x:Type:hash` and `...:t:x:m:Orders:Type:hash` with one string, so entries written with no module (D2 option A) are **not** matched by `m:*`; matching them needs a second pattern, which is an `ICacheKeyGenerator` interface change (not planned) |
| **C) `[InvalidatesCache]` gains an `AllModules` flag, default per attribute** | The application chooses per command | One more setting; a wrong default leaves stale data |

### Chosen Option: **B — patterns span all modules** (recommended, pending the maintainer)

### Rationale

Deleting an entry is always correct for a cache; leaving a stale one is not. B keeps `GeneratePattern` and `GeneratePatternFromTemplate` aligned with the EF pattern. The pattern shape is `{prefix}:t:{tenant}:m:*:{RequestType}:*` (the `*` also absorbs a `u:` segment that follows the module), so it matches every module-scoped key of the request type; this deviates from the issue, which applies `m:*` only when no module is known and `m:{current}` otherwise (that is option A). A wins when module teams own disjoint data and strict isolation of cache operations is a requirement; C wins when both kinds of commands exist in one application. Note the existing mismatch recorded in Prerequisites: `GeneratePattern` emits `t:*` for a context without tenant while keys then have no `t:` segment; the plan does not copy that mismatch for modules, and a property test asserts that a generated pattern matches every module-scoped key a request type can produce (module-less keys are out of its scope, as stated above).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Segment form and position, and which generators change</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `m:{module}` after the tenant and before the user segment; `DefaultCacheKeyGenerator`, `CacheConfiguration<TRequest>` and the EF generator change; `ShardCacheKeyGenerator` does not** | One key grammar next to `t:` and `u:`; shard directory and topology entries are cluster infrastructure, not request data; entries cached before the change miss once (pre-1.0, accepted) | The module segment sits between two identifying segments |
| **B) `m:{module}` before the tenant segment** | Groups a module's keys together | Every existing pattern with a tenant prefix changes shape |
| **C) A prefix per module in `KeyPrefix` configuration, or the module hashed into the request hash** | No new segment | Per-module configuration of every cache site, or patterns that can no longer target a module and unreadable keys |

### Chosen Option: **A — one `m:` segment after the tenant**

### Rationale

The segment follows the existing grammar of `DefaultCacheKeyGenerator`. Module names are code identifiers, not personal data, so `CacheKeyRedactor` (which masks `t:` and `u:`) needs no change for the `m:` segment. The position is a convention with no security effect (every shape carries its segments in a fixed order), and the shard generator holds no request data.

**Class:** Trivial

</details>

<details>
<summary><strong>5. Default of `VaryByModule` (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `VaryByModule` defaults to true on `[Cache]` and `CacheConfiguration<TRequest>`** | Isolated by default, the same default as `VaryByTenant`; a developer must opt out to share an entry across modules | A request type deliberately shared across modules caches once per module until the developer sets `false` |
| **B) Defaults to false** | Existing keys unchanged; sharing needs no flag | Isolation is opt-in, so the data leak this issue closes stays open for every request type nobody annotated |

### Chosen Option: **A — default true** (recommended, pending the maintainer)

### Rationale

A is the secure default and mirrors `VaryByTenant`; B wins if the maintainer prefers pay-for-what-you-use for modules (a non-modular application sets no module, so A costs it nothing either way: the segment is omitted when no module is known).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Escaping the module and the EF tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Escape the module and, in the EF generator, also the tenant, with `CacheKeyRedactor.EscapeSegment` (the rule of `t:` and `u:`)** | A tenant id containing `:` cannot forge a `m:` segment and cross a tenant or module boundary; one escape rule | Existing EF keys of tenants whose id contains a separator change (they miss once) |
| **B) Leave the EF tenant raw** | No key change | Today the EF tenant is concatenated raw, so a crafted tenant id can inject a segment into the key of the isolation dimension this feature adds |

### Chosen Option: **A — escape both**

### Rationale

Only A keeps a request in one tenant or module from addressing another's entries by crafting an identifier, which is what `AGENTS.md` §3 asks of a gate that must not be bypassed.

**Class:** Settled

**Settled by:** `AGENTS.md` §3 (security gates fail closed: a forgeable segment is a bypass) and the existing use of `CacheKeyRedactor.EscapeSegment` by `DefaultCacheKeyGenerator` for the same ids.

</details>

<details>
<summary><strong>7. How the EF package reaches `EscapeSegment` (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `InternalsVisibleTo` for `Encina.EntityFrameworkCore` in `Encina.Caching.csproj`** (the list today names the unit, integration, contract and property test projects and the Memory, Redis, Garnet, Valkey, Dragonfly and KeyDB providers, not EF Core) | No new public API; one more friend assembly | EF depends on an internal member of another package |
| **B) A public helper (for example `CacheKeySegment.Escape`) in `Encina.Caching`** | Explicit contract for any generator author | New public API to document, track in PublicAPI and keep stable |

### Chosen Option: **A — `InternalsVisibleTo`** (recommended, pending the maintainer)

### Rationale

A keeps the public surface unchanged; B wins if the maintainer wants third-party generators to escape segments the same way.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. EF key grammar and log redaction (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the positional tenant segment of the EF key (`{prefix}:{tenant}:{entity}:{hash}`), escaped; add `m:{module}` after it** | Smallest change; invalidation `{prefix}:*:{entityType}:*` keeps its shape | `CacheKeyRedactor` masks only `t:` and `u:` segments, so the raw tenant of an EF key is not masked, and `QueryCacheInterceptor` logs `cacheKey.Key` unredacted (hit, miss and populated messages): a gap that exists today and that this plan records, not fixes, as a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/ef-query-cache-key-logged-unredacted.md) |
| **B) Adopt a `t:{tenant}` marker in the EF key, as `DefaultCacheKeyGenerator` does** | The redactor then masks the tenant; one grammar across both generators | Changes every existing EF key and the pattern shapes that match them |

### Chosen Option: **A — keep the positional tenant** (recommended, pending the maintainer)

### Rationale

A confines this feature to the module dimension; B wins if the maintainer wants the redaction gap closed in this pull request. The Phase 3 tests state the current behavior explicitly (the redactor does not mask the positional EF tenant, the `m:` segment is left unmasked as non-personal data) so a change of grammar later is a visible test change.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Where `CacheConfiguration<TRequest>` gets the module (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICacheConfiguration<TRequest>.GenerateKey` gains an optional `string? module = null` parameter; `CacheConfiguration<TRequest>` adds the `m:` segment when it is non-null and `VaryByModule` is true** | Explicit, no ambient state in a class created by `new` and registered as a singleton with no dependency injection; callers pass `IModuleExecutionContext.CurrentModule` | Interface signature change (every implementer and caller in `src/` and `tests/` is updated: `ICacheConfiguration<TRequest>` is implemented by `CacheConfiguration<TRequest>` and referenced in 5 test files; the optional parameter keeps all 24 test calls compiling; `src/` has no caller of `GenerateKey`, verified) |
| **B) Read `context.GetModuleName()` (request-context metadata)** | No signature change | Nothing populates that metadata today, so the segment would never appear |
| **C) Leave `CacheConfiguration<TRequest>` without a module segment** | No change | The public type keeps producing module-less keys |

### Chosen Option: **A — an explicit `module` parameter** (recommended, pending the maintainer)

### Rationale

The class cannot read the ambient module through "the context the helper reads": it is not a DI service. A passes the value in, B waits for a producer that does not exist, C leaves a public key generator out of the isolation. Pre-1.0 breaking changes are acceptable, so the interface change is not a cost in itself.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Lifetime of `IModuleExecutionContext`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TryAddSingleton` in the four registrations (ADO.SqlServer, Dapper.SqlServer, EF Core, MongoDB)** | Its state is a static `AsyncLocal` (`ModuleExecutionContext._currentModule`), so lifetime is irrelevant to behavior; the singleton `DefaultCacheKeyGenerator` can take it without failing `ValidateScopes` | A lifetime change of a shared abstraction in four packages, each proven by a DI test in the same pull request |
| **B) Keep scoped and resolve through `IServiceScopeFactory` in the generator** | No registration change | A scope per key generation on a hot path; more moving parts |

### Chosen Option: **A — singleton, a consequence of D1 option B**

### Rationale

If D1 picks option A or C the lifetime change disappears with it. No consumer observes a difference because the state is not per-instance.

**Class:** Trivial

</details>

<details>
<summary><strong>11. Registration in the PostgreSQL and MySQL ADO and Dapper packages (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Out of scope: a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/module-execution-context-producer-outside-ef.md) covers the missing producer and the missing registration in the four packages** | Keeps this plan at the key-generation change | The cache stays module-less there until that issue lands (the D2 consequence) |
| **B) Register `IModuleExecutionContext` in those four packages here** | Closes the `AGENTS.md` §5 provider-coherence gap now | Registering the context without a producer changes nothing in behavior; four more registration files and tests |

### Chosen Option: **A — a follow-up issue** (recommended, pending the maintainer)

### Rationale

Registration without a producer adds files and no isolation; the producer and the registrations are one problem and belong to one issue. B wins if the maintainer wants coherence of registrations before the producer exists.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Pipeline-order requirement (Dilemma D12)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Document the required order (the module behavior outer, the caching behavior inner) and prove it with a pipeline-order test; do not reorder behaviors inside `AddEncinaCaching`** | No hidden registration side effects | A wrongly ordered application silently gets module-less keys |
| **B) `QueryCachingPipelineBehavior` fails closed (bypasses the cache) when module isolation is enabled and no module is set at the point it runs** | A wrong order cannot leak | Needs the "module isolation enabled" marker of D2 option B |

### Chosen Option: **A — document and test** (recommended, pending the maintainer)

### Rationale

A is the smallest change; B is the same fail-closed choice as D2 option B applied to the ordering problem, and the two should be decided together.

**Class:** Dilemma

</details>

<details>
<summary><strong>13. Module name casing and shape</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The module name is used as given by `IModuleExecutionContext.CurrentModule`, escaped, compared ordinally; no length limit** | Cannot merge two distinct modules into one entry; names are code identifiers | Two spellings of one module would cache separately (an extra miss, never a leak) |
| **B) Lower-case the name** | One entry per module regardless of spelling | Could merge two modules whose names differ only by case, a leak across modules |

### Chosen Option: **A — as given, escaped**

### Rationale

Only A cannot cause a cross-module hit. A separator inside the name is neutralized by the escape rule of Design Choice 6; length is bounded by the identifier the module author chose.

**Class:** Trivial

</details>

<details>
<summary><strong>14. Providers are unchanged; how the contract is proven (Dilemma D14)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No provider code; module-scoped cases (store under module A, miss under module B, pattern removal) added to the `CacheProviderContractTests` suite that the #277 plan creates, in a fourth and last pull request** | Keys are opaque strings to `ICacheProvider`; one suite proves all eight providers | The suite does not exist on main (verified: no such file under `tests/`), so the issue's acceptance criterion "contract test passes for every existing cache provider" and the closing of #746 wait for #277's suite |
| **B) Per-provider module tests** | Independent of #277 | Eight copies of one assertion |
| **C) Create the contract suite in this plan** | #746 closes on its own | Duplicates the suite #277 owns and conflicts with it |
| **D) Test only the generator and defer the acceptance criterion through a follow-up issue** | Cheapest, no wait | Does not prove the providers store and remove module-scoped keys and patterns |

### Chosen Option: **A — contract cases in the shared suite, in the last pull request** (recommended, pending the maintainer)

### Rationale

`AGENTS.md` §5 requires coherence across all eight caches, and a shared suite is the cheapest proof; it is a recommendation, not a settled rule, because B, C and D also satisfy §5 to different degrees. Pull requests 1 to 3 do not reference the suite: the generator behavior is covered by unit and property tests that do not wait for #277. Pull request 4 is last and opens only after the suite is on main; if #277 never lands it, the maintainer chooses between B, C and D at that point.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Lifetime of the module source (pull request 1)

<details>
<summary>Tasks</summary>

1. Register `IModuleExecutionContext` with `TryAddSingleton` (D10; it is backed by a static `AsyncLocal`) in the four registrations that use `TryAddScoped` today: `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` (line 168), `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs` (169), `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` (304), `src/Encina.MongoDB/ServiceCollectionExtensions.cs` (785). The PostgreSQL and MySQL ADO and Dapper packages register nothing: D11.
2. One DI test file (`tests/Encina.UnitTests/Modules/ModuleExecutionContextLifetimeTests.cs`) with one case per package that builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves `IModuleExecutionContext` from the root provider (a scoped registration fails there) and checks the singleton and the scoped consumers (`ModuleAwareConnectionFactory`, `SchemaValidatingCommand`, `ModuleExecutionContextBehavior`) still resolve.
3. Coverage manifest entries for the four registration files, in the same pull request as the DI test (Phase 6).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Cache key generation will read the ambient module through IModuleExecutionContext (src/Encina/Modules/Isolation/IModuleExecutionContext.cs, AsyncLocal-backed). DefaultCacheKeyGenerator is a singleton; IModuleExecutionContext is registered scoped in four packages.
TASK: Change the four registrations to TryAddSingleton and add the DI test with ValidateOnBuild and ValidateScopes, one case per package, and the four manifest entries.
KEY RULES: No [Obsolete] or compatibility shim; do not add registrations to the PostgreSQL or MySQL ADO and Dapper packages (D11); zero warnings.
REFERENCE FILES: src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs, src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, src/Encina.MongoDB/ServiceCollectionExtensions.cs, src/Encina/Modules/Isolation/ModuleExecutionContext.cs.
```

</details>

### Phase 2: Generators in `Encina.Caching` (pull request 2)

<details>
<summary>Tasks</summary>

1. Internal static `CacheKeyModule` in `src/Encina.Caching/KeyGeneration/` (new file): `string? ResolveSegment(string? module)` returns `m:{EscapeSegment(module)}` or `null` when the module is null or whitespace (D2 option A, D13).
2. `DefaultCacheKeyGenerator`: the constructor gains `IModuleExecutionContext? moduleContext = null` (an optional parameter, so the 27 existing `new DefaultCacheKeyGenerator(options)` call sites (24 in 9 test files, 3 in benchmarks) compile unchanged); the module segment goes after the tenant and before the user segment in `GenerateDefaultKey` and `GenerateFromTemplate`, honouring `CacheAttribute.VaryByModule` (D5); `GeneratePattern` and `GeneratePatternFromTemplate` carry the pattern chosen in D3 (`m:*` for option B).
3. `CacheAttribute`: `public bool VaryByModule { get; init; } = true;` with XML docs; PublicAPI line.
4. `CacheConfiguration<TRequest>` (in `CacheableQueryAttribute.cs`): `VaryByModule` property and the module part in `ScopeParts`; per D9 option A, `ICacheConfiguration<TRequest>.GenerateKey` gains `string? module = null` (an optional parameter, so the 24 `GenerateKey` calls in the two `CacheConfigurationTests.cs` files compile unchanged; the only implementer is `CacheConfiguration<TRequest>` and `src/` has no caller). PublicAPI lines for the interface and class changes.
5. A pipeline-order test (Phase 4) in the same pull request, since it proves the generator reads the module.
6. Update the class remarks that describe the key format (`DefaultCacheKeyGenerator` summary lists `{prefix}:{tenant}:{user}:...`).
7. `ShardCacheKeyGenerator` is left unchanged (Design Choice 4).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 (pull request 1) made IModuleExecutionContext a singleton. Keys are built at four places in DefaultCacheKeyGenerator and one in CacheConfiguration<TRequest>.
TASK: Add the CacheKeyModule helper; inject IModuleExecutionContext? (optional) into DefaultCacheKeyGenerator; add the m:{module} segment (after t:, before u:) to GenerateDefaultKey, GenerateFromTemplate and CacheConfiguration.ScopeParts (D9 option A: a string? module parameter); add VaryByModule (default true, D5) to CacheAttribute and CacheConfiguration; make GeneratePattern and GeneratePatternFromTemplate follow Dilemma D3 as the maintainer answered it (until then option B: m:* in the module position, which matches module-scoped keys only).
KEY RULES: The segment is omitted when no module is known (D2); the module is escaped with the same rule as tenant and user; a generated pattern must match every key the same request type can produce.
REFERENCE FILES: src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs, src/Encina.Caching/Attributes/CacheAttribute.cs, src/Encina.Caching/Attributes/CacheableQueryAttribute.cs, tests/Encina.UnitTests/Caching/Base/DefaultCacheKeyGeneratorTests.cs.
```

</details>

### Phase 3: EF Core query cache key generator (pull request 3)

<details>
<summary>Tasks</summary>

1. `DefaultQueryCacheKeyGenerator.Generate(command, context)` and `Generate(command, context, requestContext)`: add `m:{module}` after the tenant when a module is known (D8 option A keeps the positional tenant). The class takes `IModuleExecutionContext? moduleContext = null` (an optional parameter, so the 10 existing `new DefaultQueryCacheKeyGenerator(options)` call sites in 4 test and benchmark files compile unchanged). The tenant is also escaped with the shared rule (Design Choice 6) so a tenant id containing `:` cannot forge a module segment (today it is concatenated raw); this changes the existing EF key of a tenant whose id contains a separator, which misses once.
2. `QueryCacheInterceptor` invalidation keeps `{prefix}:*:{entityType}:*`: a test proves it removes keys with and without a module and with and without a tenant.
3. Make `EscapeSegment` reachable from `Encina.EntityFrameworkCore` per D7 (option A: `InternalsVisibleTo` in `Encina.Caching.csproj`; PublicAPI if option B).
4. `QueryCacheInvalidationCdcHandler` (`sm:qc:*:Orders:*`) is covered by the same test since it uses the same pattern shape.
5. A test records the redaction behavior of D8: `CacheKeyRedactor` leaves the positional EF tenant unmasked (the gap is the follow-up issue to be opened, draft: artifacts/decisions/plan-batch/followups/ef-query-cache-key-logged-unredacted.md) and does not mask the `m:` segment.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The EF query cache builds keys from SQL commands; the interceptor runs inside the handler scope where IModuleExecutionContext.CurrentModule is set by ModuleExecutionContextBehavior. Keys are {prefix}:{tenant}:{entity}:{hash} today.
TASK: Add the module segment to both Generate overloads of DefaultQueryCacheKeyGenerator, escape the tenant and module, expose the escape helper to the EF package as D7 was answered (InternalsVisibleTo until then), add tests that the invalidation pattern still removes keys of every shape and the redaction-behavior test of D8.
KEY RULES: No change to the invalidation pattern format; the module segment is omitted when no module is known; update the class XML example keys; PublicAPI for any new public member.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs, src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs, src/Encina.EntityFrameworkCore/Modules/ModuleExecutionContextBehavior.cs, tests/Encina.UnitTests/EntityFrameworkCore/Caching.
```

</details>

### Phase 4: Configuration, DI & pipeline order (pull request 2, README in pull request 3)

<details>
<summary>Tasks</summary>

1. Confirm that `AddEncinaCaching` still registers `DefaultCacheKeyGenerator` with `TryAddSingleton` and that the optional module dependency is resolved when any of the four registrations ran, and is null otherwise (this is the DI test that proves the generator's new constructor parameter, in pull request 2, on top of the lifetime change of pull request 1).
2. A pipeline-order test (D12 option A): run a `[Cache]` request through the real pipeline with `ModuleExecutionContextBehavior` registered before and after `QueryCachingPipelineBehavior`; assert the key contains the module only when the module behavior is outer, and document the required order in the `Encina.Caching` README (pull request 3).
3. A custom `ICacheKeyGenerator` registered with `AddCacheKeyGenerator<T>` keeps working; the interface does not change.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Behavior order in Encina is the order of registration. The module behavior lives in Encina.EntityFrameworkCore; the caching behaviors in Encina.Caching.
TASK: Add the DI and pipeline-order tests; document the required order; verify AddCacheKeyGenerator<T> is unaffected.
KEY RULES: Registration completeness is proven by a provider built with ValidateOnBuild and ValidateScopes; do not reorder behaviors silently inside AddEncinaCaching.
REFERENCE FILES: src/Encina.Caching/ServiceCollectionExtensions.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, tests/Encina.UnitTests/Caching/Base/BaseServiceCollectionExtensionsTests.cs.
```

</details>

### Phase 5: Cross-Cutting Integration and Observability

<details>
<summary>Tasks</summary>

1. Multi-tenancy: the tenant and module segments are tested together (tenant only, module only, both, neither, and a tenant id containing `:`).
2. Module isolation: the feature itself.
3. Observability: not applicable. Key computation is a pure function; no span or metric is added, and logs must never contain keys (existing `CacheKeyRedactor` rule). No EventIds are allocated.
4. All other functions: not applicable (matrix below).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The matrix at the end of the plan marks only Caching, Multi-Tenancy and Module Isolation as included.
TASK: Add the combined tenant and module key tests; add no ActivitySource, Meter or log messages.
KEY RULES: Keys never reach logs unredacted; the module name is not personal data and needs no redaction; do not allocate an EventId range.
REFERENCE FILES: src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs, tests/Encina.UnitTests/Caching/Base/DefaultCacheKeyGeneratorTests.cs.
```

</details>

### Phase 6: Testing — 7 test types

<details>
<summary>Tasks</summary>

Per-flag targets for the files the pull requests add or change, entered in the package manifest (`.github/coverage-manifest/Encina.Caching.json`, `Encina.EntityFrameworkCore.json` and the three others of pull request 1) **in the same pull request as the tests that meet them**, each with a one-sentence justification in the manifest's `justifications`. Targets never go down: an existing target is kept or raised, and any baseline is measured at work start (`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` per flag, then `coverage-report.cs`).

| Pull request | File | unit | guard | contract | property | Justification |
|:---:|------|:---:|:---:|:---:|:---:|---------------|
| 1 | The four `ServiceCollectionExtensions.cs` registration files (ADO.SqlServer, Dapper.SqlServer, EF Core, MongoDB) | existing target kept (origin/main: unit 55, 52, 64, 55 and guard 55, 5, 60, 2 respectively) or higher | kept | kept | kept | The new DI test runs the changed registration line in each package; the rest of each file keeps the target of its existing registration tests. |
| 2 | `Encina.Caching/KeyGeneration/CacheKeyModule.cs` (new, internal) | 100 | 0 | 0 | 80 | A single small function with null, whitespace and value branches that unit tests reach fully and the key property test drives for generated modules; guard 0 because the type is internal and `Encina.Caching.csproj` grants `InternalsVisibleTo` to the unit, integration, contract and property projects but not to `Encina.GuardTests`; contract 0 because an internal helper has no contract. |
| 2 | `Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` | 95 (kept) | 11 (kept) | 0 | 60 (new) | The new module branches in key, template and pattern generation are unit-tested with and without module, tenant and user; the guard flag reaches only the argument checks and the valid-null constructor case; the property tests drive key and pattern generation for generated inputs. |
| 2 | `Encina.Caching/Attributes/CacheAttribute.cs` | 100 | 0 | 0 | 0 | One new `init` property read by the generator tests; no parameters or branches for guard, contract or property tests to reach. |
| 2 | `Encina.Caching/Attributes/CacheableQueryAttribute.cs` | 100 (kept; origin/main has unit 100, guard 0) | 25 (raised from 0) | 0 | 0 | `ScopeParts` and `GenerateKey` gain the module branch covered by the `CacheConfigurationTests` cases, so the unit target stays at 100; guard reaches the valid-null `module` case of the new parameter. |
| 3 | `Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs` (no target on origin/main today) | 90 (new) | 25 (new) | 0 | 50 (new) | Both `Generate` overloads gain the module and escaping branches reached by unit tests; guard covers the argument checks and the valid-null module context; the property test covers key stability and distinct modules. |

Contract is 0 for every file: no public interface or abstract class is added, and AGENTS.md section 9 never accepts a `.md` justification for contract tests; the real contract proof is pull request 4 (D14).

1. **Unit**: keys and patterns with and without module and tenant and user (`tests/Encina.UnitTests/Caching/Base/DefaultCacheKeyGeneratorTests.cs`, `CacheAttributeTests.cs`, `CacheConfigurationTests.cs` in `Caching/Base/`, a new `CacheKeyModuleTests.cs`, and in pull request 3 `tests/Encina.UnitTests/EntityFrameworkCore/Caching`); `VaryByModule = false`; a module name containing `:`; the pipeline-order test (pull request 2).
2. **Guard**: `DefaultCacheKeyGeneratorGuardTests.cs` (pull request 2): a null module context gives a working generator and a null module yields a module-less key (valid-null results, not exception tests, per the #3798 obligation); `QueryCachingGuardTests.cs` (pull request 3) for the EF constructor parameter in the same way.
3. **Contract**: pull request 4 only (D14 option A): the module-scoped cases (store under module A, miss under module B, pattern removal) added to the `CacheProviderContractTests` suite created by the #277 plan; no earlier pull request references it.
4. **Property**: two distinct modules never produce the same key for equal requests; a generated pattern matches every module-scoped key the same request type can produce (D3); the key is stable for the same inputs (`CacheKeyGeneratorPropertyTests.cs`, `QueryCacheKeyPropertyTests.cs`).
5. **Integration**: justification `.md` (keys are strings; the contract suite of pull request 4 covers providers; non-database feature).
6. **Load**: justification `.md` (pure computation).
7. **Benchmark**: justification `.md` (key generation is not a measured hot path today; one `--job dry` run in pull request 2 confirms no regression of `DefaultCacheKeyGenerator`). The three justification files (integration, load, benchmark) land in pull request 3, the last pull request that changes code.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/Encina.Caching.json and Encina.EntityFrameworkCore.json; targets never go down. Tests must execute real code.
TASK: Write the unit, guard and property tests, the manifest entries with their justification sentences and the three justification files; add the contract cases to the shared suite only in pull request 4, when it exists on main; measure each flag and run the CRAP check on the changed methods.
KEY RULES: Shouldly assertions; no Thread.Sleep; builders for test data; justification files only for integration (non-database), load and benchmark, never for unit, guard or contract; guard tests of a nullable parameter assert the valid result.
REFERENCE FILES: tests/Encina.UnitTests/Caching/Base/DefaultCacheKeyGeneratorTests.cs, tests/Encina.PropertyTests/Infrastructure/Caching, .github/coverage-manifest/Encina.Caching.json, docs/testing/coverage-measurement-methodology.md.
```

</details>

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs for `VaryByModule` and the constructor change; update the key-format remarks.
2. `src/Encina.Caching/README.md`: the key grammar with the module segment, the required pipeline order, the D2 behavior.
3. Configuration guide and the cache key section of the caching guide (via `docs-writer`).
4. ADR only if D1 changes `IRequestContext` (option A); not needed for B (ADR number assigned when the ADR pull request opens, if ever).
5. `changelog.d/746-module-id-cache-keys.changed.md` (cached entries miss once after the upgrade).
6. PublicAPI files; `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Code and tests are done. Documentation is English, never types coverage figures, and cites ADRs and SPECs.
TASK: Update the XML docs, the Encina.Caching README, the caching guide key section and write the changelog fragment through the specialists that own them.
KEY RULES: Never edit CHANGELOG.md; explain that existing cached entries miss once; show one before/after key example.
REFERENCE FILES: src/Encina.Caching/README.md, changelog.d/README.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` §6, function 11 | Module isolation | Cache is the surface being isolated |
| `AGENTS.md` §5 (Caching) | Provider coherence | Proven by the contract suite of #277 |
| `AGENTS.md` §3 | Registration completeness | DI test with `ValidateOnBuild`/`ValidateScopes` for the lifetime change |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DefaultCacheKeyGenerator` | `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` | Main change site |
| `CacheConfiguration<TRequest>` | `src/Encina.Caching/Attributes/CacheableQueryAttribute.cs` | Second key site |
| `CacheAttribute` (`VaryByTenant`, `VaryByUser`) | `src/Encina.Caching/Attributes/CacheAttribute.cs` | Template for `VaryByModule` |
| `CacheKeyRedactor.EscapeSegment` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | Segment escaping (internal today) |
| `IModuleExecutionContext`, `ModuleExecutionContext` | `src/Encina/Modules/Isolation/` | Ambient module source (D1) |
| `ModuleExecutionContextBehavior` | `src/Encina.EntityFrameworkCore/Modules/` | The only producer of `CurrentModule` |
| `IRequestContext.GetModuleName()` | `src/Encina/Modules/RequestContextModuleExtensions.cs` | Option A source; read by ABAC and Retention |
| `DefaultQueryCacheKeyGenerator`, `QueryCacheInterceptor` | `src/Encina.EntityFrameworkCore/Caching/` | EF key and invalidation pattern |
| `QueryCachingPipelineBehavior.TenantAllowsCache` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | Tenant fail-closed precedent (D2) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` | none | No logging is added; the existing range 3500-3549 is untouched |
| `Encina.EntityFrameworkCore` (query cache) | none | Existing range 2400-2449 is untouched |

### Estimated File Count

| Pull request | Production | PublicAPI | Manifests | Tests | Justifications | Docs | Total |
|--------------|-----------:|----------:|----------:|------:|---------------:|-----:|------:|
| 1: lifetime of `IModuleExecutionContext` | 4 (the four `ServiceCollectionExtensions.cs`) | 0 | 4 (`Encina.ADO.SqlServer.json`, `Encina.Dapper.SqlServer.json`, `Encina.EntityFrameworkCore.json`, `Encina.MongoDB.json`) | 1 (DI test, four cases) | 0 | 0 | **9** |
| 2: `Encina.Caching` key generation | 4 (`CacheKeyModule.cs` new, `DefaultCacheKeyGenerator.cs`, `CacheAttribute.cs`, `CacheableQueryAttribute.cs`) | 1 (`Encina.Caching`) | 1 (`Encina.Caching.json`) | 7 (unit x3 extended and 1 new, guard, property, pipeline-order and DI) | 0 | 0 | **13** |
| 3: EF generator and docs | 2 (`DefaultQueryCacheKeyGenerator.cs`, `Encina.Caching.csproj` for D7 option A) | 1 (`Encina.EntityFrameworkCore`) | 1 (`Encina.EntityFrameworkCore.json`, touched again) | 5 (unit x2, redaction behavior, guard, property) | 3 (integration, load, benchmark) | 3 (README, guide section, changelog fragment) | **15** |
| 4: contract cases (waits for the #277 suite) | 0 | 0 | 0 | 1 (cases added to `CacheProviderContractTests`) | 0 | 0 | **1** |
| **Total** | 10 | 2 | 6 | 14 | 3 | 3 | **38 file touches, 37 distinct files** (`Encina.EntityFrameworkCore.json` is touched in pull requests 1 and 3) |

Every pull request is at most 15 files and about 1,500 lines or fewer (about 300 production, 900 test and a few hundred documentation lines in total across the four). The existing test call sites of the changed constructors and of `GenerateKey` (27 of `DefaultCacheKeyGenerator`, 10 of `DefaultQueryCacheKeyGenerator`, 24 of `CacheConfiguration<TRequest>.GenerateKey`) are not counted because the added parameters are optional and they compile unchanged. No pull request uses a type or project created by a later one: pull request 2 relies on the lifetime of pull request 1, pull request 3 on the helper of pull request 2, and pull request 4 on the contract suite that #277 puts on main before it opens.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Caching has eight providers (AGENTS.md §5) that treat keys as opaque strings; keys are produced by DefaultCacheKeyGenerator, CacheConfiguration<TRequest> and the EF DefaultQueryCacheKeyGenerator. No backward compatibility, zero warnings.

IMPLEMENTATION OVERVIEW: Four pull requests. 1: singleton registration of IModuleExecutionContext in four packages with its DI test and manifests; 2: segment helper, optional IModuleExecutionContext injection, module segment and VaryByModule in Encina.Caching generators and patterns (D3, D5, D9), DI and pipeline-order tests; 3: EF generator, EscapeSegment access (D7), redaction-behavior test (D8), docs; 4: module-scoped contract cases in the #277 suite once it is on main (D14). Phase 5 tenant plus module combination tests, no observability. Dilemmas D1 (module source), D2 (no module), D3 (invalidation across modules), D5, D7, D8, D9, D11, D12 and D14 are implemented as the maintainer answered them, the recommended options until then.

KEY PATTERNS: m:{module} after t: and before u:, escaped with the shared rule; omitted when no module; patterns match every key a request can produce; ValidateOnBuild and ValidateScopes for the lifetime change; Shouldly; targets in the coverage manifests never go down; CRAP at or below 10 on changed methods.

REFERENCE FILES: src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs, src/Encina.Caching/Attributes/CacheableQueryAttribute.cs, src/Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs, src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs, src/Encina/Modules/Isolation/IModuleExecutionContext.cs, tests/Encina.UnitTests/Caching/Base/DefaultCacheKeyGeneratorTests.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | This is the caching key; `[Cache]` gains `VaryByModule`. |
| 2 | OpenTelemetry | ❌ | Pure key computation, no operation worth a span or metric. |
| 3 | Structured Logging | ❌ | No operational event; keys must not be logged unredacted anyway. |
| 4 | Health Checks | ❌ | No dependency to check. |
| 5 | Validation | ❌ | No input received. |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | No shared state mutated. |
| 8 | Transactions | ❌ | No transactional state is involved in key generation. |
| 9 | Idempotency | ❌ | The idempotency behavior keys are caller-provided and not module data. |
| 10 | Multi-Tenancy | ✅ | The module segment sits after the tenant segment; both dimensions are tested together. |
| 11 | Module Isolation | ✅ | This feature. |
| 12 | Audit Trail | ❌ | Key computation generates no audit events. |

---

## Prerequisites & Dependencies

- **#277** owns the `ICacheProvider` contract suite (`CacheProviderContractTests`, not on main today); pull request 4 adds the module-scoped cases there and is the last pull request of this plan. Pull requests 1 to 3 do not wait for it and never reference it, but #746 closes only when pull request 4 merges (D14).
- **Follow-up issues to be opened** (drafts in `artifacts/decisions/plan-batch/followups/`; open them before pull request 1 merges):
  - the missing producer of `IModuleExecutionContext` outside the EF package, and the missing registration in the PostgreSQL and MySQL ADO and Dapper packages: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/module-execution-context-producer-outside-ef.md);
  - cache invalidation patterns that do not match keys without a tenant segment (`GeneratePattern` emitting `t:*`, and `{prefix}:*:{entityType}:*` against `{prefix}:{entity}:{hash}`): follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/cache-invalidation-pattern-tenantless-mismatch.md);
  - `CacheConfiguration<TRequest>.GenerateKey` using `request.GetHashCode()` as the key: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/cache-configuration-key-uses-gethashcode.md);
  - the EF query cache logging its key unredacted: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/ef-query-cache-key-logged-unredacted.md).
- **Related**: #710 (authorization cache) follows this key grammar for its module dimension.

---

## Next Steps

1. Ask the maintainer D1, D2, D3, D5, D7, D8, D9, D11, D12 and D14 when work on #746 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1 (9 files): Phase 1, the lifetime change in the four packages with its DI test and manifests.
3. Pull request 2 (13 files): Phase 2 and the pipeline-order test of Phase 4 for `Encina.Caching`, with unit, guard and property tests, PublicAPI and the manifest.
4. Pull request 3 (15 files, at the cap): Phase 3 (EF generator and escape access), the three justification files, the README, guide section and changelog fragment of Phase 7.
5. Pull request 4 (1 file): the module-scoped cases in the #277 contract suite, opened only after that suite is on main.
