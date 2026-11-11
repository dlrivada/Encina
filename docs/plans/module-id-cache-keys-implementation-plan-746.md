# Implementation Plan: ModuleId dimension in cache key generation

> **Issue**: [#746](https://github.com/dlrivada/Encina/issues/746)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 2 packages touched, 2 pull requests)
> **Estimated Scope**: ~250-350 lines of production code + ~900 lines of tests

---

## Summary

Cache keys carry the tenant (`t:{tenant}`) and optionally the user (`u:{user}`) but not the module. In a modular monolith, module A's cached entry for `GetOrderQuery` can be served to module B when the request type name and content are equal. This plan adds a `m:{module}` segment to every key and invalidation pattern that Encina generates for request and query caching, so cached entries are isolated per module (`AGENTS.md` §6, function 11).

Where keys are built today (all verified in the code):

| Site | File | Key shape today |
|------|------|-----------------|
| `DefaultCacheKeyGenerator` (key, template key, `GeneratePattern`, `GeneratePatternFromTemplate`) | `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` | `{prefix}:t:{tenant}:u:{user}:{RequestType}:{hash}` |
| `CacheConfiguration<TRequest>.GenerateKey` | `src/Encina.Caching/Attributes/CacheableQueryAttribute.cs` | `t:`/`u:` parts, type name, hash |
| `DefaultQueryCacheKeyGenerator` (EF Core query cache) | `src/Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs` | `{prefix}:{tenant}:{entity}:{hash}` (no `t:` marker, tenant not escaped) |
| `ShardCacheKeyGenerator` | `src/Encina.Caching/Sharding/ShardCacheKeyGenerator.cs` | infrastructure keys (directory, topology), not request data |

The eight cache providers need no change: `ICacheProvider` takes the key as an opaque string. Provider coherence (`AGENTS.md` §5) is proven by module-scoped cases in the shared `ICacheProvider` contract suite that the Memcached plan (#277) owns.

Three facts shape the design (details in Design Choices):

1. The module is known in two places: the ambient `IModuleExecutionContext.CurrentModule` (an `AsyncLocal`, set by `ModuleExecutionContextBehavior` in `Encina.EntityFrameworkCore` from `IModuleHandlerRegistry`) and the request-context metadata read by `IRequestContext.GetModuleName()` (`src/Encina/Modules/RequestContextModuleExtensions.cs`), which no framework component sets. The issue text says the context has no module property; the metadata accessor exists but is never populated.
2. `IModuleExecutionContext` is registered `TryAddScoped` in the ADO, Dapper, EF Core and MongoDB packages, while `DefaultCacheKeyGenerator` is a singleton (`TryAddSingleton` in `AddEncinaCaching`); injecting it as is would fail `ValidateScopes`.
3. Invalidation patterns already use a wildcard in the middle (`{prefix}:*:{entityType}:*` in `QueryCacheInterceptor`, lines 542-590), which keeps matching keys that gain a module segment.

Provider category: **Caching (8)**, key generation only. Affected packages: `Encina.Caching`, `Encina.EntityFrameworkCore`, and the registration of `IModuleExecutionContext` in `Encina.ADO.*`/`Encina.Dapper.*`/`Encina.MongoDB` if D1 keeps the ambient source.

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
| **A) Omit the `m:` segment** (non-modular applications unaffected) | Same rule the tenant segment follows in the generator; keys of non-modular apps do not change | If module resolution fails for a module's handler, its entries land in the shared module-less scope |
| **B) Bypass the cache when module isolation is enabled but no module is known** (fail closed, like `TenantAllowsCache` does for tenants when multi-tenancy is on) | No shared module-less entries when isolation is on | Needs a marker for "module isolation enabled"; handlers outside any module (shared code) would lose caching |
| **C) Use a fixed placeholder such as `m:none`** | Every key has the segment, patterns are uniform | Changes all keys of non-modular apps; adds a segment with no information |

### Chosen Option: **A — omit the segment** (recommended, pending the maintainer)

### Rationale

The module is a scoping dimension, not an access gate, so the fail-closed rule for compliance gates (`AGENTS.md` §3) does not apply, and a module-less entry cannot be read by a module-scoped lookup because the key shapes differ. `QueryCachingPipelineBehavior.TenantAllowsCache` bypasses the cache for a missing tenant because a tenant-less key would be shared by every tenant; the module analogue (B) wins in deployments that treat module isolation as a security boundary and cannot tolerate a shared module-less scope. The issue listed A as settled; because the tenant code has a fail-closed branch, the choice is recorded as a Dilemma.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Invalidation across modules (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A pattern carries the executing module** (`m:{current}`), so a command in module A invalidates only A's entries | Strict isolation, consistent with the keys | A write in module A leaves stale entries for the same data cached under module B |
| **B) A pattern spans all modules** (a wildcard in the module position, also matching module-less keys) | Removal is always safe: a write never leaves a stale entry in another module; the EF pattern `{prefix}:*:{entityType}:*` already behaves this way | Invalidation reaches into other modules' entries (it only deletes, no module reads another's data) |
| **C) `[InvalidatesCache]` gains an `AllModules` flag, default per attribute** | The application chooses per command | One more setting; a wrong default leaves stale data |

### Chosen Option: **B — patterns span all modules** (recommended, pending the maintainer)

### Rationale

Deleting an entry is always correct for a cache; leaving a stale one is not. B keeps `GeneratePattern` and `GeneratePatternFromTemplate` aligned with the EF pattern. A wins when module teams own disjoint data and strict isolation of cache operations is a requirement; C wins when both kinds of commands exist in one application. Note the existing mismatch recorded in Prerequisites: `GeneratePattern` emits `t:*` for a context without tenant while keys then have no `t:` segment; the plan does not copy that mismatch for modules, and a property test asserts that a generated pattern matches every key a request can produce.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Key format, attribute flag and scope of the change</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `m:{module}` placed after the tenant and before the user segment, escaped with the same rule as `t:`/`u:`; `VaryByModule` (default true) on `[Cache]` and `CacheConfiguration<TRequest>`; the three generators of `Encina.Caching` and the EF generator change; `ShardCacheKeyGenerator` does not** | One key grammar; same opt-out style as `VaryByTenant`; shard directory and topology entries are cluster infrastructure, not module data | Entries cached before the change miss once (pre-1.0, accepted) |
| **B) A prefix per module in `KeyPrefix` configuration** | No code in generators | Per-module configuration of every cache site; easy to forget |
| **C) Hash the module into the request hash** | No new segment | Patterns can no longer target a module; the key stops being readable |

### Chosen Option: **A — one `m:` segment, `VaryByModule`, four generators**

### Rationale

The segment follows the existing grammar of `DefaultCacheKeyGenerator`. Module names are code identifiers, not personal data, so `CacheKeyRedactor` (which masks `t:` and `u:`) needs no change; the separator is escaped with `CacheKeyRedactor.EscapeSegment`, which is `internal` today, so the EF package needs `InternalsVisibleTo` or a shared public helper (fixed at implementation, recorded in PublicAPI). The EF key has no `t:` marker, so the module segment follows the tenant as `m:{module}`: keys with a module have one more segment than keys without, which keeps the shapes distinct. Adding a module also makes keys of tenant-less EF entries match the `{prefix}:*:{entityType}:*` invalidation pattern (see the findings in Prerequisites).

**Class:** Trivial

**Settled by:** `AGENTS.md` §6 (function 11, module isolation) and the existing grammar of `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` (`VaryByTenant` precedent in `CacheAttribute.cs`).

</details>

<details>
<summary><strong>5. Providers are unchanged; proof by the shared contract suite</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No provider code; module-scoped cases in the `ICacheProvider` contract suite of #277** | Keys are opaque strings to `ICacheProvider`; one test proves all eight | The contract step waits for #277 if it merges later |
| **B) Per-provider module tests** | Independent of #277 | Eight copies of one assertion |
| **C) Test only the generator** | Cheapest | Does not prove providers store and remove module-scoped keys and patterns |

### Chosen Option: **A — contract cases in the shared suite**

### Rationale

`AGENTS.md` §5 requires coherence across all eight caches; a shared suite is the cheapest way to prove it. The generator's own behavior is covered by unit and property tests in this feature's pull requests, which do not wait for #277.

**Class:** Settled

**Settled by:** `AGENTS.md` §5 (Caching row, provider coherence), `src/Encina.Caching/Abstractions/ICacheProvider.cs` (opaque keys), and the #277 plan, which owns the suite.

</details>

---

## Implementation Phases

### Phase 1: Core — module source and the segment helper

<details>
<summary>Tasks</summary>

1. Internal static `CacheKeyModule` (or a method on `CacheKeyRedactor`) in `src/Encina.Caching/KeyGeneration/`: `string? ResolveSegment(string? module)` returns `m:{EscapeSegment(module)}` or `null` when the module is null or whitespace (D2 option A).
2. `DefaultCacheKeyGenerator` constructor gains `IModuleExecutionContext? moduleContext = null`; an internal `CurrentModule` property returns `_moduleContext?.CurrentModule`.
3. Register `IModuleExecutionContext` with `TryAddSingleton` (it is backed by a static `AsyncLocal`) in the four registrations that use `TryAddScoped` today: `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` (line 168), `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs` (169; the PostgreSQL and MySQL ADO/Dapper packages contain no such registration at all, recorded in the module-producer finding), `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` (304), `src/Encina.MongoDB/ServiceCollectionExtensions.cs` (785). Confirm with a DI test using `ValidateOnBuild` and `ValidateScopes` that the singleton generator and the scoped consumers resolve.
4. PublicAPI: constructor change of `DefaultCacheKeyGenerator` in `src/Encina.Caching/PublicAPI.Unshipped.txt`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Cache keys need a module segment. The module is available as IModuleExecutionContext.CurrentModule (src/Encina/Modules/Isolation/IModuleExecutionContext.cs, AsyncLocal-backed). DefaultCacheKeyGenerator is a singleton; IModuleExecutionContext is currently registered scoped in four packages.
TASK: Add the m: segment helper, inject IModuleExecutionContext? into DefaultCacheKeyGenerator, change the four registrations to TryAddSingleton, update PublicAPI.Unshipped.txt, and add the DI test with ValidateOnBuild and ValidateScopes.
KEY RULES: No [Obsolete] or compatibility shim; the dependency is optional so non-modular applications keep working; XML docs on every public member; zero warnings.
REFERENCE FILES: src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs, src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs, src/Encina/Modules/Isolation/ModuleExecutionContext.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs.
```

</details>

### Phase 2: Generators in `Encina.Caching`

<details>
<summary>Tasks</summary>

1. `DefaultCacheKeyGenerator`: the module segment after the tenant and before the user segment in `GenerateDefaultKey` and `GenerateFromTemplate`, honouring `CacheAttribute.VaryByModule`; `GeneratePattern` and `GeneratePatternFromTemplate` carry the pattern chosen in D3 (a wildcard in the module position for B).
2. `CacheAttribute`: `public bool VaryByModule { get; init; } = true;` with XML docs; PublicAPI line.
3. `CacheConfiguration<TRequest>` (in `CacheableQueryAttribute.cs`): `VaryByModule` property and the module part in `ScopeParts`; it needs the current module, so `GenerateKey` reads it through the same helper (the class is created by `AddCacheConfiguration`, so the module is passed in through the context the helper reads, decided with D1).
4. Update the class remarks that describe the key format (`DefaultCacheKeyGenerator` summary lists `{prefix}:{tenant}:{user}:...`).
5. `ShardCacheKeyGenerator` is left unchanged (Design Choice 4).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 added the segment helper and the injected module source. Keys are built at four places in DefaultCacheKeyGenerator and one in CacheConfiguration<TRequest>.
TASK: Add the m:{module} segment (after t:, before u:) to GenerateDefaultKey, GenerateFromTemplate and CacheConfiguration.ScopeParts; add VaryByModule (default true) to CacheAttribute and CacheConfiguration; make GeneratePattern and GeneratePatternFromTemplate follow Dilemma D3 as the maintainer answered it (until then option B, a module wildcard that also matches module-less keys).
KEY RULES: The segment is omitted when no module is known (D2); the module is escaped with the same rule as tenant and user; a generated pattern must match every key the same request type can produce.
REFERENCE FILES: src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs, src/Encina.Caching/Attributes/CacheAttribute.cs, src/Encina.Caching/Attributes/CacheableQueryAttribute.cs, tests/Encina.UnitTests/Caching/Base/DefaultCacheKeyGeneratorTests.cs.
```

</details>

### Phase 3: EF Core query cache key generator

<details>
<summary>Tasks</summary>

1. `DefaultQueryCacheKeyGenerator.Generate(command, context)` and `Generate(command, context, requestContext)`: add `m:{module}` after the tenant when a module is known. The class takes `IModuleExecutionContext? moduleContext = null`. The tenant is also escaped with the shared rule so a tenant id containing `:` cannot forge a module segment (today it is concatenated raw).
2. `QueryCacheInterceptor` invalidation keeps `{prefix}:*:{entityType}:*`: a test proves it removes keys with and without a module and with and without a tenant.
3. Make `EscapeSegment` reachable from `Encina.EntityFrameworkCore` (`InternalsVisibleTo` or a public helper; PublicAPI if public).
4. `QueryCacheInvalidationCdcHandler` (`sm:qc:*:Orders:*`) is covered by the same test since it uses the same pattern shape.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The EF query cache builds keys from SQL commands; the interceptor runs inside the handler scope where IModuleExecutionContext.CurrentModule is set by ModuleExecutionContextBehavior. Keys are {prefix}:{tenant}:{entity}:{hash} today.
TASK: Add the module segment to both Generate overloads of DefaultQueryCacheKeyGenerator, escape the tenant and module, expose the escape helper to the EF package, and add tests that the invalidation pattern still removes keys of every shape.
KEY RULES: No change to the invalidation pattern format; the module segment is omitted when no module is known; update the class XML example keys; PublicAPI for any new public member.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs, src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs, src/Encina.EntityFrameworkCore/Modules/ModuleExecutionContextBehavior.cs, tests/Encina.UnitTests/EntityFrameworkCore/Caching.
```

</details>

### Phase 4: Configuration, DI & pipeline order

<details>
<summary>Tasks</summary>

1. Confirm that `AddEncinaCaching` still registers `DefaultCacheKeyGenerator` with `TryAddSingleton` and that the optional module dependency is resolved when any of the four registrations ran, and is null otherwise.
2. A pipeline-order test: run a `[Cache]` request through the real pipeline with `ModuleExecutionContextBehavior` registered before and after `QueryCachingPipelineBehavior`; assert the key contains the module only when the module behavior is outer, and document the required order in the `Encina.Caching` README.
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

Targets proposed for the touched files (manifest entries are raised, never lowered; measured values replace these when the code exists):

| File | unit | guard | contract | property |
|------|:---:|:---:|:---:|:---:|
| `KeyGeneration/DefaultCacheKeyGenerator.cs` | 95 (unchanged) | 11 (unchanged) | 0 | 60 |
| `Attributes/CacheAttribute.cs`, `CacheableQueryAttribute.cs` | existing | existing | 0 | 0 |
| `KeyGeneration/` helper for the module segment | 100 | 20 | 0 | 80 |
| `Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs` | 90 | 25 | 0 | 50 |

1. **Unit**: keys and patterns with and without module and tenant (`tests/Encina.UnitTests/Caching/Base/DefaultCacheKeyGeneratorTests.cs`, `CacheAttributeTests.cs`, `CacheConfigurationTests.cs`, `tests/Encina.UnitTests/EntityFrameworkCore/Caching`); `VaryByModule = false`; module name containing `:`.
2. **Guard**: new constructor parameter and public members.
3. **Contract**: module-scoped cases (store under module A, miss under module B, pattern removal) added to the `CacheProviderContractTests` suite owned by #277; if #277 has not merged, this step moves to its pull request.
4. **Property**: two distinct modules never produce the same key for equal requests; a generated pattern matches every key the same request type can produce; the key is stable for the same inputs.
5. **Integration**: justification `.md` (keys are strings, the contract suite covers providers).
6. **Load**: justification `.md` (pure computation).
7. **Benchmark**: justification `.md` (key generation is not a measured hot path today; one `--job dry` run confirms no regression of `DefaultCacheKeyGenerator`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/Encina.Caching.json and Encina.EntityFrameworkCore.json; targets never go down. Tests must execute real code.
TASK: Write the unit, guard, property tests and the three justification files; add the contract cases to the shared suite when it exists; measure each flag and run the CRAP check on the changed methods.
KEY RULES: Shouldly assertions; no Thread.Sleep; builders for test data; justification files only for integration (non-database), load and benchmark, never for unit, guard or contract.
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

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 8 | two generators, `CacheableQueryAttribute.cs`, `CacheAttribute.cs`, helper, EF generator, `EscapeSegment` access, DI |
| Lifetime change | 4 | ADO, Dapper, EF Core, MongoDB registrations (more files if each provider has its own registration) |
| PublicAPI | 1-2 | `Encina.Caching`, possibly EF |
| Tests | 8 | unit x4, guard x1, property x1, DI/order x1, contract case x1 |
| Justifications | 3 | integration, load, benchmark |
| Documentation | 3 | README, guide section, changelog |
| **Total** | **~29** | Two pull requests: (1) `Encina.Caching` generators and tests, (2) EF generator, lifetime change, contract case, docs |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Caching has eight providers (AGENTS.md §5) that treat keys as opaque strings; keys are produced by DefaultCacheKeyGenerator, CacheConfiguration<TRequest> and the EF DefaultQueryCacheKeyGenerator. No backward compatibility, zero warnings.

IMPLEMENTATION OVERVIEW: Phase 1 segment helper, optional IModuleExecutionContext injection, singleton registration in four packages; Phase 2 module segment and VaryByModule in Encina.Caching generators and patterns (D3); Phase 3 EF generator; Phase 4 DI and pipeline-order tests; Phase 5 tenant plus module combination tests, no observability; Phase 6 tests by flag; Phase 7 docs. Dilemmas D1 (module source), D2 (no module) and D3 (invalidation across modules) are implemented as the maintainer answered them, the recommended options until then.

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

- **#277** owns the `ICacheProvider` contract suite; the contract cases of this feature are added there (Phase 6). Nothing else waits for it.
- **Findings recorded as issue files** (see the run report): the missing producer of `IModuleExecutionContext` outside the EF package (ADO, Dapper and MongoDB register the context and read it in their schema-validating wrappers, but nothing in `src/` sets it for them); cache invalidation patterns that do not match keys without a tenant segment (`GeneratePattern` emitting `t:*`, and `{prefix}:*:{entityType}:*` against `{prefix}:{entity}:{hash}`); `CacheConfiguration<TRequest>.GenerateKey` using `request.GetHashCode()` as the key.
- **Related**: #710 (authorization cache) follows this key grammar for its module dimension.

---

## Next Steps

1. Ask the maintainer D1, D2 and D3 when work on #746 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: Phases 1, 2 and 4 for `Encina.Caching` with unit, guard and property tests (at most 15 files).
3. Pull request 2: Phase 3 (EF generator and escape access), the lifetime change in the four packages, the contract cases, and documentation.
