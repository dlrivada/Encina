# Implementation Plan: Tenant existence and status validation on every entry point (multi-tenant context, remaining work)

> **Issue**: [#338](https://github.com/dlrivada/Encina/issues/338)
> **Type**: Feature (child of the EPIC #876; the middleware, the resolvers and the cache-key prefix of its text are already implemented)
> **Complexity**: Medium (3 phases of code, 1 testing and documentation phase, 3 worker pull requests)
> **Estimated Scope**: ~7 production files, ~6 test files, ~7 metadata and documentation files
> **Related plans** (cited by issue number): #346 (the tenant gate, owner of the EventId range `TenancyPipeline` and of `[TenantAgnostic]`), #1257 (shared tenant-scope primitive and tenant-id rules), #711 (`CachedTenantStore`), #304 (ownership validation), #385 (EF filter for any context and the security guide), #125 (messaging dispatch guard), #2676 (tenant telemetry helper), #596 (persistent tenant store)

---

## Summary

#338 asks for tenant resolution, context enrichment, query filtering and two tenant-aware behaviors. Resolution and filtering exist in `Encina.Tenancy`, `Encina.Tenancy.AspNetCore` and the provider packages; what the issue still needs, and no other plan owns, is **validation that the tenant is a known and active tenant** wherever a request enters the pipeline, plus the status a suspended tenant needs to be denied.

**State of the code on `ec129cae5` (current `origin/main`; the cited files were read on it).**

| Item of #338 | State | Evidence or owner |
|--------------|-------|-------------------|
| Resolvers: header, subdomain, claim, route; custom resolvers; middleware that fills `IRequestContext.TenantId` | Done | `src/Encina.Tenancy.AspNetCore/Resolution/`, `Middleware/TenantResolutionMiddleware.cs` |
| `TenantInfo` with connection string and schema | Done (the connection string is `[JsonIgnore]` and masked by `ToString()`) | `src/Encina.Tenancy/Abstractions/TenantInfo.cs` |
| Tenant "exists" validation | Partial: only in the HTTP middleware, only when `TenancyOptions.ValidateTenantOnRequest` is on (default `false`), and an unknown tenant is treated as "no tenant" | `TenantResolutionMiddleware.cs` (`ValidateTenantAsync`) |
| Tenant "active" validation (`TenantSuspended`) | **Missing: this plan.** `TenantInfo` has no status and `TenancyErrors` has only `TenantNotFound` and `TenantRequired` | `TenancyErrors.cs` |
| `TenantValidationBehavior`, `TenantIsolationBehavior` | Validation: **this plan**; the presence gate is #346; ownership of referenced resources is #304 | |
| `IRequestContext.Tenant` (full `TenantInfo` on the context) | Not built | Design Choice 3 |
| EF Core `ITenantEntity` and query filter | Done for `TenantDbContext` descendants; any other context is #385 | `src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs` |
| Cache key tenant prefix | Done (`t:<escaped tenant>` segment); the injective segment helper is #1257 | `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` |
| OpenTelemetry tags for the tenant | The `Encina.Tenancy` spans use the tag `tenancy.tenant_id`; the ADR-054 D4 key and the helper are #2676 | `src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs` |
| Finbuckle adapter ("could be a separate package") | Not built | Design Choice 4 |
| Documentation for each isolation level | Done (`docs/features/multi-tenancy.md`: three isolation sections and a decision flowchart); this plan adds the validation section | |

**What is missing, by reading the code.** A request that reaches the pipeline outside HTTP (a restored background context, a gRPC bridge, a host that sets the context itself) carries a tenant id nobody has checked: a deleted tenant's queued messages still dispatch, and a suspended tenant cannot be represented at all. Validation exists once, in the middleware, and is off by default, so even HTTP accepts any header value that a trusted proxy forwards.

**Scope.** `TenantStatus`, the status parameter of `TenantInfo`, `TenancyErrors.TenantSuspended()`, `TenantValidationPipelineBehavior<TRequest, TResponse>` and its stream twin, two log messages and one counter in the range registered by #346, documentation. **Out of scope**: the presence gate (#346), ownership of referenced resources (#304), caching of the store lookup (#711), the persistent tenant store (#596), the tenant telemetry helper (#2676).

**Affected packages**: `Encina.Tenancy` (all code). **Provider category**: none (the behavior reads `ITenantStore`; no provider-specific code).

---

## Design Choices

<details>
<summary><strong>1. How a suspended tenant is represented</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantStatus` (`Active`, `Suspended`) as a trailing parameter of `TenantInfo`, default `Active`** | One record answers "does it exist and may it work"; every store returns it; additive: the 10 construction sites in `src/` and the 103 in `tests/` still compile | The tenant store (or its backing table, #596) must hold the status; a billing system that owns the truth has to write it there |
| **B) A separate `ITenantStatusProvider` the host implements** | The status lives where billing lives; `TenantInfo` unchanged | A second lookup per request; every host implements it or the feature does nothing; two sources of truth for "is this tenant usable" |
| **C) No status: existence only** | Smallest change | A suspended or delinquent tenant keeps working until its row is deleted |

### Chosen Option: **A — `TenantStatus` on `TenantInfo`** (recommended, pending the maintainer)

### Rationale

- A wins when the tenant store is the system of record for tenants (the case `InMemoryTenantStore` and the planned persistent store #596 model); B wins when a billing or provisioning service decides who is suspended and the tenant store is a read-through copy; C wins for deployments that never suspend a tenant. The status is a closed enum with explicit values (`Active = 0`, `Suspended = 1`), additive only; a store that returns a record without a status gets `Active`, the same as today.
- **Errors (follow whichever option is chosen).** An unknown tenant reuses `TenancyErrors.TenantNotFound()` (`tenancy.tenant_not_found`, `NotFound`, already asserted by tests); under A or B a suspended tenant adds `TenancyErrors.TenantSuspended()` (`tenancy.tenant_suspended`, `ErrorCategory.Forbidden`, ADR-062 R6: a denial), both with fixed messages that carry no tenant id. A tenant restored from a persisted context was not named by the current request, so ADR-062 R1 would class that lookup `Internal`; the one existing code is kept for both rather than splitting it.

</details>

<details>
<summary><strong>2. Where validation runs: only the HTTP middleware behind a flag, or the pipeline for every entry point</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The behavior always validates a non-blank tenant against the store; `ValidateTenantOnRequest` keeps meaning "also reject early in the middleware"** | Fail closed on every entry point; an unknown tenant never reaches a handler; the middleware flag only chooses where the first rejection happens | One store lookup per request (a dictionary today; a database for #596 unless the `CachedTenantStore` of #711 wraps it) |
| **B) The behavior honors `ValidateTenantOnRequest` (default `false`)** | No cost unless asked | Fail open by default: any tenant id a trusted proxy forwards, or a restored context carries, is accepted |
| **C) Remove `ValidateTenantOnRequest` and validate only in the behavior** | One place | The middleware would pass an unknown tenant to the pipeline instead of answering 400 early, and the 400 body that clients rely on changes |

### Chosen Option: **A — Always validate in the behavior** (recommended, pending the maintainer)

### Rationale

- A wins when the store lookup is cheap or cached (the in-memory store, or #711's decorator), which holds for the shipped stores; B wins for a deployment whose only store is a slow remote service and which tolerates unvalidated ids; C wins if the maintainer wants a single code path and accepts the changed HTTP behavior. `AGENTS.md` section 3 (gates fail closed) is what makes the unvalidated default the weaker option.

</details>

<details>
<summary><strong>3. Does the request context carry the full tenant details</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No: `IRequestContext` keeps only `TenantId`; code that needs details calls `ITenantProvider.GetCurrentTenantAsync()`** | The context is small and serializable; `PersistedRequestIdentity` carries the tenant id into outbox, inbox and scheduled rows (#1164) without a connection string | A handler that needs the name or strategy makes one (cacheable) call |
| **B) Add `TenantInfo? Tenant` to the context (issue text)** | One property away | The context is persisted with background messages (#1164) and flows through logs and diagnostics; a record that holds a connection string must never travel there, and a stale copy outlives a status change |

### Chosen Option: **A — Keep only `TenantId` on the context** (recommended, pending the maintainer)

### Rationale

- A wins whenever the context is persisted or logged, which it is; B wins only for an application that never persists a context and reads tenant details on every request. `TenantInfo` masks its connection string in `ToString()` and `[JsonIgnore]`s it (#3618), but a copy on the context would still be a second, stale source of the status this plan introduces.

</details>

<details>
<summary><strong>4. A Finbuckle adapter</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate `Encina.Tenancy.Finbuckle` package that reads the Finbuckle tenant context into `IRequestContext`** | Lets an existing Finbuckle application adopt Encina behaviors | A package to maintain for a dependency nobody has asked for; two tenant models |
| **B) Not built; a host that uses Finbuckle sets `IRequestContext.TenantId` itself (one line in a middleware)** | No package; pay-for-what-you-use | Each Finbuckle host writes the line |

### Chosen Option: **B — Not built** (recommended, pending the maintainer)

### Rationale

- A wins when there is demand from Finbuckle users; B wins now, with no installed base and a one-line bridge. The issue itself marks the adapter optional.

</details>

<details>
<summary><strong>5. What the behavior reads: the singleton store and the explicit context, or the scoped provider</strong></summary>

**Class:** Trivial
**Settled by:** `DefaultTenantProvider` reads the same two sources (`IRequestContextAccessor` and `ITenantStore`, `src/Encina.Tenancy/Core/DefaultTenantProvider.cs`); `ITenantProvider` is registered scoped (`AddEncinaTenancy`), so a behavior that holds it would hold a scoped service, and the behavior receives the request's `IRequestContext` as an argument.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inject `ITenantStore` (singleton) and read `context.TenantId`** | No scoped dependency; the behavior validates the context it is given, not an ambient one | Duplicates two lines of `DefaultTenantProvider` |
| **B) Inject `ITenantProvider` (scoped)** | Reuses the provider | The behavior would hold a scoped service (`AddEncinaTenancy` registers the provider scoped); reads the ambient context, not the explicit one |

### Chosen Option: **A — The store and the explicit context**

### Rationale

- The pipeline passes the explicit context; validating it keeps the behavior correct for a request dispatched with an explicit `Send(request, context, ct)` overload, which background dispatch uses (#1164).

</details>

---

## Implementation Phases

### Phase 1: Core Models & Errors (PR 338-A)

> **Goal**: a status a store can return and the error that denies it.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Tenancy/Abstractions/TenantStatus.cs`**: `public enum TenantStatus { Active = 0, Suspended = 1 }` with XML docs ("additive only, values are never renumbered"; the meaning of each).
2. **Modify `src/Encina.Tenancy/Abstractions/TenantInfo.cs`**: add the trailing parameter `TenantStatus Status = TenantStatus.Active` to the positional record; `ToString()` includes it; `IsActive` convenience property. The parameter is trailing and optional, so the 10 construction sites in `src/` and the 103 in `tests/` (counted by grep on `origin/main`) compile unchanged.
3. **Modify `src/Encina.Tenancy/TenancyErrors.cs`**: `TenantSuspendedCode = "tenancy.tenant_suspended"`, `TenantSuspended()` with `ErrorCategory.Forbidden` (ADR-062 R6) and a fixed message without the tenant id; XML docs cite the rule.
4. PublicAPI lines for the enum, the parameter, the property and the factory (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #338, plan tenant-validation-behavior-implementation-plan-338.md. TenantInfo is a positional record with trailing optional parameters; TenancyErrors holds fixed-message factories with categories (ADR-062).

TASK:
Add TenantStatus, the trailing Status parameter of TenantInfo (default Active) and TenancyErrors.TenantSuspended().

KEY RULES:
- Additive only: no existing construction site changes; the connection string stays masked in ToString() and JsonIgnore.
- The error message carries no tenant id; category Forbidden (ADR-062 R6); a unit test asserts code, category and the absence of a tenant value, like the existing factories.
- XML docs on every public member; zero warnings.

REFERENCE FILES:
- src/Encina.Tenancy/Abstractions/TenantInfo.cs
- src/Encina.Tenancy/TenancyErrors.cs
- tests/Encina.UnitTests/Tenancy/ (TenantInfoTests, TenancyErrorsTests)
```

</details>

### Phase 2: Pipeline Behavior and Registration (PR 338-B)

> **Goal**: validate the tenant of every request that has one.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Tenancy/Validation/TenantValidationPipelineBehavior.cs`**: `[PipelineStage(PipelineStage.Authorization, Order = 6)] public sealed class TenantValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`; constructor `(ITenantStore store, IOptions<TenancyOptions> options, ILogger<TenantValidationPipelineBehavior<TRequest, TResponse>> logger)`. `Handle`: a blank tenant passes (absence is the gate of #346, which runs one order earlier); a request type with `[TenantAgnostic]` passes; otherwise `await store.GetTenantAsync(context.TenantId, ct)`: `null` returns `Left(TenancyErrors.TenantNotFound())` (EventId 7853), `Status == Suspended` returns `Left(TenancyErrors.TenantSuspended())` (EventId 7854), else `nextStep()`. An exception from the store propagates (the store contract returns no `Either`, `ITenantStore.GetTenantAsync` returns `ValueTask<TenantInfo?>`); nothing is swallowed and nothing is treated as "valid".
2. **Create `TenantValidationStreamPipelineBehavior<TRequest, TItem>`** (same stage and order): a denied stream yields one `Left` and ends.
3. **Create `src/Encina.Tenancy/Diagnostics/TenantValidationMetrics.cs`** (`internal static`): counter `encina.tenancy.validation.denied` on the shared `Encina` meter, tag `outcome` = `not_found` or `suspended`; no tenant tag (ADR-054 D4: the tag helper is #2676).
4. **Modify `src/Encina.Tenancy/Diagnostics/TenancyPipelineLog.cs`** (created by #346): add EventIds 7853 and 7854 (codes only).
5. **Modify `src/Encina.Tenancy/ServiceCollectionExtensions.cs`**: `TryAddEnumerable` transient registrations of both behaviors; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` and asserts the store resolves from the singleton registration.
6. PublicAPI lines for the two behaviors (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- PR 338-A merged (TenantStatus, TenantInfo.Status, TenancyErrors.TenantSuspended) and PR 346-A merged (TenantAgnosticAttribute, the gate, the TenancyPipeline EventId range and TenancyPipelineLog).

TASK:
Write TenantValidationPipelineBehavior and its stream twin, the denied counter, the two log messages and the registrations.

KEY RULES:
- Fail closed: an unknown tenant and a suspended tenant are Left; a store exception propagates; never a fallback tenant.
- The behavior reads the explicit IRequestContext and the singleton ITenantStore (no scoped service).
- Logs and the counter carry the outcome code and the request type, never a tenant value and never EncinaError.Message.
- EventIds 7853 and 7854 are the next free ids of the range registered by #346; no id outside it.

REFERENCE FILES:
- src/Encina.Tenancy/Core/DefaultTenantProvider.cs
- src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs
- src/Encina.Tenancy.AspNetCore/Middleware/TenantResolutionMiddleware.cs (the existing ValidateTenantAsync)
```

</details>

### Phase 3: Observability and Cross-Cutting Integration (PR 338-B)

> **Goal**: the denial is visible without exposing a tenant.

<details>
<summary><strong>Tasks</strong></summary>

1. Log messages 7853 (tenant not found, Warning) and 7854 (tenant suspended, Warning) packed after #346's 7850-7852; the range `TenancyPipeline` and the assembly map entry come from PR 346-A, so this PR adds no registration.
2. The denied counter of Phase 2 task 3; no span (one store call; the store traces itself when it is a database store).
3. `TenantHealthCheck` stays as it is; it checks the provider, not the status.
4. Cache of the lookup: not here (#711's `CachedTenantStore` wraps `ITenantStore`, so the behavior needs no change when it lands).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- ADR-021: EventIds come from the registered range; this plan's ids are 7853-7854 of TenancyPipeline (7850-7879), assigned in merge order after #346's 7850-7852 and before #304's 7855-7856.

TASK:
Add the two log methods and the counter; run the architecture tests that scan EventIds.

KEY RULES:
- Packed sequentially, no gaps, XML docs name the range.
- No tenant value in any message or tag.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Tenancy/Diagnostics/TenancyPipelineLog.cs (from PR 346-A)
```

</details>

### Phase 4: Testing, Documentation & Finalization (tests in PR 338-A and 338-B; documentation in PR 338-C)

> **Goal**: every flag at its target; the guide says what the code does.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** `tests/Encina.UnitTests/Tenancy/Validation/TenantValidationPipelineBehaviorTests.cs`: known active tenant passes; unknown denied with `tenancy.tenant_not_found`; suspended denied with `tenancy.tenant_suspended` and category `Forbidden`; blank tenant passes through; `[TenantAgnostic]` passes; a store that throws propagates the exception; the explicit context is the one validated (not an ambient one); the stream twin. `TenantInfoTests` and the errors tests gain the status cases.
2. **Guard tests** `tests/Encina.GuardTests/Tenancy/`: constructors of both behaviors reject null arguments.
3. **Contract test**: `ITenantStore` implementers (`InMemoryTenantStore`) return the status they were given (`tests/Encina.ContractTests/`).
4. **Integration**: justification in the test class comment: no database is involved (a non-database feature); the persistent store of #596 adds its own integration tests with the status column.
5. **Load and benchmark**: justification `.md` files (`tests/Encina.LoadTests/Tenancy/TenantValidation.md`, `tests/Encina.BenchmarkTests/Tenancy/TenantValidation.md`): one dictionary lookup on the hot path, no shared mutable state; the lookup cost of a database store is #711's benchmark.
6. **Documentation (PR 338-C, docs-writer)**: `docs/features/multi-tenancy.md` gains "Tenant status and validation" and the corrected statement of what `ValidateTenantOnRequest` now does; the FAQ "What happens if tenant resolution fails?" lists the three outcomes (400 required, 404 unknown, 403 suspended); `src/Encina.Tenancy/README.md`.
7. **Changelog fragments** `changelog.d/338-tenant-status.added.md` (PR 338-A) and `changelog.d/338-tenant-validation-behavior.added.md` (PR 338-B), manifests and PublicAPI through mechanical-fixer.

**Per-file targets** (proposed; measured values replace them and never go down):

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Validation/TenantValidationPipelineBehavior.cs` (new) | new | unit 100, guard 90, contract 85 | every branch is a unit case; guard covers the constructor; contract runs the behavior against two store implementations. |
| `Validation/TenantValidationStreamPipelineBehavior.cs` (new) | new | unit 100, guard 90 | one denied and one allowed stream. |
| `Diagnostics/TenantValidationMetrics.cs` (new) | new | unit 100 | counter read through an in-memory meter listener. |
| `Abstractions/TenantStatus.cs` (new) | new | unit 0 | an enum has no executable line. |
| `Abstractions/TenantInfo.cs` | unit 100, guard 20 | unchanged | the new parameter and property are asserted in `TenantInfoTests`; the guard target stays because the record has no guard clause. |
| `TenancyErrors.cs` | unit 100, guard 0 | unchanged | the new factory is asserted for code, category and message. |
| `Diagnostics/TenancyPipelineLog.cs` | set by #346 | raised | two more generated methods exercised with a fake logger. |
| `ServiceCollectionExtensions.cs` | unit 95, guard 70 | unchanged | two more registrations in a covered method. |

CRAP gate: `Handle` delegates the lookup and the two denials to small private methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Code phases written. Per-flag coverage is measured separately (AGENTS.md section 9); tests execute real code.

TASK:
Write the tests; delegate manifests, PublicAPI and fragments to mechanical-fixer and the pages to docs-writer; measure per-flag coverage.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; no reflection-only tests.
- Justification .md only for load and benchmark; never for unit, guard or contract.
- Never edit CHANGELOG.md; documentation never types coverage figures.

REFERENCE FILES:
- tests/Encina.UnitTests/Tenancy/
- .github/coverage-manifest/Encina.Tenancy.json
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| `AGENTS.md` section 3 | gates fail closed; errors never swallowed | Design Choices 2 and 5, the store exception propagates |
| SPEC-002 REQ-061, DEC-006 | no cross-tenant use; fail closed | an unknown or suspended tenant never reaches a handler |
| ADR-062 (R1, R6) | `NotFound` only when the request names the thing; denials are `Forbidden` | Design Choice 1 (errors) |
| ADR-054 D4 | tenant tag only when multi-tenancy is on, through one helper | no tenant tag here (#2676) |
| ADR-018, ADR-021 | 12-function evaluation; EventIds from a registered range | matrix, Phase 3 |
| OWASP ASVS V4.1 | access control checks on a trusted server-side layer | validation in the pipeline, not only in an edge middleware |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `ITenantStore`, `InMemoryTenantStore` | `src/Encina.Tenancy/Abstractions/ITenantStore.cs`, `src/Encina.Tenancy/Stores/InMemoryTenantStore.cs` | the lookup; the default store |
| `TenantInfo`, `TenancyErrors` | `src/Encina.Tenancy/Abstractions/TenantInfo.cs`, `src/Encina.Tenancy/TenancyErrors.cs` | gain the status and its error |
| `DefaultTenantProvider` | `src/Encina.Tenancy/Core/DefaultTenantProvider.cs` | the two sources the behavior reads |
| `TenancyOptions.ValidateTenantOnRequest`, `RequireTenant` | `src/Encina.Tenancy/Configuration/TenancyOptions.cs` | the middleware flag keeps its early-rejection meaning |
| `TenantResolutionMiddleware.ValidateTenantAsync` | `src/Encina.Tenancy.AspNetCore/Middleware/TenantResolutionMiddleware.cs` | the existing HTTP-only existence check |
| `TenantGatePipelineBehavior`, `TenantAgnosticAttribute` (core `Encina`), `TenancyPipelineLog` | planned in #346 (`src/Encina.Tenancy/Gate/`, `src/Encina/Abstractions/`, `Diagnostics/`) | the preceding order, the opt-out, the log class and the range |
| `ProcessorValidationPipelineBehavior<,>` | `src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs` | pattern for a validating behavior |
| `CachedTenantStore` | planned in #711 (`Encina.Tenancy`) | wraps the store; the behavior is unaffected |
| `PersistedRequestIdentity` | `src/Encina/Identity/PersistedRequestIdentity.cs` | why the context carries only the tenant id (Design Choice 3) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Tenancy` (pipeline) | 7853-7854 inside `TenancyPipeline` (7850-7879), registered by #346 | this plan reserves 7853-7854 (assigned in merge order, packed after #346's 7850-7852 and before #304's 7855-7856) |
| `Encina.Tenancy` (existing) | none | `Tenancy = (1800, 1899)` is not touched |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, status and error (enum, `TenantInfo`, `TenancyErrors`) | 3 | PR 338-A |
| Production, behaviors, metrics, log edit, registration edit | 5 | PR 338-B |
| Tests, status (`TenantInfoTests`, errors test) | 2 | PR 338-A |
| Tests, behavior (unit, guard, contract, DI edit) | 4 | PR 338-B |
| Metadata (manifest, `PublicAPI.Unshipped.txt`, two changelog fragments) | 4 | both |
| Justification files (load, benchmark) | 2 | PR 338-B |
| Documentation (`multi-tenancy.md`, Tenancy README) | 2 | PR 338-C |
| Total | 22 | three worker pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>). Issue #338: tenant resolution and filtering already exist in Encina.Tenancy and its satellites; what is missing is validation of the tenant (existing, active) on every entry point.
- The tenant is IRequestContext.TenantId; the store is ITenantStore (singleton); the gate of #346 runs first (Authorization, Order 5) and this behavior second (Order 6).

IMPLEMENTATION OVERVIEW:
1. TenantStatus enum, trailing optional Status on TenantInfo, TenancyErrors.TenantSuspended() (Forbidden).
2. TenantValidationPipelineBehavior and the stream twin: blank passes (the gate's job), [TenantAgnostic] passes, unknown -> TenantNotFound, suspended -> TenantSuspended, store exception propagates.
3. Counter encina.tenancy.validation.denied; EventIds 7853-7854 of the TenancyPipeline range registered by #346.
4. Registration with TryAddEnumerable (transient); DI test with ValidateOnBuild and ValidateScopes.
5. Tests of unit, guard and contract types; justifications for load and benchmark; documentation and fragments by specialists.

KEY PATTERNS:
- Fail closed; no fallback tenant; no tenant value in any log, tag or error message.
- The behavior reads the explicit context and the singleton store; no scoped service.
- Additive API: trailing optional parameter, enum values never renumbered.

REFERENCE FILES:
- src/Encina.Tenancy/Abstractions/TenantInfo.cs
- src/Encina.Tenancy/TenancyErrors.cs
- src/Encina.Tenancy/Core/DefaultTenantProvider.cs
- src/Encina.Tenancy.AspNetCore/Middleware/TenantResolutionMiddleware.cs
- docs/features/multi-tenancy.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | The per-request store lookup is cached by the `CachedTenantStore` of #711 (open, planned); the behavior is unchanged when it lands. |
| 2 | OpenTelemetry | ✅ | Counter `encina.tenancy.validation.denied` with no tenant tag; the ADR-054 D4 tenant tag is #2676. |
| 3 | Structured Logging | ✅ | EventIds 7853-7854 in the `TenancyPipeline` range registered by #346; codes only. |
| 4 | Health Checks | ❌ | The existing `TenantHealthCheck` checks the provider; the status is not a dependency. |
| 5 | Validation | ✅ | This feature: existence and status of the tenant. |
| 6 | Resilience | ❌ | One store lookup; the resilience of a remote store belongs to that store (#596). |
| 7 | Distributed Locks | ❌ | No shared state is updated. |
| 8 | Transactions | ❌ | Runs before the transaction stage and writes nothing. |
| 9 | Idempotency | ❌ | A pure check; replay returns the same result. |
| 10 | Multi-Tenancy | ✅ | This feature. |
| 11 | Module Isolation | ❌ | `ModuleId` is #1636; the tenant status is not module scoped. |
| 12 | Audit Trail | ⏭️ | An audit event for a denial needs a dependency on `Encina.Security.Audit`: the follow-up issue file `tenant-boundary-audit` (written with #304). |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|--------------|-------|---------------------|
| PR 346-A (`[TenantAgnostic]`, the gate, the EventId range, `TenancyPipelineLog`) | planned | PR 338-B builds on it; PR 338-A has no dependency on it |
| #711 (`CachedTenantStore`) | plan merged | lowers the cost of the lookup; not a dependency |
| #596 (persistent tenant store) | open | must persist `Status`; its plan adds the column |
| #3617 (connection factories return the default connection string for an unknown tenant) | open | a related fail-open defect of the factories; this behavior denies the request before the factory is reached on the pipeline path, the factory fix is tracked there |

**Sequencing**: PR 338-A (independent), then PR 338-B after PR 346-A, then PR 338-C.

---

## Next Steps

1. Maintainer answers Design Choices 1, 2, 3 and 4 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. Worker pull requests (counts include tests, manifests, PublicAPI lines and fragments; recount by grep when the worker starts):
   - **PR 338-A** (status and error): `TenantStatus.cs`, `TenantInfo.cs`, `TenancyErrors.cs` (3); `TenantInfoTests.cs`, `TenancyErrorsTests.cs` (2); `Encina.Tenancy.json`, `PublicAPI.Unshipped.txt`, `338-tenant-status.added.md` (3): 8 files.
   - **PR 338-B** (behaviors): `TenantValidationPipelineBehavior.cs`, `TenantValidationStreamPipelineBehavior.cs`, `TenantValidationMetrics.cs`, `TenancyPipelineLog.cs` edit, `ServiceCollectionExtensions.cs` edit (5); behavior unit test, guard test, contract test, DI test edit (4); `Encina.Tenancy.json`, `PublicAPI.Unshipped.txt`, `338-tenant-validation-behavior.added.md`, two justification files (5): 14 files.
   - **PR 338-C** (documentation): `docs/features/multi-tenancy.md`, `src/Encina.Tenancy/README.md`: 2 files.
3. Link this plan from #338; the note on the closed-out items of the issue goes in the same comment.
