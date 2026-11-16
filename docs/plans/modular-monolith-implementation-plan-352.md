# Implementation Plan: Modular Monolith Support (module boundaries, `[Module]`, module context producer, family map)

> **Issue**: [#352](https://github.com/dlrivada/Encina/issues/352)
> **Type**: Feature (umbrella of the modular-monolith family)
> **Complexity**: Medium (6 phases, 4 pull requests of 10, 15, 15 and 9 files)
> **Estimated Scope**: about 700 lines of production code and about 1,500 lines of tests, split over four pull requests
> **Family**: #360 (graph, exports, lifecycle: prerequisite), #358 (events and module API), #359 (data isolation), #364 (health); module id on cache keys #746 and on audit rows #1636

---

## Summary

The issue asks for a modular monolith in a new `Encina.Modules` package with `EncinaModule`, `[Module]`, a registry, an event bus, contracts, a module isolation behavior, database strategies and a startup orchestrator. Most of it exists or belongs to a sibling issue. State of `origin/main` (`e694eb032`), verified by reading and by grep:

| Issue item | Today | Owner of what remains |
|------------|-------|-----------------------|
| `EncinaModule` base class, `Name`, `ConfigureServices`, hooks | `IModule`, `IModuleLifecycle` in core `src/Encina/Modules/` | #360 (base class, hooks, dependencies, exports) |
| `IModuleRegistry` | Exists (`Modules`, `GetModule`, `ContainsModule`, `GetLifecycleModules`) | Nothing (the issue's `IsModuleEnabled` has no meaning: modules are never toggled at runtime) |
| `[Module]` attribute on handlers | Missing; handler to module is by assembly (`ModuleHandlerRegistry`: "if multiple modules share the same assembly, the last one wins") | This plan, Phase 2 |
| `IModuleEventBus`, `IModuleEvent`, module contracts | Missing; `Encina.DomainModeling` has `IIntegrationEvent` that is not an `INotification` | #358 |
| `ModuleIsolationPipelineBehavior` (cross-module access) | Missing | This plan, Phase 3 |
| Database strategies (schema, dedicated, logical) | Exist as `ModuleIsolationStrategy` and `ModuleIsolationOptions`, enforced by the connection wrappers of ADO.NET, Dapper, EF Core and MongoDB | #359 |
| `ModuleStartupOrchestrator` | `ModuleLifecycleHostedService` starts in registration order | #360 (dependency order) |
| `UseEncinaModules()` and `Configure(IApplicationBuilder)` | Missing | Design Choice 9 |
| Per-module health | `IModuleWithHealthChecks` in `Encina.AspNetCore` | #364 |

Two facts shape this plan:

1. **The module producer is incomplete.** The ambient `IModuleExecutionContext.CurrentModule` (an `AsyncLocal`, `src/Encina/Modules/Isolation/ModuleExecutionContext.cs`) is set only by `ModuleExecutionContextBehavior` in `Encina.EntityFrameworkCore`, which `RegisterModuleIsolation` registers only when `MessagingConfiguration.UseModuleIsolation` is on (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`, lines 295-307). ADO.NET, Dapper and MongoDB register `IModuleExecutionContext` but nothing sets it, and consumers read it: two optionally (`TransferBlockingPipelineBehavior`, `DataMinimizationPipelineBehavior`) and the EF `ModuleSchemaValidationInterceptor` as a required dependency. A boundary rule needs the caller module, so the producer moves to core and runs whenever `AddEncinaModules` is used. The same gap is recorded in the plans of #746 and #703.
2. **The caller is the ambient module, not the request.** `IRequestContext.GetModuleName()` reads a metadata key that any code can set with `WithModuleName`; nothing in the framework sets it. A rule keyed on it would be bypassed by the caller.

The plan delivers, in core `Encina` (no new package, same placement as #360):

1. The module context behavior moved to core and registered by `AddEncinaModules` (the producer).
2. `[Module("Name")]` on a handler class, which overrides the assembly mapping.
3. `ModuleBoundaryOptions` (`EnforceModuleBoundaries`, `AllowCrossModuleQueries`, `AllowCrossModuleCommands`), a pure `ModuleBoundaryEvaluator`, the error `encina.modules.boundary_violation` (ADR-062 category `Forbidden`) and the check inside the module context behavior.
4. Logging, a denial counter, a guide page that maps the issue's acceptance criteria to the family, and the follow-up issue files.

**Provider category**: none. The boundary rule is in-process and touches no store; the 10-provider database matrix does not apply (data isolation per provider is #359).

**Terminology**: ADR-050 calls an Encina feature (Outbox, Inbox) a "module". This plan says "application module" for the unit that implements `IModule`.

---

## Design Choices

<details>
<summary><strong>1. Placement: the existing core namespace, no new package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `src/Encina/Modules/` in core** | The registry, handler registry, behaviors, diagnostics and isolation types are already there | Core grows by about 8 files |
| **B) A new `Encina.Modules` package, as the issue says** | Opt-in at package level | The shipped module types would have to move out of core and every package re-point; no new capability |

### Chosen Option: **A — extend the core namespace**

### Rationale

Same decision as #360 (one placement for the family). The module system ships in core today (`ROADMAP.md` line 329); B wins only if the maintainer wants a separate package as a later refactor.

**Class:** Trivial
**Settled by:** existing placement: `src/Encina/Modules/*.cs` and `ROADMAP.md` line 329.

</details>

<details>
<summary><strong>2. One behavior that sets the module context and checks the boundary, or two (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One behavior, `ModuleContextPipelineBehavior`: resolve the handler module, evaluate the boundary against the ambient caller, then open the module scope** | The caller is read before the scope replaces it, by construction; one pipeline step; one place for the module metrics | A behavior with two responsibilities |
| **B) Two behaviors (a boundary gate and the existing context scope), ordered by `PipelineStage`** | Single responsibility each | The gate must run strictly before the scope or it reads the target as the caller; the order is a convention that a custom stage can break |

### Chosen Option: **A — one behavior** (recommended, pending the maintainer)

### Rationale

A wins when correctness must not depend on pipeline ordering: the caller is `CurrentModule` as it is before the scope for this request opens. B wins if the maintainer wants the boundary gate usable without the context scope (for example boundary checks in an application that sets the context itself). The recommended option includes the stage: the behavior declares `PipelineStage.Admission` (200, `src/Encina/Pipeline/Ordering/PipelineStage.cs`, resolved by `PipelineOrderResolver`), so the module is known to the audit, authorization and caching steps that read it (#746, #1636); the stage is part of the public behavior, not left to the implementing pull request.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Who is the caller (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only the ambient `IModuleExecutionContext.CurrentModule`, which the framework sets** | A caller cannot declare itself another module; works through nested `Send` calls | Requests sent from a thread that lost the `AsyncLocal` (a raw `new Thread`) look like host calls |
| **B) The request-context metadata `Encina.ModuleName` (`IRequestContext.GetModuleName()`)** | Explicit per request | Any code can write it with `WithModuleName`, so the rule is bypassable; nothing sets it today |
| **C) Either, metadata first** | Flexible | Inherits B's bypass |

### Chosen Option: **A — the ambient context only** (recommended, pending the maintainer)

### Rationale

A wins when the rule is meant to stop accidental coupling and to be hard to bypass; B wins only for a host that runs modules in separate execution contexts and propagates the module explicitly. `RequestContextModuleExtensions` stays unchanged; it remains a metadata convenience for cache keys and audit (#746, #1636), not an authority.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What makes a cross-module request allowed (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The request type is in the exports of the handling module (`IModuleGraph.GetExports`, #360), and the command or query switch allows its kind** | One declaration per module; matches NestJS exports | Any module can call any exporting module, declared or not |
| **B) A, and the caller module declares `[DependsOn]` on the handling module** | The dependency graph is the real architecture: an undeclared call is a violation (the Spring Modulith `allowedDependencies` idea); startup order is guaranteed for every call | Declaring dependencies is mandatory before enforcement works |
| **C) The issue's `[Module("Orders", Internal = true)]` flag on the handler** | Needs no module class changes | A second visibility mechanism next to exports; per handler instead of per module |

### Chosen Option: **B — exported and declared** (recommended, pending the maintainer)

### Rationale

B wins when the dependency graph must stay truthful (the architecture tests of `Encina.Testing` read the same attributes). A wins when a team adopts exports first and declares dependencies later; C wins only if exports are not wanted. Kind rules: an `ICommand<>` request needs `AllowCrossModuleCommands` (default `false`, the issue's example) and an `IQuery<>` request needs `AllowCrossModuleQueries` (default `true`); any other `IRequest<>` follows the stricter command rule. All three values apply only while `EnforceModuleBoundaries` is on.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Enforcement is opt-in and off by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EnforceModuleBoundaries` defaults to `false`; a registered module application works as before** | Pay-for-what-you-use; no surprise on upgrade | Boundaries are not enforced until switched on |
| **B) Default `true`** | Safe by default | Every module application must declare exports and dependencies before it runs; a hard break |

### Chosen Option: **A — opt-in, default off**

### Rationale

Boundary enforcement is an architecture aid, not a compliance control, so the fail-closed rule for compliance gates does not force it on; it is on when requested and, once on, it fails closed for any cross-module request it cannot prove allowed.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (design principle: pay-for-what-you-use, every feature opt-in).

</details>

<details>
<summary><strong>6. Host calls and unowned handlers (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No ambient module (a call from the host, an endpoint or a background job) and a handler with no module (shared, host-level) are always allowed** | The application layer can call modules; shared handlers keep working | A shared handler can become a back door between modules |
| **B) Deny a module's call into an unowned handler unless it is exported by a module named "Shared"** | Closes the back door | Needs a convention for shared code and a module for it |
| **C) Deny host calls too** | Strictest | Nothing can start a request |

### Chosen Option: **A — both allowed** (recommended, pending the maintainer)

### Rationale

A wins for applications where the host composes modules (the normal case). B wins for teams that want shared kernels to be explicit modules (a `SharedKernel` module is already the issue #360 example). A request whose handler module cannot be resolved while a module is the caller is logged at Debug with the request type name only.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. The `[Module]` attribute (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[Module("Orders")]` on a handler class names its module and overrides the assembly mapping; an unknown module name fails at registration; no `Internal` flag (visibility is exports, D4)** | Fixes the shared-assembly edge case; explicit | A second way to name a module next to the assembly |
| **B) No attribute; the assembly is the module boundary** | Nothing new | Two modules in one assembly stay unsupported (the last wins silently today) |

### Chosen Option: **A — attribute without `Internal`** (recommended, pending the maintainer)

### Rationale

A wins when a codebase keeps several modules in one assembly (small applications, tests). B wins when the maintainer wants one assembly per module as a rule, which the architecture tests can then enforce. `ModuleHandlerRegistry` resolves the attribute first and the assembly second, and a handler that names a module that is not registered throws `InvalidOperationException` when `AddEncinaModules` runs.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. One event bus, owned by #358 (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only #358's `IIntegrationEventBus`; no `IModuleEventBus`, no `PublishToModuleAsync`** | One bus, one event model; publishers do not know subscribers | Targeted delivery to one module is not offered |
| **B) Also `IModuleEventBus` with `PublishToModuleAsync`** | Matches this issue's sketch | Two buses and two event interfaces for one concept; targeted publishing couples the publisher to the subscriber |

### Chosen Option: **A — one bus** (recommended, pending the maintainer)

### Rationale

A wins for decoupled modules and for the microservice extraction path (an event contract that works over a broker). B wins when a module must command exactly one other module asynchronously, which is a command with a delivery target rather than an event and can use a request through the boundary rule.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. `UseEncinaModules()` and `Configure(IApplicationBuilder)` (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not in this plan; a follow-up issue adds an optional `IModuleWithApplication` and `UseEncinaModules` to `Encina.AspNetCore`** | Core stays free of ASP.NET; the family stays at 4 pull requests | The issue's `Configure(app)` example is not delivered now |
| **B) Add both in this plan** | Complete with the issue text | A fifth pull request in `Encina.AspNetCore`; no acceptance criterion needs it |

### Chosen Option: **A — follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins because none of the eight acceptance criteria depends on middleware; B wins if the maintainer wants modules to own their endpoints in this release. Follow-up issue file: `artifacts/issues/module-application-builder-configuration.md` (this plan's worktree).

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: The module context producer in core (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/ModuleContextPipelineBehavior.cs` (public sealed, `IPipelineBehavior<TRequest, TResponse>`): a copy of `src/Encina.EntityFrameworkCore/Modules/ModuleExecutionContextBehavior.cs` with its three log messages; constructor `(IModuleExecutionContext, IModuleHandlerRegistry, ILogger<...>)`; opens `using (_moduleContext.CreateScope(moduleName))` around `nextStep()`; when the handler has no module it forwards unchanged. The EF original is deleted in pull request 2, so between the two pull requests both behaviors run and set the same scope twice, which is harmless (the scope restores the previous module on dispose).
2. `ModuleServiceCollectionExtensions.cs`: `AddEncinaModules` registers `TryAddScoped<IModuleExecutionContext, ModuleExecutionContext>()` and `AddScoped(typeof(IPipelineBehavior<,>), typeof(ModuleContextPipelineBehavior<,>))`.
3. `src/Encina/Modules/Diagnostics/ModuleBoundaryLog.cs`: `[LoggerMessage]` for the three messages, EventIds 7620-7622; `EventIdRanges.ModulesBoundary = (7620, 7629)`; add it to the `Encina` entry of the allocation test map.
4. Tests: new `tests/Encina.UnitTests/Core/Modules/ModuleContextPipelineBehaviorTests.cs` (the cases of the EF test file, which pull request 2 deletes), a DI test `ModuleContextRegistrationTests.cs` (`ValidateOnBuild` and `ValidateScopes`, the behavior resolves and sets the module for a handler of a registered module), a guard test for the constructor, the allocation test edit.
5. `PublicAPI.Unshipped.txt` of `Encina` and the `Encina.json` manifest entries (through `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: IModuleExecutionContext.CurrentModule is set only by ModuleExecutionContextBehavior in Encina.EntityFrameworkCore, and only with UseModuleIsolation. ADO.NET, Dapper and MongoDB register the context but never set it. D1 (core placement) and D2 (one behavior) apply.
TASK: Add the behavior to core as ModuleContextPipelineBehavior, register it and the context in AddEncinaModules, convert its logs to [LoggerMessage] in the new ModulesBoundary range, and add its tests. The EF original is removed in pull request 2.
KEY RULES: Register the EventId range in EventIdRanges.cs first and in the allocation test map; ids packed from 7620; the behavior never swallows an error (a Left from nextStep passes through unchanged); a DI test with ValidateOnBuild and ValidateScopes; keep CRAP <= 10.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Modules/ModuleExecutionContextBehavior.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (RegisterModuleIsolation), src/Encina/Modules/ModuleServiceCollectionExtensions.cs, tests/Encina.UnitTests/EntityFrameworkCore/Modules/ModuleExecutionContextBehaviorTests.cs.
```

</details>

### Phase 2: The `[Module]` attribute and the removal of the EF original (pull request 2)

<details>
<summary>Tasks</summary>

0. Removal: delete `src/Encina.EntityFrameworkCore/Modules/ModuleExecutionContextBehavior.cs` and its test `tests/Encina.UnitTests/EntityFrameworkCore/Modules/ModuleExecutionContextBehaviorTests.cs`; `RegisterModuleIsolation` in `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` drops the behavior registration (line 307) and keeps `IModuleExecutionContext`; the three EF log messages `SettingModuleContext`, `ClearingModuleContext` and `NoModuleFoundForHandler` (EventIds 3037, 3038 and 3039 in `src/Encina.EntityFrameworkCore/Log.cs`) are removed and those ids are retired, not reused; PublicAPI and manifest of `Encina.EntityFrameworkCore`.
1. `src/Encina/Modules/ModuleAttribute.cs`: `[AttributeUsage(AttributeTargets.Class, Inherited = false)] public sealed class ModuleAttribute(string name) : Attribute`, `Name` validated with `ArgumentException.ThrowIfNullOrWhiteSpace` (D7).
2. `ModuleHandlerRegistry.cs`: in `PopulateModuleHandlers`/`ProcessTypeInterfaces`, read the attribute first; resolve the module by name (case-insensitive) among the descriptors; an unknown name throws `InvalidOperationException` naming the handler type.
3. Tests: unit (attribute wins over assembly, two modules in one assembly, unknown module fails, no attribute keeps today's mapping), guard for the attribute, PublicAPI, manifest, and the three justification files (`tests/Encina.IntegrationTests/Core/Modules/ModuleBoundary.md`, `tests/Encina.LoadTests/Modules/ModuleBoundary.md`, `tests/Encina.BenchmarkTests/Modules/ModuleBoundary.md`; the load and benchmark files cover Phase 3 as well).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: ModuleHandlerRegistry maps handlers to modules by assembly and lets the last module win when two share an assembly. D7 adds [Module] without an Internal flag.
TASK: Remove the EF behavior, its test, its registration and its three log messages; add ModuleAttribute and make the registry resolve it before the assembly; add the tests and the three justification files.
KEY RULES: Names compare OrdinalIgnoreCase; an unknown module name is a startup error, never silently ignored; justification files only for integration, load and benchmark of this non-database feature, never for unit, guard or contract.
REFERENCE FILES: src/Encina/Modules/ModuleHandlerRegistry.cs, tests/Encina.GuardTests/Core/Modules/ModuleHandlerRegistryGuardTests.cs, tests/Encina.UnitTests/Core/Modules/NullModuleHandlerRegistryTests.cs.
```

</details>

### Phase 3: The boundary rule (pull request 3)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/ModuleBoundaryOptions.cs` (sealed): `bool EnforceModuleBoundaries` (default `false`), `bool AllowCrossModuleQueries` (default `true`), `bool AllowCrossModuleCommands` (default `false`); `ModuleConfiguration` exposes the same three properties and `AddEncinaModules` registers the options as a singleton (the pattern of `services.AddSingleton(config.ModuleIsolationOptions)` in the EF registration); the XML docs of the class state the D4 defaults (`AllowCrossModuleQueries` true, `AllowCrossModuleCommands` false) and that enforcement is off by default (D5).
2. `src/Encina/Modules/ModuleBoundaryEvaluator.cs` (internal static): `BoundaryDecision Evaluate(string? callerModule, string? targetModule, Type requestType, IModuleGraph graph, ModuleBoundaryOptions options)` returning `Allowed`, or `Denied(reason)` with the reasons `NotExported`, `DependencyNotDeclared`, `CommandsNotAllowed`, `QueriesNotAllowed`; the whole decision table in one pure method, every branch tested (D4, D6).
3. `src/Encina/Modules/ModuleBoundaryErrors.cs` (public static): `ModuleBoundaryViolation(string callerModule, string targetModule, Type requestType, reason)` creating `EncinaErrors.Create("encina.modules.boundary_violation", ErrorCategory.Forbidden, message, details: ...)`; the message names the two modules and the request type name, never request content.
4. `ModuleContextPipelineBehavior.cs`: when the options enforce, call the evaluator before opening the scope and return `Left(ModuleBoundaryViolation)` on a denial (a denial is an `Either` result, never an exception); log 7623 (denied, Warning, module names and request type name only), 7624 (allowed cross-module call, Debug); `ModuleMetrics` gains the counter `encina.modules.boundary_denials_total` with tags `caller_module`, `target_module`, `request_kind` and, when `MultiTenancyMarker` is registered, the tenant tag required by ADR-054 D4.
5. `ModuleBoundaryLog.cs`: 7623, 7624 and 7625 (startup Information: enforcement on, number of modules without exports).
6. Tests: unit `ModuleBoundaryEvaluatorTests` (every row of the decision table), `ModuleContextPipelineBehaviorTests` (denied, allowed, host call, unowned handler, kind switches, a nested call keeps the original caller), `ModuleBoundaryErrorsTests`; guard `ModuleBoundaryGuardTests` for options and errors, and the edit of `ModuleContextPipelineBehaviorGuardTests` (created in pull request 1) because this pull request changes the behavior's constructor to take `IModuleGraph` and `ModuleBoundaryOptions` in addition to the three original parameters; the edit of any direct `new ModuleContextPipelineBehavior(...)` in `ModuleContextPipelineBehaviorTests` is in the listed edit of that file; PublicAPI; manifest. The contract test `ModuleBoundaryContractTests` (the error code and its `ErrorCategory.Forbidden` through `GetCategory`) and the property test `ModuleBoundaryEvaluatorPropertyTests` (a same-module call is always allowed; a denial always has a reason; disabling enforcement always allows) ship in pull request 4 to keep pull request 3 at 15 files, together with the manifest entries of their flags (the contract and property targets of Phase 5 are entered there, never before their tests).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Pull requests 1 and 2 are merged and #360's IModuleGraph (dependencies and exports) is on main. D3 (ambient caller only), D4 (exported and declared), D5 (opt-in), D6 (host calls and unowned handlers allowed) are implemented as the maintainer answered, the recommended options until then.
TASK: Add the options, the pure evaluator, the error factory, the check in ModuleContextPipelineBehavior, the logs 7623-7625, the denial counter and every test listed.
KEY RULES: A denial is Left(EncinaError) with category Forbidden, never an exception; logs and tags carry module names and the request type name, never EncinaError.Message or request content; the tenant tag only when multi-tenancy is on; the evaluator has no side effects and cyclomatic complexity of 8 or less per method (split the table into small methods); never read request.GetModuleName() as the caller.
REFERENCE FILES: src/Encina/Modules/ModuleContextPipelineBehavior.cs, src/Encina/Results/EncinaErrors.cs, src/Encina/Errors/ErrorCategory.cs, src/Encina/Modules/Diagnostics/ModuleMetrics.cs, src/Encina/Abstractions/MultiTenancyMarker.cs, tests/Encina.UnitTests/Core/Modules/ModuleLifecycleHostedServiceTests.cs.
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Observability (functions 2 and 3): the counter and the log messages of Phase 3; the span of the request already exists, so the behavior adds the tag `module.name` for the target module through `ModuleActivitySource`.
2. Module isolation (function 11): the behavior is the request-level half of module isolation; #359 is the data half.
3. Tenancy (function 10): the boundary is independent of the tenant; it never resolves or stamps a tenant, so it calls neither `ITenantScopeResolver.ResolveRead` nor `ResolveWrite` (#1257, #798), and it creates no tenant-qualified identifier (the reserved `default` tenant segment and injectivity rules of those plans do not apply). Telemetry carries the tenant tag only when multi-tenancy is on (ADR-054 D4).
4. Fail-closed: with enforcement on, a cross-module request that is not exported, not declared or of a denied kind is denied; a missing caller or an unowned handler is a documented allow (D6).
5. Error code and category (ADR-062): `encina.modules.boundary_violation` is `ErrorCategory.Forbidden`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The boundary rule is request-level only; data isolation is #359, events are #358.
TASK: Verify the integrations above exist in code and add missing tags or tests.
KEY RULES: No tenant resolution in this feature; the tenant tag is conditional on MultiTenancyMarker; never log EncinaError.Message.
REFERENCE FILES: AGENTS.md sections 3 and 6, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 5: Testing, 7 test types

<details>
<summary>Tasks</summary>

Per-flag targets for the files this work adds or changes, entered in `.github/coverage-manifest/Encina.json` (and `Encina.EntityFrameworkCore.json` for the removed file) in the pull request that adds their tests. Baselines of the edited files (`ModuleHandlerRegistry.cs`, `ModuleServiceCollectionExtensions.cs`, `ModuleMetrics.cs`) are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` per flag and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`; each target is the measured value or higher, never below it.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `Modules/ModuleContextPipelineBehavior.cs` | 95 | 60 | 0 | 0 | 0 |
| `Modules/ModuleBoundaryEvaluator.cs` | 100 | 0 | 0 | 90 | 0 |
| `Modules/ModuleBoundaryOptions.cs` | 100 | 0 | 0 | 0 | 0 |
| `Modules/ModuleBoundaryErrors.cs` | 100 | 80 | 100 | 0 | 0 |
| `Modules/ModuleAttribute.cs` | 100 | 100 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| `ModuleContextPipelineBehavior.cs` unit 95, guard 60 | Every path (no module, scope, denial, allowed cross call, `Left` passthrough) runs from unit tests with fakes; guard tests reach the three constructor checks and the null request checks only. |
| `ModuleBoundaryEvaluator.cs` unit 100, property 90 | A pure decision table whose rows are each a unit test; generated callers, targets and options cover the loop of the property. |
| `ModuleBoundaryOptions.cs` unit 100 | Three auto-properties with defaults, asserted by a unit test; guard 0 because the type has no parameterized member. |
| `ModuleBoundaryErrors.cs` unit 100, guard 80, contract 100 | One factory whose argument checks guard tests reach, and whose code and category the contract test asserts on the real error. |
| `ModuleAttribute.cs` unit 100, guard 100 | One constructor with one check; both flags reach it. |
| All files, integration 0 | In-process rule with no external dependency; the existing database module isolation integration tests keep running; justification file from Phase 2. |

1. **Unit**: `tests/Encina.UnitTests/Core/Modules/` (files named in Phases 1 to 3), using the real behavior and `ModuleHandlerRegistry`.
2. **Guard**: `tests/Encina.GuardTests/Core/Modules/` for every new public constructor and method with parameters.
3. **Contract**: `tests/Encina.ContractTests/Core/Modules/ModuleBoundaryContractTests.cs`.
4. **Property**: `tests/Encina.PropertyTests/Core/ModuleBoundaryEvaluatorPropertyTests.cs`.
5. **Integration**: justification file (Phase 2).
6. **Load**: justification file; the behavior adds one dictionary lookup and one set lookup per request and no shared state beyond the existing `AsyncLocal`.
7. **Benchmark**: justification file; a decision table of constant cost is not a hot path of its own, and the pipeline benchmarks already include the behavior list.

CRAP table: measure every added or changed method locally with the commands above; keep each below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/*.json; targets never go down; tests execute real code.
TASK: Write the tests of the phases, measure each flag and the CRAP table, and add the manifest entries with one-sentence justifications in the same pull request as the tests.
KEY RULES: Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; the evaluator tests cover every row of D4 and D6; contract tests use the real error factory.
REFERENCE FILES: tests/Encina.UnitTests/Core/Modules/ModuleLifecycleHostedServiceTests.cs, tests/Encina.GuardTests/Core/Modules/ModuleHandlerRegistryGuardTests.cs, tests/Encina.ContractTests/Database/ModuleIsolation/ModuleIsolationContractTests.cs.
```

</details>

### Phase 6: Documentation and finalization (pull request 4)

<details>
<summary>Tasks</summary>

1. XML docs with examples on every public member.
2. `docs/features/modular-monolith.md` through `docs-writer`: the family map (table of this plan's Summary), how a boundary is declared (exports, dependencies, attribute), how to enable enforcement, the acceptance criteria of the issue mapped to #360, #358, #359 and #364, and the microservice extraction path (exports are the contracts).
3. `docs/INVENTORY.md` and `ROADMAP.md` lines; changelog fragment `changelog.d/352-modular-monolith-boundaries.added.md` (through `mechanical-fixer`).
4. ADR: one ADR for the shared module visibility model (exports, declared dependencies, ambient caller); ADR number assigned when the ADR pull request opens; the pull request also adds the row to `docs/architecture/adr/index.md`.
5. `dotnet build --configuration Release` with zero warnings, all tests green, every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Code and tests are merged. The guide must not type coverage figures by hand.
TASK: Write the guide page, the inventory and roadmap lines, the ADR and the changelog fragment through the specialists that own them.
KEY RULES: Never edit CHANGELOG.md; one Diataxis quadrant per page; no emojis; link sibling features by issue number until their pages exist.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, docs/features/module-isolation.md, docs/architecture/module-dependency-matrix.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 1, 3 | Opt-in features; gates fail closed; ROP | D5; denial is `Left` |
| `AGENTS.md` section 6 | 12 functions, function 11 is module isolation | This plan is its request-level half |
| ADR-018 | Cross-cutting integration check | Matrix below |
| ADR-021 | EventId ranges registered first | `ModulesBoundary` 7620-7629 |
| ADR-054 D4 | Tenant tag only with multi-tenancy | Denial counter |
| ADR-062 | `ErrorCategory` on every new code | `Forbidden` |
| Spring Modulith, NestJS modules | Sources of the allowed-dependency and exports ideas | Named by #360 and this issue |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IModuleExecutionContext`, `ModuleExecutionContext` | `src/Encina/Modules/Isolation/` | The ambient caller and the scope |
| `ModuleExecutionContextBehavior` | `src/Encina.EntityFrameworkCore/Modules/` | Moved to core |
| `IModuleHandlerRegistry`, `ModuleHandlerRegistry` | `src/Encina/Modules/` | Handler to module, `[Module]` |
| `IModuleGraph` (exports, dependencies) | `src/Encina/Modules/` (#360) | Inputs of the evaluator |
| `ICommand<>`, `IQuery<>` | `src/Encina/Abstractions/CqrsContracts.cs` | Request kind switch |
| `EncinaErrors.Create(code, category, message, ...)` | `src/Encina/Results/EncinaErrors.cs` | The error factory |
| `ModuleMetrics`, `ModuleActivitySource` | `src/Encina/Modules/Diagnostics/` | Counter and tag |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Conditional tenant tag |
| `ModuleArchitectureAnalyzer`, `ModuleArchitectureRules` | `src/Encina.Testing/Modules/` | Architecture-test companion |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`ModulesBoundary`, new) | 7620-7629, ids 7620-7625 used | From the assigned block 7600-7649 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); #360 takes 7600-7619, #358 7630-7644, #364 7645-7649; #359 stays in the existing 1900-1999 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 11 | New: `ModuleContextPipelineBehavior`, `ModuleAttribute`, `ModuleBoundaryOptions`, `ModuleBoundaryEvaluator`, `ModuleBoundaryErrors`, `ModuleBoundaryLog`; edited: `ModuleHandlerRegistry`, `ModuleServiceCollectionExtensions`, `ModuleConfiguration`, `ModuleMetrics`, `EventIdRanges`; plus the EF `ServiceCollectionExtensions` edit and the deleted `ModuleExecutionContextBehavior` (13 touches) |
| Public API and manifests | 2 to 4 per pull request | `Encina` PublicAPI and `Encina.json` each time; `Encina.EntityFrameworkCore` PublicAPI and manifest in pull request 2 |
| Tests | 17 touches | unit 7 (the EF test file is deleted in pull request 2), guard 3, contract 1, property 1, DI 1, allocation map edit 1 |
| Justification files | 3 | integration, load, benchmark (pull request 2) |
| Documentation | 6 | guide, inventory, roadmap, ADR, ADR index line, changelog fragment |
| **Pull request 1** | **10** | 4 production (`ModuleContextPipelineBehavior`, `ModuleServiceCollectionExtensions`, `ModuleBoundaryLog`, `EventIdRanges`), `Encina` PublicAPI, `Encina.json`, 4 tests (`ModuleContextPipelineBehaviorTests`, `ModuleContextRegistrationTests`, `ModuleContextPipelineBehaviorGuardTests`, allocation map edit); about 450 lines |
| **Pull request 2** | **15** | removal side (6): deleted `ModuleExecutionContextBehavior`, EF `ServiceCollectionExtensions`, EF `Log.cs`, EF PublicAPI, EF manifest, deleted `ModuleExecutionContextBehaviorTests`; attribute side (9): `ModuleAttribute`, `ModuleHandlerRegistry`, `Encina` PublicAPI, `Encina.json`, 2 tests (`ModuleHandlerRegistryAttributeTests`, `ModuleAttributeGuardTests`), 3 justification files; about 400 lines |
| **Pull request 3** | **15** | `ModuleBoundaryOptions`, `ModuleBoundaryEvaluator`, `ModuleBoundaryErrors`, `ModuleContextPipelineBehavior`, `ModuleConfiguration`, `ModuleServiceCollectionExtensions`, `ModuleMetrics`, `ModuleBoundaryLog` (8), PublicAPI, manifest, 5 tests (`ModuleBoundaryEvaluatorTests`, `ModuleBoundaryErrorsTests`, `ModuleContextPipelineBehaviorTests` edit, `ModuleBoundaryGuardTests`, `ModuleContextPipelineBehaviorGuardTests` edit); about 800 lines |
| **Pull request 4** | **9** | `ModuleBoundaryEvaluatorPropertyTests`, `ModuleBoundaryContractTests`, manifest, guide, inventory, roadmap, ADR, `docs/architecture/adr/index.md` line, changelog fragment |

Callers and implementers of the changed code, by grep on `origin/main`: `ModuleExecutionContextBehavior` has one registration (EF `ServiceCollectionExtensions.cs:307`) and one test file; `IModuleExecutionContext` has registrations in ADO.SqlServer, Dapper.SqlServer, EF Core and MongoDB (`TryAddScoped`, so the core registration wins or yields in any order) and optional readers in `TransferBlockingPipelineBehavior` and `DataMinimizationPipelineBehavior`. The PostgreSQL and MySQL ADO.NET and Dapper packages inject the context without registering it; with `AddEncinaModules` registering it in core they gain a producer too (their module isolation tests stay green). Interface changes: none; the pull requests change no interface, so none can leave the build broken.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP. The application-module system lives in core (src/Encina/Modules). The ambient module (IModuleExecutionContext.CurrentModule) is set only by an EF Core behavior today; there is no cross-module access rule and no [Module] attribute.

IMPLEMENTATION OVERVIEW: PR1 add the context behavior to core as ModuleContextPipelineBehavior and register it from AddEncinaModules, with the ModulesBoundary EventId range 7620-7629; PR2 remove the EF original and add the [Module] attribute with registry support and the three justification files; PR3 ModuleBoundaryOptions, the pure ModuleBoundaryEvaluator, the error encina.modules.boundary_violation (Forbidden) and the check inside the behavior, with a denial counter; PR4 guide, ADR, inventory, roadmap and changelog. Dilemmas D2, D3, D4, D6, D7, D8 and D9 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: The caller is the ambient module only; a denial is Left with ErrorCategory.Forbidden; opt-in enforcement; host calls and unowned handlers allowed; logs and tags carry module names and request type names, never error messages; tenant tag only when multi-tenancy is on; each pull request stands alone with tests and manifest entries; CRAP <= 10; targets never go down.

REFERENCE FILES: src/Encina/Modules/*.cs, src/Encina.EntityFrameworkCore/Modules/ModuleExecutionContextBehavior.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Results/EncinaErrors.cs, tests/Encina.UnitTests/Core/Modules/*.cs, .github/coverage-manifest/Encina.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | The module dimension of cache keys is #746 (open); this plan only makes the module known to the key generator by producing the context in every package. |
| 2 | OpenTelemetry | ✅ | `encina.modules.boundary_denials_total` with caller, target and kind tags, the `module.name` tag on the request span, and the tenant tag only when multi-tenancy is on (ADR-054 D4). |
| 3 | Structured Logging | ✅ | `ModuleBoundaryLog`, EventIds 7620-7625 in the registered `ModulesBoundary` range. |
| 4 | Health Checks | ⏭️ | Per-module health is #364 (open, same family); the boundary has no dependency to check. |
| 5 | Validation | ✅ | `[Module]` names validated at registration; the evaluator validates every cross-module request against exports, declared dependencies and the command and query switches. |
| 6 | Resilience | ❌ | In-process calls with no external system. |
| 7 | Distributed Locks | ❌ | Single process; no shared state beyond the existing `AsyncLocal`. |
| 8 | Transactions | ❌ | Cross-module commands are denied by default and coordination goes through events (#358); the boundary adds no transactional behavior. |
| 9 | Idempotency | ❌ | A request-level rule; idempotent consumption of integration events is tracked in a follow-up issue file of #358's plan. |
| 10 | Multi-Tenancy | ✅ | The rule is tenant-independent and never resolves a tenant (no call to `ResolveRead` or `ResolveWrite` of #1257 and #798); the denial counter takes the tenant tag only with multi-tenancy on. |
| 11 | Module Isolation | ✅ | This plan is the request-level half of module isolation; the data half is #359. |
| 12 | Audit Trail | ⏭️ | Module attribution on audit rows is #1636 (open); audit of denied cross-module calls and events is the follow-up issue file `artifacts/issues/module-cross-module-audit.md` (this plan's worktree). |

---

## Prerequisites & Dependencies

- **#360, pull requests 1 and 3**: `IModuleGraph` (dependencies, exports) and `EncinaModule.Exports` are the inputs of pull request 3 of this plan. Pull requests 1 and 2 of this plan do not need them and can go first.
- **Related, not blocking**: #746 (module id in cache keys) and #1636 (module id on audit rows) read the module this plan now produces in every package; #703's note on the missing producer is closed by pull request 1.
- **Siblings**: #358 builds the event bus on the same ambient module; #359 and #364 are independent of this plan's code.

---

## Next Steps

1. Ask the maintainer D2, D3, D4, D6, D7, D8 and D9 when work on #352 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: the module context behavior in core (10 files).
3. Pull request 2: removal of the EF original, `[Module]` and the justification files (15 files).
4. Pull request 3: the boundary rule (15 files), after #360's graph is merged.
5. Pull request 4: property and contract tests, manifest, guide, ADR and its index line, inventory, roadmap and changelog fragment (9 files).
