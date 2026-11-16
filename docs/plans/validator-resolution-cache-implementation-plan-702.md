# Implementation Plan: Validator Resolution Cache for `FluentValidationProvider`

> **Issue**: [#702](https://github.com/dlrivada/Encina/issues/702)
> **Type**: Feature (performance)
> **Related**: #230 (owns `IRequestValidator<T>` and `RequestValidatorRunner`; its plan also adds an optional constructor parameter to `FluentValidationProvider`), #726 (central validation instrumentation), #945 (individual validation provider benchmarks), #1338 and #1339 (guard and DI proof tests of the providers), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Low (one internal-behavior class and two small edits in one package; the hard part is lifetime correctness, not code volume)
> **Estimated Scope**: ~170 lines of production code + ~600 lines of tests and benchmark + ~30 lines of documentation, in two pull requests of at most 14 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-16, main `3c95c4b2c`)

- `FluentValidationProvider.ValidateAsync<TRequest>` resolves the validators of the request type on every call: `_serviceProvider.GetServices<IValidator<TRequest>>().ToList()` (`FluentValidationProvider.cs:44`), then builds a `Task[]` with LINQ (`:66-68`), awaits `Task.WhenAll` (`:70`) and flattens the failures with LINQ into a `List<ValidationError>` (`:73-77`). When no validator is registered it returns `ValidationResult.Success` after the resolution and the list allocation (`:46-49`).
- The provider is registered scoped (`TryAddScoped<IValidationProvider, FluentValidationProvider>`, `ServiceCollectionExtensions.cs:62` and `:104`), so a new instance exists per scope and an instance-level cache would never be hit twice. Validators are registered by assembly scanning with `ServiceLifetime.Singleton` by default (`RegisterValidators(..., lifetime = ServiceLifetime.Singleton)`, `:114`, descriptor built at `:138`); the second overload registers them with the lifetime the caller passes (the XML text names scoped validators that depend on a `DbContext` as the use case), and an application may register any validator manually with any lifetime (the existing benchmark registers one with `AddTransient<IValidator<FluentCommand>, FluentCommandValidator>()`, `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs`).
- `DataAnnotationsValidationProvider` and `MiniValidationProvider` never resolve anything from the container: they call `Validator.TryValidateObject` (`DataAnnotationsValidationProvider.cs:45`) and `MiniValidator.TryValidate` (`MiniValidationProvider.cs:31`) on the request instance and have no `IServiceProvider`. There is no validator list to cache in them.
- Not in `src`: any cache of validator resolution, any benchmark that isolates the resolution cost (the existing `ValidationBenchmarks` measures the whole `IEncina.Send` path of all three providers and the guard clauses), any test that resolves `IValidator<T>` with a mixed lifetime.

### What the issue proposes and why it cannot be applied as written

The issue stores the resolved validator instances in a `static ConcurrentDictionary<Type, object>` inside the provider. That is correct only when every registration is a singleton and only one container exists in the process:

- A scoped or transient validator (the documented use case of the lifetime overload, and the shape of the repository's own benchmark) would be captured by a process-wide static: it would outlive its scope (a disposed `DbContext` reaching a later request) and be shared across threads and requests (Microsoft's dependency injection guidelines, section "Captive dependency").
- A static dictionary is shared by every container in the process: two hosts, or two test fixtures, that register different validators for the same request type would read each other's validators.

### What this plan delivers

A cache that keeps only what is true for the lifetime of the container and is correct for every lifetime: a container-level singleton `ValidatorResolutionCache` that, per request type, learns once whether the registrations are `None` (no validator: the call returns `Success` without resolving or allocating), `Singleton` (every registration is a singleton and the container confirms it: the resolved array is reused) or `Dynamic` (at least one scoped or transient registration, or any doubt: resolved on every call, exactly as today). How the cache learns is Design Choice 2, a Dilemma: a probe alone can misclassify a factory that returns a shared instance chosen by scoped state. The provider also loses its avoidable per-call allocations (the `ToList`, the `Task[]` LINQ chain). Validation behavior is identical for every lifetime. Nothing changes for DataAnnotations and MiniValidator (nothing to cache, see Design Choice 3).

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
**Settled by:** Microsoft DI guidelines, section "Captive dependency" (a longer-lived holder must not keep a shorter-lived service) and the repository's own tests: `ServiceCollectionExtensionsTests.cs:86-109` register validators as `Scoped` and `Transient` through the lifetime overload, and `FluentValidationProviderTests.cs` builds a new container per test (`BuildServiceProvider` at `:274`, `:298`) registering different validators for the same request type (`:347` a factory validator for `RequiredFieldRequest`, `:431-434` a generic helper that registers a different `TValidator` per test), which a process-wide static would mix; only option C is container-correct and effective

</details>

<details>
<summary><strong>2. How the cache learns whether a request type may be cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Read the `ServiceDescriptor` lifetimes of the `IServiceCollection` (a catalog captured by the registration method)** | Deterministic, no extra construction of validators; sees a `Transient` or `Scoped` factory registration whatever the factory returns | A registration made by hand after `AddEncinaFluentValidation` is seen only if the catalog reads the collection lazily, and a collection mutated after the provider was built (a pattern some test fixtures use) makes the catalog disagree with the container: a transient validator could be misread as singleton and captured |
| **B) Probe the container once per request type: open two scopes from `IServiceScopeFactory`, resolve `IEnumerable<IValidator<TRequest>>` in each, and compare the instances; all empty gives `None`, every instance reference-equal across both scopes gives `Singleton`, anything else (or an exception while constructing) gives `Dynamic`** | Observes what the container really does, for any registration style, container implementation or registration order; scoped and transient validators that build a new instance per resolution return different instances per scope and are classified `Dynamic` | **Misclassifies a factory registration that returns a shared, pre-built instance chosen by scoped state.** Example: `AddTransient<IValidator<T>>(sp => sp.GetRequiredService<ITenantContext>().IsPremium ? premiumValidator : standardValidator)` with both validators built once. Both probe scopes see the same default tenant context, return the very same instance, the type is classified `Singleton`, and every later scope (another tenant) receives the wrong validator for the life of the container: a validation bypass per tenant, which the descriptor lifetime (`Transient`) would have exposed. The validators of the probed type are also constructed up to two extra times (see register row C3 for first-call cost, side effects and cancellation) |
| **C) No instance caching: cache only the `None` fact and drop the `ToList`/LINQ allocations** | No classification, no probe, nothing to get wrong | Resolution of existing validators still happens per call (the part the issue wants to remove) |
| **D) Hybrid: a type is `Singleton` only when A reports every registration of `IValidator<TRequest>` as a `Singleton` descriptor AND the B probe confirms reference-equal instances; a non-singleton descriptor, an unknown descriptor set or a probe disagreement gives `Dynamic`** | Catches the tenant-factory scenario above (its descriptor is `Transient` or `Scoped`, so the type is `Dynamic` before any probe); the probe still catches a singleton descriptor whose factory returns different instances | Carries both the staleness risk of A (the catalog is read lazily from the `IServiceCollection` the extension method received, so a descriptor added after the container was built is unseen: the type then falls to the probe verdict only for singletons already built, never to a wrong `Singleton` from a non-singleton descriptor) and the probe cost of B. A `Singleton` descriptor whose factory returns a shared instance chosen by scoped state cannot be built at all: `ValidateScopes` rejects a scoped service in a singleton factory, and without it the factory fails or sees the root provider |

### Chosen Option: **D — Descriptor lifetimes, confirmed by the two-scope probe** (recommended, pending the maintainer)

### Rationale

- The failure that matters is a wrong `Singleton` classification: it hands a scoped or per-tenant service to a later request, a fail-open validation outcome. Probe-only B proves "the same instance twice", not "the same instance always": the tenant-factory registration above passes B and fails D. This plan therefore does not claim that B never misclassifies.
- B wins when the application replaces the DI container with one whose `ServiceDescriptor` lifetimes are not the truth about instance lifetime and every validator registration is a plain type (no factory); A alone wins when validators have heavy or side-effecting constructors and probing is unacceptable; C wins when the benchmark of phase 1 shows the resolution is not the cost (Design Choice 5).
- If the maintainer picks B, the limitation is stated in the XML documentation of `ValidatorResolutionCache` ("a validator factory must not return an instance that depends on scoped state; register it with the scoped lifetime and build a new instance") and pinned by a test that registers the tenant factory and asserts the documented, wrong `Singleton` verdict, so the limitation cannot be forgotten. If the maintainer picks D, the same registration is a test that asserts `Dynamic` and the per-tenant validator result.

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
| **A) Two pull requests: S1 adds the benchmark and removes the avoidable allocations; S2 adds the cache only if the S1 benchmark shows the resolution cost is at least 10 percent of the allocated bytes per `ValidateAsync` call with singleton validators (formula and arms below)** | Complexity is added only against a measured cost; the allocation clean-up ships regardless | An extra pull request and a threshold that is a judgement |
| **B) One pull request with the benchmark and the cache** | One review | If the cost is negligible the cache is complexity with no return |

### Chosen Option: **A — Measure first, threshold 10 percent** (recommended, pending the maintainer)

### The gate, defined (applies to option A)

- **Baseline first.** S1 edits the provider in the same pull request that adds the benchmark, so the "before" is measured before the edit: the worker adds the benchmark file, runs it against the UNCHANGED provider (the state of `origin/main`), records the numbers, and only then edits `FluentValidationProvider.cs` and runs it again for the "after".
- **Arms** (one class, `[MemoryDiagnoser]`, the singleton-validator scenario is the one the gate reads, because that is the only scenario the cache can improve; the `none` and `transient` scenarios are reported for information):
  1. `ResolveOnly`: `scopedProvider.GetServices<IValidator<TRequest>>().ToList()` in an already-open scope (the control arm: the resolution cost with nothing else).
  2. `WholeCallBefore`: `FluentValidationProvider.ValidateAsync` on the unchanged provider.
  3. `WholeCallAfter`: the same call after the allocation clean-up (run last).
  The scope is opened once in `[GlobalSetup]` and reused, so scope creation is excluded from every arm; the request, context and validator are built in setup.
- **Formula:** `resolution share (%) = 100 x Allocated(ResolveOnly) / Allocated(WholeCallBefore)`, with `Allocated` the `Allocated` column of BenchmarkDotNet (bytes per operation). S2 is opened only when the share is 10 or more. The after-clean-up share, `100 x Allocated(ResolveOnly) / Allocated(WholeCallAfter)`, is reported next to it so the maintainer sees whether the clean-up alone already removed the cost.
- **Where the numbers go:** a table in the description of the S1 pull request (arm, scenario, mean, allocated bytes, share) and the same table in the commit message of the benchmark commit; the plan file is not edited. The maintainer reads that table to decide S2.

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
| C3 | Probe behavior (applies to options B and D of Design Choice 2): (a) the probe runs on the first request path of each request type, so that first call pays up to two extra constructions of every validator of the type and its constructor dependencies (a validator whose constructor opens a `DbContext` or other resource builds it in a probe scope, and the probe scopes are disposed at once); (b) any exception except `OperationCanceledException` classifies the type `Dynamic`, is not logged (no message, no data) and the real call path reports a genuine failure; (c) an `OperationCanceledException` raised while constructing a validator propagates and is not cached, because cancellation is not an error to swallow (ADR-052) and a cancelled first call must not pin a classification | Dilemma (recommended, pending the maintainer): (a) a first-call latency and side-effect cost the maintainer accepts or avoids by choosing option A of Design Choice 2 (no probe); (b)/(c) are the recommended defaults | follows Design Choice 2; `AGENTS.md` section 3 (gates fail closed: the safe classification is the one that validates exactly as before; background cancellation propagates, ADR-052) |
| C4 | No `[LoggerMessage]` is added: the cache is a hot path with no operational event; no EventId range is needed | Trivial | `AGENTS.md` section 7 (ranges only for logging code that exists) |
| C5 | No new error code is created, so there is no ADR-062 `ErrorCategory` to assign; a validation failure keeps going through `ValidationResult` and the existing orchestrator `Left` | Settled | ADR-062 contract (a category is chosen where an error is created; none is created here) |
| C6 | The cache type is `public sealed` with a public constructor so the registration and the tests can build it; its only public member besides the constructor is the generic resolve method | Dilemma (recommended, pending the maintainer) | public surface; `internal` plus `InternalsVisibleTo` is the alternative (the guard test project reaches it either way) |
| C7 | Public names and shape: `ValidatorResolutionCache` and `Resolve<TRequest>(IServiceProvider scopedProvider)` returning `IReadOnlyList<IValidator<TRequest>>` (read-only view of the shared array; callers cannot mutate the cached instances) | Dilemma (recommended, pending the maintainer) | public surface; alternatives are a different type name, `IEnumerable<IValidator<TRequest>>` (no indexer, lazier) or `ReadOnlySpan` (not usable across `await`, so rejected by the async call path); recorded with C6 |
| C8 | `Resolve` calls `ArgumentNullException.ThrowIfNull(scopedProvider)` first even when the answer is `None` or `Singleton` and the provider is never touched, so a caller bug fails the same way for every classification | Trivial | `AGENTS.md` section 9 (guard tests for validated parameters) and the sibling guard in `FluentValidationProvider.cs:40-41` |
| C9 | Constructor parameter order on `FluentValidationProvider`: `(IServiceProvider serviceProvider, ValidatorResolutionCache? cache = null)` today; when #230 also adds `RequestValidatorRunner? runner = null`, the pull request that lands second appends its parameter after the other's, both optional, and every call site that passes the first optional parameter positionally is updated in that pull request | Dilemma (recommended, pending the maintainer) | the order is a public-signature choice the two plans share; the alternative is named arguments only at the call sites with the registration building the provider through the container; recorded here and in the #230 plan |

---

## Implementation Phases

### Phase 1: Benchmark and Allocation Clean-up (pull request S1)

> **Goal**: Measure the resolution cost with the cache absent, and remove the per-call allocations that do not depend on any cache.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/FluentValidationResolutionBenchmarks.cs` (`[MemoryDiagnoser]`; the three arms `ResolveOnly`, `WholeCallBefore`, `WholeCallAfter` of Design Choice 5 over three scenarios: no validator registered, one singleton validator, one transient validator; plus the DataAnnotations and MiniValidator calls as the control of Design Choice 3). Order of work: (a) add the benchmark file; (b) run it on the unchanged provider with `dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj -- --filter "*FluentValidationResolution*"` and record `ResolveOnly` and `WholeCallBefore`; (c) only then edit the provider (task 2) and run again for `WholeCallAfter`; (d) compute the share of Design Choice 5 and put the table in the pull request description. Verify the filter first with `--list flat --filter "*FluentValidationResolution*"`.
2. **Modify** `src/Encina.FluentValidation/FluentValidationProvider.cs`: keep the resolution as it is; replace `.ToList()` by `.ToArray()` with an early return on `Length == 0`, and replace the `Select`/`ToArray`, `SelectMany`/`Where`/`Select`/`ToList` chains by two loops with a pre-sized `Task<FluentValidation.Results.ValidationResult>[]` and a single `List<ValidationError>` (behavior and error order unchanged).
3. **Modify** `.github/coverage-manifest/Encina.FluentValidation.json` through `mechanical-fixer`: the explicit per-file unit target (95) for `FluentValidationProvider.cs` (table in Research; the other flags are entered by S2 with their tests).
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
   plus DataAnnotations and MiniValidator controls; arms ResolveOnly = GetServices<IValidator<T>>().ToList() in a scope
   opened once in GlobalSetup, WholeCallBefore = ValidateAsync on the UNCHANGED provider, WholeCallAfter).
2. BEFORE editing the provider run the benchmark and record Allocated bytes of ResolveOnly and WholeCallBefore for the
   singleton scenario. Then in FluentValidationProvider.ValidateAsync replace the ToList and the LINQ chains by an array
   and two loops. Result values and error ORDER must not change. Run again for WholeCallAfter.
3. Compute share = 100 * Allocated(ResolveOnly) / Allocated(WholeCallBefore) (singleton scenario), also with
   WholeCallAfter, and put the table (arm, scenario, mean, allocated bytes, share) in the pull request description
   and the benchmark commit message. S2 is opened only when the share is 10 or more.

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

1. **Create** `src/Encina.FluentValidation/ValidatorResolutionCache.cs`: `public sealed class ValidatorResolutionCache`; constructor `(IServiceScopeFactory scopeFactory)` (a singleton can take the factory, never a scoped service); one public method `IReadOnlyList<IValidator<TRequest>> Resolve<TRequest>(IServiceProvider scopedProvider)` that returns the shared array for `Singleton`, an empty list for `None` without touching `scopedProvider` (the `ArgumentNullException.ThrowIfNull(scopedProvider)` runs first in every case, register row C8), and `scopedProvider.GetServices<IValidator<TRequest>>().ToArray()` for `Dynamic`. An internal `Classify(Type)` runs the classification of Design Choice 2 as the maintainer decides it (the plan assumes option D: descriptor lifetimes first, then the two-scope reference-equality probe; any exception except `OperationCanceledException` gives `Dynamic`, register row C3). Under option D the registration method also hands the cache the descriptor catalog (a second internal constructor parameter, an `IServiceCollection` reference captured by the `TryAddSingleton` factory); the public constructor stays `(IServiceScopeFactory)`.
2. **Modify** `FluentValidationProvider.cs`: optional trailing constructor parameter `ValidatorResolutionCache? cache = null`; when null the provider resolves as in phase 1. The plan of #230 adds its own optional `RequestValidatorRunner? runner = null` to the same constructor: the pull request that lands second appends its parameter after the other's, both optional, so the 6 existing `new FluentValidationProvider(serviceProvider)` sites compile unchanged (verified with `git grep -c "new FluentValidationProvider(" -- tests`).
3. **Modify** `src/Encina.FluentValidation/ServiceCollectionExtensions.cs`: `services.TryAddSingleton<ValidatorResolutionCache>()` in both `AddEncinaFluentValidation` overloads (next to `:62` and `:104`); the singleton takes `IServiceScopeFactory`, which the container always provides.
4. **Modify** `src/Encina.FluentValidation/PublicAPI.Unshipped.txt` (the type, its constructor and `Resolve`) and `.github/coverage-manifest/Encina.FluentValidation.json` (a new entry for `ValidatorResolutionCache.cs`) through `mechanical-fixer`.
5. **Modify** `src/Encina.FluentValidation/README.md` through `docs-writer`: a short "Performance" note (what is cached, what is not, the lifetime guarantee) and the changelog fragment `changelog.d/702-validator-resolution-cache.changed.md` through `mechanical-fixer`.
6. Tests (all new files): `ValidatorResolutionCacheTests` (unit: `None`, `Singleton` returns the same array on the second call, `Dynamic` with a scoped validator returns a different instance per scope, transient likewise, mixed registrations are `Dynamic`, a validator whose constructor throws in the probe is `Dynamic`, an `OperationCanceledException` thrown by a validator constructor in the probe propagates and is not classified, the same request type in two caches does not share state, 64 concurrent first calls give one stable classification, and the tenant-factory scenario of Design Choice 2: a `Transient` factory registration returning one of two pre-built validators by a scoped `ITenantContext`, resolved in two scopes with different tenants, must give each tenant its own validator under option D (`Dynamic`) or, under option B, the test asserts the documented limitation); `FluentValidationProviderCacheTests` (unit: results identical with and without the cache for each lifetime); `ValidatorResolutionCacheGuardTests` (guard: null `scopeFactory`, null `scopedProvider`, and the `FluentValidationProvider` constructor, request and context checks; a null cache in the provider is a valid case that keeps the uncached behavior, asserted as a valid result, not an exception); `ValidatorResolutionCacheRegistrationTests` (DI: builds the provider with `ValidateOnBuild` and `ValidateScopes` for both `AddEncinaFluentValidation` overloads and resolves the provider, the orchestrator and the cache); `ValidatorResolutionCacheProperties` (FsCheck: for any mix of lifetimes the cached provider returns the same `ValidationResult` as the uncached one); `ValidatorResolutionCacheContractTests` in `tests/Encina.ContractTests/Validation/FluentValidation/` (contract: the public contract of `Resolve<TRequest>`: the same instances for a singleton type across scopes, distinct instances per scope for scoped and transient types, an empty list for an unregistered type, `ArgumentNullException` for a null provider; real instances, no reflection), which needs one `ProjectReference` to `Encina.FluentValidation` added to `tests/Encina.ContractTests/Encina.ContractTests.csproj` (that project does not reference it today; both files are counted below).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #702 plan docs/plans/validator-resolution-cache-implementation-plan-702.md, phase 2 of 2. Only do this phase when
the phase 1 benchmark shows the resolution cost threshold of Design Choice 5. Registrations are immutable after the
container is built; validators may be singleton, scoped or transient.

TASK:
1. Create ValidatorResolutionCache (container singleton, ctor IServiceScopeFactory) classifying each request type per
   the maintainer's answer to Design Choice 2 (recommended: descriptor lifetimes first, then the two-scope probe):
   None, Singleton (all singleton descriptors AND reference-equal instances across two scopes), Dynamic (everything
   else, or a throwing probe; OperationCanceledException propagates). A factory registration that returns a shared
   instance chosen by scoped state (per tenant) MUST NOT end up Singleton: add the test of Design Choice 2.
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
| S2 | Resolution cache | 1 (`ValidatorResolutionCache.cs`) | 4 (provider, `ServiceCollectionExtensions.cs`, PublicAPI, `Encina.ContractTests.csproj` reference) | 6 (cache unit, provider unit, guard, DI, property, contract) | 0 | 1 | 2 (README, changelog fragment) | 14 | ~900 |
| **Total** | | **1** | **5** | **7** | **0** | **2** | **2** | **17** | **~1,100** |

The columns sum to the total (1 + 5 + 7 + 0 + 2 + 2 = 17 files; S1 3 + S2 14 = 17); each pull request is at most 15 files and 1500 lines. S2 is not opened when the S1 measurement is under the threshold of Design Choice 5; the plan then ends after S1 and issue #702 is left with its acceptance criteria "validator list cached ... ConcurrentDictionary", "thread-safe" and "applied to all 3" unmet by design. The criteria are the maintainer's to amend (this plan never comments on an issue): the proposed amended text, for the maintainer to apply, is the draft `artifacts/decisions/plan-batch/followups/validator-resolution-cache-criteria-amendment.md`.

Per-file coverage targets (per flag, with a one-sentence justification), written into `.github/coverage-manifest/Encina.FluentValidation.json` by the pull request that adds or touches the file:

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `ValidatorResolutionCache.cs` | 95 | 90 | 85 | 60 | Every classification (none, singleton, scoped, transient, mixed, tenant factory, throwing probe) and the concurrent first call are reachable with plain registrations; the guard tests reach the constructor and `scopedProvider` argument checks; the contract tests execute the real `Resolve` for each lifetime (its remaining lines are the probe error paths only unit tests drive); the property test drives random lifetime mixes through the resolve method |
| `FluentValidationProvider.cs` | 95 | 60 | 0 | 50 | The loops and the empty path are covered by the existing provider tests and the new cache tests; the guard tests reach the constructor, request and context checks (every validated parameter, the rest of the file is the async body they cannot reach); no contract applies (the provider implements `IValidationProvider`, whose contract is already pinned in core); the property test compares cached and uncached results |
| `ServiceCollectionExtensions.cs` | 90 | 85 | 0 | 0 | The new `TryAddSingleton` lines are covered by the DI test; the existing null checks are guard-tested; no contract or property applies to registration |

The S1 pull request enters only the `FluentValidationProvider.cs` unit target (95, met by the existing provider tests, which it edits); no guard test of the provider exists on `main` (verified by searching `tests/Encina.GuardTests` for `FluentValidationProvider`), so the guard, contract and property targets of the table are entered by S2, in the same pull request as the tests that meet them (the S2 guard test class also covers the provider's constructor, request and context checks).

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
2. S2 (only if the benchmark shows share = 100 * Allocated(ResolveOnly) / Allocated(WholeCallBefore) of at least 10
   for the singleton scenario, baseline measured before the provider edit): ValidatorResolutionCache, a
   container singleton that classifies each request type as None, Singleton or Dynamic (descriptor lifetimes confirmed
   by a two-scope probe, per the maintainer's answer to Design Choice 2), so a scoped, transient or per-tenant factory
   validator is never cached; optional constructor parameter on the provider; TryAddSingleton in
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

1. Link this plan from #702 and ask the maintainer to answer Design Choices 2 and 5 and register rows C3, C6, C7 and C9 when work starts.
2. Implement S1 and read its benchmark table; open S2 only when the threshold of Design Choice 5 is met, with `Fixes #702` on the last pull request that ships. Maintainer action if S2 is never opened: amend the acceptance criteria of #702 (cache, thread safety, "all 3") to the measurement outcome with the text of `artifacts/decisions/plan-batch/followups/validator-resolution-cache-criteria-amendment.md`, then close it; the plan itself never comments on the issue.
3. Open the follow-up issue when the first pull request opens: the same resolution cost in the `IRequestValidator<T>` runner of #230 (follow-up issue to be opened, draft: `artifacts/decisions/plan-batch/followups/validation-runner-resolution-cache.md`).
