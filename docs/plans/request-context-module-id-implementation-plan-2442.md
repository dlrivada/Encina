# Implementation Plan: `ModuleId` on `IRequestContext`, persisted with the context and restored on dispatch

> **Issue**: [#2442](https://github.com/dlrivada/Encina/issues/2442)
> **Type**: Feature
> **Complexity**: Medium-High (7 phases, no store column, 5 pull requests; it changes a core interface with 8 implementers)
> **Estimated Scope**: ~330 lines of production code + ~1,700 lines of tests, manifests and documentation (about 2,000 lines over 5 pull requests; the split table is the authority)
> **Family**: modular monolith. This plan owns the typed module on the request context, its persisted form and its restoration. The module column of the messaging rows is #747 (outbox, saga, scheduled message, dead letter) and #761 (inbox); the persistence of the context with each row is #1164; the module context producer is #352

---

## Summary

Make the module a typed, immutable member of the request context, next to `TenantId`: `IRequestContext.ModuleId` and `IRequestContext.WithModuleId(string?)`. The framework fills it for every dispatch from the module that owns the handler; `PersistedRequestIdentity` carries it with a deferred message; `IRequestContextScopeFactory.RunRestoredAsync` rebuilds it and re-opens the module execution scope when a background processor dispatches, so handlers of a restored message run in the module that wrote it. The issue's model question is answered: Option A (comment of 2026-10-17 on #2442: "Settled by elimination"), with the `IModuleContext` references corrected to the existing `IModuleExecutionContext`.

**State of `origin/main` (`ec129cae5`), verified by reading and grep (adds to the issue text).**

- `IRequestContext` (`src/Encina/Abstractions/IRequestContext.cs`) has `CorrelationId`, `CausationId`, `Identity`, `IdempotencyKey`, `TenantId`, `Timestamp`, `Metadata`, `WithMetadata`, `WithIdempotencyKey` and `WithTenantId`. Implementers (grep over `src` and `tests`): `RequestContext` (`src/Encina/Core/RequestContext.cs`), `IdentityPinnedRequestContext` (`src/Encina.Caching/Behaviors/`) and six test doubles: `BenchmarkRequestContext` (`CacheKeyGeneratorBenchmarks.cs`), the `TestRequestContext` classes in `SoftDeleteQueryFilterBehaviorGuardTests.cs`, `RoutingPipelineMongoDBIntegrationTests.cs`, `CacheKeyGeneratorPropertyTests.cs` and `EventMetadataEnrichmentServiceTests.cs`, and the record `TestRequestContext` in `tests/Encina.TestInfrastructure/PropertyTests/`. 437 test sites use `Substitute.For<IRequestContext>()` and need no edit.
- **A metadata-based module name already exists, and nothing sets it.** `RequestContextModuleExtensions` (`src/Encina/Modules/RequestContextModuleExtensions.cs`) offers `GetModuleName`, `WithModuleName` (two overloads) and `IsInModule` (two overloads) over the metadata key `Encina.ModuleName` (the same key is a `public const` on `ModuleBehaviorAdapter`). It is Option C of the issue, already in the code, untyped and writable by any caller. Its consumers are `RetentionValidationPipelineBehavior.cs` line 299 (`moduleId: context?.GetModuleName()`), `ABACDecisionRecordFactory.cs` line 57, `ModuleTestEntityBuilder.cs` (three sites) and two test files; the plans of #352 and #747 reached the same finding (nothing in the framework sets the key).
- The ambient module is `IModuleExecutionContext.CurrentModule` (`src/Encina/Modules/Isolation/`, an `AsyncLocal` behind a **scoped** registration, `TryAddScoped` in the ADO.NET SQL Server, Dapper SQL Server, EF Core and MongoDB registrations). `IModuleRegistry` and `IModuleHandlerRegistry` are singletons; `AddEncina` registers `NullModuleHandlerRegistry.Instance` with `TryAddSingleton` (`src/Encina/Core/ServiceCollectionExtensions.cs`, line 74), and `ModuleHandlerRegistry.GetModuleName(typeof(IRequestHandler<TRequest, TResponse>))` already answers the owning module of a request type pair from a precomputed cache (`GetModuleName`, lines 102-123).
- The context is resolved before the handler: `Encina.SendCore` calls `AmbientRequestContext.Resolve` (`Encina.cs` line 93), and `RequestDispatcher.ExecuteAsync` enters it as the ambient context before looking up the cached `RequestHandlerBase` wrapper, whose `HandlerServiceType` is the `IRequestHandler<,>` service type. `RequestContext.ForNestedDispatch` copies the parent (`new RequestContext(source)`), nulls the idempotency key and sets the nested marker (`RequestContext.cs`, lines 211-223).
- The persistence and restoration seam exists for the identity: `PersistedRequestIdentity(Kind, ActorId, TenantId, CorrelationId, CausationId)` (`src/Encina/Identity/`), `RequestIdentity.ToPersisted(tenantId, correlationId, causationId)` (line 201), and `IRequestContextScopeFactory.RunRestoredAsync(persisted, source, work, configuredTenantId, ct)` (`RequestContextScopeFactory.Restore.cs`), which validates the row, rebuilds the context with `RequestOrigin.Restored` and, for a `PersistedIdentitySource.External` row, ignores the actor and the tenant ("a row is never a source of authority", `IRequestContextScopeFactory.cs` lines 181-189). No production caller of `RunRestoredAsync` exists yet: #1164 wires the processors. `RequestContextScopeFactory` is a **singleton** (`RequestIdentityServiceCollectionExtensions.cs`, line 73) with the constructor `(accessor, catalog, identityFactory, options, timeProvider, logger?)`, constructed with `new` in 7 test files.
- `IModuleContext` appears outside `docs/plans/` in six files: `AGENTS.md` (section 6 row 11), `.github/ISSUE_TEMPLATE/feature_request.md`, `.opencode/skills/cross-cutting-check/SKILL.md`, `.opencode/agents/encina-review.md`, `docs/architecture/adr/018-cross-cutting-integration-principle.md` and the frozen `docs/engineering/ENGINEERING-HANDBOOK.md`. There is no such type in `src/`.
- #747's recommended source for stamping a message row is the ambient `IModuleExecutionContext` (its D1, pending the maintainer), and its restoration wraps the processors' dispatch with `MessagingModuleScope` (its D6). This plan does not change that; it adds the context-level path that #1164 uses once it persists the context.

**Not in scope**: the module column of the messaging rows (#747, #761); persisting the context with each row and wiring the processors to `RunRestoredAsync` (#1164); per-handler module stamping of notifications (Design Choice 1; the module event bus is #358); module attribution on audit records (#1636); a typed `IModuleContext` abstraction (Design Choice 6); OpenTelemetry baggage (ADR-054 D4 forbids it).

**Affected packages**: `Encina` (interface, `RequestContext`, dispatcher, persisted record, scope factory, log, EventIds), `Encina.Caching` (`IdentityPinnedRequestContext`), `Encina.Compliance.Retention` and `Encina.Security.ABAC` (move from the metadata accessor to `ModuleId`), documentation files. **Provider category**: none (the field and the restoration are provider independent; the stores that persist it are #747, #761 and #1164, with their own 10-provider plans).

---

## Design Choices

<details>
<summary><strong>1. Who sets `ModuleId` on a live request (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The dispatcher stamps it: for `Send` and `Stream`, after the context is resolved and before it becomes the ambient context, `ModuleId` is set to the module that owns the handler of that request type (`IModuleHandlerRegistry.GetModuleName(typeof(IRequestHandler<,>)...)`, a cached lookup), or to `null` for a host handler; it is overwritten on every dispatch, so it is never inherited from the caller and never taken from an explicit context. `Publish` keeps the module of the dispatching context (the publisher's)** | The pipeline context, the ambient accessor, every behavior and the handler see the same truthful value; a caller cannot declare itself another module; consumers that read the field work with no module pipeline behavior | One registry lookup per dispatch when modules are registered (skipped for `NullModuleHandlerRegistry`); the `Encina` constructor gains an optional parameter; notification handlers of several modules share the publisher's value |
| **B) The module pipeline behavior of #352 sets the ambient accessor context (`accessor.RequestContext = context.WithModuleId(name)`) for the duration of `nextStep`** | No change to the dispatcher | The `context` parameter handed to behaviors and the handler is a different object from the ambient one and still has `null`; two truths in one request |
| **C) Not populated for live requests: the field is `null` unless restored or set with `WithModuleId` by the caller** | Smallest change | Contradicts the issue ("populated from the module resolved for the request"); persistence would need the ambient module read separately, which is Option B of the issue |

### Chosen Option: **A — the dispatcher stamps the owning module** (recommended, pending the maintainer)

### Rationale

A wins when every reader (behaviors, handlers, persistence) must see one value that no caller can forge, which is the property the compliance consumers (`RetentionValidationPipelineBehavior`, `ABACDecisionRecordFactory`) and a persisted snapshot need. B wins if the maintainer does not want the dispatcher to know about modules and accepts that only ambient readers see the module. C wins for an application that never persists the context and wants no cost. The value is the registered name as `IModule.Name` writes it (the same string `IModuleExecutionContext.CurrentModule` carries), compared case-insensitively like `ModuleRegistry`; it is never lower-cased here (the lower-invariant stored form of #747's column is that plan's concern). A per-handler value for notifications waits for the module event bus (#358): publishing from a handler of module M runs every notification handler with `ModuleId = M`, and the handlers' own module scope stays what #352's behavior opens.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How the singleton scope factory enters the module scope (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RequestContextScopeFactory` takes optional `IServiceScopeFactory? scopeFactory` and `IModuleRegistry? modules`; for a restored message with a module it opens a DI scope for the duration of `work`, resolves the scoped `IModuleExecutionContext` from it, and calls `CreateScope(canonicalName)`** | No scoped service is injected into a singleton (`ValidateScopes` stays green); a test double of `IModuleExecutionContext` is honored | One short-lived DI scope per restored message that has a module |
| **B) Make `IModuleExecutionContext` a singleton and inject it** | No extra scope | Changes four packages' `TryAddScoped` registrations; the interface was designed per scope |
| **C) A static helper that sets the `AsyncLocal` of `ModuleExecutionContext` directly** | No DI | Bypasses the registered abstraction; a substitute context sees nothing |

### Chosen Option: **A — a DI scope per restored message** (recommended, pending the maintainer)

### Rationale

A wins because the registration is scoped and the factory is a singleton: a scoped service injected into a singleton fails the `ValidateScopes` registration test that `AGENTS.md` section 3 (registration completeness) requires. B wins if a later plan makes the module context a singleton for other reasons. C wins only for the lowest possible overhead. With no `IModuleRegistry` and no `IServiceScopeFactory` registered nothing is restored and the module of the row is ignored (the module system is not in use).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The metadata accessor is removed, not kept beside the field</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete `GetModuleName` and both `WithModuleName` overloads and the `ModuleNameKey` constant; keep `IsInModule` (two overloads) as extension methods that read `ModuleId`; move the five consumers to `context.ModuleId`** | One storage of the module; no writable untyped duplicate; the consumers start receiving a value | Public API removal (pre-1.0) |
| **B) Keep the metadata accessor and fill the key from the field** | Nothing to migrate | Two representations of one fact, one forgeable (a compatibility layer) |

### Chosen Option: **A — one storage, the typed field**

### Rationale

Pre-1.0 has no compatibility obligation and the rule is to change something completely, never to keep an alias.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (pre-1.0, no backward compatibility) and section 3 (NEVER add legacy code, compatibility aliases or compatibility layers); the issue's Option C ("untyped, easy to misspell, no compile-time contract, inconsistent with `TenantId`").

</details>

<details>
<summary><strong>4. A persisted module from an external source is never restored</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) For `PersistedIdentitySource.External` the row's `ModuleId` is ignored: the context has `ModuleId = null` and no module scope is opened; for internal sources it is restored (Design Choice 5)** | A webhook or queue row cannot claim a module; same rule as actor and tenant | An inbound message handled by a module handler gets its module from the dispatcher (Design Choice 1), not from the row |
| **B) Trust the row's module for every source** | One rule | A forged value from outside becomes the ambient module |

### Chosen Option: **A — external rows never name the module**

### Rationale

`RunRestoredAsync` already treats the row as untrusted for external messages: it ignores the actor and the tenant and uses only the configured tenant (`IRequestContextScopeFactory.cs` lines 181-189, `OpenRestored`: `external ? RequestIdentity.Anonymous : RestoredIdentity(persisted)`).

**Class:** Settled
**Settled by:** `src/Encina/Identity/IRequestContextScopeFactory.cs` lines 181-189 and `src/Encina/Identity/RequestContextScopeFactory.Restore.cs` (`OpenRestored`), the rule "a row is never a source of authority", and SPEC-002 REQ-015.

</details>

<details>
<summary><strong>5. A persisted module that is not registered (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: for an internal row whose `ModuleId` is not in `IModuleRegistry`, `RunRestoredAsync` returns the existing `encina.identity.invalid_persisted_identity` with reason `moduleId` and the message does not run (no new error code; the code keeps the category the ADR-062 migration gives it); the module scope is created with the registered canonical name, not the stored text** | A forged or stale value cannot become the ambient module; the failure goes through the existing retry and dead letter handling of the processor | A removed module blocks its old rows until they are dead lettered |
| **B) Run the message with `ModuleId = null` and log** | Nothing blocks | The message runs as a host call and escapes module boundary rules |
| **C) Run it in the stored module without checking the registry** | Simplest | A corrupted value becomes the ambient module |

### Chosen Option: **A — fail closed** (recommended, pending the maintainer)

### Rationale

A wins when module isolation must hold for background work (`AGENTS.md` section 3: fail closed on missing or invalid context, a `Left` fails the operation). B wins if module removal during the life of persisted rows is a real operating case. This is the same question as #747's D6; the two plans give the same recommendation, and if the maintainer decides differently in one, the other follows. A module system that is not registered (no `IModuleRegistry`) restores nothing and ignores the field. A restored row without a module (`null`) runs with `ModuleId = null` and no scope: that is a host row.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Naming: correct the references, add no second abstraction</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Correct `IModuleContext` to `IModuleExecutionContext` in `AGENTS.md`, the issue template, the two `.opencode` files and ADR-018; add no type** | One abstraction; matches the code | A documentation-only pull request |
| **B) Introduce `IModuleContext` as a typed abstraction over the name and the registry** | Matches the documents | A second abstraction for the same ambient value with no new behavior |

### Chosen Option: **A — correct the documents**

### Rationale

The maintainer-side answer recorded on the issue ("Settled by elimination", 2026-10-17) is Option A with the correction of the references. The frozen `docs/engineering/ENGINEERING-HANDBOOK.md` is not edited (it is a snapshot).

**Class:** Settled
**Settled by:** the comment of 2026-10-17 on #2442 ("correct the `IModuleContext` references in AGENTS.md and templates to the existing `IModuleExecutionContext` instead of adding a second abstraction") and `AGENTS.md` section 1 (clean architecture, every line serves a current purpose).

</details>

<details>
<summary><strong>7. Telemetry and logs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the tag `module.name` on the existing dispatch activity when `ModuleId` is set; three `[LoggerMessage]` methods only for the restoration path (no per-dispatch log); no new metric** | One vocabulary; no hot-path logging | None |
| **B) A new counter for stamped dispatches** | A number | A per-request counter for a lookup; `ModuleMetrics.RecordDispatch` already counts dispatches per module |

### Chosen Option: **A — the existing tag and three restoration logs**

### Rationale

`module.name` is the module tag of `ModuleActivitySource`; the tenant tag appears only where a tenant exists (ADR-054 D4).

**Class:** Trivial
**Settled by:** `src/Encina/Modules/Diagnostics/ModuleActivitySource.cs` (tag `module.name`), `src/Encina/Modules/Diagnostics/ModuleMetrics.cs` (`RecordDispatch`) and [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4.

</details>

---

## Implementation Phases

### Phase 1: The member and its implementers (pull request 1)

> **Goal**: the typed field exists everywhere `IRequestContext` is implemented; the build is whole.

<details>
<summary>Tasks</summary>

1. `src/Encina/Abstractions/IRequestContext.cs`: `string? ModuleId { get; }` (XML docs: the registered name of the module that owns the handler of this dispatch, `null` for host work; set by the dispatcher, restored by `RunRestoredAsync`, never read from a message) and `IRequestContext WithModuleId(string? moduleId)` (immutable copy, like `WithTenantId`; a blank value is stored as `null`).
2. `src/Encina/Core/RequestContext.cs`: `public string? ModuleId { get; init; }`; the private copy constructor and `CopyOf` copy it; `WithModuleId`; `ForNestedDispatch` keeps the parent's value (the dispatcher overwrites it, Phase 2); `ToString()` prints it.
3. `src/Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs`: delegates `ModuleId` to the inner context and `WithModuleId` to a new pinned copy, like `WithTenantId`.
4. The six test doubles (listed in the Summary) gain the member; each is a few lines.
5. Tests: unit (`RequestContextModuleIdTests`: default null, `WithModuleId` copies and keeps `TenantId` independent, blank becomes null, copies and nested dispatch keep the value, the inner object is not mutated), guard (`WithModuleId(null)` is a valid-result test), contract (`IRequestContextContractTests` edit: every implementer returns a new instance and keeps `TenantId` and `ModuleId` independent).
6. `PublicAPI.Unshipped.txt` of `Encina` and the manifest `Encina.json` (through `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #2442; plan docs/plans/request-context-module-id-implementation-plan-2442.md. D1-D3 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no default interface members, no [Obsolete]. An interface change lands together with every implementer in one pull request.

TASK:
Add IRequestContext.ModuleId and WithModuleId and update RequestContext, IdentityPinnedRequestContext and the six test doubles, with their tests.

KEY RULES:
- Immutable pattern: WithModuleId returns a new instance and never mutates the original.
- XML docs on every public member; zero warnings; PublicAPI.Unshipped.txt updated.
- Grep for implementers again at work start (class|record ... : IRequestContext) and list every one.

REFERENCE FILES:
- src/Encina/Abstractions/IRequestContext.cs
- src/Encina/Core/RequestContext.cs
- src/Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs
- tests/Encina.TestInfrastructure/PropertyTests/TestRequestContext.cs
```

</details>

---

### Phase 2: Stamping in the dispatcher (pull request 2)

> **Goal**: every `Send` and `Stream` dispatch carries the owning module.

<details>
<summary>Tasks</summary>

1. `src/Encina/Core/Encina.cs`: optional constructor parameter `IModuleHandlerRegistry? moduleHandlerRegistry = null` (default `NullModuleHandlerRegistry.Instance`); `RequestDispatcher.ExecuteAsync` looks up the cached `RequestHandlerBase` wrapper before it enters the ambient context, computes `moduleId = registry.GetModuleName(wrapper.HandlerServiceType)` (skipped when the registry is the null instance), and when it differs from `context.ModuleId` replaces the context by `RequestContext.CopyOf(context).WithModuleId(moduleId)` (an `IRequestContext` that is not a `RequestContext` is copied once, as the explicit-context rule already does); `src/Encina/Core/StreamDispatcher.cs` does the same for streams. `Publish` is unchanged.
2. The dispatch activity gets the tag `module.name` when `moduleId` is set (`EncinaDiagnostics.SendStarted` takes an optional module name).
3. DI: `IModuleHandlerRegistry` is already registered by `AddEncina` (`TryAddSingleton`, line 74) and replaced by `AddEncinaModules`; `Encina` is built by DI, so the optional parameter needs no registration. The DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` with and without `AddEncinaModules`.
4. Tests: unit (a request handled in module A sees `context.ModuleId == "A"` in a behavior and in the handler and in the ambient accessor; a host handler sees `null`; an explicit context carrying another module is overwritten; a nested dispatch to module B sees B and the outer one still sees A after it returns; no registered modules means no copy: the same instance is used), guard (the optional parameter `null` is valid, a valid-result test), property (stamping is idempotent and never changes `TenantId`, `CorrelationId` or the identity), contract (`IEncina` dispatch gives the same value through `Send`, `Stream` and a nested `Send`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #2442. Phase 1 is merged. The dispatcher enters the ambient context at the top of RequestDispatcher.ExecuteAsync (src/Encina/Dispatchers/Encina.RequestDispatcher.cs); the stamped context must be the one that becomes ambient.

TASK:
Stamp ModuleId in the Send and Stream dispatchers from IModuleHandlerRegistry and test it.

KEY RULES:
- Hot path: no allocation and no lookup when the registry is NullModuleHandlerRegistry; a cached lookup otherwise.
- The value is overwritten on every dispatch; never inherited from a caller or an explicit context.
- Never claim atomicity or a transaction; no scoped service injected into a singleton.

REFERENCE FILES:
- src/Encina/Core/Encina.cs
- src/Encina/Dispatchers/Encina.RequestDispatcher.cs
- src/Encina/Core/StreamDispatcher.cs
- src/Encina/Modules/ModuleHandlerRegistry.cs
- tests/Encina.UnitTests/Core/AmbientRequestContextTests.cs
```

</details>

---

### Phase 3: Replace the metadata accessor (pull request 3)

> **Goal**: one storage of the module.

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/RequestContextModuleExtensions.cs`: remove `GetModuleName` and both `WithModuleName` overloads; `IsInModule(context, name)` and `IsInModule<TModule>(context, module)` compare `context.ModuleId` case-insensitively; remove `ModuleBehaviorAdapter.ModuleNameKey` (`src/Encina/Modules/ModuleBehaviorAdapter.cs` line 36) and the doc text of `IModulePipelineBehavior` that names the key.
2. Consumers: `src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs` line 299 and `src/Encina.Security.ABAC/DecisionAudit/ABACDecisionRecordFactory.cs` line 57 read `context.ModuleId`; `tests/Encina.TestInfrastructure/Builders/ModuleTestEntityBuilder.cs` (three sites), `tests/Encina.ContractTests/Compliance/Retention/RetentionValidationPipelineBehaviorContractTests.cs` line 240 and `tests/Encina.UnitTests/Security/ABAC/DecisionAudit/ABACPipelineDecisionAuditTests.cs` line 1061 move to `WithModuleId`.
3. Tests: unit (`RequestContextModuleExtensionsTests` for the two `IsInModule` overloads, case-insensitive, `null` module id is not in any module), guard (null context and blank module name throw), and the consumers' existing tests prove the new source.
4. `PublicAPI.Unshipped.txt` (remove five lines, one `IsInModule` line stays), manifests of `Encina`, `Encina.Compliance.Retention` and `Encina.Security.ABAC` (through `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #2442. The typed field exists (Phase 1) and is filled by the dispatcher (Phase 2). Design Choice 3: no alias, no second storage.

TASK:
Remove the metadata accessor, keep IsInModule over the field, and migrate every consumer found by grep (GetModuleName(), WithModuleName(, ModuleNameKey, Encina.ModuleName).

KEY RULES:
- Grep again at work start; the list in the plan is the minimum.
- A removed public member is removed from PublicAPI.Unshipped.txt (RS0017) and from the docs that name it.

REFERENCE FILES:
- src/Encina/Modules/RequestContextModuleExtensions.cs
- src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionRecordFactory.cs
```

</details>

---

### Phase 4: Persist and restore (pull request 4)

> **Goal**: the module travels with a deferred message and is restored on dispatch.

<details>
<summary>Tasks</summary>

1. `src/Encina/Identity/PersistedRequestIdentity.cs`: optional trailing parameter `string? ModuleId = null` (named-argument callers compile unchanged); `RequestIdentity.ToPersisted(tenantId, correlationId, causationId, moduleId = null)` (line 201); `ToString()` stays kind-only.
2. `src/Encina/Identity/RequestContextScopeFactory.cs`: optional constructor parameters `IServiceScopeFactory? scopeFactory = null, IModuleRegistry? modules = null` (7 test constructions keep compiling).
3. `RequestContextScopeFactory.Restore.cs`: for an internal source, `ActorProblem` gains the module check (a non-null `ModuleId` that is blank, longer than 128 characters or not in `IModuleRegistry` returns reason `moduleId`, Design Choice 5); `OpenRestored` sets `WithModuleId(canonicalName)` on the rebuilt context and wraps `work` in a DI scope that resolves `IModuleExecutionContext` and opens `CreateScope(canonicalName)` (Design Choice 2); an `External` row sets nothing (Design Choice 4); with no registry the field is ignored.
4. `IRequestContextScopeFactory.cs`: the remarks of `RunRestoredAsync` describe the module rule.
5. Logs: `src/Encina/Identity/RequestContextModuleLog.cs` (`[LoggerMessage]`): EventIds 7710 module restored for a message (Debug, no module name from the row), 7711 module of an external row ignored (Debug), 7712 persisted module refused (Warning, reason code only). Range `EventIdRanges.RequestContextModule = (7710, 7719)` registered first in `src/Encina/Diagnostics/EventIdRanges.cs` and in the `Encina` entry of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
6. Tests: unit (`RunRestoredModuleTests`: restored module visible as `context.ModuleId` and as `CurrentModule` inside `work` and gone after it, the scope is disposed when `work` returns a `Left` or throws, an unknown module returns `invalid_persisted_identity` with reason `moduleId` and `work` never runs, an external row ignores the module, no registry ignores the field, a `null` module runs as a host row), guard (the two new constructor parameters `null` are valid-result tests; `ToPersisted` with a blank module), contract (`RequestContextScopeFactoryContractTests` edit: `ToPersisted` then `RunRestoredAsync` round-trips the module), integration (`tests/Encina.IntegrationTests/Modules/ModuleRequestContextIntegrationTests.cs`: a host with `AddEncinaModules` and two modules; a request handled by module A writes `ToPersisted(context)`, a processor restores it and the handler observes module A; no database is involved: the stores that persist the record are #747, #761 and #1164, which carry the real-database tests of the 10 providers and Marten).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #2442. Phases 1-3 are merged. RunRestoredAsync has no production caller yet (#1164 adds them); tests call it directly.

TASK:
Add ModuleId to the persisted record, validate and restore it in RunRestoredAsync, and add the log class and the range.

KEY RULES:
- Fail closed: an unregistered module on an internal row means the message does not run (D3).
- RequestContextScopeFactory is a singleton: never inject IModuleExecutionContext; open a DI scope per restoration (D2).
- Logs carry reason codes only, never the row's module text; never EncinaError.Message.
- Register the EventId range first; ids packed from 7710 with no gaps; run the architecture tests.

REFERENCE FILES:
- src/Encina/Identity/RequestContextScopeFactory.Restore.cs
- src/Encina/Identity/PersistedRequestIdentity.cs
- src/Encina/Identity/IRequestContextScopeFactory.cs
- tests/Encina.UnitTests/Core/Identity/RunRestoredAsyncTests.cs
- src/Encina/Modules/Isolation/ModuleExecutionContext.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration (pull requests 2 and 4)

<details>
<summary>Tasks</summary>

1. Module isolation (function 11): Phases 1 to 4.
2. Multi-tenancy (function 10): `ModuleId` and `TenantId` are independent members persisted together; no tenant-qualified identifier is built, so the reserved `default` tenant segment and the injectivity rule do not apply.
3. Observability: the `module.name` tag of Design Choice 7 (pull request 2) and the log class (pull request 4).
4. Deferred, as an issue file in this plan's worktree: `artifacts/issues/notification-handler-module-stamping.md` (per-handler `ModuleId` for notifications that several modules handle, with the module event bus of #358).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #2442; the cross-cutting matrix of the plan is the contract.

TASK:
Verify that each Include row of the matrix exists in code and tests and that the deferred issue file is written.

KEY RULES:
- ADR-054 D4: tenant tag only where a tenant exists; no baggage.
- Never log the persisted module text of a row.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 6: Testing (all pull requests)

<details>
<summary>Tasks</summary>

Tests travel with the pull request that adds the code they cover.

1. **Unit, Guard, Contract, Property, Integration**: as listed in Phases 1 to 4. **Load** and **Benchmark**: justification files `tests/Encina.LoadTests/Core/RequestContextModuleId.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/RequestContextModuleId.md` (the dispatch path adds one cached lookup that is skipped when no module system is registered; the existing dispatcher benchmarks cover the path), written in pull request 2.
2. **Per-file targets** (written into `.github/coverage-manifest/Encina.json` and the consumer manifests with one-sentence justifications; baselines of edited files are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; a target is the measured value or higher, never lower):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `Core/RequestContext.cs` (edit) | 95 | 80 | 80 | 70 | 0 | The new member and copies are pure; contract proves every implementer; integration 0 because the real-database proof of persistence belongs to #747, #761 and #1164 |
   | `Abstractions/IRequestContext.cs` | 0 | 0 | 0 | 0 | 0 | An interface has no executable code |
   | `Core/Encina.cs`, `Dispatchers/Encina.RequestDispatcher.cs`, `Core/StreamDispatcher.cs` (edits) | 90 | 80 | 80 | 60 | 60 | Stamping runs in dispatcher unit tests; the module host test runs the real pipeline |
   | `Identity/PersistedRequestIdentity.cs` (edit) | 95 | 0 | 80 | 60 | 0 | A record: guard 0, it has no checked parameter (`ModuleId` is nullable by design) |
   | `Identity/RequestContextScopeFactory.Restore.cs`, `RequestContextScopeFactory.cs` (edits) | 95 | 80 | 80 | 60 | 80 | Every restoration branch has a unit test; the host test restores through a real provider |
   | `Identity/RequestContextModuleLog.cs` | 90 | 0 | 0 | 0 | 0 | Source-generated log methods reached through the restoration tests; no parameterized public member |
   | `Modules/RequestContextModuleExtensions.cs` (edit) | 95 | 90 | 0 | 0 | 0 | Two pure readers; guard covers null context and blank name |
   | `Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs` (edit) | 90 | 0 | 80 | 0 | 0 | Delegation reached through the pinned-context tests; guard 0 because the type is internal without `InternalsVisibleTo` for `Encina.GuardTests` |

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #2442. Tests travel with the pull request that adds the code they cover; coverage is per flag (AGENTS.md section 9).

TASK:
Write the tests of Phase 6 and fill the manifest targets with one-sentence justifications.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; never swallow an exception thrown by the code under test.
- A nullable parameter that accepts null gets a valid-result test, not an exception test.
- Targets never go down.

REFERENCE FILES:
- tests/Encina.UnitTests/Core/Identity/RunRestoredAsyncTests.cs
- tests/Encina.UnitTests/Core/NestedDispatchContextTests.cs
- .github/coverage-manifest/Encina.json
```

</details>

---

### Phase 7: Documentation & Finalization (pull requests 2 and 5)

<details>
<summary>Tasks</summary>

1. XML docs on every new public member; `PublicAPI.Unshipped.txt` of `Encina` (through `mechanical-fixer`).
2. Pull request 5 (documents only): `AGENTS.md` section 6 row 11, `.github/ISSUE_TEMPLATE/feature_request.md`, `.opencode/skills/cross-cutting-check/SKILL.md`, `.opencode/agents/encina-review.md` and ADR-018 read `IModuleExecutionContext` instead of `IModuleContext` (through `docs-writer` and `mechanical-fixer`; the frozen handbook is untouched); `docs/features/module-isolation.md` gains "The module on the request context"; `docs/INVENTORY.md` line; changelog fragment `changelog.d/2442-request-context-module-id.added.md` (through `mechanical-fixer`).
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target; CRAP of every changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #2442 is implemented and tested.

TASK:
Write the documentation set through the specialists that own them.

KEY RULES:
- Pages go through docs-writer; the fragment and PublicAPI lines through mechanical-fixer; never edit CHANGELOG.md or the frozen handbook; coverage figures are cited with covref markers, never typed.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
- docs/features/module-isolation.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Order rule: the interface and its implementers land together (PR 1); every later pull request builds and carries its own tests, `PublicAPI.Unshipped.txt` and manifest entries.

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | `IRequestContext`, `RequestContext`, `IdentityPinnedRequestContext`, 6 test doubles, `PublicAPI.Unshipped.txt`, `Encina.json` manifest, unit test, guard test, contract test edit | 14 | 650 |
| 2 | `Encina.cs`, `Encina.RequestDispatcher.cs`, `StreamDispatcher.cs`, `EncinaDiagnostics` (tag), `PublicAPI.Unshipped.txt`, manifest, dispatcher unit and property tests, DI test, `EncinaGuardTests` edit, 2 justification files | 12 | 700 |
| 3 | `RequestContextModuleExtensions`, `ModuleBehaviorAdapter`, `IModulePipelineBehavior` docs, Retention behavior, ABAC factory, `ModuleTestEntityBuilder`, 2 consumer test edits, extension unit and guard tests, `PublicAPI.Unshipped.txt`, manifests x2 (`Encina.json` and the consumer ones counted as 2) | 14 | 500 |
| 4 | `PersistedRequestIdentity`, `RequestIdentity`, `RequestContextScopeFactory` and `.Restore`, `IRequestContextScopeFactory` docs, `RequestContextModuleLog`, `EventIdRanges`, allocation test map, `PublicAPI.Unshipped.txt`, manifest, `RunRestoredModuleTests`, guard edit, contract edit, integration test | 15 | 1,200 |
| 5 | `AGENTS.md`, issue template, 2 `.opencode` files, ADR-018, `module-isolation.md`, inventory, changelog fragment | 8 | 150 |

The recount: PR 1 = 3 production + 6 doubles + 1 PublicAPI + 1 manifest + 3 tests (unit, guard, contract edit) = 14. PR 3 = 5 production (`RequestContextModuleExtensions`, `ModuleBehaviorAdapter`, `IModulePipelineBehavior`, Retention behavior, ABAC factory) + 1 test builder + 2 test edits + 2 new tests + 1 PublicAPI + 3 manifests (`Encina.json`, Retention, ABAC) = 14. PR 4 = 5 production (`PersistedRequestIdentity`, `RequestIdentity`, `RequestContextScopeFactory`, `.Restore`, `IRequestContextScopeFactory`) + 3 registry and log (`RequestContextModuleLog`, `EventIdRanges`, allocation test map) + 1 PublicAPI + 1 manifest + 5 tests = 15. When a measured pull request passes 15 files or 1,500 lines it is split again (PR 4's integration test moves to its own pull request).

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` section 3 | Fail closed on missing or invalid context; no compatibility layers; `EncinaError.Message` never logged | D3, Design Choice 3, logs |
| `AGENTS.md` section 6, function 11 | `ModuleId` and the module dimension | The feature itself |
| SPEC-002 REQ-015 | A deferred message restores its originating actor; a row is never a source of authority | Design Choice 4 |
| [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4 | Tenant tag only where a tenant exists; no baggage | Tags |
| [ADR-062](../architecture/adr/062-error-category-contract.md) | Category on every new error code | No new code: the existing `encina.identity.invalid_persisted_identity` is reused |
| [ADR-050](../architecture/adr/index.md) | Closed list of module dependencies | Not changed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext`, `RequestContext` | `src/Encina/Abstractions/`, `src/Encina/Core/` | The member |
| `AmbientRequestContext`, `RequestDispatcher`, `StreamDispatcher` | `src/Encina/Core/`, `src/Encina/Dispatchers/` | Where the context is resolved and entered |
| `IModuleHandlerRegistry` / `ModuleHandlerRegistry` / `NullModuleHandlerRegistry` | `src/Encina/Modules/` | Owning module of a request type |
| `IModuleRegistry`, `IModuleExecutionContext.CreateScope` | `src/Encina/Modules/`, `src/Encina/Modules/Isolation/` | Validate and re-open the module scope |
| `PersistedRequestIdentity`, `RunRestoredAsync`, `PersistedIdentitySource` | `src/Encina/Identity/` | The persistence and restoration seam |
| `RequestContextModuleExtensions` | `src/Encina/Modules/RequestContextModuleExtensions.cs` | Replaced |
| `ModuleActivitySource`, `ModuleMetrics` | `src/Encina/Modules/Diagnostics/` | Tag vocabulary |
| `EventIdRanges`, allocation test | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Range registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`RequestContextModule`, new) | 7710-7719, ids 7710-7712 used | From the assigned block 7700-7749 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 7713-7719 free; the allocation test map gains the range under `Encina`. Other plans of the family: #761 7700-7709, #753 7720-7729 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 16 | interface, `RequestContext`, pinned context, dispatchers (3), diagnostics, persisted record, identity (3), log, extensions, adapter, 2 consumers |
| Registry, PublicAPI, manifests | 10 | `EventIdRanges`, allocation map, PublicAPI x4 touches, manifests x4 touches |
| Tests | 22 | 6 doubles, unit (4), guard (3), contract (2), property, DI, integration, 2 consumer edits, builder, 2 justification files |
| Documentation | 6 | `AGENTS.md`, template, 2 `.opencode` files, ADR-018, guide; plus inventory and changelog counted in PR 5 |
| **Total** | **63 file touches across 5 pull requests** | The sum of the rows of the split table is the authority (14 + 12 + 14 + 15 + 8 = 63); a file edited in several pull requests counts in each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers, no default interface members.
- Issue #2442: IRequestContext.ModuleId and WithModuleId, set by the dispatcher, persisted in PersistedRequestIdentity and restored by RunRestoredAsync. Plan: docs/plans/request-context-module-id-implementation-plan-2442.md; D1-D3 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (5 pull requests, every one builds):
1. The member, RequestContext, IdentityPinnedRequestContext and the six test doubles (Phase 1).
2. Stamping in the Send and Stream dispatchers (Phase 2).
3. Remove the metadata accessor and migrate its consumers (Phase 3).
4. Persisted record, validation and restoration, log class and range (Phase 4).
5. Documentation: IModuleContext becomes IModuleExecutionContext (Phase 7).

KEY PATTERNS:
- ModuleId is the registered module name, overwritten on every dispatch from IModuleHandlerRegistry; never taken from a caller, an explicit context or an external row.
- Restoration opens a DI scope from the singleton factory to reach the scoped IModuleExecutionContext; an unregistered module on an internal row fails closed with invalid_persisted_identity (reason moduleId).
- Logs carry reason codes only; telemetry reuses the module.name tag; no baggage.
- Tests: Shouldly, real implementations, per-flag coverage from the manifests; real-database proof of persistence belongs to #747, #761 and #1164.

REFERENCE FILES:
- src/Encina/Abstractions/IRequestContext.cs
- src/Encina/Core/RequestContext.cs
- src/Encina/Dispatchers/Encina.RequestDispatcher.cs
- src/Encina/Identity/RequestContextScopeFactory.Restore.cs
- src/Encina/Modules/ModuleHandlerRegistry.cs
- src/Encina/Modules/RequestContextModuleExtensions.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the module dimension of cache keys is #746; `IdentityPinnedRequestContext` only delegates the new member |
| 2 | OpenTelemetry | ✅ | Phase 2: `module.name` tag on the dispatch activity when the module is set; no baggage (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 4: `RequestContextModuleLog`, EventIds 7710-7712, reason codes only |
| 4 | Health Checks | ❌ | Not applicable: no new dependency to check |
| 5 | Validation | ✅ | Phase 4: a persisted module is validated (non-blank, at most 128 characters, registered) before it is restored |
| 6 | Resilience | ✅ | A refused restoration is a `Left` that the processor's existing retry and dead letter handling takes; no new external call |
| 7 | Distributed Locks | ❌ | Not applicable: no shared mutable state |
| 8 | Transactions | ❌ | Not applicable: the field is read and written in memory; commit ownership of the rows is #718, #720 and #1236 |
| 9 | Idempotency | ❌ | Not applicable: the module-scoped inbox key is #761 |
| 10 | Multi-Tenancy | ✅ | Independent of `TenantId`, persisted together, never combined into one id; no tenant-qualified identifier is built, so the reserved `default` segment and injectivity rules do not apply |
| 11 | Module Isolation | ✅ | This is the feature |
| 12 | Audit Trail | ⏭️ | Deferred: feeding `AuditRecord.ModuleId` and the compliance aggregates from `context.ModuleId` is #1636 (open); the consumers moved in Phase 3 (Retention, ABAC) already take it from the field |

---

## Prerequisites & Dependencies

- #352 pull request 1 (module context producer in core) makes `IModuleExecutionContext.CurrentModule` meaningful outside EF Core; this plan does not need it for the field (the dispatcher uses the handler registry), only for the module scope that a restored message re-opens, which is the same scope the producer opens for live requests.
- #747 and #761 add the module column to the messaging rows and #1164 persists the context with each row and wires the processors to `RunRestoredAsync`; the three edit stores and processors, never `src/Encina/Identity/`. #747's `MessagingModuleScope` (its pull request 25) and this plan's restoration set the same ambient value; when #1164 lands and the processors use `RunRestoredAsync`, whether `MessagingModuleScope` stays is that plan's decision (a nested scope of the same name is harmless meanwhile).
- #3545 (ADR-062 contract) is on `main`; no new error code is added here.
- ADR number: none assigned to this issue; the model choice is recorded in the plan and the issue comment of 2026-10-17, and an ADR is written only if the maintainer's answers to D1 to D3 change the architecture (ADR number assigned when the ADR pull request opens).

---

## Next Steps

1. The maintainer answers D1 to D3 when work on #2442 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 first (the interface and its implementers), then 2, 3, 4 and 5 in order.
3. Open the issue file `artifacts/issues/notification-handler-module-stamping.md` when this plan merges.
