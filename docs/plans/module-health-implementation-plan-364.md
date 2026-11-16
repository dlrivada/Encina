# Implementation Plan: Module Health and Readiness Checks (per-module registrations, dependency-aware propagation, endpoints)

> **Issue**: [#364](https://github.com/dlrivada/Encina/issues/364)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 3 pull requests of 13, 14 and 6 files)
> **Estimated Scope**: about 450 lines of production code and about 1,000 lines of tests, split over three pull requests
> **Family**: modular monolith (#352 umbrella, #360 supplies the module graph and state, #358, #359); this plan owns module health, readiness and propagation

---

## Summary

The issue asks for `IModuleHealthCheck`, `IModuleReadinessCheck`, `ModuleHealthStatus`, a `ModuleHealthCheckBase`, dependency-aware propagation, ASP.NET Core integration with per-module endpoints and a JSON response. Part of that exists under other names, and what exists works at the wrong granularity. State of `origin/main` (`e694eb032`), verified by reading and grep:

| Issue item | Today | Owner of what remains |
|------------|-------|-----------------------|
| `IModuleHealthCheck` | `Encina.Messaging.Health.IModuleHealthCheck : IEncinaHealthCheck` (`ModuleName`), no implementer in `src/` | Reused (Design Choice 2) |
| Module owns its checks | `Encina.AspNetCore.Modules.IModuleWithHealthChecks.GetHealthChecks()` | Reused |
| `ModuleHealthStatus`, health status values | `Encina.Messaging.Health.HealthCheckResult` and `HealthStatus` (`Unhealthy`, `Degraded`, `Healthy`); ASP.NET reports duration per entry | Reused |
| `ModuleHealthCheckBase` | `EncinaHealthCheck` (name, `encina` tag, exception turned into `Health check failed: <type name>`, no message) | Reused (Design Choice 7) |
| `AddEncinaModuleHealthChecks()` | `HealthCheckBuilderExtensions.cs:367` registers **one** composite check named `encina-modules`; the generic overload at `:421` names the registration from the **type name** (`typeof(TModule).Name.ToLowerInvariant().Replace("module", "")`, so `OrdersModule` and `ModuleOrders` both become `orders`) | This plan, Phases 2 and 3 |
| Dependency-aware propagation | Missing; modules have no declared dependencies until #360 | This plan, Phase 1 (consumes #360's `IModuleGraph`) |
| Readiness | Missing per module; the module lifecycle has no readable state until #360 | This plan, Phase 1 (consumes #360's `IModuleStateProvider`) |
| Per-module endpoints, JSON response | Missing; probe endpoints and the Encina JSON writer are #454 | This plan, Phase 3 (module endpoints), #454 (writer, probe options) |
| OpenTelemetry health metrics | None per check today; #622 plans `IHealthCheckPublisher` emitting `encina.health.check_status` per check | #622 (every module registration is one of its checks) |

The wrong granularity: because `AddEncinaModuleHealthChecks()` registers one composite, ASP.NET's tag predicates cannot select a module, so no per-module endpoint, per-module readiness or per-module metric is possible. The plan therefore registers one health check and one readiness check per module, named from the module's own `Name`, and lets #622's publisher and #454's probes see them.

Delivered in `Encina.AspNetCore` (no new package, Design Choice 1): a `ModuleHealthEvaluator` that evaluates every module once per probe and applies dependency propagation; per-module health and readiness registrations (added from `IModuleRegistry` at options-build time, so registration order does not matter); `MapEncinaModuleHealthChecks` with one endpoint per module; logging (EventIds 7645-7649); guide updates.

**Provider category**: none. Module checks read other checks and the lifecycle state; the database, cache and transport checks they aggregate already exist per provider (`Encina.Messaging.Health`, the provider packages) and are not changed here.

**Terminology**: ADR-050 calls an Encina feature (Outbox, Inbox) a "module"; this plan says "application module" for `IModule`.

---

## Design Choices

<details>
<summary><strong>1. Placement: `Encina.AspNetCore`, reading #360's core abstractions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `src/Encina.AspNetCore/Health` and `Modules`, where `AddEncinaModuleHealthChecks` and `IModuleWithHealthChecks` already live** | ASP.NET types (`IHealthCheck`, `HealthCheckRegistration`, endpoint routing) are available; no move | Applications without ASP.NET get no module health |
| **B) A new `Encina.Modules` package, as the issue says** | One module package | The module system is in core and the health integration needs ASP.NET: a new package would duplicate both references |

### Chosen Option: **A — extend `Encina.AspNetCore`**

### Rationale

The existing extensions, the `IModuleWithHealthChecks` interface and their tests (`ModuleHealthCheckTests`, `HealthCheckBuilderExtensionsTests`) are in `Encina.AspNetCore` and its unit tests. A host without ASP.NET can still read `IModuleStateProvider` (#360) directly.

**Class:** Trivial
**Settled by:** existing placement: `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs`, `src/Encina.AspNetCore/Modules/IModuleWithHealthChecks.cs`.

</details>

<details>
<summary><strong>2. Reuse the shipped types, do not add the issue's parallel ones</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `IModuleHealthCheck`, `IModuleWithHealthChecks`, `HealthCheckResult`, `HealthStatus`; the module of a check is the module that returns it from `GetHealthChecks()` (an `IModuleHealthCheck.ModuleName` that disagrees with the owning module is rejected at registration)** | No second health model; no name clash with the existing `IModuleHealthCheck` | The issue's `ModuleHealthStatus` record is not created |
| **B) Add the issue's `IModuleHealthCheck`, `IModuleReadinessCheck` and `ModuleHealthStatus`** | Matches the issue text | Two interfaces with one name in two namespaces and two result types: a compatibility layer |

### Chosen Option: **A — reuse**

### Rationale

`AGENTS.md` section 3 forbids parallel aliases and layers, and `Encina.Messaging.Health.IModuleHealthCheck` already has the issue's name.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (no compatibility aliases or layers) and `src/Encina.Messaging/Health/IModuleHealthCheck.cs` (the existing interface).

</details>

<details>
<summary><strong>3. The readiness model (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Readiness of a module is its lifecycle state from #360's `IModuleStateProvider`: `Running` is ready, `Initializing`, `Stopping`, `Stopped` and `Failed` are not (every registered module, plain `IModule` classes included, reaches `Running` only because of #360's Design Choice 11, see Prerequisites); a module that needs more registers its own `IHealthCheck` tagged `ready` and `module:{name}`** | No new interface; reuses the lifecycle; one rule for every module | A module with warm-up work beyond its initialize hook must register a check itself |
| **B) The issue's `IModuleReadinessCheck` interface that modules implement** | Explicit readiness logic per module | A new interface and a second way to contribute checks next to `GetHealthChecks()` |
| **C) Both** | Covers both needs | Two mechanisms to document and test |

### Chosen Option: **A — readiness is the lifecycle state** (recommended, pending the maintainer)

### Rationale

A wins when readiness means "the module finished its startup and is not stopping", which is what an orchestrator asks. B wins for modules with many independent warm-up steps and a team that wants one interface for them. The readiness registration carries the tags `ready` and `startup` of #454's `EncinaHealthTags` (liveness is process-wide, never per module).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How per-module registrations are created (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One composite registration, per-module results in its `data`** | Smallest change | Tag predicates cannot select a module; no per-module endpoint, readiness or metric |
| **B) An `IConfigureOptions<HealthCheckServiceOptions>` registered by `AddEncinaModuleHealthChecks` adds, at options-build time, one health and one readiness registration per module of `IModuleRegistry`** | Independent of the order of `AddEncinaModules` and `AddHealthChecks`; uses ASP.NET's own model; every module is a check for #622's publisher and #454's probes | Registrations exist only after the options are built (first use) |
| **C) Read `IModuleRegistry` from the `IServiceCollection` descriptors when the extension runs** | Registrations exist immediately | Depends on `AddEncinaModules` having run first and on descriptor internals |

### Chosen Option: **B — an options setup that adds the registrations** (recommended, pending the maintainer)

### Rationale

B wins because registration order is a recurring defect class in Encina (`AGENTS.md` section 3, registration completeness), and it keeps standard ASP.NET behavior. A wins only if per-module selection is not wanted. Names: `encina-module-{name}` for health and `encina-module-{name}-ready` for readiness, with the name lowered by `ToLowerInvariant`; tags: `encina`, `modules`, `module:{name}`. The mapping from module to name is injective: `ModuleRegistry` rejects two names equal ignoring case, and the setup rejects two modules whose lowered names are equal (an `InvalidOperationException` when the options are first built, and at host startup through a `ValidateOnStart` check of `ModuleHealthOptions`), so no two modules share a name or a tag. The tenant concept does not enter these names.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Dependency propagation rule (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A module is at most `Degraded` because of a dependency: if any declared dependency (by #360's `IModuleGraph`, transitively through the dependency's own propagated status) is `Degraded` or `Unhealthy`, the module reports `Degraded` with the description `Dependent module '{name}' is degraded`** | Matches the issue's example; an upstream outage does not mark every dependent unhealthy; names only, no exception text | A dependent that cannot work at all still reports `Degraded` |
| **B) Mirror the dependency's status exactly** | Strict; simple to explain | One failing module turns all dependents `Unhealthy`, which orchestrators may answer by restarting healthy instances |
| **C) No propagation** | Independent modules | The issue's second acceptance criterion is not met |

### Chosen Option: **A — dependents are at most degraded** (recommended, pending the maintainer)

### Rationale

A wins when a restart would not fix an upstream problem and operators need the signal without a restart loop. B wins for dependents that are useless without their dependency and for readiness semantics, where a failing dependency should keep traffic away (readiness already mirrors the lifecycle state per D3, so traffic routing is not weakened). C wins if modules are deliberately independent. The graph is acyclic by #360's registration check, so propagation terminates; each module is evaluated once per probe in dependency order.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Avoid evaluating a dependency once per dependent (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A singleton `ModuleHealthEvaluator` with single-flight: concurrent registrations of one probe join one in-flight evaluation of all modules; no result is kept after it completes** | Each check runs once per probe; always fresh | A registration that starts just after an evaluation ended starts a second one (harmless extra work) |
| **B) Each registration evaluates its module and, recursively, its dependencies** | No shared state | A dependency's checks run once per dependent and per ancestor; a database ping repeated many times per probe |
| **C) A time-to-live cache of the last report** | Cheapest | A health result must reflect real time; a stale healthy answer is wrong exactly when an outage starts |

### Chosen Option: **A — single-flight evaluator, no retained result** (recommended, pending the maintainer)

### Rationale

A wins when probes are frequent and checks hit a database, because it deduplicates only work that is already running. B wins for the smallest implementation in small applications with a handful of cheap checks. C wins if the maintainer accepts a documented staleness window (the issue's matrix says a cached health result "would be misleading"). The evaluator never throws: a check that throws becomes `Unhealthy` with the exception type only (the `EncinaHealthCheck` rule), and cancellation of the probe cancels the shared evaluation only when every joined caller cancelled.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. No new health check base class</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Module checks derive from `EncinaHealthCheck` (name, `encina` tag, exceptions reduced to the exception type)** | One base; already tested | The issue's `CheckDatabaseAsync` and `CheckExternalDependenciesAsync` template is not provided |
| **B) A `ModuleHealthCheckBase` with the issue's template methods** | Matches the issue | A second base that must repeat the exception rule; the database check differs per provider |

### Chosen Option: **A — use `EncinaHealthCheck`**

### Rationale

`EncinaHealthCheck` already turns an exception into `Health check failed: <type name>` and never forwards a message (its comment cites #1259); a second base would have to duplicate that safety rule.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (`EncinaError.Message` never reaches health-check results; exception type only) and `src/Encina.Messaging/Health/EncinaHealthCheck.cs`.

</details>

<details>
<summary><strong>8. Per-module endpoints (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MapEncinaModuleHealthChecks(pattern)` maps `{pattern}` for all modules and one fixed endpoint `{pattern}/{module name lowered}` per module of the registry (read from `endpoints.ServiceProvider`), each with ASP.NET's `HealthCheckOptions` and a tag predicate; it returns the conventions builder so callers chain `RequireAuthorization()`** | Standard ASP.NET behavior and status codes; an unknown module is a plain 404; no user input reaches a predicate | Endpoints are fixed at mapping time |
| **B) One endpoint with a `{moduleName}` route parameter and a handler that runs `HealthCheckService` with a predicate built from the value** | One route | Reimplements the response writing and status mapping; a predicate built from user input |

### Chosen Option: **A — one fixed endpoint per module** (recommended, pending the maintainer)

### Rationale

A wins because module names are known at startup and every ASP.NET convention (authorization, rate limiting, CORS) applies unchanged; B wins only for modules added at runtime, which Encina does not support. The response format is the caller's `HealthCheckOptions.ResponseWriter`; the JSON writer, `IncludeDetails` (default false) and the authorization policy name belong to #454's `EncinaProbeOptions`, so module details are not exposed by default. Health responses carry module names, statuses and fixed descriptions only; data entries never contain exception messages.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: The evaluator, the checks and the log (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.AspNetCore/Modules/ModuleHealthEvaluator.cs` (internal sealed, singleton): constructor `(IModuleRegistry, IModuleGraph, IModuleStateProvider, IServiceProvider, ILogger<...>)` (all registered by `AddEncinaModules`; the evaluator itself is registered by a factory delegate in pull request 2, so it is built only when a registry exists, see Phase 2 task 1); `Task<IReadOnlyDictionary<string, ModuleHealthEntry>> EvaluateAsync(CancellationToken)` implements D6 (single-flight) and D5 (propagation in `IModuleGraph.StartOrder`); a module's own result is the worst `HealthStatus` of its `IModuleWithHealthChecks.GetHealthChecks()` results (a module without that interface has no own checks and is `Healthy` while its state is `Running`); an `IModuleHealthCheck` whose `ModuleName` differs from the owning module is reported `Unhealthy` once with a fixed description. `ModuleHealthEntry` is an internal record (`Status`, `Description`, `Data`).
2. Scoped dependencies: `IModuleWithHealthChecks.GetHealthChecks()` may build scoped checks, so the evaluator resolves nothing scoped itself; a check needing a scope uses `IServiceScopeFactory` internally, as the existing checks do.
3. `ModuleHealthCheck` (`IHealthCheck`, internal) reads its module's entry from the evaluator and maps it to ASP.NET's `HealthCheckResult` through the same status mapping as `CompositeEncinaHealthCheck`; `ModuleReadinessCheck` maps the module's lifecycle state (D3): `Running` healthy, anything else unhealthy with a fixed description naming the state.
4. `src/Encina.AspNetCore/Modules/Diagnostics/ModuleHealthLog.cs` (`[LoggerMessage]`, EventIds 7645 evaluated, 7646 propagated degradation, 7647 check failed by exception type, 7648 module not ready, 7649 no module registry found, used by pull request 2's setup) and `EventIdRanges.ModulesHealth = (7645, 7649)` plus the `Encina.AspNetCore` entry of the allocation test map (`tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`) and the core `src/Encina/PublicAPI.Unshipped.txt` line for the new field (`static readonly Encina.Diagnostics.EventIdRanges.ModulesHealth -> (int Min, int Max)`, through `mechanical-fixer`; the sibling `EventIdRanges.AspNetCore` line is there at line 2038).
5. Tests: unit `ModuleHealthEvaluatorTests` (worst-of own checks, propagation through a chain and a diamond, a throwing check, cancellation, single-flight with a gate that holds the first evaluation), `ModuleHealthChecksTests` (status mapping, readiness per state), property `ModuleHealthEvaluatorPropertyTests` (a module never reports healthier than the worst of its own checks, never `Unhealthy` only because of a dependency, and every module appears exactly once), allocation map edit; manifest entries for the new files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: #360 provides IModuleGraph (StartOrder, GetDependencies) and IModuleStateProvider; #454 provides EncinaHealthTags. Today AddEncinaModuleHealthChecks registers one composite check. D3 (readiness is lifecycle state), D5 (dependents at most Degraded) and D6 (single-flight evaluator) are implemented as the maintainer answered, the recommended options until then.
TASK: Add the evaluator, the two IHealthCheck classes and the log with its EventId range, with the unit and property tests.
KEY RULES: Descriptions and data never contain exception messages (exception type only, the EncinaHealthCheck rule); the evaluator never throws and joins in-flight work instead of caching results; a singleton takes no scoped service (use IServiceScopeFactory inside checks); log by error code or module name only; cyclomatic complexity of 8 or less per method (CRAP <= 10).
REFERENCE FILES: src/Encina.AspNetCore/Health/CompositeEncinaHealthCheck.cs, src/Encina.AspNetCore/Modules/IModuleWithHealthChecks.cs, src/Encina.Messaging/Health/EncinaHealthCheck.cs, src/Encina/Modules/IModuleRegistry.cs, tests/Encina.UnitTests/AspNetCore/ModuleHealthCheckTests.cs.
```

</details>

### Phase 2: Per-module registration (pull request 2)

<details>
<summary>Tasks</summary>

1. `src/Encina.AspNetCore/Modules/ModuleHealthRegistrationSetup.cs` (internal sealed, `IConfigureOptions<HealthCheckServiceOptions>`): constructor `(IServiceProvider, IOptions<ModuleHealthOptions>, ILogger<ModuleHealthRegistrationSetup>)`. Neither `IModuleRegistry` nor `ModuleHealthEvaluator` is a constructor dependency (the evaluator needs the registry, the graph and the state provider, so a constructor dependency on it would fail resolution in an application without modules): the setup reads the registry with `serviceProvider.GetService<IModuleRegistry>()`, which returns null in an application without modules; then it adds nothing and logs once at Warning with EventId 7649 (`ModuleHealthLog`, added in pull request 1, inside the block 7645-7649). Only when the registry exists do the registrations resolve the evaluator, lazily inside each `HealthCheckRegistration` factory with `GetRequiredService<ModuleHealthEvaluator>()`; `AddEncinaModuleHealthChecks` registers the evaluator by a factory delegate (`TryAddSingleton(sp => new ModuleHealthEvaluator(...))`), which `ValidateOnBuild` does not expand, so a provider built with no modules is valid and the "adds nothing and logs 7649" path is reachable. For each module (after the `ModuleNames` filter) it adds `HealthCheckRegistration` `encina-module-{name}` (tags `encina`, `modules`, `module:{name}`, plus `ModuleHealthOptions.Tags`) and `encina-module-{name}-ready` (tags `modules`, `module:{name}`, `ready`, `startup` from #454's `EncinaHealthTags`), each with `ModuleHealthOptions.FailureStatus` and `ModuleHealthOptions.Timeout`; rejects two modules whose lowered names are equal with `InvalidOperationException`. That check runs when `HealthCheckServiceOptions` is first built (the first probe or the first resolution of `HealthCheckService`), so a duplicate surfaces as a failed probe, not at startup; `AddEncinaModuleHealthChecks` therefore also registers a `ValidateOnStart` check on `ModuleHealthOptions` that builds the same name set from the registry, so a duplicate fails host startup.
2. `HealthCheckBuilderExtensions.cs`: `AddEncinaModuleHealthChecks(tags, failureStatus, timeout, moduleNames)` now registers the evaluator, the setup and `ModuleHealthOptions` (replacing the single composite named `encina-modules`); the caller's `tags`, `failureStatus` and `timeout` reach the setup only through `ModuleHealthOptions` (see task 3), because the setup is created by the container and has no other carrier. The generic `AddEncinaModuleHealthChecks<TModule>` overload is removed (its type-name-derived name was not injective) and replaced by the optional `string[] moduleNames` filter on the main method. New parameters are optional, so the two existing calls in `tests/Encina.GuardTests/AspNetCore/AspNetCoreGuardTests.cs` (lines 72-75 and 163-167, no arguments) still compile unchanged.
3. `ModuleHealthOptions` (`Timeout`, default 10 seconds, validated greater than zero; `FailureStatus`, `HealthStatus?` default null meaning ASP.NET's default; `Tags`, `IReadOnlyCollection<string>` default empty; `ModuleNames`, `IReadOnlyCollection<string>?` default null meaning every module) registered through `AddOptions<ModuleHealthOptions>().Configure(...).Validate(...)`; the extension copies its arguments into it.
4. Existing tests updated: `tests/Encina.UnitTests/AspNetCore/ModuleHealthCheckTests.cs` and `tests/Encina.UnitTests/AspNetCore/Health/HealthCheckBuilderExtensionsTests.cs` (the `encina-modules` registration name and the removed overload); `tests/Encina.GuardTests/AspNetCore/AspNetCoreGuardTests.cs` is edited to add a guard test for the new `moduleNames` filter (a null or empty element is rejected) (#3798), its two existing calls stay as they are.
5. Tests: unit `ModuleHealthRegistrationSetupTests` (names, tags, caller tags and failure status carried, two registrations per module, duplicate lowered names throw, filter, a missing registry adds nothing and logs 7649 once), guard `ModuleHealthOptionsGuardTests`, a DI test `ModuleHealthRegistrationTests` building the provider with `ValidateOnBuild` and `ValidateScopes` in both orders of `AddHealthChecks` and `AddEncinaModules`, and a case with no `AddEncinaModules` at all (the provider builds, resolving `HealthCheckServiceOptions` adds no registration and logs 7649); the endpoint `TestServer` test includes a host whose modules are plain `IModule` classes (no hooks) and asserts the readiness endpoint returns 200 after host start, which depends on #360's rule that every module reaches `Running` (its D11); PublicAPI (`Encina.AspNetCore`, through `mechanical-fixer`), manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Pull request 1 added the evaluator and the checks. Registrations must exist per module and be independent of registration order (AGENTS.md section 3, registration completeness). D4 (options setup adds the registrations) applies.
TASK: Add the options setup, ModuleHealthOptions, update AddEncinaModuleHealthChecks and the two existing test files, and prove both registration orders with a DI test.
KEY RULES: Names and tags derive from IModule.Name lowered, injective by the duplicate check; no user input reaches a tag; no [Obsolete] overload left behind; ValidateOnBuild and ValidateScopes in the DI test; options validated at startup.
REFERENCE FILES: src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs, src/Encina.AspNetCore/Modules/IModuleWithHealthChecks.cs, tests/Encina.UnitTests/AspNetCore/Health/HealthCheckBuilderExtensionsTests.cs, tests/Encina.UnitTests/AspNetCore/ModuleHealthCheckTests.cs.
```

</details>

### Phase 3: Per-module endpoints (pull request 2)

<details>
<summary>Tasks</summary>

1. `src/Encina.AspNetCore/Modules/ModuleHealthEndpointRouteBuilderExtensions.cs`: `MapEncinaModuleHealthChecks(this IEndpointRouteBuilder endpoints, string pattern = "/health/modules", Action<HealthCheckOptions>? configure = null)` returns an `IEndpointConventionBuilder` (a composite over the mapped endpoints): `{pattern}` with a predicate on the tag `modules` and not `ready`, and one `{pattern}/{moduleName lowered}` per module with the predicate on `module:{name}` and not `ready`, plus the readiness variants under `{pattern}/{moduleName lowered}/ready` (D8). `pattern` is validated (leading slash, no route parameters).
2. The response writer and the details flag are the caller's `HealthCheckOptions` (and #454's writer and `IncludeDetails`, default false); this plan adds none.
3. Tests: unit (the mapped route set for a registry of three modules, an unknown module path is 404 through `TestServer`, a degraded dependent returns 200 and an unhealthy module 503, `RequireAuthorization()` chains), guard, PublicAPI, manifest; the end-to-end `TestServer` test counts as the integration test for this non-database feature (no external service).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Registrations exist per module with tags module:{name}. D8 (one fixed endpoint per module, read from the registry at mapping time) applies; the JSON writer and IncludeDetails are #454's.
TASK: Add MapEncinaModuleHealthChecks and its tests with a TestServer host.
KEY RULES: No user input in predicates: endpoints are built from registry names at mapping time; return the conventions builder so authorization can be chained; module details only through the caller's response writer, never by default; a missing IModuleRegistry maps nothing and throws InvalidOperationException (fail closed, not an empty 200).
REFERENCE FILES: src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs, tests/Encina.UnitTests/AspNetCore/Health/HealthCheckBuilderExtensionsTests.cs, docs/guides/health-checks.md.
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. OpenTelemetry (function 2): no new instrument; every module registration is a check, so #622's `IHealthCheckPublisher` (`encina.health.check_status`, tag `check.name`) emits one series per module health and readiness check.
2. Logging (function 3): `ModuleHealthLog`, EventIds 7645-7649 in `ModulesHealth` (7645-7649, all five used).
3. Resilience (function 6): `HealthCheckRegistration.Timeout` per module registration from `ModuleHealthOptions.Timeout`; a check that exceeds it is `Unhealthy` by ASP.NET's own handling.
4. Module isolation (function 11): per-module health is module-scoped; the module name is the only identifier used.
5. Multi-tenancy (function 10): health is per process and per module; there is no tenant-scoped module, so no tenant tag, no tenant-qualified identifier and no call to `ResolveRead` or `ResolveWrite` of #1257 and #798 (tenant-scoped resource health belongs to the tenancy packages).
6. Errors (ADR-062): this feature adds no `EncinaError` code; results are `HealthCheckResult`. Descriptions and data never carry `EncinaError.Message` or an exception message (`AGENTS.md` section 3).
7. Fail-closed: an unknown module is 404; a module whose state is not `Running` is not ready; a missing registry maps no endpoints and throws.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The feature adds no instrument and no store; the integrations are decisions recorded above.
TASK: Verify each integration exists in code and tests, and that no description or data entry can carry a message.
KEY RULES: Exception type only; never log or return EncinaError.Message; module names only as identifiers.
REFERENCE FILES: AGENTS.md sections 3 and 6, docs/architecture/adr/062-error-category-contract.md, docs/plans (the #454 and #622 plans, by issue number).
```

</details>

### Phase 5: Testing, 7 test types

<details>
<summary>Tasks</summary>

Per-flag targets for the files this work adds or changes, entered in `.github/coverage-manifest/Encina.AspNetCore.json` in the pull request that adds their tests. Baselines of the edited files (`HealthCheckBuilderExtensions.cs`, `IModuleWithHealthChecks.cs`) are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` per flag and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`; each target is the measured value or higher, never below it.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `Modules/ModuleHealthEvaluator.cs` | 95 | 0 | 0 | 80 | 0 |
| `Modules/ModuleHealthCheck.cs` and `ModuleReadinessCheck.cs` | 100 | 0 | 0 | 0 | 0 |
| `Modules/ModuleHealthRegistrationSetup.cs` | 95 | 0 | 0 | 0 | 0 |
| `Modules/ModuleHealthOptions.cs` | 100 | 80 | 0 | 0 | 0 |
| `Modules/ModuleHealthEndpointRouteBuilderExtensions.cs` | 90 | 70 | 0 | 0 | 80 |
| `Health/HealthCheckBuilderExtensions.cs` (edited) | measured baseline or higher | measured baseline or higher | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| `ModuleHealthEvaluator.cs` unit 95, property 80 | Propagation, throwing checks, cancellation and single-flight are unit-testable with fakes; the property test covers generated graphs; the remaining lines are defensive paths of task cancellation. Guard 0: the type is internal and its constructor takes registered services, with no `InternalsVisibleTo` for `Encina.GuardTests` to reach it (verified at work start; raised if present). |
| Check classes unit 100 | Two small `IHealthCheck` classes whose every state mapping runs in unit tests; guard 0 for the same internal-type reason. |
| `ModuleHealthRegistrationSetup.cs` unit 95 | Names, tags and the duplicate check run through `HealthCheckServiceOptions`; the registry-missing path is one branch. |
| `ModuleHealthOptions.cs` unit 100, guard 80 | Validation of the timeout; guard tests reach the argument checks. |
| Endpoint extensions unit 90, guard 70, integration 80 | The route set and argument checks run in unit tests; `TestServer` requests prove the status codes and the 404, and miss only host start-up lines. |
| Contract 0 for every file | No new public interface or abstract class is added (the contracts used are `IModuleHealthCheck`, `IModuleWithHealthChecks` and ASP.NET's `IHealthCheck`, already covered where they exist). |

1. **Unit, Guard, Property, Integration (`TestServer`)** as listed in the phases.
2. **Load**: justification file `tests/Encina.LoadTests/Health/ModuleHealth.md`: the evaluator's concurrency is exercised by a single-flight unit test and probes are not a throughput path.
3. **Benchmark**: justification file `tests/Encina.BenchmarkTests/Health/ModuleHealth.md`: a probe path of a handful of in-memory checks, not a hot path; both justification files ship in pull request 1.
4. **Documentation (pull request 3)**: extend `docs/guides/health-checks.md` (module section: per-module checks, readiness, propagation, endpoints, authorization, what is never exposed) and replace the removed generic overload `AddEncinaModuleHealthChecks<OrdersModule>()` still shown at `src/Encina.AspNetCore/README.md:815` (verified on origin/main; line 810 shows the non-generic call, which stays valid) and at `docs/guides/health-checks.md` lines 144-145 (the two lines are replaced by the `moduleNames` filter form) through `docs-writer`, XML docs with examples, `docs/INVENTORY.md` and `ROADMAP.md` lines, `changelog.d/364-module-health.added.md` and `364-module-health-registration.changed.md` through `mechanical-fixer`; no ADR (no new cross-package decision; health tags are #454's ADR).
5. `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target; CRAP of every changed method below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/Encina.AspNetCore.json; targets never go down; tests execute real code.
TASK: Write the tests, the two justification files, the manifest entries with one-sentence justifications in the same pull request as their tests, and the documentation and changelog fragments through the specialists that own them.
KEY RULES: Shouldly through Encina.Testing.Shouldly; no Thread.Sleep (gate single-flight tests with TaskCompletionSource); justification files only for load and benchmark; no hand-typed coverage figures in docs; never edit CHANGELOG.md.
REFERENCE FILES: .github/coverage-manifest/Encina.AspNetCore.json, tests/Encina.UnitTests/AspNetCore/ModuleHealthCheckTests.cs, docs/guides/health-checks.md, .claude/skills/encina-docs/SKILL.md, changelog.d/README.md.
```

</details>

### Phase 6: Finalization

<details>
<summary>Tasks</summary>

1. XML docs with examples on every public member (`MapEncinaModuleHealthChecks`, `ModuleHealthOptions`, the changed builder extension).
2. Confirm the three follow-ups are tracked: #454 (probe options, writer, tags), #622 (health metrics publisher), #360 (graph and state).
3. Final verification as in Phase 5, item 5.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Code, tests and docs are merged.
TASK: Check the XML docs and the cross references and run the final build and test verification.
KEY RULES: Zero warnings; every flag at its target; never edit CHANGELOG.md.
REFERENCE FILES: AGENTS.md section 8, .github/coverage-manifest/Encina.AspNetCore.json.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` section 3 | No aliases; exception messages never in health results; registration completeness | D2, D7, D4 |
| `AGENTS.md` section 6 | 12 functions | Matrix below |
| ADR-021 | EventId ranges registered first | `ModulesHealth` 7645-7649 |
| ASP.NET Core health checks (`HealthCheckRegistration`, `HealthCheckOptions`, `IHealthCheckPublisher`) | The integration surface | Named by the issue |
| Kubernetes liveness, readiness and startup probes | Why readiness is per module and liveness is not | Issue references; #454 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IModuleHealthCheck`, `IEncinaHealthCheck`, `HealthCheckResult`, `HealthStatus`, `EncinaHealthCheck` | `src/Encina.Messaging/Health/` | Shipped types, reused |
| `IModuleWithHealthChecks` | `src/Encina.AspNetCore/Modules/IModuleWithHealthChecks.cs` | How a module contributes checks |
| `HealthCheckBuilderExtensions.AddEncinaModuleHealthChecks`, `CompositeEncinaHealthCheck`, `EncinaHealthCheckAdapter` | `src/Encina.AspNetCore/Health/` | Edited and reused for status mapping |
| `IModuleRegistry`, `ModuleRegistry` | `src/Encina/Modules/` | The modules and their unique names |
| `IModuleGraph`, `IModuleStateProvider` | `src/Encina/Modules/` (#360) | Dependencies and lifecycle state |
| `EncinaHealthTags`, probe options and writer | `Encina.AspNetCore` (#454) | Tag constants, JSON writer, `IncludeDetails` |
| `HealthCheckMetricsPublisher` | `Encina.OpenTelemetry` (#622) | Metrics for every check |
| `ModuleHealthCheckTests`, `HealthCheckBuilderExtensionsTests` | `tests/Encina.UnitTests/AspNetCore/` | Existing tests to update |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/`, `tests/Encina.UnitTests/Testing/Architecture/` | Range registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AspNetCore` (`ModulesHealth`, new) | 7645-7649, all five used (7649 is the setup's missing-registry warning) | From the assigned block 7600-7649 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); #360 7600-7619, #352 7620-7629, #358 7630-7644 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 9 | New: `ModuleHealthEvaluator`, `ModuleHealthCheck`, `ModuleReadinessCheck`, `ModuleHealthLog`, `ModuleHealthRegistrationSetup`, `ModuleHealthOptions`, `ModuleHealthEndpointRouteBuilderExtensions`; edited: `HealthCheckBuilderExtensions`, `EventIdRanges` |
| Public API and manifest | 3 | core `src/Encina/PublicAPI.Unshipped.txt` (pull request 1), `Encina.AspNetCore` PublicAPI (pull request 2) and `Encina.AspNetCore.json` (touched in pull requests 1 and 2, so counted in both) |
| Tests | 12 | unit 4 (evaluator, checks, setup, endpoints), property 1, guard 2 (options, endpoints), DI 1, allocation map edit, three existing test files edited (`ModuleHealthCheckTests`, `HealthCheckBuilderExtensionsTests`, `AspNetCoreGuardTests`) |
| Justification files | 2 | load, benchmark (pull request 1) |
| Documentation | 6 | guide edit, `src/Encina.AspNetCore/README.md` edit, inventory, roadmap, 2 changelog fragments |
| **Pull request 1** | **13** | 5 production (`ModuleHealthEvaluator`, `ModuleHealthCheck`, `ModuleReadinessCheck`, `ModuleHealthLog`, `EventIdRanges`), core PublicAPI, manifest, 4 tests (evaluator unit, checks unit, property, allocation map edit), 2 justification files; about 600 lines |
| **Pull request 2** | **14** | `ModuleHealthRegistrationSetup`, `ModuleHealthOptions`, `HealthCheckBuilderExtensions`, `ModuleHealthEndpointRouteBuilderExtensions` (4), PublicAPI, manifest, 8 tests (setup unit, endpoint unit with `TestServer`, options guard, endpoint guard, DI test, the three edited existing files including `AspNetCoreGuardTests`); about 850 lines |
| **Pull request 3** | **6** | guide, `src/Encina.AspNetCore/README.md`, inventory, roadmap, 2 changelog fragments |

Callers and implementers, by grep on `origin/main`: `AddEncinaModuleHealthChecks` is called only by its own tests (`ModuleHealthCheckTests`, `HealthCheckBuilderExtensionsTests`, `AspNetCoreGuardTests`) and is documented in `docs/guides/health-checks.md` and `src/Encina.AspNetCore/README.md`; `IModuleWithHealthChecks` has implementers only in tests; `IModuleHealthCheck` has no implementer in `src/`. Pull request 2 removes the generic overload and the `encina-modules` name together with the tests that use them (the two unit test files; `AspNetCoreGuardTests` only uses the non-generic call), so no pull request leaves a broken build. Pull request 1 adds only internal types and a log, with its own tests, so it builds and passes alone.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP. Application modules live in core (src/Encina/Modules); Encina.AspNetCore registers one composite module health check named encina-modules today; #360 adds IModuleGraph and IModuleStateProvider; #454 adds probe tags, options and the JSON writer; #622 adds the health metrics publisher.

IMPLEMENTATION OVERVIEW: PR1 ModuleHealthEvaluator (single-flight, dependency propagation, never throws), the health and readiness IHealthCheck classes, ModuleHealthLog 7645-7649, the ModulesHealth range (with its core PublicAPI line) and the justification files; PR2 an IConfigureOptions<HealthCheckServiceOptions> that adds one health and one readiness registration per module, AddEncinaModuleHealthChecks rewritten, MapEncinaModuleHealthChecks with one fixed endpoint per module; PR3 guide, package README, inventory, roadmap and changelog; a missing IModuleRegistry is read with GetService (never a constructor dependency) and logs 7649; the caller's tags, failure status and module filter reach the setup through ModuleHealthOptions. Dilemmas D3, D4, D5, D6 and D8 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Reuse IModuleHealthCheck, IModuleWithHealthChecks, HealthCheckResult and EncinaHealthCheck; dependents are at most Degraded; readiness is the lifecycle state; descriptions carry fixed text and exception types only, never messages; names and tags from the lowered module name with a duplicate check; registration independent of order, proven by a DI test with ValidateOnBuild and ValidateScopes; an unknown module is 404; details only through the caller's writer; CRAP <= 10; targets never go down.

REFERENCE FILES: src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs, src/Encina.AspNetCore/Modules/IModuleWithHealthChecks.cs, src/Encina.Messaging/Health/EncinaHealthCheck.cs, src/Encina/Modules/*.cs, tests/Encina.UnitTests/AspNetCore/ModuleHealthCheckTests.cs, docs/guides/health-checks.md, .github/coverage-manifest/Encina.AspNetCore.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A health result must reflect real time, so nothing is cached (D6 coalesces only work already in flight). |
| 2 | OpenTelemetry | ✅ | Every module health and readiness registration is a check for #622's `encina.health.check_status` publisher; no new instrument is added here. |
| 3 | Structured Logging | ✅ | `ModuleHealthLog`, EventIds 7645-7649 in the registered `ModulesHealth` range. |
| 4 | Health Checks | ✅ | This plan is the module health integration: per-module health, readiness, propagation and endpoints. |
| 5 | Validation | ❌ | Probes take no external input; the endpoint pattern and `ModuleHealthOptions` are validated at startup, which is configuration validation, not request validation. |
| 6 | Resilience | ✅ | Per-registration `Timeout` from `ModuleHealthOptions`; a check that throws becomes `Unhealthy` with the exception type only. |
| 7 | Distributed Locks | ❌ | Each instance checks its own modules. |
| 8 | Transactions | ❌ | Probes are read-only. |
| 9 | Idempotency | ❌ | Probes are idempotent reads. |
| 10 | Multi-Tenancy | ❌ | Module health is per process; no tenant-scoped module exists, so no tenant tag or tenant-qualified identifier is created (tenant resource health is a tenancy concern). |
| 11 | Module Isolation | ✅ | Health is reported and exposed per module, named only by `IModule.Name`; dependency propagation follows the declared graph (#360). |
| 12 | Audit Trail | ❌ | Infrastructure monitoring, not an auditable business operation. |

---

## Prerequisites & Dependencies

- **#360, pull requests 1, 3 and 4**: `IModuleGraph` (dependencies, start order) and `IModuleStateProvider` (lifecycle state). Without them the evaluator has no graph and the readiness check no state; pull request 1 of this plan therefore waits for #360's graph and state pull requests. Pull request 2 (the readiness endpoint) additionally depends on #360's Design Choice 11 (pull request 3 registers `ModuleLifecycleHostedService` for every application using `AddEncinaModules`, pull request 4 marks every module `Running`, plain `IModule` modules included): on `origin/main` the hosted service is registered only when a module implements `IModuleLifecycle`, so without that rule a plain module never leaves `Configured` and this plan's readiness would report it not ready forever. If the maintainer chooses another option for #360's D11, the readiness rule of D3 here changes with it.
- **#454, pull request 1** (`EncinaHealthTags`, tag constants `live`, `ready`, `startup`) before pull request 2; its writer and `EncinaProbeOptions` are optional for this plan (the module endpoints take the caller's `HealthCheckOptions`).
- **Related, not blocking**: #622 (health metrics publisher, makes every module check a metric series), #2396 (processor liveness), and #352 (the module context producer; independent of health).

---

## Next Steps

1. Ask the maintainer D3, D4, D5, D6 and D8 when work on #364 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: evaluator, checks, log and range (13 files), after #360's graph and state pull requests; pull request 2 also needs #360's rule that plain modules reach `Running`.
3. Pull request 2: per-module registration and endpoints (14 files), after #454 pull request 1.
4. Pull request 3: documentation and changelog fragments (6 files).
