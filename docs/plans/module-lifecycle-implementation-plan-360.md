# Implementation Plan: Module Lifecycle Enhancement (discovery, `[DependsOn]`, ordered phases, exports, module graph)

> **Issue**: [#360](https://github.com/dlrivada/Encina/issues/360)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 4 pull requests of 13, 14, 15 and 14 files)
> **Estimated Scope**: about 900 lines of production code and about 1,800 lines of tests, split over four pull requests
> **Family**: modular monolith (#352 umbrella, #358, #359, #364); this plan owns the dependency graph, the exports, the lifecycle hooks and the module state that the other four consume

---

## Summary

Encina already has a module system, and the issue's "current state" (three empty interfaces) is stale. On `origin/main` (`e694eb032`) the system lives in the core `Encina` package, namespace `Encina.Modules` (`src/Encina/Modules/`, 28 files):

- `IModule` (`Name`, `ConfigureServices(IServiceCollection)`), `IModuleLifecycle : IModule` (`OnStartAsync`, `OnStopAsync`), `IModuleRegistry` (`Modules`, `GetModule`, `ContainsModule`, `GetLifecycleModules`), `ModuleRegistry` (unique names, case-insensitive).
- `ModuleConfiguration.AddModule<T>()`, `AddModule(IModule)`, `AddModule(IModule, Assembly?)`, `WithoutHandlerDiscovery()`, and `AddEncinaModules` (`ModuleServiceCollectionExtensions.cs`), which registers the registry, the handler registry, each module as a singleton, `ModuleLifecycleHostedService` (today only when a module implements `IModuleLifecycle`, `ModuleServiceCollectionExtensions.cs:83-86`; Design Choice 11 makes it unconditional) and calls `ConfigureServices` of every module in registration order.
- `ModuleLifecycleHostedService` starts modules in registration order and stops them in reverse; a failed start throws (the host fails to start), a failed stop is collected into an `AggregateException` after every module was asked to stop. Its log lines are hand-written `LogInformation` calls under `#pragma warning disable CA1848`, with no EventIds.
- `ModuleActivitySource.StartLifecycle(moduleName, operation)` and `ModuleMetrics` exist (`src/Encina/Modules/Diagnostics/`), but the hosted service calls neither.

What the issue asks for and the code does not have (verified by grep for `DependsOn`, `ModuleGraph`, `Exports`, `IHostApplicationLifetime` and `DiscoverModules` under `src/`): module discovery, dependency declaration, dependency-ordered startup and shutdown with cycle detection, rich lifecycle phases, module-scoped configuration, module exports, a `ModuleGraph` for visualization and a lifecycle state that other features can read.

This plan delivers, in `Encina` core with no new package (Design Choice 1):

1. `[DependsOn(typeof(X))]`, `IModuleGraph` and an internal topological sort with cycle and missing-dependency detection at registration.
2. Discovery: `DiscoverModulesFromAssemblies(...)` and a pattern overload over the loaded assemblies.
3. `EncinaModule`, an abstract base class with no-op hooks and `Exports`, and the new `IModuleLifecycle` hooks (the old `OnStartAsync`/`OnStopAsync` are replaced, never aliased).
4. Module-scoped configuration through a context object.
5. `IModuleStateProvider` (a thread-safe per-module state that reaches `Running` for every module, plain `IModule` classes included) and the `IHostApplicationLifetime` integration for the "started" phase.
6. `[LoggerMessage]` logging with EventIds, lifecycle spans and a duration histogram.

**Out of scope here, owned by siblings (cited by issue number):** the cross-module access rule that consumes `Exports` is #352; the integration event bus is #358; schema and database isolation is #359; per-module health that reads the state and graph is #364; the module-id dimension on cache keys is #746, on audit rows #1636.

**Provider category**: none (no database, cache, transport or lock is touched). Test coverage of the 10 database providers is therefore not applicable.

**Terminology**: ADR-050 uses "module" for an Encina feature module such as Outbox. This plan says "application module" for the modular-monolith unit `IModule`.

---

## Design Choices

<details>
<summary><strong>1. Where the types live: the existing `Encina.Modules` namespace in core</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `src/Encina/Modules/` in the core package** | The module registry, handler registry, behaviors and diagnostics are already there; `Encina.AspNetCore`, `Encina.Testing` and the database packages already reference them; no move | Core grows by about 10 files |
| **B) A new `Encina.Modules` package, as the issue says** | Pay-for-what-you-use at package level | Requires moving the shipped module types out of core and re-pointing every package; the move is a large breaking change for no new capability |

### Chosen Option: **A — extend the core namespace**

### Rationale

The module system already ships in core (`ROADMAP.md` marks "Module registry and lifecycle hooks" and "Module-scoped behaviors" done), so a new package would split one feature over two assemblies. B wins only if the maintainer wants core free of any module code, which is a separate refactor.

**Class:** Trivial
**Settled by:** existing placement: `src/Encina/Modules/*.cs` (28 files) and `ROADMAP.md` line 329.

</details>

<details>
<summary><strong>2. The lifecycle hook model (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's seven async hooks (`PreConfigure`, `Configure`, `PostConfigure`, `Initialize`, `Started`, `Stopping`, `Stopped`) on `IModuleLifecycle`** | Matches the issue text word for word | The first three run while the service collection is being built, before any host exists; an async signature there forces `GetAwaiter().GetResult()`; `Configure` duplicates the existing `ConfigureServices` |
| **B) Two synchronous registration hooks (`OnPreConfigure`, `OnPostConfigure`) that wrap the existing `ConfigureServices`, plus four async runtime hooks (`OnInitializeAsync`, `OnStartedAsync`, `OnStoppingAsync`, `OnStoppedAsync`), all on `IModuleLifecycle`; `EncinaModule` supplies no-op defaults** | Seven phases in total with `ConfigureServices` as the "configure" phase; no sync-over-async; one interface | An implementer of the raw interface writes all six members (it extends `EncinaModule` instead) |
| **C) The same phases as separate opt-in interfaces (`IModuleInitializer`, `IModuleStartedHandler`, ...)** | A module implements only what it needs, with no base class | Six small interfaces to discover and order; the registry needs one list per interface |

### Chosen Option: **B — two sync registration hooks plus four async runtime hooks** (recommended, pending the maintainer)

### Rationale

B wins when modules are written by teams that mostly extend `EncinaModule` and the registration phases must stay synchronous because DI is built synchronously. A wins if the maintainer wants the issue signatures verbatim and accepts blocking calls at registration. C wins when most modules need exactly one hook and the maintainer prefers interface segregation to a base class. Order of the phases: `OnPreConfigure` of every module in dependency order, then `ConfigureServices` of every module, then `OnPostConfigure` of every module; at host start `OnInitializeAsync` in dependency order, then (after the host reports started) `OnStartedAsync`; at host stop `OnStoppingAsync` then `OnStoppedAsync` in reverse dependency order.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What happens to the existing `OnStartAsync` and `OnStopAsync`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replace them by the new members; no alias, no default interface method** | One lifecycle surface; follows the pre-1.0 rule | Every implementer changes: `ModuleLifecycleHostedServiceTests` in `tests/Encina.UnitTests/Core/Modules/` (the only file that calls `OnStartAsync`/`OnStopAsync`; `ModuleRegistryTests` uses `Substitute.For<IModule, IModuleLifecycle>()`, which compiles unchanged) |
| **B) Keep them and add the new hooks beside them** | No test edits | Two parallel start paths; this is a compatibility layer |

### Chosen Option: **A — replace**

### Rationale

`AGENTS.md` section 3 forbids compatibility aliases and layers; section 1 states there is no existing user. The old `OnStartAsync` becomes `OnInitializeAsync` and the old `OnStopAsync` becomes `OnStoppingAsync`.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (pre-1.0, breaking changes encouraged) and section 3 (NEVER add compatibility aliases or layers).

</details>

<details>
<summary><strong>4. How a module declares its dependencies (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[DependsOn(typeof(X))]` attributes (repeatable) on the module class** | Static, readable, found by reflection and by `Encina.Testing`'s `ModuleArchitectureAnalyzer`; matches the issue | Cannot depend on a runtime condition |
| **B) A virtual `IReadOnlyCollection<Type> Dependencies` on `EncinaModule`** | Can be computed from configuration | Invisible to static tools; plain `IModule` implementers cannot declare |
| **C) Both, merged** | Covers both needs | Two sources of truth to reconcile and test |

### Chosen Option: **A — the attribute** (recommended, pending the maintainer)

### Rationale

A wins for the common case: a dependency is a design fact of the module, not a runtime setting, and the attribute works for plain `IModule` classes too. B wins when an optional module is only a dependency under some configuration; the maintainer can then add it later without changing A. The attribute is `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]`.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Ordering algorithm and what a bad graph does at registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Kahn topological sort, ties broken by registration order; a cycle or a declared-but-unregistered dependency throws `InvalidOperationException` from `AddEncinaModules` naming the modules and the cycle path** | Deterministic; startup error, never a half-started host; same channel as `ModuleConfiguration`'s duplicate-name error | An exception, not an `Either` (registration is startup configuration) |
| **B) Sort what can be sorted, log a warning for the rest and start anyway** | The application still starts | A module starts before its dependency: the exact failure the feature prevents |

### Chosen Option: **A — fail at registration**

### Rationale

Ordering is a precondition of every later phase, so a bad graph must stop the application before the first hook runs. `ModuleConfiguration.AddModule` (`src/Encina/Modules/ModuleConfiguration.cs`) already throws `InvalidOperationException` for a duplicate name, and `ADR-050` defines "enabling a module without what it requires is a startup error". Stop order is the exact reverse of the start order, so dependents stop before their dependencies.

**Class:** Settled
**Settled by:** `ADR-050` (a missing requirement is a startup error) and the existing duplicate-name behavior in `ModuleConfiguration.AddModule`.

</details>

<details>
<summary><strong>6. Discovery by assembly and by name pattern (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DiscoverModulesFromAssemblies(params Assembly[])` only; no pattern overload** | Deterministic, explicit, no scanning | The issue's `DiscoverModulesFromPattern("*.Modules.*")` is not delivered |
| **B) A, plus `DiscoverModulesFromPattern(string)` that matches the simple names of the assemblies already loaded in the `AppDomain` (glob `*` and `?`), never probing the disk** | Delivers the issue API; no disk scan; testable | An assembly not yet loaded at registration is missed, which can surprise a reader |
| **C) A, plus a pattern that also loads matching assemblies from the application directory** | Finds modules in unreferenced assemblies | Loads code the application did not reference; hard to trim; a supply-chain surface |

### Chosen Option: **B — assemblies plus a pattern over loaded assemblies** (recommended, pending the maintainer)

### Rationale

B wins when modules are referenced projects, the normal modular-monolith case. C wins for plugin-style hosts. A wins if the maintainer wants no reflection scan beyond named assemblies. Discovery picks non-abstract, non-generic classes assignable to `IModule` with a public parameterless constructor; a module with constructor arguments is registered with `AddModule(instance)`. Discovered modules enter in a stable order (assembly name, then type full name) and go through the same duplicate-name and graph checks as explicit ones; explicit `AddModule` stays.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Module-scoped configuration (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaModules(services, IConfiguration, configure)` overload; the hooks receive `ModuleConfigurationContext` with `Services`, `ModuleName` and a `Configuration` property already scoped to the section `Modules:{Name}`** | Explicit; no hidden lookup; a module cannot read another module's section by accident | Callers pass the configuration |
| **B) Take `IConfiguration` from the already registered service descriptors of `services`** | Callers pass nothing | Depends on registration order; reflection over descriptors |
| **C) Modules read `IConfiguration` from DI in `OnInitializeAsync` and bind their own section** | No change to registration | Too late for `services.Configure<TOptions>`; no isolation |

### Chosen Option: **A — explicit `IConfiguration` overload and a scoped section** (recommended, pending the maintainer)

### Rationale

A wins when isolation matters more than convenience: the context hands each module only its section. C wins for modules that need configuration only at runtime. With no `IConfiguration` passed, `Configuration` is an empty in-memory configuration (`ConfigurationBuilder`, never `null`), which needs a `PackageReference` to `Microsoft.Extensions.Configuration` in `src/Encina/Encina.csproj` (core has only `Hosting.Abstractions`, which brings `Configuration.Abstractions`); the version is already central in `Directory.Packages.props`, so this is the one new dependency of core and it is counted in pull request 2. The section name `Modules:{Name}` is the issue's example; the key comparison follows .NET configuration (case-insensitive). Options classes that hold secrets keep the `[JsonIgnore]` and `ToString()` rule of `AGENTS.md` section 3 (the context never logs values).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Where exports are declared (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaModule.Exports` (virtual `IReadOnlyCollection<Type>`, default empty), read into `IModuleGraph.GetExports(name)`** | One place per module; matches the issue and NestJS; the contract types may live in another assembly | A plain `IModule` cannot declare exports |
| **B) A `[ModuleExport]` attribute on each exported type** | Works without the base class; found by analyzers | The type must name its module; exports are scattered |
| **C) Both** | Covers both | Two sources of truth |

### Chosen Option: **A — `Exports` on the module** (recommended, pending the maintainer)

### Rationale

A wins when the module class is the single manifest of the module's public surface. B wins when contract assemblies must not know their module class. This plan only declares and exposes exports; the rule that denies a cross-module request for a type that is not exported is #352's boundary behavior. Exports never grant anything by themselves.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Failure policy of the hooks (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `OnInitializeAsync` failure aborts host start (today's behavior); `OnStartedAsync` failure marks the module `Failed`, logs the error code and exception type, and keeps the host running; `OnStoppingAsync` and `OnStoppedAsync` failures are collected and thrown as `AggregateException` after every module ran (today's behavior)** | Keeps existing start and stop semantics; a late non-critical failure does not take the process down; visible through state and health (#364) | A module whose started-work is mandatory keeps running broken until health reports it |
| **B) Any hook failure stops the host (`IHostApplicationLifetime.StopApplication`)** | Fail-fast everywhere | One non-critical started hook can take every module down |
| **C) A per-module policy property (`FailureMode`)** | Each module chooses | More surface; mixed behavior to document |

### Chosen Option: **A — abort on initialize, record and continue on started** (recommended, pending the maintainer)

### Rationale

A wins when modules are independent business areas and health endpoints drive orchestration restarts. B wins for applications where every module is critical, and C when both kinds coexist. The `OnStartedAsync` callback runs from the `ApplicationStarted` token and therefore catches every exception (a throwing callback on a registered token would surface in the thread that cancels it); the failure is never swallowed: state `Failed`, an Error log by error code and `ForLogging()` exception data, and a failed-hook counter (`AGENTS.md` section 3, background code).

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Module state for other features: an in-memory provider, no persistence</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IModuleStateProvider` over an internal thread-safe tracker (`Registered`, `Configured`, `Initializing`, `Running`, `Stopping`, `Stopped`, `Failed`), singleton, updated by the hosted service** | Readable by #364 without a dependency on the hosted service; no store | State is per process, which matches a per-process lifecycle |
| **B) Persist state in a store** | Visible across instances | A lifecycle is per process; no consumer needs it; needs 10 providers |

### Chosen Option: **A — in-memory state provider**

### Rationale

The lifecycle runs once per process; health is also per instance (issue #364: "each instance checks its own health"). Persistence would only add a store, a migration and the 10-provider matrix for no consumer.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 1 (pay-for-what-you-use design principle) and the per-instance scope of the lifecycle itself (`ModuleLifecycleHostedService` runs per host).

</details>

<details>
<summary><strong>11. The state of a plain `IModule` that has no hooks (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Register `ModuleLifecycleHostedService` whenever `AddEncinaModules` is used (drop the `GetLifecycleModules().Count > 0` condition at `ModuleServiceCollectionExtensions.cs:83-86`); after the last `OnInitializeAsync` succeeds it marks every module `Running`, plain modules included (they go `Configured` to `Running` and skip `Initializing`)** | Every module reaches `Running`, so #364's readiness (`Running` is ready) is correct for every application; one rule for all modules | A hosted service now exists for applications with no hooks (a loop over a handful of modules, no work) |
| **B) Keep the conditional registration; the state provider reports `Configured` for modules that have no hooks and consumers (#364) treat "no hooks and `Configured`" as ready** | No hosted service for hook-less applications | Every consumer must know the special case; `Running` is not a reliable "up" signal |
| **C) The tracker marks hook-less modules `Running` when `AddEncinaModules` runs, before any host exists** | No hosted service change | `Running` is reported before the host started and before dependencies initialized; wrong for a state that other features read |

### Chosen Option: **A — always register; every module reaches `Running`** (recommended, pending the maintainer)

### Rationale

A wins when the state is a signal other features read (#364 readiness: `Running` is ready, anything else is not ready): it must be true for every module, and `AddEncinaModules` is already the opt-in of the module system. B wins when avoiding one extra hosted service matters more than a uniform state. C wins only if the state is meant to mean "registered and configured". The condition is removed in pull request 3 (which already edits `ModuleServiceCollectionExtensions.cs`) and the `Running` transition for every module is recorded in pull request 4; pull request 4 includes a unit test with plain `IModule` modules only that asserts `Running` after start. #364's dependency note states that its readiness check relies on this behavior.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Dependency graph and ordered registration (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/DependsOnAttribute.cs`: `public sealed class DependsOnAttribute(Type moduleType) : Attribute`, `Type ModuleType`, usage `Class`, `AllowMultiple = true`, `Inherited = false`; the constructor throws `ArgumentNullException` for null and `ArgumentException` when the type does not implement `IModule` (checked when the graph is built, so the attribute stays cheap).
2. `src/Encina/Modules/IModuleGraph.cs`: `IReadOnlyList<string> StartOrder`, `IReadOnlyCollection<string> GetDependencies(string moduleName)`, `IReadOnlyCollection<string> GetDependents(string moduleName)`, `IReadOnlySet<Type> GetExports(string moduleName)` (empty until Phase 3 fills it), `string ToMermaid()`. Names compare case-insensitively like `ModuleRegistry`.
3. `src/Encina/Modules/ModuleGraph.cs` (internal sealed) and `ModuleDependencyResolver.cs` (internal static): read the `[DependsOn]` attributes of each descriptor, resolve each dependency type to a registered module (a module type registered twice is already impossible: names are unique), run Kahn's algorithm with registration order as the tie-breaker, throw `InvalidOperationException` for a missing dependency (names both modules) and for a cycle (names the path `A -> B -> A`).
4. `ModuleConfiguration.cs`: `ModuleDescriptor` becomes `(IModule Module, Assembly HandlerAssembly, IReadOnlyList<Type> Dependencies)`; the ordered list is produced when `AddEncinaModules` runs.
5. `ModuleServiceCollectionExtensions.cs`: build the graph before `ModuleRegistry`, pass the ordered modules to it (so `Modules` and `GetLifecycleModules()` are in start order), `TryAddSingleton<IModuleGraph>`.
6. `src/Encina/PublicAPI.Unshipped.txt`: the new public symbols (through `mechanical-fixer`).
7. Tests: unit `ModuleGraphTests.cs` (order with a diamond, tie-break, missing dependency, two-node and three-node cycle messages, self-dependency, dependents, Mermaid text), `ModuleConfigurationDependencyTests.cs` (end-to-end through `AddEncinaModules`, registry order equals graph order); guard `ModuleGraphGuardTests.cs`; property `ModuleGraphPropertyTests.cs` (for any generated acyclic graph, every module appears after all its dependencies; for any graph with an injected cycle, registration throws); contract `ModuleGraphContractTests.cs`.
8. Coverage manifest entries for the new files in `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina core already has Encina.Modules (src/Encina/Modules). AddEncinaModules registers modules in registration order. This phase adds [DependsOn], IModuleGraph and a dependency-ordered module list; decisions D4 (attribute) and the settled choice 5 (fail at registration) apply.
TASK: Create DependsOnAttribute, IModuleGraph, ModuleGraph, ModuleDependencyResolver; extend ModuleConfiguration and AddEncinaModules so the registry receives modules in dependency order. Add the unit, guard, property and contract tests listed, the PublicAPI lines and the coverage manifest entries through the specialists that own them.
KEY RULES: Names compare OrdinalIgnoreCase; Kahn with registration-order ties; InvalidOperationException for a cycle or a missing dependency, never a silent skip; no [Obsolete]; XML docs on every public member; every method at cyclomatic complexity of 5 or less (CRAP <= 10).
REFERENCE FILES: src/Encina/Modules/ModuleConfiguration.cs, src/Encina/Modules/ModuleServiceCollectionExtensions.cs, src/Encina/Modules/ModuleRegistry.cs, tests/Encina.UnitTests/Core/Modules/ModuleRegistryTests.cs, tests/Encina.GuardTests/Core/Modules/ModuleHandlerRegistryGuardTests.cs.
```

</details>

### Phase 2: Discovery and module-scoped configuration (pull request 2)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/ModuleDiscovery.cs` (internal static): `IReadOnlyList<IModule> FromAssemblies(IEnumerable<Assembly>)` and `FromAssemblyNamePattern(string pattern, IEnumerable<Assembly> loaded)` (glob to anchored regex with a match timeout, `*` and `?` only); loadable types only (the `ReflectionTypeLoadException` handling already used by `ModuleHandlerRegistry.GetLoadableTypes`).
2. `ModuleConfiguration.cs`: `DiscoverModulesFromAssemblies(params Assembly[] assemblies)` and `DiscoverModulesFromPattern(string pattern)` (uses `AppDomain.CurrentDomain.GetAssemblies()` at call time), both feeding `AddModule` so duplicate names and the graph checks apply.
3. `ModuleServiceCollectionExtensions.cs`: overload `AddEncinaModules(this IServiceCollection, IConfiguration, Action<ModuleConfiguration>)`; the configuration is kept on the `ModuleConfiguration` for the hook contexts of Phase 3.
4. `src/Encina/Modules/ModuleConfigurationExtensions.cs`: `IConfiguration GetModuleSection(this IConfiguration configuration, string moduleName)` returning `configuration.GetSection($"Modules:{moduleName}")`.
5. `src/Encina/Encina.csproj`: `PackageReference Include="Microsoft.Extensions.Configuration"` (D7, empty configuration object).
6. PublicAPI lines, tests (unit `ModuleDiscoveryTests.cs`, `ModuleConfigurationExtensionsTests.cs`; guard `ModuleDiscoveryGuardTests.cs` and `ModuleConfigurationGuardTests.cs`, the latter for `GetModuleSection(null/whitespace moduleName)`, the `AddEncinaModules(services, configuration, configure)` overload with null arguments and `DiscoverModulesFromAssemblies`/`DiscoverModulesFromPattern` null arguments), manifest entries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 produced IModuleGraph and the ordered registration. D6 (discovery over loaded assemblies) and D7 (explicit IConfiguration, section Modules:{Name}) are implemented as the maintainer answered, the recommended options until then.
TASK: Add ModuleDiscovery, the two Discover methods, the IConfiguration overload and GetModuleSection, with their tests.
KEY RULES: Never probe the disk or load assemblies; the pattern regex has a match timeout; a type needs a public parameterless constructor to be discovered; discovered modules take the same duplicate-name and cycle checks as explicit ones; Shouldly, no Thread.Sleep.
REFERENCE FILES: src/Encina/Modules/ModuleHandlerRegistry.cs (GetLoadableTypes), src/Encina/Modules/ModuleConfiguration.cs, tests/Encina.UnitTests/Core/Modules/ModuleRegistryTests.cs.
```

</details>

### Phase 3: `EncinaModule`, the new hooks and the ordered hosted service (pull request 3)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/IModuleLifecycle.cs` (rewritten): `void OnPreConfigure(ModuleConfigurationContext)`, `void OnPostConfigure(ModuleConfigurationContext)`, `Task OnInitializeAsync(ModuleInitializationContext, CancellationToken)`, `Task OnStartedAsync(ModuleStartedContext, CancellationToken)`, `Task OnStoppingAsync(ModuleStoppingContext, CancellationToken)`, `Task OnStoppedAsync(ModuleStoppedContext, CancellationToken)`.
2. `src/Encina/Modules/ModuleLifecycleContexts.cs`: sealed records `ModuleConfigurationContext(string ModuleName, IServiceCollection Services, IConfiguration Configuration)`, `ModuleInitializationContext(string ModuleName, IServiceProvider Services)`, `ModuleStartedContext`, `ModuleStoppingContext`, `ModuleStoppedContext` (same two members); each validates its arguments.
3. `src/Encina/Modules/EncinaModule.cs`: `public abstract class EncinaModule : IModuleLifecycle` with abstract `Name` and `ConfigureServices`, virtual no-op hooks returning `Task.CompletedTask`, and `public virtual IReadOnlyCollection<Type> Exports => []` (D8).
4. `ModuleServiceCollectionExtensions.cs`: after the graph is built run `OnPreConfigure` of every lifecycle module in order, then `ConfigureServices` of every module, then `OnPostConfigure`; exports are copied into the graph; register `ModuleLifecycleHostedService` unconditionally (D11: the `GetLifecycleModules().Count > 0` condition is removed; the service skips modules that implement no hook).
5. `ModuleLifecycleHostedService.cs`: start `OnInitializeAsync` in graph order, stop `OnStoppingAsync` then `OnStoppedAsync` in reverse order; failure rules of D9 for initialize, stopping and stopped; hand-written log lines become `ModuleLifecycleLog` (`src/Encina/Modules/Diagnostics/ModuleLifecycleLog.cs`, `[LoggerMessage]`, EventIds 7600-7617 packed).
6. `src/Encina/Diagnostics/EventIdRanges.cs`: `ModulesLifecycle = (7600, 7619)`; `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`: add the range to the `Encina` entry of the assembly map.
7. Tests: unit (`ModuleLifecycleHostedServiceTests` extended to the new hooks and order and to a host with plain `IModule` modules only, `EncinaModuleTests` new; `ModuleRegistryTests` is not edited: it only calls `Substitute.For<IModule, IModuleLifecycle>()`, which compiles unchanged), guard (`ModuleLifecycleContextsGuardTests` and `ModuleLifecycleHostedServiceGuardTests`, the constructor null checks of the hosted service), contract (`tests/Encina.ContractTests/Core/Modules/ModuleLifecycleContractTests.cs`: `EncinaModule` keeps the documented hook order and defaults).
8. PublicAPI (replace the two removed lines, add the new ones) and manifest entries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 are merged: IModuleGraph orders modules; configuration is scoped per module. D2 (two sync registration hooks plus four async runtime hooks), D3 (replace OnStartAsync/OnStopAsync, no alias), D8 (Exports on EncinaModule) and D9 (failure policy) are implemented as the maintainer answered, the recommended options until then.
TASK: Rewrite IModuleLifecycle, add the context records and EncinaModule, run the registration phases in AddEncinaModules, rewrite ModuleLifecycleHostedService on the graph order with source-generated logs, register the EventId range and update every existing implementer and test.
KEY RULES: Register the EventId range in EventIdRanges.cs first and add it to the allocation test map; ids packed without gaps from 7600; log the error code or exception type through ForLogging(), never an exception message; stop order is the exact reverse of the start order and every module is asked to stop even after a failure; no [Obsolete]; keep every method at CRAP <= 10.
REFERENCE FILES: src/Encina/Modules/ModuleLifecycleHostedService.cs, src/Encina/Modules/Diagnostics/ModuleLog.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Core/Modules/ModuleLifecycleHostedServiceTests.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 4: Module state and the host lifetime (pull request 4)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/ModuleLifecycleState.cs`: enum and `IModuleStateProvider` (`ModuleLifecycleState GetState(string moduleName)`, `IReadOnlyDictionary<string, ModuleLifecycleState> GetAll()`); internal `ModuleStateTracker` (a lock-protected dictionary, case-insensitive) registered as singleton and seeded `Registered` then `Configured` by `AddEncinaModules`; the hosted service (registered for every application using `AddEncinaModules`, D11) records every transition and, after the last `OnInitializeAsync` succeeds, marks every module `Running`, including plain `IModule` modules that have no hooks (they go `Configured` to `Running`). A failed initialize marks that module `Failed`.
2. `ModuleLifecycleHostedService.cs`: register `OnStartedAsync` on `IHostApplicationLifetime.ApplicationStarted` (the constructor gains `IHostApplicationLifetime`); the callback catches every exception (D9) and never throws.
3. Observability: a span per hook through `ModuleActivitySource.StartLifecycle` (tag `module.name`, plus the `lifecycle.phase` tag; no tenant tag, see the telemetry note below), a histogram `encina.modules.lifecycle_duration` and a counter `encina.modules.lifecycle_failures_total` in `ModuleMetrics` (tags `module`, `phase`); the OpenTelemetry package already initializes `ModuleMetrics` (`src/Encina.OpenTelemetry/Modules/ModuleMetricsInitializer.cs`).
4. DI test `tests/Encina.UnitTests/Core/Modules/ModuleLifecycleRegistrationTests.cs`: builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves `IModuleGraph`, `IModuleStateProvider` and the hosted service.
5. Documentation through `docs-writer`: a new `docs/features/module-lifecycle.md` next to `docs/features/module-isolation.md`, the `docs/INVENTORY.md` line for `IModuleLifecycle`, the `ROADMAP.md` line for #360; changelog fragment `changelog.d/360-module-lifecycle.added.md` (through `mechanical-fixer`).
6. Tests: unit `ModuleStateTrackerTests.cs`, hosted-service tests for the started and failed paths and for plain `IModule` modules only (state `Running` after start, D11); guard `ModuleStateTrackerGuardTests.cs` (the justification files ship in pull request 2, see Phase 6).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phase 3 is merged; hooks run in graph order. #364 will read IModuleStateProvider; #352 reads exports from IModuleGraph.
TASK: Add the state tracker and provider, wire IHostApplicationLifetime for the started phase, add spans, histogram and counter, the DI registration test, docs and the changelog fragment.
KEY RULES: The ApplicationStarted callback catches every exception and records state Failed with the error code or exception type only; never read an exception message into logs, tags or state; singletons never take scoped services; TimeProvider for durations (Stopwatch.GetTimestamp is acceptable only through TimeProvider.GetTimestamp).
REFERENCE FILES: src/Encina/Modules/Diagnostics/ModuleActivitySource.cs, src/Encina/Modules/Diagnostics/ModuleMetrics.cs, src/Encina.OpenTelemetry/Modules/ModuleMetricsInitializer.cs, docs/features/module-isolation.md.
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Structured logging and spans as in Phases 3 and 4 (functions 2 and 3).
2. State provider for health (function 4): #364 consumes it and relies on every module, plain `IModule` included, reaching `Running` (D11); this plan adds no health check.
3. Module isolation (function 11): the graph and exports are the inputs of #352's boundary behavior.
4. Telemetry tenancy (ADR-054 D4): lifecycle spans and metrics are process-level and carry no tenant tag; a tenant tag is emitted only when multi-tenancy is enabled and the operation has a tenant, which a startup hook does not. No tenant-qualified identifier exists in this feature, so the reserved `default` tenant segment and injectivity rules do not apply.
5. Error codes: this feature adds no `EncinaError` code (registration problems are `InvalidOperationException` at startup, the channel of `ModuleConfiguration`; hook failures are exceptions of the module's own code). If a later design wraps them in an `Either`, each new code needs an ADR-062 `ErrorCategory` (a missing dependency would be `FailedPrecondition`, a cycle `InvalidRequest`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The lifecycle is process-wide; no tenant or store is involved.
TASK: Verify the integrations above exist in code: spans, histogram, logs, state provider; confirm no tenant tag is emitted by lifecycle telemetry.
KEY RULES: Pay for what you use: all instrumentation is cheap when no listener is attached; the only new dependency of core is the Microsoft.Extensions.Configuration package reference of pull request 2 (D7).
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md.
```

</details>

### Phase 6: Testing, 7 test types

<details>
<summary>Tasks</summary>

Per-flag targets for the files this work adds or changes, entered in `.github/coverage-manifest/Encina.json` in the pull request that adds the tests that meet them (a manifest entry never precedes its tests). The baseline of the edited files (`ModuleConfiguration.cs`, `ModuleServiceCollectionExtensions.cs`, `ModuleLifecycleHostedService.cs`, `IModuleLifecycle.cs`) is measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` per flag and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`; each target is the measured value or higher, never below it.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `Modules/ModuleGraph.cs` | 95 | 70 | 0 | 80 | 0 |
| `Modules/ModuleConfigurationExtensions.cs` | 100 | 100 | 0 | 0 | 0 |
| `Modules/ModuleDependencyResolver.cs` | 100 | 0 | 0 | 90 | 0 |
| `Modules/DependsOnAttribute.cs` | 100 | 100 | 0 | 0 | 0 |
| `Modules/ModuleDiscovery.cs` | 95 | 60 | 0 | 0 | 0 |
| `Modules/EncinaModule.cs` | 100 | 0 | 0 | 0 | 0 |
| `Modules/ModuleLifecycleContexts.cs` | 100 | 100 | 0 | 0 | 0 |
| `Modules/ModuleStateTracker.cs` | 100 | 60 | 0 | 0 | 0 |
| `Modules/ModuleLifecycleHostedService.cs` | 95 | 40 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| `ModuleGraph.cs` unit 95, guard 70, property 80 | The ordering, dependents and Mermaid branches are all reachable from unit tests; `ModuleGraphGuardTests` reach the null and whitespace checks of `GetDependencies`, `GetDependents` and `GetExports` (not the sort); the property test reaches the sort for generated graphs but not the text rendering. |
| `ModuleConfigurationExtensions.cs` unit 100, guard 100 | One method with a null and a whitespace check, both driven by `ModuleConfigurationGuardTests` and `ModuleConfigurationExtensionsTests`. |
| `ModuleDependencyResolver.cs` unit 100, property 90 | A small pure function whose every branch (order, tie, missing, cycle) a unit test drives; generated cyclic and acyclic graphs cover the loop. |
| `DependsOnAttribute.cs` unit 100, guard 100 | A constructor with two guard branches, both reachable by guard tests. |
| `ModuleDiscovery.cs` unit 95, guard 60 | Discovery over a test assembly reaches every filter branch; guard tests reach the null and empty-pattern checks only. |
| `EncinaModule.cs` unit 100 | Default members are one-line returns that a unit test calls; guard 0 because the type has no parameterized public member. |
| `ModuleLifecycleContexts.cs` unit 100, guard 100 | Records whose constructors validate arguments; both flags reach every check. |
| `ModuleStateTracker.cs` unit 100, guard 60 | Transitions and reads are unit-testable; guard tests reach the name checks. |
| `ModuleLifecycleHostedService.cs` unit 95, guard 40 | The hook order, failure and cancellation paths run from unit tests with fakes; `ModuleLifecycleHostedServiceGuardTests` (pull request 3) reach the constructor checks only; the interface and contract flags are 0 for every file above because contract coverage comes from `ModuleGraphContractTests`, which targets the interfaces (no executable lines). |
| All files, integration 0 | In-process code with no external dependency; justification file `tests/Encina.IntegrationTests/Core/Modules/ModuleLifecycle.md` (non-database feature). |

1. **Unit**: `tests/Encina.UnitTests/Core/Modules/` (files named in the phases); a host built with `HostBuilder` and the real `ModuleLifecycleHostedService` for the started callback; `FakeTimeProvider` for durations.
2. **Guard**: `tests/Encina.GuardTests/Core/Modules/` for every new public constructor and method with parameters (null and whitespace names, null contexts, null patterns).
3. **Contract**: `tests/Encina.ContractTests/Core/Modules/ModuleGraphContractTests.cs`: `IModuleGraph` and `IModuleLifecycle` keep their documented behavior with real implementations (`ModuleGraph`, `EncinaModule`).
4. **Property**: `tests/Encina.PropertyTests/Core/ModuleGraphPropertyTests.cs`.
5. **Integration**: justification file (above); the existing module isolation integration tests keep running unchanged.
6. **Load**: justification file `tests/Encina.LoadTests/Modules/ModuleLifecycle.md` (a once-per-process path, not concurrent).
7. **Benchmark**: justification file `tests/Encina.BenchmarkTests/Modules/ModuleLifecycle.md` (a startup path with a handful of modules, not a hot path).

CRAP table: measure every added or changed method locally with the commands above; keep each below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/Encina.json; targets never go down; tests execute real code.
TASK: Write the unit, guard, contract and property tests and the three justification files, measure each flag and the CRAP table, and add the manifest entries with their one-sentence justifications in the same pull request as the tests.
KEY RULES: Shouldly through Encina.Testing.Shouldly; no Thread.Sleep (use FakeTimeProvider); a justification file only for load, benchmark and integration of a non-database feature, never for unit, guard or contract; contract tests instantiate real implementations.
REFERENCE FILES: tests/Encina.UnitTests/Core/Modules/ModuleLifecycleHostedServiceTests.cs, tests/Encina.GuardTests/Core/Modules/ModuleHandlerRegistryGuardTests.cs, tests/Encina.ContractTests/Database/ModuleIsolation/ModuleIsolationContractTests.cs, .github/coverage-manifest/Encina.json.
```

</details>

### Phase 7: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs on every public member (with an `<example>` for `[DependsOn]` and `EncinaModule`).
2. `docs/features/module-lifecycle.md` and the `docs/INVENTORY.md` line (through `docs-writer`; one Diataxis quadrant, no hand-typed figures).
3. `changelog.d/360-module-lifecycle.added.md` and `PublicAPI.Unshipped.txt` through `mechanical-fixer`.
4. No ADR: no new package and no cross-package rule beyond ADR-050; the shared module visibility model (exports and dependencies) is recorded in the #352 plan, and the ADR number is assigned when that ADR pull request opens.
5. `dotnet build --configuration Release` with zero warnings and all tests green; every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Code and tests are merged; documentation is English with no hand-typed coverage figures.
TASK: Finish the XML docs, the lifecycle page, the inventory and roadmap lines and the changelog fragment through the specialists that own them.
KEY RULES: Never edit CHANGELOG.md; no emojis; cite covref markers if coverage is shown.
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md, docs/features/module-isolation.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` section 3 | No compatibility layers; background code never swallows an error | Replace the old hooks; D9 records failures |
| ADR-050 | A missing requirement is a startup error | Missing dependency and cycle throw at registration |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on | Lifecycle telemetry has no tenant |
| ADR-062 | Every new `EncinaError` code has an `ErrorCategory` | No new code in this plan |
| ADR-021 | EventId ranges registered first, packed | `ModulesLifecycle` 7600-7619 |
| NestJS modules, Orleans lifecycle | Inspiration named by the issue | Exports and staged lifecycle |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IModule`, `IModuleLifecycle`, `IModuleRegistry`, `ModuleRegistry` | `src/Encina/Modules/` | Extended and reordered |
| `ModuleConfiguration`, `AddEncinaModules` | `src/Encina/Modules/ModuleConfiguration.cs`, `ModuleServiceCollectionExtensions.cs` | Discovery, graph, phases |
| `ModuleLifecycleHostedService` | `src/Encina/Modules/ModuleLifecycleHostedService.cs` | Rewritten on the graph order |
| `ModuleActivitySource`, `ModuleMetrics`, `ModuleLog` | `src/Encina/Modules/Diagnostics/` | Spans, instruments, log style |
| `ModuleMetricsInitializer` | `src/Encina.OpenTelemetry/Modules/` | Already wires module metrics |
| `ModuleArchitectureAnalyzer` | `src/Encina.Testing/Modules/` | Reads `[DependsOn]` for architecture tests (follow-up of #362, closed) |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | Range registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`ModulesLifecycle`, new) | 7600-7619, ids 7600-7617 used | From the assigned block 7600-7649 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); the siblings take 7620-7629 (#352), 7630-7644 (#358) and 7645-7649 (#364); #359 stays inside the existing `ModuleIsolation` 1900-1999 (ids 1907 onward are free; `ModuleLog.cs` uses 1900-1906) and its plan lists its script-writer ids there |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 17 | 11 new (`DependsOnAttribute`, `IModuleGraph`, `ModuleGraph`, `ModuleDependencyResolver`, `ModuleDiscovery`, `ModuleConfigurationExtensions`, `EncinaModule`, `ModuleLifecycleContexts`, `ModuleLifecycleState`, `ModuleStateTracker`, `ModuleLifecycleLog`) and 6 edited (`IModuleLifecycle`, `ModuleConfiguration`, `ModuleServiceCollectionExtensions`, `ModuleLifecycleHostedService`, `ModuleMetrics`, `EventIdRanges`) |
| Public API and manifest | 2 per pull request | `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json`, edited in each of the four pull requests |
| Project file | 1 | `src/Encina/Encina.csproj` (Microsoft.Extensions.Configuration reference, pull request 2) |
| Tests | 19 | 5 in pull request 1, 4 in pull request 2, 6 in pull request 3, 4 in pull request 4 (new and edited; the edited existing files are `ModuleLifecycleHostedServiceTests`, in pull requests 3 and 4, and `EncinaEventIdAllocationTests`) |
| Justification files | 3 | integration, load, benchmark (pull request 2) |
| Documentation | 4 | page, inventory line, roadmap line, changelog fragment (pull request 4) |
| **Pull request 1** | **13** | 6 production (`DependsOnAttribute`, `IModuleGraph`, `ModuleGraph`, `ModuleDependencyResolver`, `ModuleConfiguration`, `ModuleServiceCollectionExtensions`), PublicAPI, manifest, 5 tests (`ModuleGraphTests`, `ModuleConfigurationDependencyTests`, `ModuleGraphGuardTests`, `ModuleGraphPropertyTests`, `ModuleGraphContractTests`); about 900 lines |
| **Pull request 2** | **14** | 4 production (`ModuleDiscovery`, `ModuleConfigurationExtensions`, `ModuleConfiguration`, `ModuleServiceCollectionExtensions`), `Encina.csproj`, PublicAPI, manifest, 4 tests (`ModuleDiscoveryTests`, `ModuleConfigurationExtensionsTests`, `ModuleDiscoveryGuardTests`, `ModuleConfigurationGuardTests`), 3 justification files; about 650 lines |
| **Pull request 3** | **15** | 7 production (`IModuleLifecycle`, `ModuleLifecycleContexts`, `EncinaModule`, `ModuleServiceCollectionExtensions`, `ModuleLifecycleHostedService`, `ModuleLifecycleLog`, `EventIdRanges`), PublicAPI, manifest, 6 tests (`ModuleLifecycleHostedServiceTests`, `EncinaModuleTests`, `ModuleLifecycleContextsGuardTests`, `ModuleLifecycleHostedServiceGuardTests`, `ModuleLifecycleContractTests`, `EncinaEventIdAllocationTests`); about 1,150 lines |
| **Pull request 4** | **14** | 4 production (`ModuleLifecycleState`, `ModuleStateTracker`, `ModuleLifecycleHostedService`, `ModuleMetrics`), PublicAPI, manifest, 4 tests (`ModuleStateTrackerTests`, `ModuleStateTrackerGuardTests`, `ModuleLifecycleRegistrationTests`, hosted-service tests edit), 4 documentation files; about 900 lines |

Implementers and callers of the changed interface, verified by grep on `origin/main`: `IModuleLifecycle` is implemented only in tests (`ModuleLifecycleHostedServiceTests`; `ModuleRegistryTests` only creates a substitute for it) and referenced by `ModuleRegistry`, `IModuleRegistry`, `ModuleLifecycleHostedService` and `ModuleServiceCollectionExtensions`; no `src/` package other than `Encina` implements it, and `ModuleTestFixture` (`Encina.Testing`) takes `IModule` only. Pull request 3 therefore changes interface and implementers together, so the build is never broken between pull requests. Each pull request has its own tests and manifest entries (a pull request that adds production code without tests fails the crap-gate).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with no backward-compatibility duty. The module system already lives in core (src/Encina/Modules): IModule, IModuleLifecycle (OnStartAsync/OnStopAsync), IModuleRegistry, ModuleConfiguration, AddEncinaModules and ModuleLifecycleHostedService, which starts in registration order. Zero warnings, ROP for operations, exceptions only for startup configuration errors.

IMPLEMENTATION OVERVIEW: PR1 [DependsOn], IModuleGraph, topological order with cycle and missing-dependency errors; PR2 discovery by assembly and by pattern over loaded assemblies, IConfiguration overload and GetModuleSection; PR3 EncinaModule, the new IModuleLifecycle (two sync registration hooks, four async runtime hooks, old hooks replaced), contexts, hosted service on the graph order, source-generated logs in ModulesLifecycle 7600-7619; PR4 IModuleStateProvider, ApplicationStarted callback, spans and metrics, DI test, docs and changelog. Dilemmas D2, D4, D6, D7, D8, D9 and D11 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Names compare OrdinalIgnoreCase; startup errors are InvalidOperationException naming the modules; every callback registered on a token or timer catches its exceptions; logs carry error codes or ForLogging() data, never exception messages; singletons take no scoped service; each pull request stands alone with tests and manifest entries; CRAP <= 10; targets never go down.

REFERENCE FILES: src/Encina/Modules/*.cs, src/Encina/Modules/Diagnostics/*.cs, src/Encina.OpenTelemetry/Modules/ModuleMetricsInitializer.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Core/Modules/*.cs, docs/features/module-isolation.md, .github/coverage-manifest/Encina.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Lifecycle state is read from memory and changes only at start and stop; module-scoped cache keys are #746. |
| 2 | OpenTelemetry | ✅ | A span per hook through `ModuleActivitySource.StartLifecycle`, a duration histogram and a failure counter in `ModuleMetrics` (Phase 4); no tenant tag (ADR-054 D4). |
| 3 | Structured Logging | ✅ | `ModuleLifecycleLog` with `[LoggerMessage]`, EventIds 7600-7617 in the registered `ModulesLifecycle` range (Phase 3). |
| 4 | Health Checks | ⏭️ | This plan exposes `IModuleStateProvider`; the checks that read it are #364 (open, same family). |
| 5 | Validation | ✅ | The graph is validated at registration: duplicate names (existing), missing dependency, cycle, a `[DependsOn]` type that is not an `IModule`. |
| 6 | Resilience | ❌ | Hooks run in process; a hung hook is bounded by the host's own startup and shutdown timeouts, and graceful degradation after a failed started hook is D9. |
| 7 | Distributed Locks | ❌ | Each process starts its own modules; cross-instance coordination of a hook's work (for example a migration) is the module author's concern and is documented in the page. |
| 8 | Transactions | ❌ | Hooks are not data operations. |
| 9 | Idempotency | ❌ | A lifecycle runs once per process; the page tells authors to keep hooks idempotent across restarts. |
| 10 | Multi-Tenancy | ❌ | Application modules are process-wide code units; per-tenant activation is a feature-flag concern, not a lifecycle concern. |
| 11 | Module Isolation | ✅ | This plan is module management: the graph and exports feed #352's boundary rule and #364's propagation. |
| 12 | Audit Trail | ❌ | Start and stop are operational events with no business operation or runtime enable/disable; structured logs, spans and the state provider record them. |

---

## Prerequisites & Dependencies

- **None blocking.** The plan depends only on code on `origin/main` (`e694eb032`).
- **Consumers waiting on this plan:** #352 (exports and graph for the boundary rule), #358 (module dependency validation for event subscribers) and #364 (graph for dependency-aware health, state for lifecycle health). Open them after pull request 1 and 3 respectively.
- **Related, not blocking:** #746 and #1636 add a module dimension to cache keys and audit rows; the module producer gap they record (the ambient `IModuleExecutionContext` is set only by the EF Core package) is addressed in #352.

---

## Next Steps

1. Ask the maintainer D2, D4, D6, D7, D8, D9 and D11 when work on #360 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: graph and ordered registration (13 files).
3. Pull request 2: discovery, scoped configuration, the configuration package reference and the three justification files (14 files).
4. Pull request 3: `EncinaModule`, new hooks, hosted service rewrite and the EventId range (15 files).
5. Pull request 4: state provider, host lifetime, observability, docs and the changelog fragment (14 files).
