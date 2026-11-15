# Implementation Plan: Validator Resolution Cache for `FluentValidationProvider`

> **Issue**: [#702](https://github.com/dlrivada/Encina/issues/702)
> **Type**: Feature (performance)
> **Related**: #230 (owns `IRequestValidator<T>` and `RequestValidatorRunner`; its plan also adds an optional constructor parameter to `FluentValidationProvider`), #726 (central validation instrumentation), #945 (individual validation provider benchmarks), #1338 and #1339 (guard and DI proof tests of the providers), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Low (one internal-behavior class and two small edits in one package; the hard part is lifetime correctness, not code volume)
> **Estimated Scope**: ~170 lines of production code + ~600 lines of tests and benchmark + ~30 lines of documentation, in two pull requests of at most 12 files and 800 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`)

- `FluentValidationProvider.ValidateAsync<TRequest>` resolves the validators of the request type on every call: `_serviceProvider.GetServices<IValidator<TRequest>>().ToList()` (`FluentValidationProvider.cs:44`), then builds a `Task[]` with LINQ (`:66-68`), awaits `Task.WhenAll` (`:70`) and flattens the failures with LINQ into a `List<ValidationError>` (`:73-77`). When no validator is registered it returns `ValidationResult.Success` after the resolution and the list allocation (`:46-49`).
- The provider is registered scoped (`TryAddScoped<IValidationProvider, FluentValidationProvider>`, `ServiceCollectionExtensions.cs:62` and `:104`), so a new instance exists per scope and an instance-level cache would never be hit twice. Validators are registered by assembly scanning with `ServiceLifetime.Singleton` by default (`RegisterValidators(..., lifetime = ServiceLifetime.Singleton)`, `:114`, descriptor built at `:138`); the second overload registers them with the lifetime the caller passes (the XML text names scoped validators that depend on a `DbContext` as the use case), and an application may register any validator manually with any lifetime (the existing benchmark registers one with `AddTransient<IValidator<FluentCommand>, FluentCommandValidator>()`, `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs`).
- `DataAnnotationsValidationProvider` and `MiniValidationProvider` never resolve anything from the container: they call `Validator.TryValidateObject` (`DataAnnotationsValidationProvider.cs:45`) and `MiniValidator.TryValidate` (`MiniValidationProvider.cs:31`) on the request instance and have no `IServiceProvider`. There is no validator list to cache in them.
- Not in `src`: any cache of validator resolution, any benchmark that isolates the resolution cost (the existing `ValidationBenchmarks` measures the whole `IEncina.Send` path of all three providers and the guard clauses), any test that resolves `IValidator<T>` with a mixed lifetime.

### What the issue proposes and why it cannot be applied as written

The issue stores the resolved validator instances in a `static ConcurrentDictionary<Type, object>` inside the provider. That is correct only when every registration is a singleton and only one container exists in the process:

- A scoped or transient validator (the documented use case of the lifetime overload, and the shape of the repository's own benchmark) would be captured by a process-wide static: it would outlive its scope (a disposed `DbContext` reaching a later request) and be shared across threads and requests ([Microsoft guidance on captive dependencies](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#captive-dependency)).
- A static dictionary is shared by every container in the process: two hosts, or two test fixtures, that register different validators for the same request type would read each other's validators.

### What this plan delivers

A cache that keeps only what is true for the lifetime of the container and is correct for every lifetime: a container-level singleton `ValidatorResolutionCache` that, per request type, learns once whether the registrations are `None` (no validator: the call returns `Success` without resolving or allocating), `Singleton` (every registration is a singleton: the resolved array is reused) or `Dynamic` (at least one scoped or transient registration: resolved on every call, exactly as today). The provider also loses its avoidable per-call allocations (the `ToList`, the `Task[]` LINQ chain). Validation behavior is identical for every lifetime. Nothing changes for DataAnnotations and MiniValidator (nothing to cache, see Design Choice 3).

- Affected packages: `Encina.FluentValidation` (the only one with a validator list to cache); `Encina` core, `Encina.DataAnnotations` and `Encina.MiniValidator` are not touched.
- Provider category: Validation (3) (`AGENTS.md` section 5): the issue's "apply to all 3 where applicable" resolves to one applicable provider (Design Choice 3); the other two keep their behavior and a benchmark confirms they have no resolution cost to remove.
- Behavior change: none observable except allocation. A provider built with `new FluentValidationProvider(serviceProvider)` (the 6 test sites) keeps the uncached behavior.

---

## Design Choices

<details>
<summary><strong>1. Where the cache lives: static field, per-provider field or container-level singleton</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `static ConcurrentDictionary<Type, object>` in the provider (the issue's text)** | Smallest code; no DI registration | Shared by every container in the process: two containers with different validators for one request type read each other's list; a test fixture leaks into the next; with a scoped or transient validator it is a captive dependency |
| **B) A field on the provider instance** | No sharing across containers | The provider is scoped, so every request starts with an empty cache: no gain |
| **C) A singleton `ValidatorResolutionCache` registered in the container and injected into the provider (optional constructor parameter)** | One cache per container, as long-lived as the registrations it describes; testable with `new`; a provider built without it behaves as today | One new public type and one registration |

### Chosen Option: **C — Container-level singleton cache**

### Rationale

- The registrations are immutable once the container is built, so the container is the correct owner of facts about them; neither a process-wide static (A) nor a per-scope instance (B) matches that lifetime.
- The cache key is the CLR request type, a compile-time value: the number of entries is bounded by the number of request types in the application, never by request data, so the cache cannot grow without limit.

**Class:** Settled
**Settled by:** [Microsoft DI guidelines, "Captive dependency"](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#captive-dependency) (a longer-lived holder must not keep a shorter-lived service) and the repository's own tests: `ServiceCollectionExtensionsTests.cs:86-109` register validators as `Scoped` and `Transient` through the lifetime overload, and `FluentValidationProviderTests.cs` builds a new container per test (`BuildServiceProvider` at `:274`, `:298`) registering different validators for the same request type (`:347` a factory validator for `RequiredFieldRequest`, `:431-434` a generic helper that registers a different `TValidator` per test), which a process-wide static would mix; only option C is container-correct and effective

</details>

<details>
<summary><strong>2. How the cache learns whether a request type may be cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Read the `ServiceDescriptor` lifetimes of the `IServiceCollection` (a catalog captured by the registration method)** | Deterministic, no extra construction of validators | A registration made by hand after `AddEncinaFluentValidation` is seen only if the catalog reads the collection lazily, and a collection mutated after the provider was built (a pattern some test fixtures use) makes the catalog disagree with the container: a transient validator could be misread as singleton and captured |
| **B) Probe the container once per request type: open two scopes from `IServiceScopeFactory`, resolve `IEnumerable<IValidator<TRequest>>` in each, and compare the instances; all empty gives `None`, every instance reference-equal across both scopes gives `Singleton`, anything else (or an exception while constructing) gives `Dynamic`** | Observes what the container really does, for any registration style, container implementation or registration order; never misclassifies a scoped or transient validator, because those return different instances per scope | The validators of the probed type are constructed up to two extra times, once per request type per container; a validator whose constructor needs request-bound state throws in the probe and is classified `Dynamic` (the safe side: it is resolved per call, as today, and fails there if it is really broken) |
| **C) No instance caching: cache only the `None` fact and drop the `ToList`/LINQ allocations** | No classification, no probe, nothing to get wrong | Resolution of existing validators still happens per call (the part the issue wants to remove) |

### Chosen Option: **B — Observe the container with a two-scope probe** (recommended, pending the maintainer)

### Rationale

- The failure that matters is a wrong `Singleton` classification: it hands a scoped service to a later request. B can only say `Singleton` when two independent scopes returned the very same instances, which is what a singleton is; A can say it from stale or incomplete descriptor data.
- A wins when the application replaces the DI container with one whose lifetime semantics differ from the built-in ones and probing is costly (validators with heavy constructors); C wins when the benchmark of phase 1 shows the resolution is not the cost (Design Choice 5).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Scope across the three validation providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache in `FluentValidationProvider` only; leave `DataAnnotationsValidationProvider` and `MiniValidationProvider` unchanged** | Each provider changes only where there is something to cache | The issue's "applied to all 3 where applicable" is met by showing that two of them have no applicable site |
| **B) Add a cache class to all three for symmetry** | Same shape everywhere | Two caches with nothing to hold; dead code (`AGENTS.md` section 3: every line serves a current purpose) |

### Chosen Option: **A — `FluentValidationProvider` only**

### Rationale

- The other two providers take no `IServiceProvider` and call the library on the request object directly (`DataAnnotationsValidationProvider.cs:31,45`; `MiniValidationProvider.cs:31`); there is no validator list to resolve. The type-metadata caching those libraries do internally is theirs and is not changed here.
- Phase 1 adds a benchmark of the three providers' per-call allocation so the claim is measured, not assumed; if it shows a resolution cost in either of them, that is a new issue, not a silent scope change.

**Class:** Settled
**Settled by:** the two provider files cited above (neither contains a `GetService`/`GetServices` call, so there is nothing to cache) and `AGENTS.md` section 3 (no code without a current purpose); the single option that complies is A

</details>

<details>
<summary><strong>4. What is cached for a singleton request type, and the thread-safety model</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An `IValidator<TRequest>[]` in an instance `ConcurrentDictionary<Type, object>` filled with `GetOrAdd`** | Lock-free reads; the array is read-only after publication; a concurrent first call may compute the classification twice, but both results are equal and one wins | The probe may run twice under a first-call race (harmless, idempotent) |
| **B) `Lazy<>` per type so the probe runs exactly once** | One probe per type | A `Lazy` that captures an exception caches the failure unless configured otherwise; more allocation per type; the probe is cheap and idempotent, so exactly-once buys nothing |

### Chosen Option: **A — Instance `ConcurrentDictionary` with idempotent `GetOrAdd`**

### Rationale

- The classification of a type is a pure function of the container's registrations: computing it twice under a race yields the same answer, so a lock or a `Lazy` adds cost without adding correctness.
- The array is allocated once per request type and handed out as a read-only span of the same instances; callers never mutate it.

**Class:** Trivial
**Settled by:** the idiom already used for per-generic-type caches in the pipeline (per-type instance `ConcurrentDictionary<Type, ...>` caches, for example `src/Encina/Pipeline/Ordering/PipelineOrderResolver.cs:46` and `src/Encina/Modules/ModuleHandlerRegistry.cs:22`) and the issue's own proposal (`ConcurrentDictionary`)

</details>

<details>
<summary><strong>5. Should the cache land only if the benchmark shows a gain</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two pull requests: S1 adds the benchmark and removes the avoidable allocations; S2 adds the cache only if the S1 benchmark shows the resolution cost is at least 10 percent of the allocated bytes per `ValidateAsync` call with singleton validators** | Complexity is added only against a measured cost; the allocation clean-up ships regardless | An extra pull request and a threshold that is a judgement |
| **B) One pull request with the benchmark and the cache** | One review | If the cost is negligible the cache is complexity with no return |

### Chosen Option: **A — Measure first, threshold 10 percent** (recommended, pending the maintainer)

### Rationale

- The issue's acceptance criteria require a benchmark ("Measure DI lookup reduction", "Recommended") and tests; a measured gain is the only evidence that the optimization is worth its risk surface.
- B wins when the maintainer already accepts the optimization on principle (the issue is `status:ready`, so this is a real possibility); the threshold is a number only the maintainer can fix.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A `Dynamic` request type is resolved on every call exactly as today, so a scoped or transient validator never outlives its scope | Settled | Microsoft DI guidelines (captive dependency), cited in Design Choice 1 |
| C2 | The cache key is the CLR `Type` only: no tenant, user or request data enters the key, so there is no tenant-qualified id to make injective and no `default` tenant segment to reserve; validators receive `TenantId` per call through `RootContextData`, unchanged | Settled | ADR-054 D4 (tenant is a tag, not a key, and only when multi-tenancy is on); the provider code (`FluentValidationProvider.cs:62`) |
| C3 | A probe that throws classifies the type `Dynamic`; the exception is not logged (no message, no data) and the real call path reports any genuine failure | Dilemma (recommended, pending the maintainer) | follows Design Choice 2 option B; `AGENTS.md` section 3 (gates fail closed: the safe classification is the one that validates exactly as before) |
| C4 | No `[LoggerMessage]` is added: the cache is a hot path with no operational event; no EventId range is needed | Trivial | `AGENTS.md` section 7 (ranges only for logging code that exists) |
| C5 | No new error code is created, so there is no ADR-062 `ErrorCategory` to assign; a validation failure keeps going through `ValidationResult` and the existing orchestrator `Left` | Settled | ADR-062 contract (a category is chosen where an error is created; none is created here) |
| C6 | The cache type is `public sealed` with a public constructor so the registration and the tests can build it; its only public member besides the constructor is the generic resolve method | Dilemma (recommended, pending the maintainer) | public surface; `internal` plus `InternalsVisibleTo` is the alternative (the guard test project reaches it either way) |

---

## Implementation Phases

### Phase 1: Benchmark and Allocation Clean-up (pull request S1)

> **Goal**: Measure the resolution cost with the cache absent, and remove the per-call allocations that do not depend on any cache.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/FluentValidationResolutionBenchmarks.cs` (`[MemoryDiagnoser]`; three scenarios of the same `ValidateAsync` call: no validator registered, one singleton validator, one transient validator; plus the DataAnnotations and MiniValidator calls as the control of Design Choice 3). Run with `dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj -- --filter "*FluentValidationResolution*"`; the measured allocated bytes per call go into the pull request description and decide Design Choice 5.
2. **Modify** `src/Encina.FluentValidation/FluentValidationProvider.cs`: keep the resolution as it is; replace `.ToList()` by `.ToArray()` with an early return on `Length == 0`, and replace the `Select`/`ToArray`, `SelectMany`/`Where`/`Select`/`ToList` chains by two loops with a pre-sized `Task<FluentValidation.Results.ValidationResult>[]` and a single `List<ValidationError>` (behavior and error order unchanged).
3. **Modify** `.github/coverage-manifest/Encina.FluentValidation.json` through `mechanical-fixer`: explicit per-file targets for `FluentValidationProvider.cs` (table in Research).
4. Tests: the existing 441-line `FluentValidationProviderTests.cs` and `ValidationPipelineBehaviorTests.cs` already pin the behavior (result equality, error order, the "no validators" path, parallel failures); the change is covered by them unchanged, and the pull request description lists which tests cover each edited branch for the crap-gate.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #702 plan: docs/plans/validator-resolution-cache-implementation-plan-702.md,
phase 1 of 2. FluentValidationProvider (src/Encina.FluentValidation/FluentValidationProvider.cs) resolves validators
with GetServices(...).ToList() on every call.

TASK:
1. Add a BenchmarkDotNet class FluentValidationResolutionBenchmarks in tests/Encina.BenchmarkTests/Encina.Benchmarks
   (use BenchmarkSwitcher conventions of the project, MemoryDiagnoser, scenarios: none/singleton/transient validators,
   plus DataAnnotations and MiniValidator controls). Run it and record allocated bytes per call.
2. In FluentValidationProvider.ValidateAsync replace the ToList and the LINQ chains by an array and two loops.
   Result values and error ORDER must not change.

KEY RULES:
- No behavior change; existing FluentValidationProviderTests must pass unchanged.
- Edit coverage manifest only through mechanical-fixer. XML docs unchanged. No [Obsolete].

REFERENCE FILES:
- src/Encina.FluentValidation/FluentValidationProvider.cs (:44-77)
- tests/Encina.UnitTests/FluentValidation/FluentValidationProviderTests.cs
- tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs
```

</details>

---

### Phase 2: Lifetime-Aware Resolution Cache (pull request S2)

> **Goal**: Skip resolution for request types with no validator and reuse the array for singleton validators (only if S1 shows the gain, Design Choice 5).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.FluentValidation/ValidatorResolutionCache.cs`: `public sealed class ValidatorResolutionCache`; constructor `(IServiceScopeFactory scopeFactory)` (a singleton can take the factory, never a scoped service); one public method `IReadOnlyList<IValidator<TRequest>> Resolve<TRequest>(IServiceProvider scopedProvider)` that returns the shared array for `Singleton`, an empty list for `None` without touching `scopedProvider`, and `scopedProvider.GetServices<IValidator<TRequest>>().ToArray()` for `Dynamic`. An internal `Classify(Type)` runs the two-scope probe of Design Choice 2 (reference equality of the instances, a thrown exception gives `Dynamic`).
2. **Modify** `FluentValidationProvider.cs`: optional trailing constructor parameter `ValidatorResolutionCache? cache = null`; when null the provider resolves as in phase 1. The plan of #230 adds its own optional `RequestValidatorRunner? runner = null` to the same constructor: the pull request that lands second appends its parameter after the other's, both optional, so the 6 existing `new FluentValidationProvider(serviceProvider)` sites compile unchanged (verified with `git grep -c "new FluentValidationProvider(" -- tests`).
3. **Modify** `src/Encina.FluentValidation/ServiceCollectionExtensions.cs`: `services.TryAddSingleton<ValidatorResolutionCache>()` in both `AddEncinaFluentValidation` overloads (next to `:62` and `:104`); the singleton takes `IServiceScopeFactory`, which the container always provides.
4. **Modify** `src/Encina.FluentValidation/PublicAPI.Unshipped.txt` (the type, its constructor and `Resolve`) and `.github/coverage-manifest/Encina.FluentValidation.json` (a new entry for `ValidatorResolutionCache.cs`) through `mechanical-fixer`.
5. **Modify** `src/Encina.FluentValidation/README.md` through `docs-writer`: a short "Performance" note (what is cached, what is not, the lifetime guarantee) and the changelog fragment `changelog.d/702-validator-resolution-cache.changed.md` through `mechanical-fixer`.
6. Tests (all new files): `ValidatorResolutionCacheTests` (unit: `None`, `Singleton` returns the same array on the second call, `Dynamic` with a scoped validator returns a different instance per scope, transient likewise, mixed registrations are `Dynamic`, a validator whose constructor throws in the probe is `Dynamic`, the same request type in two caches does not share state, 64 concurrent first calls give one stable classification); `FluentValidationProviderCacheTests` (unit: results identical with and without the cache for each lifetime); `ValidatorResolutionCacheGuardTests` (guard: null `scopeFactory`, null `scopedProvider`; a null cache in the provider is a valid case that uncached behavior); `ValidatorResolutionCacheRegistrationTests` (DI: builds the provider with `ValidateOnBuild` and `ValidateScopes` for both `AddEncinaFluentValidation` overloads and resolves the provider, the orchestrator and the cache); `ValidatorResolutionCacheProperties` (FsCheck: for any mix of lifetimes the cached provider returns the same `ValidationResult` as the uncached one).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #702 plan docs/plans/validator-resolution-cache-implementation-plan-702.md, phase 2 of 2. Only do this phase when
the phase 1 benchmark shows the resolution cost threshold of Design Choice 5. Registrations are immutable after the
container is built; validators may be singleton, scoped or transient.

TASK:
1. Create ValidatorResolutionCache (container singleton, ctor IServiceScopeFactory) with the two-scope probe: None,
   Singleton (reference-equal instances across two scopes), Dynamic (everything else, or a throwing probe).
2. Give FluentValidationProvider an optional trailing ValidatorResolutionCache? parameter; null keeps today's behavior.
3. Register the cache with TryAddSingleton in both AddEncinaFluentValidation overloads.

KEY RULES:
- NEVER cache an instance of a scoped or transient validator; NEVER use a static dictionary.
- No logging, no new error codes. A scoped service is never injected into the singleton.
- Existing tests that call new FluentValidationProvider(serviceProvider) compile unchanged.
- PublicAPI.Unshipped.txt, the coverage manifest and the changelog fragment go through mechanical-fixer; the README
  through docs-writer.

REFERENCE FILES:
- src/Encina.FluentValidation/FluentValidationProvider.cs, ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/FluentValidation/ServiceCollectionExtensionsTests.cs
- docs/plans/async-cross-field-validation-implementation-plan-230.md (issue #230: the other constructor parameter)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Microsoft DI guidelines | Captive dependency: a service must not hold a shorter-lived service | Why a static cache of resolved validators is wrong for scoped and transient validators (Design Choice 1) |
| FluentValidation 12.1.1 (`Directory.Packages.props`) | `IValidator<T>` lifetime is the application's choice; validators are not required to be singletons | Why the lifetime must be observed, not assumed |
| ADR-054 | D3 one meter and source per package; D4 tenant tag only when multi-tenancy is on; settled point 7 instrumentation costs nothing without a listener | Why no instrument is added here (#726 owns validation telemetry) and why the cache key carries no tenant |
| ADR-062 | Error category contract | No error is created; nothing to categorize (register row C5) |
| `AGENTS.md` section 3 | Registration completeness proven by a `ValidateOnBuild` + `ValidateScopes` DI test; never a scoped service into a singleton | DI test and constructor shape of Phase 2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `FluentValidationProvider` | `src/Encina.FluentValidation/FluentValidationProvider.cs` | The only call site that resolves `IValidator<T>` |
| `AddEncinaFluentValidation` (two overloads) | `src/Encina.FluentValidation/ServiceCollectionExtensions.cs:62,104` | Registers the cache next to the provider |
| `ValidationBenchmarks` | `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs` | Existing end-to-end benchmark; the new class isolates resolution |
| Provider unit, property and DI tests | `tests/Encina.UnitTests/FluentValidation/`, `tests/Encina.PropertyTests/Validation/FluentValidation/ValidationInvariantProperties.cs` | Behavior that must stay identical |
| `IServiceScopeFactory` | `Microsoft.Extensions.DependencyInjection.Abstractions` | The probe's scopes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.FluentValidation` | none | No logging code is added (register row C4); the block 600-649 assigned to this family is used by the sibling plans #233, #234, #235 and #236 |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI, the README and the changelog fragment; each pull request builds and passes alone.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Benchmark and allocation clean-up | 0 | 1 (`FluentValidationProvider.cs`) | 1 (benchmark) | 0 | 1 | 0 | 3 | ~200 |
| S2 | Resolution cache | 1 (`ValidatorResolutionCache.cs`) | 3 (provider, `ServiceCollectionExtensions.cs`, PublicAPI) | 5 (cache unit, provider unit, guard, DI, property) | 0 | 1 | 2 (README, changelog fragment) | 12 | ~800 |
| **Total** | | **1** | **4** | **6** | **0** | **2** | **2** | **15** | **~1,000** |

The columns sum to the total (1 + 4 + 6 + 0 + 2 + 2 = 15 files; S1 3 + S2 12 = 15). S2 is not opened when the S1 measurement is under the threshold of Design Choice 5; the plan then ends after S1 and the issue closes with the measurement as its evidence.

Per-file coverage targets (per flag, with a one-sentence justification), written into `.github/coverage-manifest/Encina.FluentValidation.json` by the pull request that adds or touches the file:

| File | Unit | Guard | Property | Justification |
|------|------|-------|----------|---------------|
| `ValidatorResolutionCache.cs` | 95 | 90 | 60 | Every classification (none, singleton, scoped, transient, mixed, throwing probe) and the concurrent first call are reachable with plain registrations; the guard tests reach both argument checks; the property test drives random lifetime mixes through the resolve method |
| `FluentValidationProvider.cs` | 95 | 40 | 50 | The loops and the empty path are covered by the existing provider tests and the new cache tests; the guard tests reach the request and context checks and the optional constructor parameter; the property test compares cached and uncached results |
| `ServiceCollectionExtensions.cs` | 90 | 85 | 0 | The new `TryAddSingleton` lines are covered by the DI test; the existing null checks are guard-tested; no property applies to registration |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #702 - a lifetime-aware validator resolution cache for FluentValidationProvider.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5): IValidationProvider, ValidationOrchestrator,
  ValidationPipelineBehavior live in core; FluentValidation, DataAnnotations and MiniValidator stay consistent.

IMPLEMENTATION OVERVIEW:
1. S1: a benchmark that isolates validator resolution, and a provider hot path without ToList or LINQ chains.
2. S2 (only if the benchmark shows at least 10 percent of allocated bytes in resolution): ValidatorResolutionCache, a
   container singleton that classifies each request type as None, Singleton or Dynamic with a two-scope probe, so a
   scoped or transient validator is never cached; optional constructor parameter on the provider; TryAddSingleton in
   both AddEncinaFluentValidation overloads.
3. DataAnnotations and MiniValidator are unchanged (nothing to resolve).

KEY PATTERNS:
- Never a static cache; never cache a scoped or transient instance; never inject a scoped service into the singleton.
- Optional trailing constructor parameter keeps the existing tests compiling; DI test with ValidateOnBuild and
  ValidateScopes; guard tests for the new public constructor.
- Coverage manifest, PublicAPI and changelog fragment through mechanical-fixer; README through docs-writer.

REFERENCE FILES:
- src/Encina.FluentValidation/FluentValidationProvider.cs, ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/FluentValidation/*.cs
- docs/plans/validator-resolution-cache-implementation-plan-702.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Phase 2 | An in-memory, per-container cache of registration facts; not `ICacheProvider`: the issue states the data is identical across instances and immutable after startup, so a distributed cache would add cost for no benefit |
| 2 | OpenTelemetry | ⏭️ #726 | #726 instruments `ValidationOrchestrator` centrally; this plan adds no instrument (ADR-054 settled point 7: no cost without a listener, and the cache is a hot path). When #726 lands, the S1 benchmark remains the evidence of the gain |
| 3 | Structured Logging | ❌ N/A | A per-request hot path with no operational event (register row C4); no EventId range |
| 4 | Health Checks | ❌ N/A | In-process, immutable facts; no checkable dependency |
| 5 | Validation | ✅ All phases | This is the validation provider; behavior is identical for every validator lifetime and the three providers stay consistent (Design Choice 3) |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | A lock-free per-process structure; no shared state across instances |
| 8 | Transactions | ❌ N/A | Validation runs before the handler; a scoped validator keeps resolving from the request scope, so it sees the handler's `DbContext` as before |
| 9 | Idempotency | ❌ N/A | A pure read of registration facts |
| 10 | Multi-Tenancy | ❌ N/A | The key is a CLR type; tenants share validators and receive `TenantId` per call (register row C2); no tenant tag is emitted because nothing is emitted |
| 11 | Module Isolation | ❌ N/A | Registrations are per container, not per module; module-scoped validator selection is the follow-up of the family (the draft `validation-tenant-module-scoped-selection` accompanies the plan of #230) |
| 12 | Audit Trail | ❌ N/A | No security or compliance state changes; rejected requests are audited by the follow-up that covers validation failures (accompanies the plan of #230) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Validation orchestrator pattern and `FluentValidationProvider` | ✅ On `main` | The only integration point |
| ADR-062 error category contract | ✅ Merged | No error code is created here, so it is not exercised |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| `IRequestValidator<T>` and its runner resolve a validator list per call as well | #230 | The cache of this plan is `FluentValidation` only; the runner's resolution is measured by the benchmark of #945 before any change (follow-up issue file `validation-runner-resolution-cache`) |
| Central validation instrumentation | #726 | Gives the production-side view of the gain |
| Individual provider benchmarks | #945 | Overlap with the S1 benchmark in intent only; separate files |
| DI proof tests of the providers | #1338, #1339 | Overlap in test files only; this plan adds new files and does not wait |

---

## Next Steps

1. Link this plan from #702 and ask the maintainer to answer Design Choices 2 and 5 and register row C6 when work starts.
2. Implement S1 and read its benchmark; open S2 only when the threshold of Design Choice 5 is met, with `Fixes #702` on the last pull request that ships.
3. Open the follow-up issue when the first pull request opens: the same resolution cost in the `IRequestValidator<T>` runner of #230 (draft: `artifacts/issues/validation-runner-resolution-cache.md`).
