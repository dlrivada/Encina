# Implementation Plan: Zero-interface handlers — convention-based handler discovery

> **Issue**: [#333](https://github.com/dlrivada/Encina/issues/333)
> **Type**: Feature (core, developer experience)
> **Complexity**: Medium (3 phases, no database provider, 3 pull requests of 15, 14 and 4 files)
> **Estimated Scope**: ~1,750 lines over 3 pull requests (production code, tests and documentation, summed per pull request in "Estimated File Count")
> **Family**: dev-experience. #343 (analyzers) checks the convention this plan defines (its rule ENC014) and #438 owns the analyzer package; ENC014 depends on this plan, and this plan depends on neither.

---

## Summary

Let a class handle a request without implementing `IRequestHandler<TRequest, TResponse>`: a class (static or instance, optionally marked `[Handler]`) whose `Handle` method takes the request first, then any services and a `CancellationToken`, and returns `Task<Either<EncinaError, TResponse>>`. The framework wraps it in an adapter that implements the existing interface, so every pipeline behavior, the module registry and the dispatcher work unchanged.

What exists today (read on `origin/main` at `30dbe89a3`, after `git fetch`):

- Discovery is a reflection scan with a per-assembly cache: `EncinaAssemblyScanner.GetRegistrations(Assembly)` walks `assembly.GetTypes()` and keeps classes that are `IsClass && !IsAbstract` and implement `IRequestHandler<,>`, `INotificationHandler<>` or `IStreamRequestHandler<,>` (`src/Encina/Dispatchers/MediatorAssemblyScanner.cs:23-46`). A static class is `abstract sealed` in reflection, so today's scan skips it by construction: the convention scanner must include static classes explicitly.
- `AddEncina(services, configure, params Assembly[])` registers each handler with `ServiceDescriptor.Describe(serviceType, implementationType, HandlerLifetime)` and `TryAdd` (`src/Encina/Core/ServiceCollectionExtensions.cs:136-143`); `HandlerLifetime` defaults to `Scoped` (`EncinaConfiguration.cs:34`).
- Dispatch resolves `IRequestHandler<TRequest, TResponse>` from the scoped provider (`src/Encina/Core/Encina.cs:168-182`), and `IEncina.Send<TResponse>` takes an `IRequest<TResponse>` (`src/Encina/Abstractions/IEncina.cs:46`), so a request type must implement `IRequest<TResponse>` (or `ICommand<>` / `IQuery<>`, `CqrsContracts.cs:29,60`) to be sent at all. The issue's "commands are POCOs" goal therefore needs a decision on the request side too (Design Choice 2).
- `ModuleHandlerRegistry` maps requests to modules by scanning module assemblies for types implementing `IRequestHandler<,>` (`src/Encina/Modules/ModuleHandlerRegistry.cs:64-90`); a convention handler class implements nothing, so without a change it would fall out of module attribution and module-scoped behaviors (fail open).
- The current mechanism and its cost are recorded in [ADR-005](../architecture/adr/005-reject-source-generators.md): reflection scan, compiled expression delegates, runtime cache; it rejects source generators for handler dispatch. Issue #50 (source generators), which this issue lists as its dependency, was closed as not planned with that ADR as evidence; there is no `Encina.Generators` project in `src/`.
- The handler return type is `Task<Either<EncinaError, TResponse>>` today; [ADR-064](../architecture/adr/064-rop-result-union.md) replaces the carrier in the 1.0 migration (#3662), so the convention names the carrier in one internal type, `ConventionHandlerShape`.

What this plan delivers: the convention, the optional `[Handler]` attribute, an adapter per convention handler built from a compiled expression (parameter injection per call), registration and module attribution, logging and a trace tag, tests and documentation.

- **Affected packages**: `Encina` (core). No new package under Design Choice 1 option A.
- **Provider category**: none (in-process dispatch).
- **Estimated size**: 3 pull requests, see the file count table.
- **Prerequisites**: none blocking. Dilemma 1 decides the mechanism; the rest of this plan assumes the recommended option A.

### Shared Pieces and Ownership (family dev-experience)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| The convention (what is a handler, accepted return shapes, special parameters), `HandlerAttribute` | **This plan (#333)** | #343 (ENC014). #438's ENC001 is defined on interface handlers only (and redefined there as a token parameter never used, because a missing token cannot compile on `IRequestHandler<,>`); it does not read convention handlers |
| Analyzer package and rules | #438 and #343 (sibling plans, by number) | none |
| Core EventIds 10086 to 10089 | **This plan** | none |

---

## Design Choices

<details>
<summary><strong>1. Mechanism — runtime scan and compiled adapter, or a source generator</strong></summary>

The issue asks for a source generator ("zero reflection", "depends on #50"). ADR-005 rejected generators for handler dispatch (Status: Rejected, 2025-12-12) and #50 was closed as obsolete; reopening the question needs a new ADR (the #50 closing comment says so).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend the existing scan: find convention handlers by reflection at startup, build one compiled expression per `Handle` method, cache per assembly, register an adapter implementing `IRequestHandler<,>`** | Consistent with ADR-005 and today's code; no new package or build step; works with every behavior and with `ValidateOnBuild`; about 400 lines | Reflection at startup (the same cost the interface scan already pays); does not meet the issue's "source generator based (zero reflection)" line; not Native AOT friendly (AOT is deferred in ADR-005) |
| **B) A new `Encina.Generators` incremental generator that emits wrapper classes and a registration method** | Meets the issue's acceptance as written; zero reflection; AOT-ready | Needs a new ADR superseding ADR-005 and a new generator package with its tests; build complexity ADR-005 lists as a reason to reject; two discovery paths to keep consistent |
| **C) A, then a generator later as an optimization behind the same convention** | The convention is stable and a generator can replace the scan without user-visible change | Two implementations eventually; the generator's justification still needs an ADR |

### Chosen Option: **A — Runtime scan and compiled adapter** (recommended, pending the maintainer)

### Rationale

- A wins while ADR-005 stands and Native AOT is deferred; B wins if the maintainer reopens ADR-005 because AOT enters 1.0 scope or zero-reflection startup is wanted; C keeps B possible without building it now. The adapter and the convention are identical under every option, only the discovery step differs.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Do request types still implement `IRequest<TResponse>`</strong></summary>

The issue's summary says handlers need no `IRequest<T>`, and says "commands/queries are POCOs"; `IEncina.Send` accepts only `IRequest<TResponse>` (`IEncina.cs:46,77`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Handlers are interface-free; requests still implement `IRequest<TResponse>`, `ICommand<>` or `IQuery<>`** | No change to `IEncina`; the response type is known at compile time; the convention can find the handler by the request's first-parameter type | Domain objects keep a framework interface (the issue's "cleaner domain" benefit is half met) |
| **B) POCO requests: add `Send<TRequest, TResponse>(TRequest, ...)` and register the response type through `[Handler]` or the `Handle` return type** | Fully interface-free domain | Changes `IEncina`, which has exactly two implementers on `origin/main` (`Encina`, `src/Encina/Core/Encina.cs:41`, and the fake `FakeEncina`, `src/Encina.Testing.Fakes/FakeEncina.cs:42`; no interface extends it; counted with `git grep -nE ":\s*IEncina\b" -- src tests`, which returns exactly those two lines: the base-list colon is matched on its own line, so it also finds `Encina`, whose declaration is a multi-line primary constructor ending in `) : IEncina` on line 41, where a single-line `class X ... : IEncina` pattern finds only `FakeEncina`; a grep of the bare type name would also match parameters and doc comments); the larger cost is the pipeline generic constraints `where TRequest : IRequest<TResponse>` (86 constraint lines in 77 files under `src/` and 33 files under `tests/`, counted with `git grep -nE "where TRequest\s*:\s*[^,{]*IRequest<TResponse>"`), which would all change; a large breaking change |

### Chosen Option: **A — Requests keep `IRequest<TResponse>`** (recommended, pending the maintainer)

### Rationale

- A wins when the handler-side boilerplate is the pain (the issue's own example uses `CreateOrderCommand` as the request type and a static handler); B wins if the maintainer wants no framework type in the domain at all and accepts an `IEncina` redesign, which deserves its own plan.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. What is a convention handler and what happens on ambiguity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public class (static or not) is a handler for each public method named `Handle` whose first parameter implements `IRequest<TResponse>` and whose return type is `Task<Either<EncinaError, TResponse>>` or `ValueTask<...>`; `[Handler]` marks a class explicitly and may be used on a class with another name; a class that also implements `IRequestHandler<,>` is not scanned twice** | Precise: the request type decides, so an `ExceptionHandler.Handle(Exception)` is never picked up; several `Handle` overloads for different requests in one class work | A misspelled method name silently makes a class a non-handler (ENC014 and the analyzers of #343 report it) |
| **B) Only classes marked `[Handler]`** | No false discovery | Contradicts the issue's convention "classes ending in `Handler`" |
| **C) Any class ending in `Handler` with any method named `Handle`** | Maximum convenience | Picks up unrelated classes; ambiguous request type |

### Chosen Option: **A — Request-typed `Handle` methods, with or without `[Handler]`** (recommended, pending the maintainer, together with Dilemmas 1 and 2)

### Rationale

- The rule "the first parameter implements `IRequest<TResponse>`" exists only under Dilemma 2 option A, and the whole scan-and-adapter mechanism only under Dilemma 1 option A; under Dilemma 2 option B the first parameter would be any POCO and the rule changes. For that reason this choice goes to the maintainer with them, not as a Trivial entry. B and C remain valid for a team that wants explicit marking or maximum convenience.
- Registration uses the existing rule of `RegisterHandlers` (`src/Encina/Core/ServiceCollectionExtensions.cs:136-143`): `TryAdd`, first registration wins, so an interface handler scanned first keeps its slot; a convention handler that loses a slot is logged (EventId 10087, PR-2) and never silent.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. How `Handle` parameters are supplied</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per-call: request, `CancellationToken` and `IRequestContext` come from the dispatch; every other parameter is resolved from the scope the adapter was resolved in; a parameter that cannot be resolved returns a `Left` with an error code at the call and is logged, never an exception** | Matches the issue's example (`IOrderRepository` injected as a parameter); scoped services work because the adapter is `Scoped` by default; an instance handler's constructor is resolved by the container as today | Under a `Singleton` `HandlerLifetime` the adapter must create a scope per call (see Rationale) |
| **B) Constructor injection only; `Handle` takes the request and the token** | No method injection | Static handlers (the issue's first example) cannot take services |

### Chosen Option: **A — Per-call parameter resolution**

### Rationale

- The adapter is registered with the configured `HandlerLifetime` (Scoped by default). Both constructor shapes exist, as two internal types sharing the compiled delegate: `ConventionHandlerAdapter<TRequest, TResponse>(IServiceProvider provider, ConventionHandlerPlan plan)` for Scoped and Transient, which resolves the parameters from the scope it was resolved in, and, for Singleton, the same adapter built over an `IServiceScopeFactory` that resolves the parameters of each call inside a new scope it disposes after the call, so a scoped service is never captured by a singleton. Registration cannot use `ServiceDescriptor.Describe(serviceType, implementationType, lifetime)` for the adapter, because the adapter takes the plan, which the container cannot construct: it uses a factory descriptor, `ServiceDescriptor.Describe(serviceType, sp => new ConventionHandlerAdapter<TRequest, TResponse>(...), lifetime)`, built through the closed generic type at registration. The adapter class repeats `where TRequest : IRequest<TResponse>`, which `IRequestHandler<TRequest, TResponse>` requires (CS0452 and CS0314 otherwise). The handler instance of a non-static convention handler: the class is not a registered service (only the adapter is), so it is created per call with `ActivatorUtilities.CreateFactory` (an `ObjectFactory` built once per handler type at registration and cached in the `ConventionHandlerPlan`) invoked with the call's scope provider; its constructor dependencies resolve from that scope, and the instance lives for one call (transient semantics, disposed after the call when it is `IDisposable` or `IAsyncDisposable`). The rule: a class that is `static` (abstract and sealed in reflection) is never instantiated; a `static` method on a non-static class is invoked without an instance; an instance method creates one instance per call as above. Under Singleton the same creation happens inside the per-call scope, so the singleton adapter never holds the instance. A constructor parameter the scope cannot supply makes `ActivatorUtilities` throw; the adapter catches that at the single creation site and returns the same `ConventionHandlerParameterUnresolved` `Left` (type name only, never the exception message), because `ValidateOnBuild` cannot see a class that is not registered. The unit tests cover static class, static method on an instance class, instance method, an unresolvable constructor parameter (returns `Left`, no exception) and both lifetimes.
- ADR-054 D4 applies to the trace tag only (Design Choice 5): a tenant tag is emitted when multi-tenancy is on, by the existing pipeline span, not by the adapter.
- **Dependence on Dilemma 1:** the settling rule (parameters are resolved per call, an unresolvable one is a `Left`, a scoped service is never captured by a singleton) holds under every mechanism; the adapter, the plan and the factory descriptor above describe option A of Dilemma 1. Under option B or C the generated wrapper applies the same rule and only the description of the mechanism is rewritten.
- **Class:** Settled
- **Settled by:** the issue's example (`Handle(CreateOrderCommand command, IOrderRepository repository, CancellationToken ct)`), `EncinaConfiguration.cs:34` (default `Scoped`), and AGENTS.md section 3 ("Registration completeness": a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`, which fails on a scoped service captured by a singleton).

</details>

<details>
<summary><strong>5. Module attribution of convention handlers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ModuleHandlerRegistry` also asks the convention finder for the handlers of a module assembly** | Module-scoped behaviors see convention handlers; no fail-open gap | One more edit to `ModuleHandlerRegistry.cs` and its tests |
| **B) Leave modules unchanged; document that convention handlers are not module-aware** | No change | A module-scoped behavior silently skips them: a fail-open gap against AGENTS.md section 3 |

### Chosen Option: **A — The registry uses the finder**

### Rationale

- The finder is internal to core and `ModuleHandlerRegistry` is in the same assembly, so no new public API.
- **Dependence on Dilemma 1:** the settling rule (module attribution must not fail open for a convention handler) holds under every mechanism; "the finder" describes option A of Dilemma 1, and under a generator the registry would read the generated handler table instead.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed"; module isolation is evaluated under section 6, function 11) and `src/Encina/Modules/ModuleHandlerRegistry.cs:64-90`, which maps by implemented interfaces only.

</details>

---

## Implementation Phases

### Phase 1: Convention, finder, adapter and registration (PR-1)

<details>
<summary>Tasks</summary>

1. `src/Encina/Abstractions/HandlerAttribute.cs`: `public sealed class HandlerAttribute : Attribute` (`AttributeUsage(AttributeTargets.Class, Inherited = false)`), XML docs with an example.
2. `src/Encina/Dispatchers/ConventionHandlerShape.cs` (`internal static`): the one place that names the result carrier and the accepted return shapes (`Task<Either<EncinaError,T>>`, `ValueTask<...>`).
3. `src/Encina/Dispatchers/ConventionHandlerFinder.cs` (`internal static`): `Find(Assembly)` returns a `ConventionScanResult` holding `ConventionHandler` records (type, method, request type, response type, parameter plan) and, separately, the skipped candidates with a reason code (unsupported shape, already registered); includes static classes; skips types that implement `IRequestHandler<,>`; caches per assembly like `EncinaAssemblyScanner.Cache`. PR-1 only records the skips in the result; PR-2 logs them (its edit list names every call site), so PR-1 is complete and not silent in effect: the skip is data a test asserts, and an unresolvable parameter at call time is a `Left` the caller sees.
4. `src/Encina/Dispatchers/ConventionHandlerCompiler.cs` (`internal static`): builds one `Func<object?, IServiceProvider, IRequestContext, TRequest, CancellationToken, Task<Either<EncinaError, TResponse>>>` per method with `System.Linq.Expressions`; keeps every method small (CRAP 10).
5. `src/Encina/Dispatchers/ConventionHandlerAdapter.cs` (`internal sealed class ConventionHandlerAdapter<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>`, the constraint is required): constructor `(IServiceProvider provider, ConventionHandlerPlan plan)` for Scoped and Transient, and a second constructor `(IServiceScopeFactory scopeFactory, ConventionHandlerPlan plan)` used under a `Singleton` `HandlerLifetime`, which resolves the parameters and the handler instance of each call in a fresh scope (Design Choice 4). A parameter that cannot be resolved returns `EncinaErrors.Create(EncinaErrorCodes.ConventionHandlerParameterUnresolved, ErrorCategory.Internal, ...)`.
6. Edit `MediatorAssemblyScanner.cs` (`AssemblyScanResult` gains `ConventionHandlerRegistrations`) and `ServiceCollectionExtensions.cs` (`RegisterConventionHandlers`, called in the per-assembly loop after `RegisterHandlers`, `TryAdd` first-wins; the adapter is registered with a factory `ServiceDescriptor` over the closed generic adapter type, Design Choice 4, because `Describe(serviceType, implementationType, lifetime)` cannot build an adapter that takes a plan).
7. Edit `src/Encina/Errors/EncinaErrorCodes.cs`: the constant `ConventionHandlerParameterUnresolved = "encina.handler.parameter_unresolved"`, declared there so the ADR-062 code-to-category rule is checkable in one file (its category is `Internal`, set at the single construction site in the adapter). `src/Encina/PublicAPI.Unshipped.txt` lines for `HandlerAttribute` and the new constant (through `mechanical-fixer`).
8. Tests in `tests/Encina.UnitTests/Dispatchers/`: `ConventionHandlerFinderTests.cs`, `ConventionHandlerAdapterTests.cs` (static and instance handler, parameter injection, a missing service returns `Left`, cancellation), `ConventionHandlerRegistrationTests.cs` (builds the provider with `ValidateOnBuild` and `ValidateScopes`, sends through `IEncina` with a behavior in the pipeline, interface handler wins a slot); `tests/Encina.GuardTests/Dispatchers/ConventionHandlerGuardTests.cs`.
9. `.github/coverage-manifest/Encina.json` entries for the six new files (through `mechanical-fixer`); changelog fragment `changelog.d/333-convention-handlers.added.md` (through `mechanical-fixer`).

File count 15 (the limit): 1 attribute, 4 internal types (shape, finder, compiler, adapter), 3 edits (scanner, extensions, `EncinaErrorCodes.cs`), PublicAPI, 4 tests, manifest, fragment; about 800 lines. The module attribution of Phase 2 follows in the next pull request; the two merge back to back and no release is cut between them, so the convention handlers' module attribution gap (fail-open) never ships. The `HandlerAttribute` guard surface is none (no parameters); the internal types are reachable by tests through `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests`, which `src/Encina/Encina.csproj` already declares, together with `Encina.PropertyTests` and `Encina.ContractTests` (checked on `origin/main`); the benchmark of Phase 2 drives `IEncina.Send` only, so it needs no access to internals and the `.csproj` is not edited.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is a pre-1.0 .NET 10 library. Handlers are discovered by EncinaAssemblyScanner (src/Encina/Dispatchers/MediatorAssemblyScanner.cs) and registered in AddEncina (src/Encina/Core/ServiceCollectionExtensions.cs). Requests implement IRequest<TResponse>. ADR-005 keeps reflection scan plus compiled expressions.
TASK: Add convention-based handlers: HandlerAttribute, ConventionHandlerShape, ConventionHandlerFinder, ConventionHandlerCompiler, ConventionHandlerAdapter, registration and tests as listed in the plan. A convention handler is a public class (static or instance) with a public method named Handle whose first parameter implements IRequest<TResponse> and which returns Task<Either<EncinaError,TResponse>> or ValueTask of it. Special parameters: CancellationToken and IRequestContext from the dispatch, everything else from the scope.
KEY RULES: No exceptions for business logic: an unresolvable parameter is a Left with EncinaErrorCodes.ConventionHandlerParameterUnresolved ("encina.handler.parameter_unresolved", declared in src/Encina/Errors/EncinaErrorCodes.cs) and category Internal (ADR-062), never the exception message in logs. The adapter repeats where TRequest : IRequest<TResponse>; registration uses a factory ServiceDescriptor. Never inject a scoped service into a singleton: with Singleton HandlerLifetime resolve parameters in a per-call scope from IServiceScopeFactory. Static classes are abstract in reflection: include them explicitly. TryAdd, first registration wins. DI test with ValidateOnBuild and ValidateScopes. Keep each method at complexity 8 or less. At most 15 files.
REFERENCE FILES: src/Encina/Dispatchers/MediatorAssemblyScanner.cs, src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina/Core/Encina.cs, src/Encina/Abstractions/IRequestHandler.cs, docs/architecture/adr/005-reject-source-generators.md.
```

</details>

### Phase 2: Modules, observability and EventIds (PR-2)

<details>
<summary>Tasks</summary>

1. Edit `ModuleHandlerRegistry.cs`: `PopulateModuleHandlers` also adds the finder's handlers of the module assembly to `_requestToModuleCache` and `_handlerModuleCache` (keyed by the handler type and the adapter's request/response pair).
2. `src/Encina/Dispatchers/ConventionHandlerLog.cs` (`[LoggerMessage]`, `internal static partial class`): ids inside the registered range `EventIdRanges.Core = (10000, 10099)`: 10086 convention handlers discovered for an assembly (Information; count only), 10087 convention handler skipped because the service type is already registered (Warning; handler type name, request type name), 10088 `Handle` method skipped because its shape is not supported (Warning; type name, reason code), 10089 parameter cannot be resolved at call time (Error; handler type name, parameter type name; never the request). The ids 10077 to 10085 are claimed by #2382 and #132, so this plan starts at 10086. No `EventIdRanges` field, no `AssemblyRanges` change (the `Encina` assembly is already mapped); the claims table of `docs/engineering/eventid-block-registry.md` gains the row `Core | 10086-10089 | #333` (through `docs-writer`).
   The log calls belong to code written in PR-1, so PR-2 edits those sites: `ConventionHandlerAdapter.cs` (10089 at the point where a parameter cannot be resolved), `ServiceCollectionExtensions.cs` (`RegisterConventionHandlers`: 10086 per assembly, and 10087 and 10088 from the skipped list the finder returns; the extension method takes the `ILoggerFactory` it already can resolve, or logs through the service provider after build, whichever the existing `RegisterHandlers` logging path uses) and the span site below. `ConventionHandlerFinder.cs` is not edited (it returns data, PR-1).
3. Trace tag: `ActivityTagNames` gains `HandlerKind` (`encina.handler.kind`, value `convention` or `interface`); the handler span set at `src/Encina/Dispatchers/Encina.RequestDispatcher.cs:106` (where `ActivityTagNames.Handler` is set) also sets it, so that file is edited too. Instrument names follow ADR-054 (lowercase, dot-separated, no `_total`); no new metric is added.
4. Tests: `ModuleHandlerRegistryConventionTests.cs`, `ConventionHandlerLogTests.cs` (fake logger asserts EventId and that the request is never logged), `tests/Encina.PropertyTests/Dispatchers/ConventionHandlerEquivalenceTests.cs` (FsCheck: for random requests the convention adapter and an interface handler with the same body return the same `Either`), `tests/Encina.BenchmarkTests/Dispatchers/ConventionHandlerBenchmarks.cs` (adapter versus interface handler, discovered through `BenchmarkSwitcher`), manifest edit. `ActivityTagNames` is `internal`, so `HandlerKind` needs no PublicAPI line.

File count 14: 5 edits (`ModuleHandlerRegistry.cs`, `ActivityTagNames.cs`, `Encina.RequestDispatcher.cs`, `ConventionHandlerAdapter.cs`, `ServiceCollectionExtensions.cs`), 1 new log class, 4 new test files (module, log, property, benchmark), 2 edits of the PR-1 test files (adapter and registration tests assert the log calls), the manifest, the registry row; about 700 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: PR-1 merged: convention handlers dispatch through ConventionHandlerAdapter. Core EventIds 10077-10085 are claimed by #2382 and #132.
TASK: Make ModuleHandlerRegistry see convention handlers, add ConventionHandlerLog with EventIds 10086-10089 inside EventIdRanges.Core, add the encina.handler.kind tag, and the property and benchmark tests listed in the plan.
KEY RULES: Log by error code or type name only, never EncinaError.Message or request data; exceptions go through ForLogging(). Pack EventIds without gaps. Tenant tag only when multi-tenancy is on (ADR-054 D4), emitted by the existing span. BenchmarkSwitcher.FromAssembly, never BenchmarkRunner.Run<T>. At most 15 files.
REFERENCE FILES: src/Encina/Modules/ModuleHandlerRegistry.cs, src/Encina/Diagnostics/RequestIdentityLog.cs, src/Encina/Diagnostics/ActivityTagNames.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md.
```

</details>

### Phase 3: Documentation and finalization (PR-3)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: `docs/features/convention-handlers.md` (the convention, parameters, lifetime rule, the module note, a migration-free comparison with interface handlers), the `Encina` README section, `docs/INVENTORY.md`, `ROADMAP.md`.
2. If the maintainer chooses option B or C of Design Choice 1, an ADR superseding ADR-005 (number assigned when the ADR pull request opens) and a plan for `Encina.Generators` replace PR-2's scope of discovery; otherwise a short ADR is not needed (the decision follows ADR-005).
3. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test` of the five test projects touched, the per-flag measurement of AGENTS.md section 9.

About 4 files and 250 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Convention-based handlers and their module and logging support are merged.
TASK: Document the convention through docs-writer; run the verification commands; report measured per-flag coverage of every new file against .github/coverage-manifest/Encina.json.
KEY RULES: No hand-typed coverage figures (covref markers). English only. Follow .claude/skills/encina-docs/SKILL.md.
REFERENCE FILES: docs/features/, docs/INVENTORY.md, src/Encina/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance | Where it applies in the feature |
|--------------------------|-----------|---------------------------------|
| ADR-005 (reject source generators) | Fixes the mechanism: reflection scan plus compiled expressions | Design Choice 1 |
| ADR-062 (error category contract) | Category of the new error code | `Internal` for an unresolvable parameter (a configuration fault, not a user input error) |
| ADR-064 (result union) | The carrier changes in the 1.0 migration | `ConventionHandlerShape` is the only place naming it |
| ADR-054 (observability conventions) | Tag and instrument naming, tenant tag rule | `encina.handler.kind` tag; no new instrument |
| Wolverine handler conventions (named in the issue) | Prior art: convention-based discovery, method injection | Design Choices 3 and 4 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaAssemblyScanner` | `src/Encina/Dispatchers/MediatorAssemblyScanner.cs` | Cache pattern and scan result type |
| `AddEncina` and `RegisterHandlers` | `src/Encina/Core/ServiceCollectionExtensions.cs` | Registration, lifetime, `TryAdd` |
| `Encina` request dispatch | `src/Encina/Core/Encina.cs` | Resolves `IRequestHandler<,>`; unchanged |
| `ModuleHandlerRegistry` | `src/Encina/Modules/ModuleHandlerRegistry.cs` | Module attribution |
| `RequestIdentityLog` | `src/Encina/Diagnostics/RequestIdentityLog.cs` | Style and range of core logging |
| Pipeline behaviors | `src/Encina/Pipeline/` | Run unchanged around the adapter |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (existing `EventIdRanges.Core` 10000-10099) | 10086-10089 | Four ids packed without gaps; 10077-10085 are claimed by #2382 (10077-10083) and #132 (10084-10085); no new range, so no new `EventIdRanges` field, `AssemblyRanges` entry or PublicAPI range line |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 12 | attribute, shape, finder, compiler, adapter, log class (6 new); edits to scanner, extensions, `EncinaErrorCodes`, `ModuleHandlerRegistry`, `ActivityTagNames` and `Encina.RequestDispatcher` (6) |
| Tests | 8 | finder, adapter, registration (DI), guard, module, log, property, benchmark (the adapter and registration tests are edited again in PR-2) |
| PublicAPI, manifest, registry row, fragment | 4 | `PublicAPI.Unshipped.txt`, `Encina.json`, registry table, `changelog.d/333-convention-handlers.added.md` |
| Documentation | 4 | feature page, README, INVENTORY, ROADMAP |
| **Total** | **~28 distinct files, 33 file touches over 3 pull requests** | PR-1 15, PR-2 14, PR-3 4 files (the manifest, the extensions, the adapter and the PublicAPI file are touched in more than one); about 800 + 700 + 250 = 1,750 lines in total (each pull request is under 1,500) |

Coverage targets per file (flags unit, guard, property; contract and integration are justified below): `ConventionHandlerFinder.cs` unit 90, guard 30, property 0 (justification: the finder is a reflection walk, its logic is covered by unit tests on fixture assemblies; guard tests reach only the null check of `Find`); `ConventionHandlerCompiler.cs` unit 90, guard 20, property 60 (the equivalence property drives the compiled delegate with random requests); `ConventionHandlerAdapter.cs` unit 95, guard 40 (constructor null checks), property 60; `ConventionHandlerShape.cs` unit 90; `ConventionHandlerLog.cs` unit 80; `HandlerAttribute.cs` unit 100 (no executable logic); the edits to `MediatorAssemblyScanner.cs`, `ServiceCollectionExtensions.cs`, `ModuleHandlerRegistry.cs` keep their current targets and add the new lines to the unit flag. Contract tests: the adapter must satisfy the handler contract the existing handler contract suite checks, which is run against a convention handler instead of a new class (target unchanged). Integration, load: not applicable (in-process; the benchmark covers the hot path).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library (AGENTS.md rules: zero warnings, XML docs, real-code tests, per-flag coverage, ROP with Either). Issue #333 adds convention-based handlers: a class with a Handle method needs no IRequestHandler interface.

IMPLEMENTATION OVERVIEW: Phase 1 convention, finder, compiler, adapter, registration and tests. Phase 2 module attribution, logging (EventIds 10086-10089 inside EventIdRanges.Core), trace tag, property and benchmark tests. Phase 3 documentation through docs-writer. Three pull requests, each at most 15 files and 1500 lines.

KEY PATTERNS: Reflection scan with per-assembly cache; one compiled expression per Handle method; adapter implements IRequestHandler<,> so the pipeline is unchanged; static classes are abstract in reflection; TryAdd first-wins; never a scoped service into a singleton; the result carrier named only in ConventionHandlerShape (ADR-064); log by code or type name only.

REFERENCE FILES: src/Encina/Dispatchers/MediatorAssemblyScanner.cs, src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina/Core/Encina.cs, src/Encina/Modules/ModuleHandlerRegistry.cs, docs/architecture/adr/005-reject-source-generators.md, docs/plans/dsr-implementation-plan-404.md (plan style).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Handler dispatch pattern; the `[Cache]` behavior wraps the adapter like any handler |
| 2 | OpenTelemetry | ✅ | The adapter runs inside the existing handler span; Phase 2 adds the `encina.handler.kind` tag (ADR-054 naming; tenant tag only when multi-tenancy is on, ADR-054 D4, by the existing span) |
| 3 | Structured Logging | ✅ | `ConventionHandlerLog`, EventIds 10086-10089 inside `EventIdRanges.Core`, error codes and type names only |
| 4 | Health Checks | ❌ | In-process dispatch, no dependency to check |
| 5 | Validation | ❌ | Runs through the existing validation behavior around the adapter; nothing to add |
| 6 | Resilience | ❌ | No external calls in the adapter; resilience behaviors wrap it as any handler |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | The transaction behavior wraps the adapter unchanged |
| 9 | Idempotency | ❌ | The inbox behavior keys on the request type (`IIdempotentRequest`), unchanged |
| 10 | Multi-Tenancy | ❌ | `IRequestContext` (with `TenantId`) is a special parameter a handler may take; the adapter adds no tenant logic and a missing tenant context is the existing behaviors' concern |
| 11 | Module Isolation | ✅ | Phase 2: `ModuleHandlerRegistry` attributes convention handlers to their module (Design Choice 5) |
| 12 | Audit Trail | ❌ | Handler invocations are audited by the existing audit behaviors around the adapter |

---

## Next Steps

1. The maintainer answers Dilemmas 1 and 2 when work starts; Phase 1 does not start before Dilemma 1 (option B or C changes Phase 1 into a generator plan with an ADR superseding ADR-005; the number is assigned when the ADR pull request opens).
2. Open the sub-issues for PR-1 to PR-3 and brief Phase 1.
3. Tell the #343 plan owner (ENC014) when the convention is merged.
