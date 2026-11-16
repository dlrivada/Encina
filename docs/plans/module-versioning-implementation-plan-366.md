# Implementation Plan: Module Versioning (`[ModuleVersion]`, `[ModuleApiVersion]`, deprecation, compatibility validation)

> **Issue**: [#366](https://github.com/dlrivada/Encina/issues/366)
> **Type**: Feature
> **Complexity**: Medium (8 phases, 6 pull requests of 12, 14, 15, 3, 6 and 7 files)
> **Estimated Scope**: ~900 lines of production code + ~2,000 lines of tests
> **Family**: modular monolith (#352 boundary rule and context producer, #358 module API marker `IModulePublicApi`, #359 analyzer package, #360 module graph and `[DependsOn]`, #364 health); this plan owns module and API versions, version resolution and deprecation

---

## Summary

The issue asks for versioned module contracts so that modules can evolve without breaking their consumers: `[ModuleVersion]` and `[ModuleMinimumVersion]` on a module, `[ModuleApiVersion]` and a deprecation marker on module API interfaces, `[HandlerVersion]` on handlers, a version resolution configuration (`AddEncinaModuleVersioning`), `RequestedVersion` on the request context, a Roslyn analyzer for deprecated usage, a startup compatibility check, version information in traces and an adapter pattern. State of `origin/main` (`0b941beeb`), verified by reading and grep (nothing named `ModuleVersion`, `ModuleApiVersion` or `WithEncinaModules` exists in `src` or `tests`):

| Issue item | Today | Owner of what remains |
|------------|-------|-----------------------|
| `[ModuleVersion]`, `[ModuleMinimumVersion]` | Missing; `IModule` has only `Name` and `ConfigureServices` (`src/Encina/Modules/IModule.cs`) | This plan, Phase 1 |
| `[ModuleApiVersion]`, deprecation marker | Missing; the module API marker `IModulePublicApi` is #358 (Phase 3) | This plan (attributes), #358 (marker) |
| `[HandlerVersion]` | Missing; a handler is selected by request type (`IRequestHandler<TRequest,TResponse>`), so V1 and V2 are different request types | This plan (Design Choice 3) |
| Version resolution and options | Missing | This plan, Phase 2 |
| `IRequestContext.RequestedVersion` | Missing; the context has a metadata bag and the precedent `RequestContextModuleExtensions` (`GetModuleName`, `WithModuleName`) | This plan (Design Choice 4) |
| Deprecation analyzer | No module analyzer exists; #359 creates `Encina.Modules.Analyzers` with `ENCMOD001-003`, the issue's `ENC010` would collide with the generic `ENC` prefix | This plan adds `ENCMOD004` to that package |
| Startup compatibility check | Missing; the dependency graph and `[DependsOn]` are #360 | This plan, Phase 2 (reads #360's graph) |
| Version in OpenTelemetry traces | `ModuleActivitySource` (`src/Encina/Modules/Diagnostics/ModuleActivitySource.cs`) tags `module.name` | This plan, Phase 3 |
| Adapter pattern | Missing | Design Choice 5 (a documented pattern, no framework type) |
| `ValidateVersionCompatibility` on `AddEncinaModules` | `ModuleConfiguration` has no such option; #360 edits that file | Design Choice 6 (own options, `ModuleConfiguration` untouched) |

Corrections to the issue text: `Encina.Analyzers` does not exist (the package is `Encina.Modules.Analyzers` from #359); `IModulePublicApi` is declared by #358, not here; the issue's `DeprecationBehavior.Throw` is replaced by `Reject` (Design Choice 7) because business rules never throw (`AGENTS.md` section 3); `[Deprecated]` is named `ModuleApiDeprecatedAttribute` to avoid a name that reads like `[Obsolete]`, which Encina's own code never uses.

**Delivered**: version value type and attributes; a version registry built from the registered modules; options and an opt-in registration; a pipeline behavior that resolves the requested version, rejects an unsupported one and logs or rejects a deprecated one; a startup compatibility validator; trace tags and a counter; the analyzer rule `ENCMOD004`; a guide with the adapter pattern.

**Not in scope**: mapping an HTTP `api-version` to the requested module version (an application calls `WithRequestedModuleVersion`; `Asp.Versioning` stays the HTTP answer, as the issue's first alternative says); version fallback and tenant-specific versions (issue file 1); versions in module health data and audit of deprecated invocations (issue file 2).

**Affected packages**: `Encina` (core, `Encina.Modules`), `Encina.Modules.Analyzers` (created by #359), `Encina.OpenTelemetry` (registers the new meter).

**Provider category**: none. Versioning is in-process and provider independent.

---

## Design Choices

<details>
<summary><strong>1. The version format and the default (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Major.Minor` (a readonly record struct `ModuleVersion`, parsed from "2.0"; compatible when the major is equal and the available minor is at least the required one); a module without `[ModuleVersion]` is version 1.0** | Matches every example of the issue; a contract changes at major and minor, never at patch | No patch or pre-release label |
| **B) Full SemVer `Major.Minor.Patch[-label]`** | Standard, comparable with package versions | A patch number says nothing about a contract; the issue's `SupportedVersions = ["1.0", "1.5", "2.0"]` would need normalizing |
| **C) An integer API revision** | Simplest | Does not express "compatible minor" |

### Chosen Option: **A — `Major.Minor`, default 1.0** (recommended, pending the maintainer)

### Rationale

A wins when versions describe contracts and the examples of the issue are the specification. B wins if module versions must be the same strings as the NuGet versions of the module's assembly. The default 1.0 means an unversioned module is compatible with a requirement of "1.0" and incompatible with "2.0", which keeps unversioned applications working without declaring anything (every feature is opt-in, `AGENTS.md` section 1).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How a module states the version it needs from another module (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate repeatable attribute `[RequiresModuleVersion(typeof(OrdersModule), "2.0")]` on the module class, read together with `[DependsOn]` of #360** | No edit to #360's attribute; a requirement without a dependency is rejected at startup | Two attributes say related things |
| **B) A `MinimumVersion` named property on `[DependsOn]`** | One attribute | Changes a type another plan owns; `DependsOnAttribute` would depend on the version type |
| **C) Implicit: the issue's startup check compares only module versions against `[ModuleMinimumVersion]` of the provider, no consumer-side requirement** | Least ceremony | The issue's example "Module A requires Orders v2.0 but Orders is v1.5" cannot be expressed |

### Chosen Option: **A — a separate attribute** (recommended, pending the maintainer)

### Rationale

A wins while #360 is unmerged and owns `DependsOnAttribute`. B wins if the maintainer wants one attribute per dependency once both plans are implemented. C wins if only provider-side minimums are wanted.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where the handler version is declared (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[RequestVersion("1.0")]` (with deprecation properties) on the request type; the pipeline behavior reads it from `typeof(TRequest)` and caches it per closed generic** | The versioned unit is the request contract; no handler lookup in a behavior | The issue's `[HandlerVersion]` name on the handler class changes |
| **B) `[HandlerVersion]` on the handler class, as in the issue** | Matches the issue | A behavior only knows `TRequest`; it would resolve the handler by a DI call per request or a registry keyed by request type |

### Chosen Option: **A — on the request type** (recommended, pending the maintainer)

### Rationale

A wins because requests V1 and V2 are different types and the framework dispatches by request type; static attribute caching per generic type is the project's behavior rule. B wins if a single request type is served by differently versioned handlers (none exists today).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How the requested version reaches the pipeline (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extension methods `WithRequestedModuleVersion(string)` and `GetRequestedModuleVersion()` over the context metadata (key `Encina.RequestedModuleVersion`), like `RequestContextModuleExtensions`** | No change to `IRequestContext`, which has 15 implementers (grep of `class|record ... : IRequestContext` over `src` and `tests`) | A string key, no compile-time member |
| **B) A new member `ModuleVersion? RequestedVersion { get; }` on `IRequestContext`, as the issue shows** | Discoverable | Every implementer changes, including test doubles; the context is Encina's central contract |

### Chosen Option: **A — metadata extensions** (recommended, pending the maintainer)

### Rationale

A wins because the requested version is a caller hint, not identity, and the module precedent uses the same shape. B wins if the maintainer wants the version to be as first-class as the correlation id. The hint is never an authorization: an unsupported or missing value never widens access (Design Choice 7).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. The version adapter (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A documented pattern: the application registers an adapter class that implements the old interface over the new one with plain DI (`services.AddScoped<IOrdersModuleApiV1, OrdersV1ToV2Adapter>()`); the guide shows it** | No framework type for a three-line registration; nothing to maintain | No shared name for the pattern |
| **B) `IModuleVersionAdapter<TOld, TNew>` and `AddModuleVersionAdapter<TAdapter>()` in core, as in the issue** | A named, discoverable pattern; the startup validator can require an adapter for a deprecated API | A framework abstraction whose only purpose is migration; pre-1.0 rules push against migration helpers in Encina itself |

### Chosen Option: **A — a documented pattern** (recommended, pending the maintainer)

### Rationale

A wins because the adapter is application code by nature (it knows both contracts) and the framework has nothing to add. B wins if the maintainer wants the validator to check that every deprecated API has an adapter before its removal version.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Opt-in and the home of `ValidateVersionCompatibility`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaModuleVersioning(Action<ModuleVersioningOptions>)` is its own opt-in registration; `ValidateVersionCompatibility` is a property of `ModuleVersioningOptions` (default `true`); `ModuleConfiguration` and `AddEncinaModules` are not edited** | Pay for what you use; no conflict with #360, which edits `ModuleConfiguration` | The issue puts the flag on `AddEncinaModules`; here it is on the versioning options |
| **B) The flag on `AddEncinaModules` configuration** | As in the issue | Edits a type that #360 and #352 also change |

### Chosen Option: **A — own options and registration**

### Rationale

Every feature is opt-in; the registration throws `InvalidOperationException` when `IModuleRegistry` is not registered (a registration error, like the bus of #358).

**Class:** Trivial
**Settled by:** `AGENTS.md` section 1 (every feature opt-in) and section 3 (registration completeness).

</details>

<details>
<summary><strong>7. Unsupported and deprecated versions are `Left`, never exceptions or a pass</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An unsupported requested version, and an explicit version on a request whose target module cannot be resolved, always return `Left(encina.modules.version_unsupported)` (`ErrorCategory.InvalidRequest`); a deprecated version follows `DeprecationBehavior` (`Log`, `Ignore` or `Reject`; the default is D9) and `Reject` returns `Left(encina.modules.version_deprecated)`, `ErrorCategory.FailedPrecondition`; `Throw` and `UnsupportedBehavior` of the issue do not exist** | Operations return `Either`; an unsupported version cannot pass; no single-valued enum | The issue's `Throw` value is renamed `Reject` |
| **B) Keep `Throw` (an exception) and an `UnsupportedBehavior` enum with `Reject` only** | Matches the issue text | Exceptions for business rules; a one-value enum |

### Chosen Option: **A — `Left` results**

### Rationale

Railway Oriented Programming is the project rule, and a gate that cannot resolve its context fails closed. A request that carries an explicit `[RequestVersion]` (or an explicit context hint) but has no resolvable target module (`IModuleHandlerRegistry.GetModuleName` returns null and there is no ambient `IModuleExecutionContext` module) is `Left(encina.modules.version_unsupported)`, never "supported": the version gate cannot prove the version, so it denies. A request with no `[RequestVersion]` and no hint is a pass-through of the behavior. The defaults for a missing hint and for a deprecated version are registered separately in D9.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (Railway Oriented Programming: operations return `Either`, never exceptions for business logic; gates fail closed) and ADR-062 (every error code carries an `ErrorCategory`).

</details>

<details>
<summary><strong>8. Where the analyzer rule lives (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ENCMOD004` in `Encina.Modules.Analyzers` (the package #359 creates): a reference to a type or member marked `[ModuleApiDeprecated]` outside its own module is a warning** | One analyzer package for the module family, the same `ENCMOD` prefix as `ENCMOD001-003` | The pull request waits for #359 pull request 7 |
| **B) A second analyzer package for versioning** | Independent | A second `netstandard2.0` component and package for one rule |

### Chosen Option: **A — the shared package** (recommended, pending the maintainer)

### Rationale

A wins when the maintainer wants one analyzer reference for module rules. B wins if versioning must ship before #359's analyzer. The rule reports Warning by default and carries the removal version in its message; there is no code fix.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Defaults for a missing version hint and a deprecated version (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A request with `[RequestVersion]` but no context hint uses the attribute's version, and with neither uses `DefaultVersion`; `DeprecationBehavior` defaults to `Log` (a deprecated version keeps serving and is logged and counted)** | Adopting versioning breaks no caller; deprecation is visible before removal | A deprecated API keeps working until the maintainer sets `Reject` |
| **B) `DeprecationBehavior` defaults to `Reject`** | A deprecated version cannot be used by accident | The first deprecation breaks every caller of the old version at once |

### Chosen Option: **A — `Log` by default** (recommended, pending the maintainer)

### Rationale

A wins for a staged removal: warn first (the analyzer rule `ENCMOD004` warns at compile time too), then set `Reject` once callers moved. B wins for teams that treat a deprecation as an immediate stop. The defaults never relax D7: an unsupported or unresolvable explicit version is always `Left`.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Version type and attributes (pull request 1)

> **Goal**: The vocabulary, with no runtime behavior.

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Versioning/ModuleVersion.cs`: `public readonly record struct ModuleVersion(int Major, int Minor) : IComparable<ModuleVersion>` with `ModuleVersion(string)`-style `Parse` and `TryParse` ("2.0", "1.5"; blank, three-part, negative or non-numeric values rejected), `IsCompatibleWith(ModuleVersion available)` (same major, available minor at least this minor), `ToString()` as "2.0" (D1).
2. `ModuleVersionAttribute(string version)`, `ModuleMinimumVersionAttribute(string version)` (module class), `ModuleApiVersionAttribute(string version)` (interface), `ModuleApiDeprecatedAttribute(string message)` with `RemovalVersion` (interface or member), `RequestVersionAttribute(string version)` with `DeprecatedMessage` and `RemovalVersion` (request type, D3), `RequiresModuleVersionAttribute(Type module, string minimumVersion)` (module class, repeatable, D2); every constructor throws `ArgumentException` for an invalid version string and `ArgumentNullException` for a null type.
3. Tests: unit (parse and compare, compatibility table, every attribute's validation), guard, property (for any two valid versions `IsCompatibleWith` is reflexive and a higher minor of the same major is compatible with a lower requirement, never the reverse); `PublicAPI.Unshipped.txt` of `Encina` and the `Encina.json` manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #366; plan docs/plans/module-versioning-implementation-plan-366.md. D1-D5, D8 and D9 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no [Obsolete] in Encina's own code; the deprecation marker is a contract attribute for users.

TASK:
Add ModuleVersion and the six attributes with their tests.

KEY RULES:
- Parsing is culture invariant and strict ("2.0" only); invalid input is an ArgumentException from constructors and false from TryParse.
- XML docs with examples on every public member; PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina/Modules/IModule.cs
- src/Encina/Modules/RequestContextModuleExtensions.cs
- tests/Encina.UnitTests/Core/Modules
```

</details>

---

### Phase 2: Registry, options, registration and the compatibility validator (pull request 2)

> **Goal**: The versions of the registered modules, the options and the startup check.

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Versioning/IModuleVersionRegistry.cs` and internal `ModuleVersionRegistry.cs`: `ModuleVersion GetVersion(string moduleName)` (default 1.0, D1), `ModuleVersion GetMinimumVersion(string moduleName)`, `bool Supports(string moduleName, ModuleVersion requested)`, `IReadOnlyList<ModuleVersion> GetSupportedVersions(string moduleName)`; built once from `IModuleRegistry.Modules` and the attributes; names are case-insensitive like `ModuleRegistry`; an unknown module name is an `ArgumentException`.
2. `ModuleVersioningOptions` (`DefaultVersion`, `SupportedVersions`, `DeprecationBehavior` {`Log`, `Ignore`, `Reject`}, `ValidateVersionCompatibility` default true; D6, D7).
3. `ModuleVersionCompatibilityValidator` (internal static, pure): over `IModuleVersionRegistry`, the `[RequiresModuleVersion]` requirements and #360's `IModuleGraph`, it lists every violation (a requirement above the provider's version, a requirement on a module that is not a declared dependency, a version below the provider's minimum); `ModuleVersionStartupValidator` (internal hosted service) throws `InvalidOperationException` listing all violations at start when `ValidateVersionCompatibility` is on.
4. `RequestContextVersionExtensions` (`WithRequestedModuleVersion`, `GetRequestedModuleVersion`, key `Encina.RequestedModuleVersion`, D4).
5. `ModuleVersioningServiceCollectionExtensions.AddEncinaModuleVersioning(...)` (D6): `TryAddSingleton` for the registry and options and the hosted validator. It does not register the behavior of Phase 3: pull request 3 edits this extension to add it, so pull request 2 builds alone.
6. Tests: unit (default and declared versions, case-insensitive names, each validator violation, no violation, enforcement on and off, the context extensions), guard, property (generated module and requirement sets: no violation when every requirement is at most the provider's version and a declared dependency), DI test (`ValidateOnBuild`, `ValidateScopes`; missing `IModuleRegistry` throws); PublicAPI, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #366. #360 (IModuleGraph, [DependsOn]) is merged. D2 (separate requirement attribute) and D4 (metadata extensions) apply.

TASK:
Add the registry, options, validator, hosted start check, context extensions and registration with their tests.

KEY RULES:
- The validator is a pure function (every branch testable); one InvalidOperationException lists every violation, never the first only; messages carry module names and version strings only.
- The registry is a singleton holding no scoped service; no scoped service in a singleton.
- Registration completeness: AddEncinaModuleVersioning registers every option and dependency it resolves, proven by a DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina/Modules/ModuleRegistry.cs
- src/Encina/Modules/ModuleServiceCollectionExtensions.cs
- src/Encina/Modules/RequestContextModuleExtensions.cs
```

</details>

---

### Phase 3: Version resolution behavior and observability (pull request 3)

> **Goal**: Requests carry a version, unsupported ones are rejected, deprecated ones are visible.

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Versioning/ModuleVersionPipelineBehavior.cs` (`[PipelineStage(PipelineStage.Validation, Order = -100)]`, `IPipelineBehavior<TRequest,TResponse>`): reads `[RequestVersion]` of `typeof(TRequest)` through a static per-generic cache; the requested version is `GetRequestedModuleVersion()` or the request attribute's or `DefaultVersion`; the target module comes from `IModuleHandlerRegistry` and the ambient `IModuleExecutionContext`; a request with no `[RequestVersion]` and no hint is a pass-through; an explicit version whose target module cannot be resolved gives `Left(encina.modules.version_unsupported)` (`ErrorCategory.InvalidRequest`, fail closed, D7); unsupported gives `Left(encina.modules.version_unsupported)`; deprecated follows `DeprecationBehavior` (D7, D9); the activity gets tags `module.version` and `module.api_version`. It also edits `ModuleVersionStartupValidator` (a pull request 2 file) to log 7667 and 7668 through `ModuleVersionLog`, and `ModuleVersioningServiceCollectionExtensions` to register the behavior.
2. `ModuleVersionErrors` (public static): the two error factories; `encina.modules.version_unsupported` is `ErrorCategory.InvalidRequest`, `encina.modules.version_deprecated` is `ErrorCategory.FailedPrecondition` (ADR-062); messages name module, request type and versions only.
3. `ModuleVersionLog` (`[LoggerMessage]`): EventIds 7665 deprecated version used (Warning), 7666 unsupported version rejected (Warning), 7667 compatibility validated at startup (Information), 7668 incompatibility found at startup (Error, count and module names only). `EventIdRanges.ModulesVersioning = (7665, 7679)` registered first and the `Encina` entry of the allocation test map `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
4. `ModuleVersionMetrics` (in `Encina`): meter `Encina.Modules.Versioning`, counter `encina.modules.version_check_total` with tags `module`, `api_version`, `outcome` (`ok`, `deprecated`, `unsupported`) and the tenant tag only when multi-tenancy is on (ADR-054 D4, [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md)). The label values are bounded: `module` is a registered module name and `api_version` comes from the declared attribute set (the registry's supported versions); an unvalidated hint is never a label, an unsupported or unresolvable one is recorded with the constant `api_version` value `unsupported`. Registration of the meter in `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` is pull request 4.
5. Tests: unit (supported, unsupported, unresolvable target module, no attribute pass-through, deprecated with each behavior, missing hint uses the default, a `Left` from `nextStep` passes through unchanged, tags and counter with a listener), guard, contract (error codes and categories on the real errors), property (any version in the supported set passes and any outside is rejected), and the pull request 2 DI test extended to assert the behavior registration; `PublicAPI.Unshipped.txt` of `Encina` and the manifest.
6. Pull request 4 (`Encina.OpenTelemetry`): the one-line `AddMeter(ModuleVersionMetrics.MeterName)` edit of `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, a unit test that the meter is registered, and the `Encina.OpenTelemetry.json` manifest entry. It adds no public symbol, so `src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt` (the file that exists, in the `PublicAPI` subfolder) is not touched.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #366. D3 (attribute on the request type) and D7 (Left results) apply. #352 pull request 1 supplies IModuleExecutionContext.CurrentModule in every package.

TASK:
Add the behavior, the error factories, the log, the meter, the EventId range and the tests.

KEY RULES:
- A missing hint uses DefaultVersion (D9); an explicit unsupported value, or an explicit version whose target module cannot be resolved, is rejected (fail closed, D7); a Left from the next step is returned unchanged, never swallowed.
- Static per-generic-type attribute caching; no per-request reflection.
- EventIds packed from 7665 with no gaps inside 7665-7679, range registered in EventIdRanges.cs first; logs carry error codes and names only, never EncinaError.Message.
- Tenant tag only with multi-tenancy (ADR-054 D4).

REFERENCE FILES:
- src/Encina/Pipeline/Ordering/PipelineStage.cs
- src/Encina/Modules/Diagnostics/ModuleActivitySource.cs
- src/Encina/Errors/ErrorCategory.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 4: The deprecation analyzer (pull request 5)

<details>
<summary>Tasks</summary>

1. `src/Encina.Modules.Analyzers/DiagnosticDescriptors.cs`: `ENCMOD004` ("a module API marked `[ModuleApiDeprecated]` is used; it is removed in version {removal}"), category `Encina.Modules`, default severity Warning; `DeprecatedModuleApiAnalyzer.cs` (`[DiagnosticAnalyzer(LanguageNames.CSharp)]`, concurrent execution enabled, generated code skipped): reports a reference to a type, member or interface implementation marked with the attribute when the referencing code is not in the module that owns it (ownership as in #359's `ModuleOwnership`).
2. Tests: analyzer tests in the project #359 creates for its analyzers (a deprecated interface used from another module warns, used inside its own module does not, no attribute no warning); the manifest of the analyzers package.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #366. #359 pull request 7 created Encina.Modules.Analyzers (ENCMOD001-003, ModuleOwnership). D8 is recommended, pending the maintainer.

TASK:
Add ENCMOD004 and its analyzer tests.

KEY RULES:
- The analyzer targets netstandard2.0 like Encina.Security.ABAC.Analyzers; no references to Encina assemblies (the attribute is matched by full name).
- Identifier prefix ENCMOD; no code fix; severity Warning.

REFERENCE FILES:
- src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj
- .github/coverage-manifest/Encina.Security.ABAC.Analyzers.json
```

</details>

---

### Phase 5: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. OpenTelemetry (function 2) and structured logging (function 3): Phase 3.
2. Validation (function 5): the startup compatibility check (Phase 2) and the version string rules (Phase 1).
3. Module isolation (function 11): version boundaries reinforce module boundaries; the behavior reads the module of the handler and the ambient module (#352) and grants nothing by version.
4. Multi-tenancy (function 10): the tenant tag on the counter only; tenant-specific module versions are deferred (issue file 1). No tenant-qualified identifier is created, so the reserved `default` tenant segment and injectivity rules do not apply.
5. Deferred, as issue files in this plan's worktree: `artifacts/issues/module-version-fallback-and-tenant-versions.md` (resilience fallback to an older version and tenant-specific versions) and `artifacts/issues/module-version-health-and-audit.md` (versions in module health data, audit of deprecated invocations). Caching of versioned responses stays with the response caching feature (a cache key that includes the version is a user decision).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #366; the cross-cutting matrix is the contract.

TASK:
Verify each Include row exists in code and tests and that the two issue files are written.

KEY RULES:
- Never log EncinaError.Message; the version hint never grants access.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. **Unit, Guard, Contract, Property, DI** as in Phases 1 to 4; **Integration**: justification file `tests/Encina.IntegrationTests/Core/Modules/ModuleVersioning.md` (in-process, no database; the behavior is exercised through the real `IEncina` in a unit test with `AddEncina` and `AddEncinaModuleVersioning`); **Load**: justification `tests/Encina.LoadTests/Core/Modules/ModuleVersioning.md` (a cached attribute read and a dictionary lookup); **Benchmark**: justification `tests/Encina.BenchmarkTests/Core/Modules/ModuleVersioning.md`.
2. **Per-file targets** (each flag with a one-sentence justification in the manifest; baselines of edited files are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; a target is the measured value or higher):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `Versioning/ModuleVersion.cs` | 100 | 90 | 0 | 90 | 0 | Pure parsing and comparison; the property test generates versions |
   | The six attributes | 100 | 90 | 0 | 0 | 0 | Argument validation is fully reachable |
   | `Versioning/ModuleVersionRegistry.cs` | 95 | 80 | 0 | 0 | 0 | Built from real modules in unit tests |
   | `Versioning/ModuleVersionCompatibilityValidator.cs` | 100 | 0 | 0 | 90 | 0 | A pure function with no parameterized public member |
   | `Versioning/ModuleVersionStartupValidator.cs`, `ModuleVersioningServiceCollectionExtensions.cs` | 90 | 70 | 0 | 0 | 0 | Enforce and skip paths run with fakes; guard reaches the argument checks |
   | `Versioning/ModuleVersionPipelineBehavior.cs` | 95 | 80 | 0 | 80 | 0 | Every outcome runs through the real pipeline in unit tests |
   | `Versioning/ModuleVersionErrors.cs`, `RequestContextVersionExtensions.cs` | 100 | 80 | 100 | 0 | 0 | One-line factories and extensions; the contract test asserts code and category |
   | `ModuleVersionLog.cs`, `ModuleVersionMetrics.cs` | 90 | 0 | 0 | 0 | 0 | Reached through behavior tests with listeners; no parameterized public member |
   | `Encina.Modules.Analyzers/DeprecatedModuleApiAnalyzer.cs` | 90 | 0 | 0 | 0 | 0 | Analyzer tests compile real snippets; analyzers have no guard surface |

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #366. Per-flag coverage per AGENTS.md section 9; targets live in the package manifests.

TASK:
Write the tests and manifest entries; the three justification files cover only integration, load and benchmark.

KEY RULES:
- Tests execute real code and never swallow an exception; a nullable parameter that accepts null gets a valid-result test, not an exception test; Shouldly through Encina.Testing.Shouldly.
- Targets never go down; CRAP <= 10 on every changed method.

REFERENCE FILES:
- tests/Encina.UnitTests/Core/Modules
- .github/coverage-manifest/Encina.json
```

</details>

---

### Phase 7: Documentation & Finalization (pull request 6)

<details>
<summary>Tasks</summary>

1. XML docs with examples on all new public APIs; `PublicAPI.Unshipped.txt`.
2. `changelog.d/366-module-versioning.added.md` (through `mechanical-fixer`); the guide `docs/features/module-versioning.md` (through `docs-writer`: the version rules, evolving an API from V1 to V2, the adapter pattern of D5 with plain DI, deprecation and removal, the analyzer rule); `docs/INVENTORY.md` and `ROADMAP.md` lines. ADR number assigned when the ADR pull request opens, only if a Dilemma is decided architecturally.
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #366 is implemented and tested.

TASK:
Write the documentation set of the Phase 7 tasks through the specialists that own them.

KEY RULES:
- Pages through docs-writer; fragments, PublicAPI lines and manifests through mechanical-fixer; never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Every pull request builds alone, adds only types and no interface member, so no implementer changes (`IModule` and `IRequestContext` are not edited).

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `ModuleVersion`, 6 attributes, PublicAPI, manifest, 3 test files (unit, guard, property) | 12 | 800 |
| 2 | Registry (interface and implementation), options, compatibility validator, startup validator, context extensions, registration extension (7), PublicAPI, manifest, 5 test files (unit, guard, property, DI, context extensions) | 14 | 1,100 |
| 3 | Behavior, errors, log, metrics, `EventIdRanges`, allocation test map, `ModuleVersionStartupValidator` edit, registration extension edit, PublicAPI, manifest (10), 4 test files (unit, guard, contract, property), DI test edit | 15 | 1,300 |
| 4 | `Encina.OpenTelemetry` `ServiceCollectionExtensions` edit, `Encina.OpenTelemetry.json` manifest, 1 test file | 3 | 120 |
| 5 | Analyzer descriptor edit, `DeprecatedModuleApiAnalyzer`, manifest, 3 analyzer test files | 6 | 500 |
| 6 | Guide, inventory and roadmap lines, changelog fragment, 3 justification files (integration, load, benchmark) | 7 | 400 |

Pull requests 2, 3 and 5 need #360 (graph), #352 pull request 1 (context producer) and, for 5, #359 pull request 7; pull request 3 needs pull request 2 and pull request 4 needs pull request 3; pull request 1 has no prerequisite. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1, 3 and 6 | Opt-in, ROP, fail-closed gates, 12 functions | D6, D7 |
| ADR-021 | EventId ranges registered first | `ModulesVersioning` 7665-7679 |
| [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4 | Tenant tag only with multi-tenancy | Counter tags |
| ADR-062 | `ErrorCategory` on every code | `InvalidRequest` and `FailedPrecondition` |
| Semantic Versioning, API evolution patterns (Fowler) | Compatible minor, breaking major | Concept source named by the issue |
| ASP.NET Core API Versioning (`Asp.Versioning`) | HTTP version negotiation | Out of scope; contrasted in the guide |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IModule`, `IModuleRegistry`, `ModuleConfiguration` | `src/Encina/Modules/` | Registered modules and their types |
| `IModuleHandlerRegistry` | `src/Encina/Modules/IModuleHandlerRegistry.cs` | Module of a request's handler |
| `IModuleExecutionContext` | `src/Encina/Modules/Isolation/` | Ambient caller module |
| `RequestContextModuleExtensions` | `src/Encina/Modules/RequestContextModuleExtensions.cs` | Precedent for the context extensions |
| `ModuleActivitySource` (`module.name`) | `src/Encina/Modules/Diagnostics/ModuleActivitySource.cs` | Trace tags |
| `PipelineStage`, `PipelineStageAttribute` | `src/Encina/Pipeline/Ordering/` | Behavior placement |
| `IModuleGraph`, `[DependsOn]` | #360 | Compatibility validation |
| `Encina.Modules.Analyzers` | #359 | `ENCMOD004` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`ModulesVersioning`, new) | 7665-7679, ids 7665-7668 used | From the assigned block 7650-7699 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 7669-7679 free; registered in `EventIdRanges.cs` and the allocation test map before use. Siblings of this family: #747 7650-7664, #365 7680-7689, #423 7690-7695, #306 none |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 20 | Version type, 6 attributes, registry (2), options, 2 validators, extensions (2), behavior, errors, log, metrics, analyzer (2) |
| Public API and manifests | 8 touches | `Encina` PublicAPI (PR 1, 2, 3), `Encina.json` (PR 1, 2, 3), `Encina.OpenTelemetry.json` and the analyzers manifest; `Encina.OpenTelemetry` PublicAPI (in its `PublicAPI` folder) is not touched |
| Tests | 17 | Unit, guard, property, contract, DI (and its edit), OpenTelemetry meter, analyzer |
| Justification files | 3 | Integration, load, benchmark |
| Documentation | 4 | Guide, inventory, roadmap, changelog fragment |
| **Total** | **57 file touches across 6 pull requests** | The sum of the rows of the split table (12 + 14 + 15 + 3 + 6 + 7) is the authority; a file edited in several pull requests counts in each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #366: module and module API versioning in core, with an analyzer rule. Plan: docs/plans/module-versioning-implementation-plan-366.md; D1-D5, D8 and D9 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (6 pull requests, each builds alone):
1. ModuleVersion and the six attributes (Phase 1).
2. Registry, options, compatibility validator, context extensions and AddEncinaModuleVersioning without the behavior (Phase 2).
3. The version pipeline behavior and its registration, errors, logs 7665-7668, metrics and range 7665-7679 (Phase 3).
4. The meter registration in Encina.OpenTelemetry (Phase 3 task 6).
5. ENCMOD004 in Encina.Modules.Analyzers (Phase 4); 6. documentation (Phase 7).

KEY PATTERNS:
- Versions are Major.Minor; a module without [ModuleVersion] is 1.0; compatible means same major and available minor at least the required one.
- Unsupported is Left(InvalidRequest), deprecated follows Log, Ignore or Reject (FailedPrecondition); a missing hint uses the default; the hint never grants access.
- Registration errors and startup compatibility failures are InvalidOperationException listing every violation; opt-in registration; no scoped service in a singleton.
- Telemetry: tags module.version and module.api_version, counter with a tenant tag only with multi-tenancy; logs carry codes and names only.

REFERENCE FILES:
- src/Encina/Modules/ModuleRegistry.cs
- src/Encina/Modules/RequestContextModuleExtensions.cs
- src/Encina/Pipeline/Ordering/PipelineStage.cs
- src/Encina/Modules/Diagnostics/ModuleActivitySource.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred as in the issue: a cache key that includes the module version is a user decision; no cache code here (version registry lookups are in-memory) |
| 2 | OpenTelemetry | ✅ | Phase 3: tags `module.version` and `module.api_version`, counter with tenant tag only with multi-tenancy (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 3: `ModuleVersionLog`, EventIds 7665-7668 |
| 4 | Health Checks | ⏭️ | Deferred as in the issue: versions in health data are the issue file `module-version-health-and-audit` (#364 reads `IModuleVersionRegistry`) |
| 5 | Validation | ✅ | Version string rules and the startup compatibility check |
| 6 | Resilience | ⏭️ | Deferred as in the issue: fallback to an older version is the issue file `module-version-fallback-and-tenant-versions` |
| 7 | Distributed Locks | ❌ | Not applicable: version resolution is stateless |
| 8 | Transactions | ❌ | Not applicable: version routing, not a data operation |
| 9 | Idempotency | ❌ | Not applicable: resolution is deterministic |
| 10 | Multi-Tenancy | ⏭️ | Deferred as in the issue: tenant-specific module versions are the issue file `module-version-fallback-and-tenant-versions`; only the counter's tenant tag is included |
| 11 | Module Isolation | ✅ | Version boundaries reinforce module boundaries (Phase 5) |
| 12 | Audit Trail | ⏭️ | Deferred as in the issue: audit of deprecated invocations is the issue file `module-version-health-and-audit` |

---

## Prerequisites & Dependencies

- **#360** (`IModuleGraph`, `[DependsOn]`, `EncinaModule`) before pull request 2; **#352 pull request 1** (module context producer) before pull request 3; **#359 pull request 7** (`Encina.Modules.Analyzers`) before pull request 5; **#358 pull request 4** declares `IModulePublicApi`, which the guide shows beside `[ModuleApiVersion]` (no code dependency).
- #364 may read `IModuleVersionRegistry` for health data once it exists (deferred issue file).

---

## Next Steps

1. The maintainer answers D1, D2, D3, D4, D5, D8 and D9 when work on #366 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 first (no prerequisite); pull requests 2 to 5 after their prerequisites; pull request 6 last.
3. Open the two issue files in `artifacts/issues/` when this plan merges.
