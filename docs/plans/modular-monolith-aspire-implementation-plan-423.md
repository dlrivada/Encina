# Implementation Plan: Modular Monolith Architecture Support with Aspire (module composition: modes, AppHost declaration, per-module health)

> **Issue**: [#423](https://github.com/dlrivada/Encina/issues/423)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 3 pull requests of 15, 9 and 6 files)
> **Estimated Scope**: ~500 lines of production code + ~1,200 lines of tests
> **Family**: modular monolith (#352 boundaries, #358 events and module API, #359 data isolation, #360 lifecycle and graph, #364 health, #365 slices, #366 versions) and Aspire (#416 `Encina.Aspire.Hosting`, #417 service defaults, #419 dashboard commands, #440 umbrella); this plan owns the composition of the two: how a module is local, remote or disabled, and how the AppHost declares and watches modules

---

## Summary

The issue is a broad "first-class modular monolith with Aspire" statement: `IEncinaModule` with lifecycle hooks, discovery, module-scoped services, a dedicated `DbContext` per module, per-module health, in-memory and outbox inter-module communication, public API contracts, an AppHost extension `WithEncinaModules`, dashboard views of modules and their dependencies, module restart and reload, and a migration path that extracts a module into a service (`WithEncinaFromModule`). State of `origin/main` (`0b941beeb`) and of the sibling plans, verified by reading and grep (nothing named `WithEncinaModules`, `WithEncinaFromModule` or `IEncinaModule` exists in `src` or `tests`; `src/Encina.Aspire.Testing` is the only Aspire package):

| Issue item | Today | Owner of what remains |
|------------|-------|-----------------------|
| `IEncinaModule` with lifecycle hooks, discovery, module registration | `IModule`, `IModuleLifecycle`, `IModuleRegistry`, `AddEncinaModules` in core `src/Encina/Modules/` | #360 (base class `EncinaModule`, hooks, discovery, graph, exports); the issue's name `IEncinaModule` is `IModule` |
| Module-scoped services, `ConfigurePipeline` | `ModuleConfiguration`, module pipeline behaviors (`IModulePipelineBehavior`, `AddEncinaModuleBehavior`) | Exists |
| Module isolation, a `DbContext` per module | `ModuleIsolationStrategy` and the connection wrappers | #359 |
| Module health checks | `AddEncinaModuleHealthChecks` (one composite check) | #364 (one check and one fixed endpoint per module, `MapEncinaModuleHealthChecks`) |
| In-memory and outbox inter-module communication, public API contracts | Missing | #358 (`IIntegrationEventBus`, outbox bus, `IModulePublicApi`) |
| AppHost extension `WithEncinaModules`, hosting package | Missing; `Encina.Aspire.Hosting` is planned by #416 | #416 (package, `WithEncina`); **this plan** adds the module extensions to it |
| Dashboard view of modules and health | Missing | **This plan** (Design Choice 6); custom dashboard commands are #419 |
| Module restart and reload | Not possible | Design Choice 5 (not delivered) |
| Migration path to microservices (`WithEncinaFromModule`) | Missing | **This plan**: module modes and the AppHost per-project mode; calling an extracted module synchronously is the spike issue file `module-extraction-remote-module-api` |
| Module-level metrics and tracing | `ModuleMetrics` and `ModuleActivitySource` in `src/Encina/Modules/Diagnostics/` | Exists |

The plan therefore delivers only what no sibling owns: the **module mode** (a module is `Local`, `Remote` or `Disabled` per process, set by configuration), the AppHost declaration of modules and their modes, and per-module health checks in the Aspire resource. Two facts shape it:

1. **A process that runs only some of the modules must still start.** A `[DependsOn]` (#360) on a module that is not in this process is today a startup error ("declared-but-unregistered dependency"). With modes, a dependency on a `Remote` module is allowed (the module runs elsewhere) and a dependency on a `Disabled` module remains an error (Design Choice 4).
2. **The AppHost cannot read the application's types.** An AppHost references the application only through the generated `Projects.*` metadata, so it can pass names, not module classes (Design Choice 2).

**Delivered**: `ModuleMode` and its configuration key; mode resolution in `AddEncinaModules` (the `IConfiguration` overload of #360); `WithEncinaModules` (declaring each module with its mode) in `Encina.Aspire.Hosting`; one HTTP health check on the Aspire resource for each module that is `Local` in that resource (a `Remote` or `Disabled` module is not registered there, so `/health/modules/{name}` would answer 404 and the check would stay unhealthy for ever).

**Not in scope**: hosting package, `AddEncina`/`WithEncina`, service defaults and dashboard commands (#416, #417, #419); module restart and reload (Design Choice 5); synchronous calls to a `Remote` module (spike issue file); cross-process trace propagation (the transport and outbox work of the messaging family).

**Affected packages**: `Encina` (core), `Encina.Aspire.Hosting` (created by #416).

**Provider category**: none (cloud-native hosting; no store, transport or lock is added).

---

## Design Choices

<details>
<summary><strong>1. What this issue is, given the sibling plans (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Plan only the composition delta (modes, AppHost declaration, per-module health in Aspire) and cite the sibling owners for the rest** | No duplicate feature; one pull-request-sized plan | The issue's other acceptance items are fulfilled by other issues, not this one |
| **B) Close #423 as an umbrella duplicate of #352, #358, #360, #364 and #416 and track the delta in a comment** | One issue per feature | The composition delta has no issue of its own |
| **C) Re-plan every item of the issue here** | Matches the issue text | Duplicates five planned features and their files |

### Chosen Option: **A — the composition delta** (recommended, pending the maintainer)

### Rationale

A wins when the maintainer wants the issue to stay as the "modular monolith plus Aspire" feature without owning the parts that already have owners. B wins if the maintainer prefers umbrella issues to be closed once their children exist. C loses in every scenario because it duplicates work.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How the AppHost learns the modules (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Explicit names in the AppHost: `.WithEncinaModules(m => m.Add("orders").Add("inventory", AppHostModuleMode.Remote))`, one call per project resource; an unknown module in a mode key fails the application start (Phase 1 task 3). A module declared in the AppHost but absent from the application is not detected by the application (it only sees `Modules__{name}__Mode` variables); its health check answers 404 and shows unhealthy on the dashboard** | Works with the `Projects.*` metadata; no assembly loading; no reflection in the AppHost | Names are written twice (module class and AppHost) |
| **B) The AppHost references the module assemblies and reads `[DependsOn]` and `Name` by reflection** | One declaration | The AppHost loads application code and references every module project |
| **C) The running application reports its modules (health or telemetry) and the dashboard shows what it receives** | Nothing to declare | The AppHost cannot set modes or health checks before the application runs |

### Chosen Option: **A — explicit names** (recommended, pending the maintainer)

### Rationale

A wins because modes and health checks must exist before the application starts. B wins for solutions where all modules are small projects the AppHost already references. C wins for observation only.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the mode is configured (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A key in the module's own configuration section from #360: `Modules:{Name}:Mode` with values `Local` (default), `Remote`, `Disabled`; the AppHost sets it as the environment variable `Modules__{Name}__Mode`** | Reuses the section #360 already scopes per module (D7 of that plan); one key; default keeps today's behavior | The value is a string, invalid values must be rejected |
| **B) A single list `Encina:Modules:Enabled=orders,inventory`** | One variable | A second source of truth beside the per-module section; `Remote` cannot be expressed |
| **C) Code only: `config.AddModule<T>(mode)` in each application** | Compile-time | The same assembly cannot run as a monolith and as an extracted service without a rebuild |

### Chosen Option: **A — `Modules:{Name}:Mode`** (recommended, pending the maintainer)

### Rationale

A wins when one build must run both as a monolith and as an extracted service by configuration only, which is the migration path of the issue. C wins for applications that never extract. An unrecognized value, or a mode key for a module the application does not know, fails the start with `InvalidOperationException` listing every problem (fail closed: a typo can never silently enable or disable a module). A missing key means `Local`: the feature is opt-in and absent configuration is today's behavior.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. A dependency on a module that is not local (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Remote` satisfies a `[DependsOn]`; `Disabled` does not (startup error naming both modules)** | The extracted service starts; a disabled dependency is still a configuration error | The process trusts that the module really runs elsewhere |
| **B) Any non-local dependency is an error until the application removes the `[DependsOn]`** | Strict | A build cannot serve both layouts; defeats the migration path |
| **C) Any non-local dependency is ignored** | Simplest | Fail-open: a disabled dependency goes unnoticed |

### Chosen Option: **A — remote satisfies, disabled does not** (recommended, pending the maintainer)

### Rationale

A wins for the extraction scenario; B wins for deployments that never run remote modules and want the strictest check; C is listed only to be rejected (`AGENTS.md` section 3, gates fail closed). The check lives in the same place as #360's missing-dependency check, so there is one dependency validation.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Module restart and reload are not provided</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not delivered: Aspire's own restart of the project resource restarts every module of that process** | Matches the platform | The issue's "restart a single module" is not possible |
| **B) Rebuild the service provider for one module** | Would satisfy the sentence | The built service provider is immutable and singletons hold state; not supported by the container |

### Chosen Option: **A — not delivered**

### Rationale

Modules are registered into a service collection that is built once, and #352's plan records that modules are never toggled at runtime (the issue's `IsModuleEnabled` has no meaning). The way to run a module on its own is to extract it with the `Remote` and `Disabled` modes of this plan and restart that resource.

**Class:** Settled
**Settled by:** the immutability of the built `IServiceProvider` of Microsoft.Extensions.DependencyInjection, and the #352 plan (module table, row `IModuleRegistry`: modules are never toggled at runtime).

</details>

<details>
<summary><strong>6. How the dashboard shows modules (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One HTTP health check per `Local` module on the project resource (`WithHttpHealthCheck("/health/modules/{name}")`, the endpoint of #364); no check for a `Remote` or `Disabled` module; the dashboard lists each check under the resource** | Uses Aspire's own health model; no custom resource type; small | Modules are not separate rows in the resource list |
| **B) One child custom resource per module (`WithParentRelationship`, state published with `ResourceNotificationService` from the health endpoint)** | Each module is a row with its own state and dependency relationship | A background poller and a custom resource type; more code to keep in step with #364 |
| **C) Both** | Richest | Most code |

### Chosen Option: **A — per-module health checks** (recommended, pending the maintainer)

### Rationale

A wins for the first release because the dashboard already shows health checks and the endpoints already exist. B wins when the module dependency graph itself must be visible as rows (`WithParentRelationship`, `CustomResourceSnapshot` and `PublishUpdateAsync` are to be verified at work start in `Aspire.Hosting.xml` of the `Aspire.Hosting` version that #416 pins; `Directory.Packages.props` today pins only `Aspire.Hosting.Testing` and `Aspire.Hosting.PostgreSQL`, both 13.6.5). C wins only if both are wanted at once.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. The extensions live in the hosting package of #416</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extension methods in `Encina.Aspire.Hosting` (folder `Modules/`), generic over `IResourceBuilder<T> where T : IResourceWithEnvironment, IResourceWithEndpoints`, so they apply to a project, a container or an executable resource and can be tested without `Projects.*` metadata** | One hosting package; testable with `DistributedApplicationTestingBuilder` | The pull request waits for #416 |
| **B) A separate package `Encina.Aspire.Hosting.Modules`** | Independent of #416 | A package for two methods |

### Chosen Option: **A — the hosting package**

### Rationale

The package is created for exactly this kind of AppHost extension (#416 body: "Encina.Aspire.Hosting (new package)"), and every feature is opt-in (an extension method costs nothing when not called).

**Class:** Trivial
**Settled by:** #416 (issue body: the hosting package and `WithEncina` extension methods) and `AGENTS.md` section 1 (pay-for-what-you-use).

</details>

---

## Implementation Phases

### Phase 1: Module modes in core (pull request 1)

> **Goal**: A module can be local, remote or disabled by configuration; dependencies are validated accordingly.

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/ModuleMode.cs` (public enum): `Local = 0`, `Remote = 1`, `Disabled = 2` (values stable, additive only).
2. `src/Encina/Modules/ModuleModeResolver.cs` (internal static, pure): `ModuleModeResolution Resolve(IReadOnlyList<string> moduleNames, IReadOnlyDictionary<string, string?> modeValues, IModuleGraph graph)` returns the mode per module and every problem: an unparsable value, a key for an unknown module (case-insensitive), a `Local` module whose dependency is `Disabled`; a `Remote` dependency is allowed (D3, D4).
3. `ModuleServiceCollectionExtensions.AddEncinaModules(services, IConfiguration, configure)` (the overload of #360): reads `Modules:{Name}:Mode`, runs the resolver, throws one `InvalidOperationException` listing every problem, then registers only `Local` modules (their `ConfigureServices`, handlers and lifecycle hooks); `Remote` and `Disabled` modules are not registered, so `IModuleRegistry` lists local modules only and the module graph is built over all declared modules.
4. `src/Encina/Modules/Diagnostics/ModuleModeLog.cs` (`[LoggerMessage]`): EventIds 7690 modes applied (Information: counts per mode), 7691 module not started locally because it is `Remote` (Information, name), 7692 module disabled (Information, name), 7693 invalid configuration (Error, count and names only); `EventIdRanges.ModulesEnablement = (7690, 7695)` registered first, and the `Encina` entry of the allocation test map `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
5. Tests: contract `ModuleModeContractTests` (the enum values `Local = 0`, `Remote = 1`, `Disabled = 2` stay stable, and `AddEncinaModules` honors each value through the public registration with a real module), unit `ModuleModeResolverTests` (default local, each value, an invalid value, an unknown module key, remote dependency allowed, disabled dependency rejected, every problem listed in one message), unit `ModuleModeRegistrationTests` (end to end through `AddEncinaModules` with an in-memory `IConfiguration`: remote and disabled modules are not registered and their handlers do not exist), guard, property (for generated graphs and modes the result has no problem exactly when no local module depends on a disabled one), DI test with `ValidateOnBuild` and `ValidateScopes`; `PublicAPI.Unshipped.txt` and the `Encina.json` manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #423; plan docs/plans/modular-monolith-aspire-implementation-plan-423.md. #360 pull requests 1 to 3 (IModuleGraph, [DependsOn], the AddEncinaModules IConfiguration overload) are merged. D3 and D4 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.

TASK:
Add ModuleMode, the pure resolver, the mode handling in AddEncinaModules, the log and the tests.

KEY RULES:
- Fail closed on configuration: an invalid value or an unknown module key is an InvalidOperationException listing every problem; absent configuration means Local.
- One dependency validation: reuse #360's missing-dependency check, do not write a second one.
- Logs carry module names and counts only; EventIds packed from 7690 inside 7690-7695, range registered in EventIdRanges.cs first.

REFERENCE FILES:
- src/Encina/Modules/ModuleServiceCollectionExtensions.cs
- src/Encina/Modules/ModuleConfiguration.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Core/Modules
```

</details>

---

### Phase 2: AppHost declaration and modes (pull request 2)

> **Goal**: The AppHost declares modules and sets each project's modes.

<details>
<summary>Tasks</summary>

1. `src/Encina.Aspire.Hosting/Modules/AppHostModuleMode.cs` (public enum with the same three names as core `ModuleMode`: `Local`, `Remote`, `Disabled`) and `EncinaModulesBuilder.cs`: `Add(string name, AppHostModuleMode mode = AppHostModuleMode.Local)` collecting declared modules (names validated: non-blank, unique case-insensitively, characters of a configuration key segment). The AppHost API deliberately does not use core `ModuleMode`: `Encina.Aspire.Hosting` takes no reference to `Encina`, so an AppHost does not load the application runtime (names, not application types, D2); the application parses the environment value into `ModuleMode`.
2. `src/Encina.Aspire.Hosting/Modules/ModuleResourceBuilderExtensions.cs`: `WithEncinaModules<T>(this IResourceBuilder<T>, Action<EncinaModulesBuilder>)` runs the action first, then, once, sets the environment `Modules__{name}__Mode={mode}` for each declared module and adds one HTTP health check (`WithHttpHealthCheck("/health/modules/{name lowered}")`, D6) only for the modules whose mode is `Local` (a health check annotation cannot be removed afterwards, which is why the checks are emitted only after the declaration is complete). A project that runs one module and delegates another is `WithEncinaModules(m => m.Add("orders").Add("inventory", AppHostModuleMode.Remote))`. There is no separate per-module override method.
3. Project files: `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` gain a `ProjectReference` to `Encina.Aspire.Hosting` (two csproj edits, counted in PR 2). Tests with `DistributedApplicationTestingBuilder` over container or executable resources: environment variables, health check annotations only for `Local` modules (a `Remote` and a `Disabled` module get the variable and no check), duplicate and invalid names, the names of `AppHostModuleMode` equal to the names of `ModuleMode` (the unit project references both); guard tests; the entries of the `Encina.Aspire.Hosting` manifest and `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #423. The package Encina.Aspire.Hosting exists (#416, which also pins Aspire.Hosting in Directory.Packages.props; today only Aspire.Hosting.Testing and Aspire.Hosting.PostgreSQL, both 13.6.5, are pinned). D2 (explicit names), D6 (health checks per module) and D7 (generic extension) apply.

TASK:
Add AppHostModuleMode, EncinaModulesBuilder and WithEncinaModules with their tests and the two test project references.

KEY RULES:
- No reflection over application types in the AppHost and no reference to Encina; names only.
- The default health path is the per-module endpoint of #364 ("/health/modules/{name lowered}"); a check is added only for a Local module and only after the declaration action has run (a Remote or Disabled module is not registered in the process, so its endpoint is a 404).
- Validate at AppHost build time; never write a secret or connection string into an environment variable value of this feature.
- XML docs with examples; PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs
- Directory.Packages.props (Aspire.Hosting.Testing 13.6.5; Aspire.Hosting is pinned by #416)
- tests/Encina.UnitTests (existing Aspire testing usage)
```

</details>

---

### Phase 3: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Module isolation (function 11): modes decide which modules are present; the boundary rule (#352) still decides what a present module may call; a call to a `Remote` module has no local handler and fails as an unknown handler, which the spike issue file addresses.
2. Health checks (function 4): Phase 2 (one HTTP health check per `Local` module on the resource) over #364's endpoints.
3. Structured logging (function 3): Phase 1.
4. Deferred, as issue files in this plan's worktree: `artifacts/issues/module-extraction-remote-module-api.md` (a `[SPIKE]` on calling an extracted module). Per-module cache isolation is #746 and the module on audit rows is #1636 (both open and planned).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #423; the cross-cutting matrix is the contract.

TASK:
Verify each Include row exists in code and tests and that the issue file is written.

KEY RULES:
- Never log EncinaError.Message; mode configuration problems are logged by module name and count only.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
```

</details>

---

### Phase 4: Observability

<details>
<summary>Tasks</summary>

1. Logging: `ModuleModeLog` (Phase 1).
2. Metrics and traces: no new metric or activity: module activity and metrics already exist (`ModuleActivitySource`, `ModuleMetrics` in `src/Encina/Modules/Diagnostics/`); a startup counter of modes would have no operational use. The AppHost itself is observed through Aspire's resource model (the health checks of Phase 2).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #423. Observability is the log of Phase 1 and the Aspire health checks of Phase 2.

TASK:
Confirm the log messages are covered by the Phase 1 tests and that no new meter is needed.

KEY RULES:
- ADR-021: ranges registered first; ADR-054 D4 (docs/architecture/adr/054-observability-conventions-and-trace-propagation.md) applies only to metrics that carry a tenant, and none is added.

REFERENCE FILES:
- src/Encina/Modules/Diagnostics/ModuleMetrics.cs
```

</details>

---

### Phase 5: Testing

<details>
<summary>Tasks</summary>

1. **Unit, Guard, Property, DI** as in Phases 1 and 2; **Contract**: `ModuleModeContractTests` in pull request 1 (a justification file is not allowed for a contract flag, AGENTS.md section 9; `ModuleMode` is public API); **Integration**: a test in `tests/Encina.IntegrationTests/Core/Modules/` builds a host from `Modules__x__Mode` environment variables and starts it (`ModuleModeHostIntegrationTests`; no database, so no provider fixture); an Aspire-level run of two real service projects with `Aspire.Hosting.Testing` is the sample of the guide and is not a CI test (the repository has no sample AppHost project yet). **Load**: justification `tests/Encina.LoadTests/Core/Modules/ModuleMode.md` (startup-time logic). **Benchmark**: justification `tests/Encina.BenchmarkTests/Core/Modules/ModuleMode.md`.
2. **Per-file targets** (each flag with a one-sentence justification in the manifest; baselines of edited files, `ModuleServiceCollectionExtensions.cs` and `EventIdRanges.cs`, are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; a target is the measured value or higher):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `Modules/ModuleModeResolver.cs` | 100 | 0 | 0 | 90 | 0 | A pure function with no parameterized public member; the property test generates graphs and modes |
   | `Modules/ModuleMode.cs` | 100 | 0 | 100 | 0 | 0 | An enum has no executable members for guard; the contract test pins its values and the resolver tests parse it |
   | `Modules/ModuleServiceCollectionExtensions.cs` (mode paths) | 95 | 80 | 0 | 0 | 80 | Registration of local, remote and disabled modules runs end to end in unit and host tests |
   | `Modules/Diagnostics/ModuleModeLog.cs` | 90 | 0 | 0 | 0 | 0 | Reached through registration tests; no parameterized public member |
   | `Encina.Aspire.Hosting/Modules/EncinaModulesBuilder.cs`, `ModuleResourceBuilderExtensions.cs`, `AppHostModuleMode.cs` | 90 | 85 | 0 | 0 | 0 | Run with the Aspire testing builder over real resources; guard reaches argument checks; no external infrastructure |

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #423. Per-flag coverage per AGENTS.md section 9; targets live in the package manifests.

TASK:
Write the tests and manifest entries; the two justification files cover only load and benchmark.

KEY RULES:
- Tests execute real code and never swallow an exception; a nullable parameter that accepts null gets a valid-result test; Shouldly through Encina.Testing.Shouldly.
- Targets never go down; CRAP <= 10 on every changed method.

REFERENCE FILES:
- tests/Encina.UnitTests/Core/Modules
- .github/coverage-manifest/Encina.json
- docs/testing/integration-tests.md
```

</details>

---

### Phase 6: Documentation & Finalization (pull request 3)

<details>
<summary>Tasks</summary>

1. XML docs with examples on all new public APIs; `PublicAPI.Unshipped.txt`.
2. `changelog.d/423-modular-monolith-aspire.added.md` (through `mechanical-fixer`); the guide `docs/features/modular-monolith-aspire.md` (through `docs-writer`: a monolith with modules and the AppHost declaration, the three modes, extracting a module step by step with outbox and a transport for events, the limits (no restart of one module, no synchronous call to a remote module yet), a table mapping each acceptance item of the issue to its owning issue); `docs/INVENTORY.md` and `ROADMAP.md` lines. ADR number assigned when the ADR pull request opens, only if a Dilemma is decided architecturally.
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #423 is implemented and tested.

TASK:
Write the documentation set of the Phase 6 tasks through the specialists that own them.

KEY RULES:
- Pages through docs-writer; fragments, PublicAPI lines and manifests through mechanical-fixer; never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed; the reference application is named only as "the reference application".

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Phase 7: Combined verification

<details>
<summary>Tasks</summary>

1. Run the mode scenarios end to end: one process with all modules `Local`; one process with a module `Remote` (its dependents start, its handlers are absent); one with a module `Disabled` and a local dependent (startup fails naming both).
2. Confirm that the pull requests build in the order of the split and that no file belongs to two of them.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #423; the three pull requests are merged in order.

TASK:
Run the three mode scenarios and report the results.

KEY RULES:
- A failed scenario is reported with its log output (module names only), never patched around.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Core/Modules
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `ModuleMode`, `ModuleModeResolver`, `ModuleServiceCollectionExtensions` edit, `ModuleModeLog`, `EventIdRanges`, allocation test map, PublicAPI, manifest, 5 test files (contract, resolver, registration, guard, property), the DI test and the host integration test | 15 | 1,300 |
| 2 | `AppHostModuleMode`, `EncinaModulesBuilder`, `ModuleResourceBuilderExtensions`, PublicAPI, hosting manifest, 2 test files (unit and guard), 2 test project `ProjectReference` edits (`Encina.UnitTests.csproj`, `Encina.GuardTests.csproj`) | 9 | 600 |
| 3 | Guide, inventory and roadmap lines, changelog fragment, 2 justification files (load, benchmark) | 6 | 400 |

Pull request 1 needs #360 pull requests 1 to 3 merged; pull request 2 needs #416's package (which also pins `Aspire.Hosting`) and pull request 1; pull request 3 is last. No interface changes, so no implementer is touched. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1, 3 and 6 | Opt-in, fail-closed, 12 functions | D3, D4 |
| ADR-021 | EventId ranges registered first | `ModulesEnablement` 7690-7695 |
| SPEC-000 section 6 | 1.0 scope and exclusions (cloud services need a free local emulator; Aspire hosting runs locally) | Hosting scope |
| .NET Aspire hosting model (`DistributedApplicationBuilder`, `WithEnvironment`, `WithHttpHealthCheck`) | The AppHost API | Phase 2 |
| Modular monolith guidance (Kamil Grzybek; Spring Modulith) | Module boundaries and extraction | Concept source named by the issue |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaModules`, `ModuleConfiguration`, `IModuleRegistry` | `src/Encina/Modules/` | Mode handling and registration |
| `IModuleGraph`, `[DependsOn]`, `IConfiguration` overload | #360 | Dependency validation and the module configuration section |
| `AddEncinaModuleHealthChecks`, per-module endpoints | `src/Encina.AspNetCore/Health/`, #364 | The health paths the AppHost watches |
| `ModuleActivitySource`, `ModuleMetrics`, `ModuleLog` | `src/Encina/Modules/Diagnostics/` | Existing module telemetry |
| `IIntegrationEventBus`, `IModulePublicApi` | #358 | Communication between modules |
| `Encina.Aspire.Hosting` | #416 | Package for the AppHost extensions |
| `Encina.Aspire.Testing` | `src/Encina.Aspire.Testing/` | Aspire testing helpers and pinned `Aspire.Hosting.Testing` 13.6.5 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`ModulesEnablement`, new) | 7690-7695, ids 7690-7693 used | From the assigned block 7650-7699 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 7694-7695 free; registered in `EventIdRanges.cs` and the allocation test map before use. `Encina.Aspire.Hosting` logs nothing (an AppHost has no application logger) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 7 | `ModuleMode`, resolver, log, `AppHostModuleMode`, builder, extension methods; edits of `ModuleServiceCollectionExtensions` and `EventIdRanges` counted in the split |
| Public API and manifests | 4 | `Encina`, hosting package, manifests |
| Tests and project files | 11 | Contract, resolver, registration, guard, property, DI, 2 hosting, host integration, allocation map edit, 2 csproj edits (counted in the split) |
| Justification files | 2 | Load, benchmark |
| Documentation | 4 | Guide, inventory, roadmap, changelog fragment |
| **Total** | **30 across 3 pull requests** | The sum of the rows of the split table is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #423: module composition (Local, Remote, Disabled) and the AppHost declaration of modules. Plan: docs/plans/modular-monolith-aspire-implementation-plan-423.md; D1-D4 and D6 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (3 pull requests, each builds alone):
1. ModuleMode, the pure resolver, the mode handling in AddEncinaModules and the log 7690-7693 (Phase 1).
2. WithEncinaModules (each module declared with its mode) with health checks for Local modules only, in Encina.Aspire.Hosting (Phase 2).
3. Documentation (Phase 6).

KEY PATTERNS:
- Mode key Modules:{Name}:Mode (Local default, Remote, Disabled); invalid values and unknown module keys fail the start listing every problem; a Remote dependency satisfies [DependsOn], a Disabled one does not.
- The AppHost passes names and modes as environment variables Modules__{name}__Mode and adds one HTTP health check per Local module (never for Remote or Disabled) at /health/modules/{name lowered}; no reflection over application types.
- No restart of a single module; no synchronous call to a remote module in this plan.

REFERENCE FILES:
- src/Encina/Modules/ModuleServiceCollectionExtensions.cs
- src/Encina/Modules/Diagnostics/ModuleMetrics.cs
- src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs
- Directory.Packages.props
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred as in the issue: per-module cache isolation is #746 (open, planned); nothing cached here |
| 2 | OpenTelemetry | ❌ | Not applicable: in-process module calls share the request trace and `ModuleActivitySource` exists; propagation across processes belongs to the transport and outbox work of the messaging family |
| 3 | Structured Logging | ✅ | Phase 1: `ModuleModeLog`, EventIds 7690-7693 |
| 4 | Health Checks | ✅ | Phase 2: one HTTP health check per `Local` module on the Aspire resource, over #364's endpoints |
| 5 | Validation | ✅ | Mode values, module keys, names and dependencies are validated at startup and AppHost build time; the issue marks structural boundaries as not applicable, but configuration is input |
| 6 | Resilience | ⏭️ | Deferred as in the issue: circuit breakers between modules are part of the spike issue file on remote calls |
| 7 | Distributed Locks | ❌ | Not applicable: configuration and declaration only |
| 8 | Transactions | ⏭️ | Deferred as in the issue: a cross-module transaction scope is the unit of work work and #1236, not module composition |
| 9 | Idempotency | ❌ | Not applicable: no message handling is added |
| 10 | Multi-Tenancy | ⏭️ | Deferred as in the issue: tenant context across modules follows the tenant plans (#737 to #739, #798); modes are per process and carry no tenant data, so the reserved `default` tenant segment and injective ids do not apply |
| 11 | Module Isolation | ✅ | Core of this feature: modes decide which modules exist in a process; #352 decides what they may call |
| 12 | Audit Trail | ⏭️ | Deferred as in the issue: audit of cross-module operations is the #358 follow-up issue file `module-cross-module-audit` and #1636 |

---

## Prerequisites & Dependencies

- **#360 pull requests 1 to 3** (graph, `[DependsOn]`, `IConfiguration` overload) before pull request 1; **#416** (the hosting package) before pull request 2; **#364** for the per-module health endpoint the AppHost watches (the path is a default that the AppHost can override).
- #417 (service defaults) and #419 (dashboard commands) are independent; the dashboard module rows (Design Choice 6 option B) would be a follow-up of #419 if the maintainer chooses it.
- The relation of #423 to #352, #358, #360, #364 and #416 is Design Choice 1.

---

## Next Steps

1. The maintainer answers D1, D2, D3, D4 and D6 when work on #423 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 after #360; pull request 2 after #416; pull request 3 last.
3. Open the issue file `artifacts/issues/module-extraction-remote-module-api.md` when this plan merges.
