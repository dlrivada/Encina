# Implementation Plan: Vertical Slice Architecture Support (`[VerticalSlice]`, slice-scoped pipelines, `encina generate slice`, `SliceTestBase<T>`)

> **Issue**: [#365](https://github.com/dlrivada/Encina/issues/365)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 5 pull requests of 15, 15, 7, 6 and 4 files)
> **Estimated Scope**: ~700 lines of production code + ~1,600 lines of tests
> **Family**: modular monolith (#352, #358, #359, #360, #364 and the module plans #366, #423); this plan owns the slice markers, the slice-scoped pipeline and the slice tooling, which sit inside a module and need no module feature

---

## Summary

The issue asks for explicit vertical slice support: a `[VerticalSlice]` marker, slice-scoped pipeline behaviors (`[SlicePipeline]`), slice discovery (`AddEncinaSlices`), a feature folder convention, the CLI command `encina generate slice`, minimal API endpoint generation, `SliceTestBase<T>` for isolated tests and a documentation generator. State of `origin/main` (`0b941beeb`), verified by reading and grep:

| Issue item | Today | Owner of what remains |
|------------|-------|-----------------------|
| `[VerticalSlice]`, `[SlicePipeline]` | Missing (grep for `VerticalSlice` and `SlicePipeline` over `src` and `tests` finds nothing) | This plan, Phase 1 |
| Slice-scoped pipeline | Every registered `IPipelineBehavior<TRequest,TResponse>` runs for every request: `PipelineBuilder<TRequest,TResponse>.ResolveSteps` (`src/Encina/Pipeline/PipelineBuilder.cs:100`) takes all behaviors from DI; the order comes from `PipelineStage` and `PipelineOrderResolver` (#2184) | This plan, Phase 2 |
| Slice discovery | Handlers are discovered by assembly scanning (`AddEncina(assemblies)`, `src/Encina/Core/ServiceCollectionExtensions.cs:33`, `MediatorAssemblyScanner`); no slice registry | This plan, Phase 1 |
| `encina generate slice` | `GenerateCommand` has `handler`, `query`, `saga`, `notification` and `stryker`; `CodeGenerator` (`src/Encina.Cli/Services/CodeGenerator.cs`, 507 lines) has one static method per kind | This plan, Phase 4 |
| `SliceTestBase<T>` | `EncinaTestFixture` (`WithHandler<T>`, `SendAsync` returning `EncinaTestContext<TResponse>` with `ShouldSucceed`) and `ModuleTestFixture` exist in `Encina.Testing` | This plan, Phase 3 |
| Endpoint generation | `Encina.AspNetCore` has no generic endpoint mapping (`DPIAEndpointExtensions` is one feature's group) | This plan, Phase 4 (generated file), Design Choice 5 |
| Documentation generator | Missing | Deferred: Design Choice 6, issue file |
| Module association | `IModule`, `IModuleRegistry`, handler to module by assembly (`ModuleHandlerRegistry`) | #352 (`[Module]` on handlers) and #360; a slice is independent of a module and only its name path "Module/Feature" reads like one |

Facts that shape the design:

1. **There is no way to run a behavior for some handlers only.** The issue's `[SlicePipeline(typeof(ValidationBehavior<,>), ...)]` needs a filter in `PipelineBuilder`. That class is internal to core, so the slice feature lives in core (Design Choice 1) and costs nothing when `AddEncinaSlices` is not called: the filter is an optional service.
2. **A slice must not be able to opt out of a security gate.** If any registered behavior could be dropped by an attribute on a handler, a handler author could remove authorization. Only behaviors registered as slice-scoped are filtered, and gate stages can never be slice-scoped (Design Choices 3 and 4).
3. **The generated code assumes no validation library.** Validation has three providers (`AGENTS.md` section 5), so the generator supports all three (Design Choice 7).

**Delivered**: the two attributes, the slice registry and discovery, the slice-scoped behavior filter, the slice context (trace tag and log scope), `SliceTestBase<T>`, and `encina generate slice`.

**Not in scope**: the slice documentation generator (Design Choice 6, issue file `artifacts/issues/vertical-slice-documentation-generator.md`); per-slice cache, idempotency, tenant and audit behaviors (deferred by the issue itself, each is a behavior that a user scopes with this feature); runtime endpoint auto-registration (Design Choice 5).

**Affected packages**: `Encina` (core), `Encina.Cli`, `Encina.Testing`, `Encina.OpenTelemetry` (activity source registration check only, no change expected).

**Provider category**: none. The feature is provider independent.

---

## Design Choices

<details>
<summary><strong>1. Placement: core, namespace `Encina.Slices`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core `Encina`, folder `Slices/`, namespace `Encina.Slices`** | The filter must read the internal `PipelineBuilder`; same placement as the module system (#352 Design Choice 1); no new package | Core grows by about eight types |
| **B) A new package `Encina.VerticalSlices` with `InternalsVisibleTo` to core** | Core stays small | A new package whose only job is to call an internal API; an `InternalsVisibleTo` to a satellite |

### Chosen Option: **A — core namespace**

### Rationale

The pipeline builder is internal and the behavior filter is a hook of it; an opt-in service resolved with `GetService` keeps the cost at zero for applications that do not use slices.

**Class:** Trivial
**Settled by:** #352 plan Design Choice 1 (module features extend the core namespace, no new package) and `src/Encina/Pipeline/PipelineBuilder.cs` (internal class); `AGENTS.md` section 1 (pay-for-what-you-use).

</details>

<details>
<summary><strong>2. How a slice is identified (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only `[VerticalSlice("Orders/PlaceOrder")]` on the slice class or on the handler; the name is a path of two or more non-blank segments separated by `/`** | Explicit, found by reflection and analyzers, no dependence on folders | Every slice carries an attribute |
| **B) Also a namespace convention (`ConventionFolder = "Features"`: `MyApp.Features.Orders.PlaceOrder` is the slice `Orders/PlaceOrder`) as a fallback when no attribute exists** | Zero ceremony for conventional code | Two ways to be a slice; a rename of a namespace silently renames a slice (and its `[SlicePipeline]` scope) |
| **C) The folder convention is used by the CLI and an analyzer only, never at runtime** | The runtime has one rule; the layout is still guided | The convention is not enforced unless an analyzer is added |

### Chosen Option: **A — the attribute only** (recommended, pending the maintainer)

### Rationale

A wins when the slice name feeds a pipeline filter and a trace tag, which must not change with a namespace edit. B wins for teams that want slices without annotations. C is how the CLI uses the convention anyway (Phase 4 writes the files into `Features/<Module>/<Name>/`); the `ConventionFolder` option of the issue is dropped from the runtime configuration.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How a behavior becomes slice-scoped (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A registration list: `AddEncinaSlices(c => c.ScopeBehavior(typeof(TransactionPipelineBehavior<,>)))`. A scoped behavior runs only for slices whose `[SlicePipeline]` lists it; every other behavior runs everywhere, as today** | Works for framework behaviors without an attribute on their classes; a handler author cannot remove an unscoped behavior | The scoping is configured in one place away from the behavior |
| **B) An attribute `[SliceScoped]` on the behavior class** | Visible at the class | Framework behaviors would need the attribute and would then stop running for slices that do not list them |
| **C) `[SlicePipeline]` replaces the whole optional behavior list of the slice (every behavior outside the gate stages)** | Matches the issue text literally | A slice that forgets a behavior silently loses validation or metrics |

### Chosen Option: **A — scope by registration** (recommended, pending the maintainer)

### Rationale

A wins because the default stays "everything runs" and an exclusion is always an explicit, central decision (`AGENTS.md` section 1: opt-in). Under A a request that belongs to no slice runs every behavior, scoped or not, because scoping only narrows the pipelines of slices (otherwise a scoped `TransactionPipelineBehavior` would silently stop running for every non-slice handler). C wins for teams that want each slice to spell out its full pipeline; B wins when the behavior author, not the host, decides. Registration fails with `InvalidOperationException` when a scoped type is not an open generic `IPipelineBehavior<,>`, when it is registered as scoped twice, or when a `[SlicePipeline]` lists a type that was not scoped (startup configuration errors, the same channel as `ModuleConfiguration`; there is no error code, so no ADR-062 category applies).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Behaviors that can never be slice-scoped (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ScopeBehavior` rejects any behavior whose `[PipelineStage]` is `Admission`, `Audit`, `Authentication`, `Authorization` or `Compliance` (the gate stages 200 to 600)** | A slice cannot opt out of load shedding, auditing, identity, authorization or compliance; behaviors without a declared stage (stage `Application`) and the stages from `Presentation` on can be scoped | Per-slice audit (function 12 of the issue) is not possible through this feature |
| **B) Reject only `Authentication`, `Authorization` and `Compliance`** | Per-slice audit and admission are possible | A slice can disable the access audit or the rate limiter |
| **C) Reject nothing; trust the host** | Maximum freedom | A handler attribute can remove authorization, against fail-closed gates |

### Chosen Option: **A — the five gate stages are never scopable** (recommended, pending the maintainer)

### Rationale

A wins when auditing must be uniform (SPEC-002 treats audit as part of compliance). B wins if the maintainer wants per-slice audit, which the issue defers. C violates `AGENTS.md` section 3 (gates fail closed) and is listed only to be rejected. The stage vocabulary is `PipelineStage` (`src/Encina/Pipeline/Ordering/PipelineStage.cs`); a behavior reads its stage with `PipelineStageAttribute` and one without it is `Application` (inside every gate).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Endpoint generation (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The CLI generates an explicit minimal API endpoint file (`PlaceOrderEndpoint.cs`, `MapPost` calling `IEncina.Send`) when `--endpoint` is given; nothing is mapped at runtime** | Visible, editable, trimming and native AOT friendly; no reflection at startup | One generated file per slice to maintain |
| **B) `config.AutoRegisterEndpoints = true` maps every slice at startup by reflection (`MapEncinaSlices`)** | No file per slice | Reflection over request and response types at startup; routes and OpenAPI metadata are implicit |
| **C) A source generator emits the mapping** | No reflection, no file per slice | A new Roslyn generator package for one feature |

### Chosen Option: **A — a generated explicit file** (recommended, pending the maintainer)

### Rationale

A wins for applications that publish OpenAPI and want to edit routes and policies per endpoint; B wins when the number of slices is large and routes are uniform; C wins when AOT compatibility and no boilerplate are both required. `AutoRegisterEndpoints` of the issue is dropped from the configuration under A.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. The slice documentation generator (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to a follow-up issue (`artifacts/issues/vertical-slice-documentation-generator.md`); this plan ships the registry that such a generator reads** | Keeps the plan within about 5 pull requests; the registry is the needed foundation | The task of the issue is not delivered now |
| **B) Implement `SliceDocumentation.ToMarkdown(ISliceRegistry)` now and an `encina slices` command** | The issue's task list is complete | The CLI would have to load the application assembly to read attributes, a design of its own (a tool that executes user code) |

### Chosen Option: **A — defer** (recommended, pending the maintainer)

### Rationale

A wins while the CLI has no assembly-loading story (`Encina.Cli` today only writes files from templates). B wins if the maintainer wants a pure `ISliceRegistry` to Markdown function inside the application (an endpoint or a test that prints the catalog) without the CLI part.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. CLI validator templates cover all three validation providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `--validator fluentvalidation\|dataannotations\|minivalidator\|none` (default `none`): one template per provider** | Consistent across the validation category; the slice stays valid for every provider | Three templates and their tests |
| **B) Only FluentValidation, as in the issue example** | One template | A feature that touches one validation provider must support all three (`AGENTS.md` section 5) |

### Chosen Option: **A — three templates and `none`**

### Rationale

The category rule requires consistency across `FluentValidation`, `DataAnnotations` and `MiniValidator`. The default is `none` so that the generated slice compiles in a project that references no validation package.

**Class:** Settled
**Settled by:** `AGENTS.md` section 5, Validation category ("a feature that touches one MUST be consistent across every provider of that category").

</details>

<details>
<summary><strong>8. The slice test base follows `ModuleTestFixture`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SliceTestBase<TSlice>` in `Encina.Testing`, wrapping `EncinaTestFixture`: it registers the slice's handler (and its validator when present), builds on first use and exposes `Slice.SendAsync(request)` returning `EncinaTestContext<TResponse>`** | Same shape as the existing module fixture; reuses `ShouldSucceed` and `ShouldFailWith` | A base class to inherit |
| **B) Only documentation showing `EncinaTestFixture.WithHandler<T>()`** | No new type | Every test repeats the registration |

### Chosen Option: **A — `SliceTestBase<TSlice>`**

### Rationale

`ModuleTestFixture` and `MockModuleApi<T>` set the precedent in the same package.

**Class:** Trivial
**Settled by:** `src/Encina.Testing/Modules/ModuleTestFixture.cs` and `src/Encina.Testing/EncinaTestFixture.cs` (`WithHandler`, `SendAsync`).

</details>

---

## Implementation Phases

### Phase 1: Core markers, registry and discovery (pull request 1)

> **Goal**: The vocabulary and the registry, with no behavior change yet.

<details>
<summary>Tasks</summary>

1. `src/Encina/Slices/VerticalSliceAttribute.cs`: `public sealed class VerticalSliceAttribute(string name) : Attribute`, `string Name`, usage `Class`, `Inherited = false`; the constructor throws `ArgumentException` for a blank name, fewer than two segments, an empty segment or a segment with characters other than letters, digits, `_` and `-`.
2. `src/Encina/Slices/SlicePipelineAttribute.cs`: `public sealed class SlicePipelineAttribute(params Type[] behaviors) : Attribute`, `IReadOnlyList<Type> Behaviors`, usage `Class`.
3. `src/Encina/Slices/SliceDescriptor.cs`: `public sealed record SliceDescriptor(string Name, Type SliceType, Type RequestType, Type HandlerType, IReadOnlyList<Type> ScopedBehaviors)`.
4. `src/Encina/Slices/ISliceRegistry.cs` and internal `SliceRegistry.cs`: `Slices`, `GetSlice(string name)` (case-insensitive), `GetSliceOf(Type requestType)`; built by internal `SliceDiscovery` from assemblies: a type with `[VerticalSlice]` is a slice; its handler is the single `IRequestHandler<,>` implementation found on the type or its nested types (zero or several handlers, or two slices with one name, is an error listing every offender); a handler class with the attribute is its own slice.
5. `src/Encina/Slices/SliceConfiguration.cs` and `SliceServiceCollectionExtensions.cs`: `AddEncinaSlices(this IServiceCollection, Action<SliceConfiguration>)` with `DiscoverSlicesFromAssembly(Assembly)` and `ScopeBehavior(Type openGenericBehavior)`; `TryAddSingleton<ISliceRegistry>`; discovery and validation run at registration and throw `InvalidOperationException` listing every problem (D2, D3, D4).
6. Tests: unit (name rules, discovery of a nested slice, a handler-level slice, duplicate names, no handler, two handlers, case-insensitive lookup), guard (`SliceConfiguration`, the extension, the attributes), DI test with `ValidateOnBuild` and `ValidateScopes` (the registry resolves); `PublicAPI.Unshipped.txt` of `Encina` and the `Encina.json` manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #365; plan docs/plans/vertical-slice-implementation-plan-365.md. D2 (attribute only) is recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina core already scans assemblies for handlers (src/Encina/Dispatchers/MediatorAssemblyScanner.cs); the slice registry is separate and does not register handlers.

TASK:
Add the attributes, the descriptor, the registry, the discovery and AddEncinaSlices with their tests.

KEY RULES:
- Registration problems are InvalidOperationException listing every problem, never one at a time.
- The registry is a singleton holding no scoped service; lookups are case-insensitive like ModuleRegistry.
- XML docs with examples on every public member; PublicAPI.Unshipped.txt; zero warnings; no [Obsolete].

REFERENCE FILES:
- src/Encina/Modules/ModuleConfiguration.cs
- src/Encina/Modules/ModuleServiceCollectionExtensions.cs
- src/Encina/Dispatchers/MediatorAssemblyScanner.cs
- src/Encina/Abstractions/IRequestHandler.cs
```

</details>

---

### Phase 2: The slice-scoped pipeline (pull request 2)

> **Goal**: A scoped behavior runs only for the slices that list it; the slice is visible in traces and logs.

<details>
<summary>Tasks</summary>

1. `src/Encina/Slices/SliceBehaviorFilter.cs` (internal sealed, singleton): built from the registry and the scoped list; `bool Applies(Type openBehaviorType, Type requestType)` is true for an unscoped behavior, true for every behavior (scoped or not) when `requestType` belongs to no slice (D3 rule: scoping only narrows slice pipelines), and, for a scoped behavior of a request that belongs to a slice, only when that slice lists it. `PipelineBuilder<TRequest,TResponse>.ResolveSteps` (a static method that keeps no static state) resolves the filter with `GetService` (null when slices are not used: no cost) and drops behaviors it rejects; the result is deterministic per `TRequest`, so the order cache of `PipelineOrderResolver` (per closed pipeline) stays valid. The per-closed-type answer is cached inside the filter instance, in a `ConcurrentDictionary<(Type Behavior, Type Request), bool>`, never in a static field: the answer depends on the slice registry and the scoped list of one container, and two hosts in one process (every test run, `WebApplicationFactory`) must not share it. The filter is a singleton registered per container by `AddEncinaSlices`.
2. `EncinaPipelineInspector` uses the same `ResolveSteps` (line 37), so the inspector shows the filtered pipeline; a unit test asserts it.
3. `src/Encina/Slices/SliceContextPipelineBehavior.cs` (stage `Observability`, order after the request activity): sets the tag `slice.name` on `Activity.Current` and opens a logging scope `{Slice}`; registered by `AddEncinaSlices`, so only slice users pay.
4. `src/Encina/Slices/Diagnostics/SliceLog.cs` (`[LoggerMessage]`, one EventId, 7680: behavior skipped for a slice (Debug, behavior type name and slice name, logged by the filter the first time it computes the answer for a closed pipeline). No "slices discovered" log exists: discovery runs at registration, where there is no `ILogger`); `EventIdRanges.VerticalSlices = (7680, 7689)` registered first, and the `Encina` entry of the allocation test map `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
5. Tests: unit (scoped behavior skipped for another slice and run for a listed one, unscoped behavior always runs, request outside every slice keeps the full pipeline (every scoped behavior runs too), two containers in one process with different scoped lists give independent answers for the same request type, gate-stage behavior rejected at registration, the order of the remaining steps is unchanged), property (for generated behavior and slice sets the steps of a request equal the unscoped steps minus the scoped behaviors the slice does not list), tag and scope test with an `ActivityListener`, guard test for the `SliceContextPipelineBehavior` constructor, the PR 1 DI test extended to assert the filter and context-behavior registrations; `SliceServiceCollectionExtensions.cs` (a PR 1 file) is edited to register them; `PublicAPI.Unshipped.txt` and the `Encina.json` manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #365. D3 (scope by registration) and D4 (gate stages never scopable) are recommended, pending the maintainer. PipelineBuilder is internal and runs on the hot path of every request.

TASK:
Add the filter, hook it into ResolveSteps, add the context behavior, the log, the EventId range and the tests.

KEY RULES:
- An unscoped behavior is never filtered; a gate-stage behavior can never be scoped (fail closed).
- No allocation per request when slices are unused: GetService returns null and the old path runs; cache the per-closed-type answer inside the singleton SliceBehaviorFilter instance (ConcurrentDictionary), NEVER in a static field (two containers in one process must not share it); a request that belongs to no slice runs every behavior.
- EventId 7680 only (behavior skipped), inside 7680-7689, range registered in EventIdRanges.cs first; logs carry type names only.
- Tags: slice.name on the existing request activity; no new metric label with a tenant (ADR-054 D4 only applies to metrics that already exist; see docs/architecture/adr/054-observability-conventions-and-trace-propagation.md).

REFERENCE FILES:
- src/Encina/Pipeline/PipelineBuilder.cs
- src/Encina/Pipeline/Ordering/PipelineStage.cs
- src/Encina/Pipeline/Ordering/EncinaPipelineInspector.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 3: `SliceTestBase<T>` (pull request 3)

<details>
<summary>Tasks</summary>

1. `src/Encina.Testing/SliceTestBase.cs`: `public abstract class SliceTestBase<TSlice> : IDisposable, IAsyncDisposable`; discovers the slice through `SliceDiscovery` over `typeof(TSlice)`, builds an `EncinaTestFixture` with the slice's handler (`WithHandler`), virtual `ConfigureServices(EncinaTestFixture fixture)` for the slice's own dependencies, and `Slice.SendAsync<TResponse>(IRequest<TResponse>, CancellationToken)` returning `EncinaTestContext<TResponse>` (`ShouldSucceed`, `ShouldFailWith`). A type that is not a slice, or has no handler, throws `ArgumentException` naming the type.
2. Tests: unit (a command slice succeeds, a handler `Left` is returned unchanged, the dependency override works, a non-slice type is rejected), guard; the `Encina.Testing.json` manifest. `src/Encina.Testing` has no `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` or `AdditionalFiles` entry (and neither has `Encina.Cli`), so its public API is not tracked today and this plan adds no PublicAPI line for it; starting to track it would need every existing public symbol declared, which is out of scope. An example in `Encina.Testing.Examples` is not part of this pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #365. Pull request 1 (SliceDiscovery) is merged. EncinaTestFixture and EncinaTestContext already provide SendAsync and the assertions.

TASK:
Add SliceTestBase<TSlice> and its tests.

KEY RULES:
- Tests exercise the real slice handler; never mock the handler under test.
- Shouldly through Encina.Testing.Shouldly; dispose the fixture; no Thread.Sleep.

REFERENCE FILES:
- src/Encina.Testing/EncinaTestFixture.cs
- src/Encina.Testing/EncinaTestContext.cs
- src/Encina.Testing/Modules/ModuleTestFixture.cs
```

</details>

---

### Phase 4: `encina generate slice` (pull request 4)

> **Goal**: One command writes a complete slice into the feature folder.

<details>
<summary>Tasks</summary>

1. `src/Encina.Cli/Services/SliceOptions.cs` and `SliceCodeGenerator.cs` (new, so `CodeGenerator.cs` does not grow past its 507 lines): `SliceOptions` (`Name`, `Module`, `Kind` command or query, `ResponseType`, `Validator` of the three providers or none, `Endpoint`, `OutputDirectory`, `Namespace`); `GenerateSliceAsync` writes `Features/<Module>/<Name>/<Name>Command.cs` (or `Query`), `<Name>Handler.cs`, optionally `<Name>Validator.cs` and `<Name>Endpoint.cs`, each carrying `[VerticalSlice("<Module>/<Name>")]` on the handler (D2, D5, D7); an existing file is never overwritten (the result reports it as skipped).
2. `src/Encina.Cli/Commands/GenerateCommand.cs`: `CreateSliceCommand()` (`generate slice <name> --module <m> --type command|query --response <t> [--validator ...] [--endpoint] [--output] [--namespace]`) added to the list at line 23-27.
3. `generate slice` stays scaffolding under the existing `generate` command of `docs/architecture/adr/058-encina-cli-command-architecture.md`: it writes files only, starts no application and writes no audit entry.
4. Tests: unit `SliceCodeGeneratorTests` (each file's text, the three validator templates, no overwrite, invalid names), `GenerateCommandTests` (the option wiring); the manifest `Encina.Cli.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #365. D5 (generated explicit endpoint) and D7 (three validator templates) apply. The CLI writes files from templates; it never loads user assemblies.

TASK:
Add SliceOptions, SliceCodeGenerator, the generate slice command and the tests.

KEY RULES:
- Never overwrite an existing file; names are validated so a path cannot leave the output directory.
- The generated handler returns Either<EncinaError, T>; the endpoint maps the Either result without throwing.
- No Unix tools; file IO through async APIs with a CancellationToken where the existing methods have one.

REFERENCE FILES:
- src/Encina.Cli/Commands/GenerateCommand.cs
- src/Encina.Cli/Services/CodeGenerator.cs
- tests/Encina.UnitTests/Cli/Commands/GenerateCommandTests.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. OpenTelemetry (function 2) and structured logging (function 3): Phase 2 (`slice.name` tag, `{Slice}` scope).
2. Transactions (function 8): the issue's slice-scoped transaction pipeline is `ScopeBehavior(typeof(TransactionPipelineBehavior<,>))` (stage `Transaction`, allowed by D4); a test registers the real behavior with a scoped list and proves a slice that does not list it runs without a transaction.
3. Validation (function 5): Phase 4 templates for the three providers.
4. Deferred (issue files in this plan's worktree): `artifacts/issues/vertical-slice-documentation-generator.md` (D6). Caching, idempotency, tenant and audit behaviors per slice are user-scoped behaviors of this feature (audit is a gate stage and not scopable, D4); no separate issue is needed.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #365; the cross-cutting matrix is the contract.

TASK:
Verify each Include row exists in code and tests and that the issue file is written.

KEY RULES:
- Never log EncinaError.Message; no tenant metric label is added.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
- src/Encina.Messaging/TransactionPipelineBehavior.cs
```

</details>

---

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. **Unit, Guard, DI, Property** as in Phases 1 to 4; **Contract**: `SliceRegistryContractTests` in pull request 1 (the registry honors the `ISliceRegistry` contract with the real implementation); **Integration**, in pull request 3 (so the integration targets below are first measured there): a test in `tests/Encina.IntegrationTests/Core/Slices/` builds a host with `AddEncina`, `AddEncinaSlices` and two slices and sends requests through the real pipeline (no database, so no provider fixture); **Load**: justification file `tests/Encina.LoadTests/Core/Slices/SlicePipeline.md` (the filter result is cached per closed type in the filter instance); **Benchmark**: `tests/Encina.BenchmarkTests/Core/Slices/SlicePipeline.md` (justification: an optional `GetService` and a per-container cache; the existing pipeline benchmarks cover the unchanged path).
2. **Per-file targets** (each flag with a one-sentence justification in the manifest; baselines of edited files, `PipelineBuilder.cs` and `EventIdRanges.cs`, are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`, and a target is the measured value or higher):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `Slices/SliceDiscovery.cs`, `SliceRegistry.cs` | 95 | 80 | 90 | 0 | 80 | Every discovery rule is a unit test; the integration test runs the real pipeline |
   | `Slices/VerticalSliceAttribute.cs`, `SlicePipelineAttribute.cs` | 100 | 90 | 0 | 0 | 0 | Name rules and argument checks are fully reachable |
   | `Slices/SliceBehaviorFilter.cs` | 100 | 0 | 0 | 90 | 80 | A pure decision whose property test generates behavior and slice sets; no parameterized public member |
   | `Slices/SliceContextPipelineBehavior.cs`, `SliceLog.cs` | 90 | 70 | 0 | 0 | 80 | Tag and scope are reached with a listener; guard reaches the constructor |
   | `Slices/SliceConfiguration.cs`, `SliceServiceCollectionExtensions.cs` | 95 | 90 | 0 | 0 | 0 | Registration rules and their failures are unit and guard tests |
   | `Encina.Testing/SliceTestBase.cs` | 90 | 80 | 0 | 0 | 0 | Run through real slice handlers; integration 0 because it is a test helper |
   | `Encina.Cli/Services/SliceCodeGenerator.cs` | 90 | 80 | 0 | 0 | 0 | Template text is asserted per file; the CLI has no integration flag |

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #365. Per-flag coverage per AGENTS.md section 9.

TASK:
Write the tests and manifest entries of Phase 6; the two justification files cover only load and benchmark.

KEY RULES:
- Tests execute real code; a nullable parameter that accepts null gets a valid-result test, not an exception test; a test never swallows an exception.
- Targets never go down; CRAP <= 10 on every changed method.

REFERENCE FILES:
- tests/Encina.UnitTests/Core/Modules
- .github/coverage-manifest/Encina.json
- docs/testing/integration-tests.md
```

</details>

---

### Phase 7: Documentation & Finalization (pull request 5)

<details>
<summary>Tasks</summary>

1. XML docs with examples on all new public APIs; `PublicAPI.Unshipped.txt`.
2. `changelog.d/365-vertical-slices.added.md` (through `mechanical-fixer`); the guide `docs/features/vertical-slices.md` (through `docs-writer`: the slice layout, the pipeline scoping and the gate rule, the CLI command, testing a slice, how it relates to modules); `docs/INVENTORY.md` and `ROADMAP.md` lines. ADR number assigned when the ADR pull request opens, only if the gate rule (D4) is decided architecturally.
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #365 is implemented and tested.

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

Order: the CLI and the test base need pull request 1; pull request 2 changes `PipelineBuilder` and is independent of 4; pull request 3 needs pull request 2 (its integration test runs the real filtered pipeline). Counts include tests, PublicAPI and manifests.

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | Attributes (2), descriptor, registry interface and implementation, discovery, configuration, registration extension (8), PublicAPI, manifest, 3 unit and guard test files, DI test, contract test | 15 | 1,050 |
| 2 | Filter, `PipelineBuilder` edit, context behavior, log, `EventIdRanges`, allocation test map, `SliceServiceCollectionExtensions` edit, PublicAPI, manifest (9), DI test edit, 3 unit tests (filter, behavior, inspector), property test, guard test (15) | 15 | 1,200 |
| 3 | `SliceTestBase<T>`, `Encina.Testing.json`, 2 test files (unit, guard), integration test, 2 justification files (load, benchmark); needs pull request 2 | 7 | 650 |
| 4 | `SliceOptions`, `SliceCodeGenerator`, `GenerateCommand` edit, `Encina.Cli.json`, 2 test files | 6 | 600 |
| 5 | Guide, inventory and roadmap lines, changelog fragment | 4 | 300 |

A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1, 3 and 5 | Opt-in, fail-closed gates, validation category consistency | D3, D4, D7 |
| ADR-021 | EventId ranges registered first | `VerticalSlices` 7680-7689 |
| [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) | Observability conventions, D4 tenant tags on metrics | `slice.name` tag only; no tenant metric label |
| [ADR-058](../architecture/adr/058-encina-cli-command-architecture.md) | CLI command architecture | `generate slice` is scaffolding under `generate` |
| #2184 and `PipelineStage` | Closed stage vocabulary, undeclared steps run inside every gate | D4 |
| Vertical Slice Architecture (Jimmy Bogard), FastEndpoints, feature folders | Named by the issue | Concept source |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `PipelineBuilder<TRequest,TResponse>.ResolveSteps` | `src/Encina/Pipeline/PipelineBuilder.cs` | The filter hook |
| `PipelineStage`, `PipelineStageAttribute`, `PipelineOrderResolver` | `src/Encina/Pipeline/Ordering/` | Gate stages and order cache |
| `EncinaPipelineInspector` | `src/Encina/Pipeline/Ordering/EncinaPipelineInspector.cs` | Shows the filtered pipeline |
| `MediatorAssemblyScanner`, `EncinaConfiguration.AddPipelineBehavior` | `src/Encina/Dispatchers/`, `src/Encina/Core/EncinaConfiguration.cs` | Handler discovery stays; behavior registration stays |
| `ModuleConfiguration`, `AddEncinaModules` | `src/Encina/Modules/` | Registration style and error channel |
| `EncinaTestFixture`, `EncinaTestContext` | `src/Encina.Testing/` | The slice test base |
| `GenerateCommand`, `CodeGenerator` | `src/Encina.Cli/` | The slice command |
| `TransactionPipelineBehavior` | `src/Encina.Messaging/TransactionPipelineBehavior.cs` | Example of a scopable behavior |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`VerticalSlices`, new) | 7680-7689, id 7680 used | From the assigned block 7650-7699 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 7681-7689 free; registered in `EventIdRanges.cs` and the allocation test map before use |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 15 | 2 attributes, descriptor, registry (2), discovery, configuration, extension, filter, context behavior, log, `SliceTestBase`, 2 CLI files, `PipelineBuilder` and `GenerateCommand` and `EventIdRanges` edits counted in the split |
| Public API and manifests | 5 touches | `Encina` PublicAPI (PR 1 and PR 2), `Encina.json` (PR 1 and PR 2), `Encina.Testing.json`, `Encina.Cli.json`; `Encina.Testing` and `Encina.Cli` have no PublicAPI files |
| Tests | 15 | Unit, guard, DI, property, contract, integration, allocation map edit |
| Justification files | 2 | Load, benchmark |
| Documentation | 4 | Guide, inventory, roadmap, changelog fragment |
| **Total** | **47 file touches across 5 pull requests** | The sum of the rows of the split table (15 + 15 + 7 + 6 + 4) is the authority; a file edited in two pull requests counts in each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #365: vertical slice support in core, the CLI and Encina.Testing. Plan: docs/plans/vertical-slice-implementation-plan-365.md; D2-D6 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (5 pull requests, each builds alone):
1. Attributes, descriptor, registry, discovery, AddEncinaSlices (Phase 1).
2. The slice-scoped behavior filter in PipelineBuilder, the context behavior, logs and the EventId range 7680-7689 (Phase 2).
3. SliceTestBase<T> (Phase 3); 4. encina generate slice (Phase 4); 5. documentation (Phase 7).

KEY PATTERNS:
- Only behaviors registered as slice-scoped are filtered; gate stages (Admission, Audit, Authentication, Authorization, Compliance) can never be scoped; unscoped behaviors always run.
- Registration errors are InvalidOperationException listing every problem; the filter is an optional service, zero cost when unused.
- The CLI never overwrites files and supports three validation providers; endpoints are generated files, never runtime reflection.
- Tests execute real code; per-flag coverage from the manifests; CRAP <= 10.

REFERENCE FILES:
- src/Encina/Pipeline/PipelineBuilder.cs
- src/Encina/Pipeline/Ordering/PipelineStage.cs
- src/Encina/Modules/ModuleConfiguration.cs
- src/Encina.Testing/EncinaTestFixture.cs
- src/Encina.Cli/Commands/GenerateCommand.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred as in the issue: a cache behavior is scoped per slice by a user with `ScopeBehavior`; no cache code in this plan |
| 2 | OpenTelemetry | ✅ | Phase 2: tag `slice.name` on the existing request activity; no new metric |
| 3 | Structured Logging | ✅ | Phase 2: `{Slice}` log scope and `SliceLog` EventId 7680 |
| 4 | Health Checks | ❌ | Not applicable: an organizational pattern with no dependency |
| 5 | Validation | ✅ | Phase 4: validator templates for the three providers (D7) |
| 6 | Resilience | ❌ | Not applicable: no external call |
| 7 | Distributed Locks | ❌ | Not applicable: stateless structure |
| 8 | Transactions | ✅ | Phase 5: a transaction behavior is slice-scoped through `ScopeBehavior` (stage `Transaction` is allowed by D4) and tested |
| 9 | Idempotency | ⏭️ | Deferred as in the issue: an idempotency behavior is slice-scoped by the user with this feature |
| 10 | Multi-Tenancy | ⏭️ | Deferred as in the issue: the registry holds no tenant data; a tenant behavior is a gate or application behavior the host configures; nothing here creates a tenant-qualified identifier |
| 11 | Module Isolation | ⏭️ | Deferred as in the issue: slices inside module boundaries use #352's rule on the request, which already applies to a slice's request |
| 12 | Audit Trail | ⏭️ | Deferred as in the issue: audit is a gate stage and cannot be slice-scoped (D4); the audit pipeline of #1636 and the operation audit behaviors apply to slices unchanged |

---

## Prerequisites & Dependencies

- None blocks pull request 1. Pull request 2 coordinates with #2184 (pipeline stage contract, which this plan reads and does not change); if #2184 changes `ResolveSteps`, the filter hook rebases onto it.
- #352 (`[Module]` on handlers) and #360 are independent; slices and modules compose through the request type.
- #423 (modular monolith with Aspire) names vertical slices inside modules; its plan consumes `ISliceRegistry` by issue number only.

---

## Next Steps

1. The maintainer answers D2, D3, D4, D5 and D6 when work on #365 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 first; pull requests 2 and 4 follow in any order; pull request 3 after pull request 2; pull request 5 last.
3. Open the issue file `artifacts/issues/vertical-slice-documentation-generator.md` when this plan merges.
