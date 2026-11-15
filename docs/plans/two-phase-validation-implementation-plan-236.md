# Implementation Plan: Two-Phase Validation Pattern (Pipeline and Domain)

> **Issue**: [#236](https://github.com/dlrivada/Encina/issues/236)
> **Type**: Feature (pattern, guidance, and the small opt-in code that makes it observable and enforceable)
> **Related**: #230 (async validators; phase 1 may host them), #234 (the aggregator that handlers can use to collect phase-2 errors), #233 (localization of phase-1 messages), #726 (central validation instrumentation: attributes phase 1), #3415 (a code-less validation `Left` never becomes a 400), #3420 (owns the `FailedPrecondition` HTTP row), ADR-001, ADR-006, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (mostly documentation and an ADR; one small pipeline behavior and one architecture rule)
> **Estimated Scope**: ~350 lines of production code + ~1,200 lines of tests and one compiled example + ~700 lines of documentation and ADR, in three pull requests of at most 14 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `96c8803bd`)

- Phase 1 exists and is complete: `ValidationPipelineBehavior<TRequest, TResponse>` runs the one registered `IValidationProvider` through `ValidationOrchestrator` before the handler and returns a `Left` without calling `nextStep()` on failure (`src/Encina/Validation/ValidationPipelineBehavior.cs:51-67`). It sits in the `Validation` pipeline stage (`PipelineStage.Validation = 900`, `src/Encina/Pipeline/Ordering/PipelineStage.cs`).
- Phase 2 is plain handler code. The domain-side building blocks already exist: `IBusinessRule`/`BusinessRule`, `BusinessRuleError`, `Check`, `CheckFirst`, `CheckAll` (`src/Encina.DomainModeling/BusinessRule.cs`), aggregates and `ValueObject`. There is no guidance that says what belongs in which phase, no ADR, and no tested example.
- The issue's handler example calls `EncinaErrors.Validation("...")` and `EncinaErrors.NotFound("Customer", cmd.CustomerId)`. Neither exists: `EncinaErrors` offers `Create` (two overloads), `Forbidden`, `Unauthorized`, `Unauthenticated`, `FromException` and the `Unknown` property (`src/Encina/Results/EncinaErrors.cs`); the categorized `Create(code, category, message)` is the way to build the errors (ADR-062).
- A phase-2 rejection is invisible as such: the `Left` leaves the handler like any other failure. The command activity behavior marks the span failed on a `Left` (`CommandActivityPipelineBehavior`) but does not say which phase rejected, and nothing logs it.
- The architecture-test package `Encina.Testing.Architecture` has `EnforceValidatorNaming()` (validators sealed and named `*Validator`) and `HandlersShouldNotDependOnInfrastructure()`; it has no rule that keeps phase-1 validators free of repositories and infrastructure.
- The repository has no `samples/` folder (`git ls-files` lists none); `docs/features/pipeline-behaviors.md` and the provider READMEs are the only places that mention validators, and `tests/Encina.Testing.Examples` holds compiled, tested examples (`Unit`, `Domain`, `Integration` folders). The issue's task "update existing samples" therefore has nothing to update; the compiled example this plan adds is the sample.

### What this plan delivers

The pattern, written down and made checkable, with the least new code that does it:

1. **Guidance and ADR** (through `docs-writer`): the responsibility matrix of the issue mapped to ADR-062 categories (Design Choice 2), what belongs in phase 1 and phase 2, how each of the three providers expresses phase 1, an ADR (number assigned when the ADR pull request opens), and a compiled, tested example (`CreateOrder` with a FluentValidation phase 1, a handler phase 2 and a DataAnnotations variant).
2. **`DomainValidationObserverBehavior<TRequest, TResponse>`**, opt-in through `AddEncinaTwoPhaseValidation()`: sits in the default `Application` stage, inside `Validation` (`PipelineStageAttribute.cs:15`); when the handler returns a `Left` whose category is a client-correctable one it logs by code and category and tags the current `Activity` with `encina.validation.phase = domain`. Phase 1 is attributed by #726's central instrumentation of the orchestrator.
3. **`ValidatorsShouldNotDependOnInfrastructure()`** in `Encina.Testing.Architecture` with the builder method `EnforceTwoPhaseValidation()` (opt-in, not part of `ApplyAllStandardRules`): `*Validator` classes must not depend on repositories, EF Core, Dapper or ADO.NET types, which keeps phase 1 fast and I/O-free for the applications that choose the pattern.

Nothing changes for an application that does none of this.

- Affected packages: `Encina` (core: the behavior, its registration and log), `Encina.Testing.Architecture` (one rule), `Encina.FluentValidation`, `Encina.DataAnnotations`, `Encina.MiniValidator` (documentation only, README and examples).
- Provider category: Validation (3) (`AGENTS.md` section 5): phase 1 is whichever provider is registered and phase 2 is provider-independent; integration tests run the same two-phase scenario through each of the three.
- Shared pieces: none owned elsewhere; the behavior consumes `ErrorCategory` and `GetCategory` (ADR-062).

---

## Design Choices

<details>
<summary><strong>1. How much code the pattern needs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Documentation, ADR and a tested example only; no runtime or tooling code** | Zero new API; fastest; the issue's own task list is documentation, examples and an ADR | The two integration items of the issue ("trace which phase rejected", "log which phase rejected") have no implementation; nothing enforces the separation |
| **B) A plus an opt-in observer behavior (log and activity tag for phase-2 rejections) and an opt-in architecture rule** | Meets the issue's cross-cutting items with about 350 lines; both pieces are opt-in; the rule is the checkable form of the guidance | Two small new public types and one rule to maintain |
| **C) A first-class phase-2 mechanism: an `IDomainValidator<TRequest>` contract that a behavior runs before the handler** | Phase 2 becomes visible to the framework | The issue rejects a "validation service layer" between pipeline and handler (its Alternatives, option 3); phase 2 needs repositories and the aggregate, which belong in the handler; it would duplicate `BusinessRule` |

### Chosen Option: **B — Documentation plus an opt-in observer and an opt-in architecture rule** (recommended, pending the maintainer)

### Rationale

- B is the smallest option that satisfies every line of the issue's cross-cutting table (trace and log the phase) and gives the guidance a test that can fail.
- A wins when the maintainer treats phase attribution as #726's job and wants no new type; C wins only if the maintainer reverses the issue's own rejection of a validation service layer.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The error category of a phase-2 rejection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The producer picks the ADR-062 category by the rules R1 to R3, and the guidance gives one row per case: a missing entity the request names is `NotFound`; a value or field rule is `Validation`; a state rule (customer not eligible, insufficient stock) is `Conflict` until #3420 opens `FailedPrecondition`** | No new helper; follows the merged contract; HTTP, gRPC and GraphQL map the category | `Conflict` for a state rule is a judgement the ADR leaves open |
| **B) Always `Validation` (400)** | One rule to teach | A missing customer would answer 400 instead of 404; ADR-062 rule R1 says `NotFound` when the request names the missing thing |
| **C) A new category such as `BusinessRule`** | A precise name | A new row in every protocol table and in the property tests (ADR-062: adding a category is a change to every table); #3420 is already deciding the compliance-style categories |

### Chosen Option: **A — The producer chooses per ADR-062, state rules are `Conflict` for now** (recommended, pending the maintainer)

### Rationale

- R1 and R3 settle the first two rows; only the state-rule row is open, because R2 forbids `FailedPrecondition` before #3420 and names `Conflict` for version mismatches, operations in progress and invalid transitions, which a stock or eligibility check resembles.
- B wins when the application wants every rejection to be a client-input error for UI reasons; C wins if #3420 introduces a category that fits and the maintainer prefers a dedicated one.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which categories the observer counts as a domain rejection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The client-correctable set of the ADR-062 table: `Validation`, `InvalidRequest`, `NotFound`, `Conflict`, `AlreadyExists`, `FailedPrecondition`; everything else (including `Internal`, authorization, rate limiting, upstream failures and cancellation) is not a validation rejection** | Separates "the request was wrong" from "the system failed"; uses only the contract | The set is a judgement; a code-less error is `Internal` and not counted |
| **B) Every `Left` from the handler** | Simplest | A store outage would be logged as a domain rejection and pollute the signal |
| **C) Only errors the handler marks explicitly** (a detail key) | Exact | Every handler author must remember the marker; the nonexistent `EncinaErrors.Validation` of the issue shows how quickly helpers drift |

### Chosen Option: **A — Client-correctable categories** (recommended, pending the maintainer)

### Rationale

- The category is the one machine-readable fact every error already carries after ADR-062; reading it needs no cooperation from handler code.
- B wins when the team wants a complete failure log from one behavior; C wins when categories are not yet assigned in the application's own producers.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. A helper for building phase-2 errors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No helper: handlers call the categorized `EncinaErrors.Create(code, category, message)`** | Nothing to maintain; the one API ADR-062 defines; `BusinessRule` already supplies `ErrorCode` and `ErrorMessage` | The guidance must show the call |
| **B) `DomainValidationErrors.NotFound(...)`, `.Rule(...)` helpers** | Reads like the issue's example | A thin wrapper over `Create` that hides the category choice; a second place to update when ADR-062 changes |

### Chosen Option: **A — Use `EncinaErrors.Create` with a category**

### Rationale

- The call is one line and keeps the category visible at the place the producer decides it, which is the point of ADR-062.

**Class:** Trivial
**Settled by:** ADR-062 ("the category is set at the call site through the categorized overloads `EncinaErrors.Create(string code, ErrorCategory category, string message, ...)`") and `AGENTS.md` section 3 ("every line serves a current purpose")

</details>

<details>
<summary><strong>5. Whether the architecture rule is part of the standard rule set</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt-in: a separate rule and builder method `EnforceTwoPhaseValidation()`, excluded from `ApplyAllStandardRules()`** | An application that deliberately runs scoped validators with a `DbContext` (a supported use) is not broken | The rule only protects applications that adopt it |
| **B) Include it in `ApplyAllStandardRules()`** | Everyone gets the guidance enforced | It would fail every application that uses the documented scoped-validator lifetime |

### Chosen Option: **A — Opt-in rule**

### Rationale

- `AddEncinaFluentValidation(ServiceLifetime, assemblies)` documents scoped validators "that depend on EF DbContext" as the reason for the lifetime overload (`src/Encina.FluentValidation/ServiceCollectionExtensions.cs:80`); forcing the rule would make that documented use fail an architecture test.

**Class:** Settled
**Settled by:** `src/Encina.FluentValidation/ServiceCollectionExtensions.cs:80` (scoped validators with a `DbContext` are a supported use) and the issue's own ADR scope ("promoting", not mandating, the separation); only the opt-in option does not break that use

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | The observer never changes the result: it reads the `Left`, logs and tags, and returns the same value; a failure inside the observer's own logging cannot reach the caller | Trivial | `AGENTS.md` section 3 (errors are never swallowed or rewritten by background-style observers; a pipeline observer is read-only) |
| C2 | Logs and tags carry the request type name, the error code and the category name, never `EncinaError.Message`, error details or request values | Settled | `AGENTS.md` section 3 (#1168, #1173, #1259, #1274); ADR-054 settled point 2 |
| C3 | The activity tag `encina.tenant_id` is set only when `context.TenantId` is not null, the existing convention of the compliance behaviors (`DPIARequiredPipelineBehavior.cs:197`); no tenant id is part of any key, so no tenant-qualified id needs to be injective and no `default` tenant segment is involved | Settled | ADR-054 D4 (tenant tag when multi-tenancy is on); `src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs:197` |
| C4 | The observer is registered with `TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), ...))`, the registration shape of `ValidationPipelineBehavior`; it is not registered by `AddEncina` | Trivial | `src/Encina.FluentValidation/ServiceCollectionExtensions.cs:64` (the same line shape); `AGENTS.md` section 1 (every feature opt-in) |
| C5 | No new `EncinaError` code is created: the observer reads categories, the guidance tells producers to use `EncinaErrors.Create` with a category; there is no ADR-062 category to assign by this plan | Settled | ADR-062 contract |
| C6 | OpenTelemetry of phase 1 (duration, failure rate by phase) is #726's: it instruments `ValidationOrchestrator` centrally and tags `validation.phase = structural`; this plan adds an activity tag for phase 2 only and no `ActivitySource` or `Meter` (ADR-054 D3 allows one per package and the tag needs none) | Settled | Issue #726 (open, proposed solution 4); ADR-054 D3 |
| C7 | EventId 630 is the only id used in the block 630-639 of the family block 600-649; the range `ValidationTwoPhase = (630, 639)` is registered first | Trivial | `AGENTS.md` section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C8 | The ADR title and number: assigned when the ADR pull request opens; it records the pattern, the responsibility matrix with categories and the opt-in observer and rule | Trivial | `artifacts/decisions/plan-batch/eventid-blocks.md` ADR table (no number is assigned to this family); `AGENTS.md` section 8 (architectural decisions get an ADR) |
| C9 | The handler example in the guidance returns `Left` for a missing customer with `NotFound` and the state rules with `Conflict` (Design Choice 2), and collects several rule failures with `BusinessRule.CheckAll` or the aggregator of #234 | Dilemma (recommended, pending the maintainer) | follows Design Choice 2; `CheckFirst` (fail fast) is the alternative when the application wants one error |

---

## Implementation Phases

### Phase 1: Phase-2 Observer, Registration and Logging (pull request S1)

> **Goal**: Opt-in visibility of phase-2 rejections, proven through the real pipeline with each of the three providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/DomainValidationObserverBehavior.cs`: `public sealed class DomainValidationObserverBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`; constructor `(ILogger<DomainValidationObserverBehavior<TRequest, TResponse>>? logger = null)`; `Handle` calls `nextStep()`, and on a `Left` whose `GetCategory()` is in the client-correctable set (Design Choice 3, a static readonly set) logs and tags, then returns the original value. `Activity.Current?.SetTag("encina.validation.phase", "domain")`, `SetTag("encina.error.code", code)` is not added (the command activity behavior already records the failure; the tag names the phase only) and the tenant tag follows register row C3.
2. **Create** `src/Encina/Validation/TwoPhaseValidationServiceCollectionExtensions.cs`: `AddEncinaTwoPhaseValidation(this IServiceCollection services)` (register row C4).
3. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationTwoPhase = (630, 639)`; **create** `src/Encina/Validation/TwoPhaseValidationLog.cs` (`[LoggerMessage]`, id 630: a request was rejected in the domain phase, information level: request type, error code, category). 631-639 stay free. XML docs name the range.
4. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina` also maps to the new range), `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
5. Tests (new): `DomainValidationObserverBehaviorTests` (unit: each client-correctable category is logged and tagged, `Internal`, `Forbidden`, `Cancelled` and a `Right` are not, the returned value is the original instance, a null logger is a valid case), `TwoPhaseValidationLogTests` (EventId and no message or value in the rendered text), `TwoPhaseValidationGuardTests` (null request, context and `nextStep`; null `services`), `TwoPhaseValidationRegistrationTests` (DI with `ValidateOnBuild` and `ValidateScopes`, and a pipeline-order assertion that the observer runs after `ValidationPipelineBehavior` for every provider registration order), and `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/TwoPhaseValidationPipelineTests.cs` (through `IEncina.Send`: a structurally invalid request is rejected by phase 1 and never reaches the handler or the observer; a structurally valid request that the handler rejects with `NotFound` is seen by the observer; no container).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #236 plan docs/plans/two-phase-validation-implementation-plan-236.md, phase 1
of 3. ValidationPipelineBehavior (phase 1) is in src/Encina/Validation/. ADR-062 gives every EncinaError a category
read with GetCategory().

TASK:
1. Create DomainValidationObserverBehavior<TRequest,TResponse>: call nextStep, and when the result is a Left whose
   category is Validation, InvalidRequest, NotFound, Conflict, AlreadyExists or FailedPrecondition, log EventId 630 and
   tag Activity.Current with encina.validation.phase=domain; return the original result unchanged.
2. AddEncinaTwoPhaseValidation registers it with TryAddEnumerable (transient IPipelineBehavior<,>).
3. Register ValidationTwoPhase (630, 639) in EventIdRanges.cs and write TwoPhaseValidationLog.

KEY RULES:
- Never log or tag EncinaError.Message or request values; codes, category names and the request type name only.
- The observer is read-only; opt-in; a null logger is valid. Tenant tag only when TenantId is not null.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer; EncinaEventIdAllocationTests must map the range.

REFERENCE FILES:
- src/Encina/Validation/ValidationPipelineBehavior.cs
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (:185-200, tag convention)
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 2: Architecture Rule (pull request S2)

> **Goal**: The separation becomes a test an application can opt into.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Testing.Architecture/EncinaArchitectureRules.cs`: `public static IArchRule ValidatorsShouldNotDependOnInfrastructure()` (classes whose name ends with `Validator` must not depend on types in `*EntityFrameworkCore*`, `*Dapper*`, `*System.Data*` namespaces or on types whose name ends with `Repository` or implements the repository abstractions of `Encina.DomainModeling`), following the shape of `HandlersShouldNotDependOnInfrastructure()` (`:52-71`).
2. **Modify** `src/Encina.Testing.Architecture/EncinaArchitectureRulesBuilder.cs`: `EnforceTwoPhaseValidation()` adds the rule; it is not added by `ApplyAllStandardRules()` (Design Choice 5).
3. **Modify** `src/Encina.Testing.Architecture/PublicAPI.Unshipped.txt`, `src/Encina.Testing.Architecture/README.md` (through `docs-writer`) and `.github/coverage-manifest/Encina.Testing.Architecture.json` (through `mechanical-fixer`).
4. Tests (edited, in the existing files): `tests/Encina.UnitTests/Testing/Architecture/EncinaArchitectureRulesTests.cs` (the rule returns a valid rule, passes for a validator with no dependencies, fails for one that takes a repository or a `DbContext`) and `EncinaArchitectureRulesBuilderTests.cs` (the builder method adds the rule; `ApplyAllStandardRules()` does not).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #236 plan docs/plans/two-phase-validation-implementation-plan-236.md, phase 2 of 3.

TASK:
1. Add ValidatorsShouldNotDependOnInfrastructure to EncinaArchitectureRules (same shape as
   HandlersShouldNotDependOnInfrastructure) and EnforceTwoPhaseValidation to EncinaArchitectureRulesBuilder.
2. Tests of the rule against fixture types that depend and do not depend on a repository and a DbContext.

KEY RULES:
- The rule is opt-in: ApplyAllStandardRules must not include it.
- Tests execute the real rule against real fixture types (no reflection-only tests).
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer; the README through docs-writer.

REFERENCE FILES:
- src/Encina.Testing.Architecture/EncinaArchitectureRules.cs (:52-71, :181-190)
- src/Encina.Testing.Architecture/EncinaArchitectureRulesBuilder.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaArchitectureRulesTests.cs
```

</details>

---

### Phase 3: Guidance, ADR, Compiled Example and Finalization (pull request S3)

> **Goal**: The pattern is documented, decided and demonstrated by code that compiles and is tested.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/two-phase-validation.md` through `docs-writer`: the responsibility matrix of the issue with the category of each row (Design Choice 2), what belongs in each phase, the three provider forms of phase 1, the observer and the rule, the failure messages a client sees, links to the ADR and to the plans of the sibling features by issue number; add its entry to `docs/features/index.md`.
2. **Create** the ADR under `docs/architecture/adr/` through `docs-writer` (number assigned when the ADR pull request opens; add it to `docs/architecture/adr/index.md`).
3. **Modify** `docs/features/pipeline-behaviors.md` (a row for the observer and a pointer to the page), `src/Encina.FluentValidation/README.md`, `src/Encina.DataAnnotations/README.md` and `src/Encina.MiniValidator/README.md` through `docs-writer`.
4. **Create** `tests/Encina.Testing.Examples/Unit/TwoPhaseValidationExamples.cs`: the `CreateOrder` scenario of the issue as a compiled, passing example (a FluentValidation phase 1, a handler that reads fake repositories, `EncinaErrors.Create` with categories, `BusinessRule.CheckAll`, an always-valid `Order.Create`), asserted with Shouldly through `Encina.Testing.Shouldly`.
5. **Create** `changelog.d/236-two-phase-validation.added.md` through `mechanical-fixer`, and the justification files for Load and Benchmark tests (`AGENTS.md` section 9): one `Left` category read per rejected request, no shared state.
6. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of the touched files against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #236 plan docs/plans/two-phase-validation-implementation-plan-236.md, phase 3 of 3. Depends on S1 and S2.

TASK:
1. Write the guidance page and the ADR (docs-writer), update the pipeline-behaviors page and the three provider READMEs.
2. Add the compiled example CreateOrder under tests/Encina.Testing.Examples/Unit and the changelog and justification files.

KEY RULES:
- Pages and the ADR go through docs-writer; fragments through mechanical-fixer. Never type coverage figures by hand.
- The example uses EncinaErrors.Create with an ErrorCategory (the issue's EncinaErrors.Validation does not exist);
  no private client application is named: write "the reference application" if one is needed.
- Link sibling plans by issue number only; the ADR number is assigned when the ADR pull request opens.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/pipeline-behaviors.md
- src/Encina.DomainModeling/BusinessRule.cs
- tests/Encina.Testing.Examples/Unit/HandlerTestExamples.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-001, ADR-006 | Railway Oriented Programming: both phases return `Either<EncinaError, T>` | Result shape of both phases |
| ADR-062 | Category contract; rules R1 (NotFound), R2 (Conflict, `FailedPrecondition` not before #3420), R3 (Validation) | Category of each phase-2 row (Design Choice 2) |
| ADR-054 D3, D4 and settled point 2 | One meter and source per package; tenant tag only when multi-tenancy is on; error status carries code or type only | Tag and log shape of the observer (rows C2, C3, C6) |
| RFC 9110 section 15.5.10 | 409 Conflict: the request conflicts with the current state of the target resource | Why a state rule is `Conflict` for now |
| Domain-driven design: always-valid domain model, invariants at the aggregate | Domain objects reject invalid states themselves | Phase 2 and the `Order.Create` step of the example |
| `AGENTS.md` sections 3 and 5 | ROP; orchestrator pattern with the three providers consistent | Provider consistency of phase 1 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ValidationPipelineBehavior`, `ValidationOrchestrator`, `IValidationProvider` | `src/Encina/Validation/` | Phase 1, unchanged |
| `PipelineStage`, `PipelineStageAttribute` | `src/Encina/Pipeline/Ordering/` | The observer's default `Application` stage, inside `Validation` |
| `ErrorCategory`, `GetCategory`, `EncinaErrors.Create` | `src/Encina/Errors/ErrorCategory.cs`, `src/Encina/Results/EncinaErrors.cs` | Reading and creating categorized errors |
| `BusinessRule`, `BusinessRuleError`, `CheckAll` | `src/Encina.DomainModeling/BusinessRule.cs` | Domain invariants in phase 2 |
| `HandlersShouldNotDependOnInfrastructure`, `EncinaArchitectureRulesBuilder` | `src/Encina.Testing.Architecture/` | Model for the new rule and builder method |
| `CommandActivityPipelineBehavior` | `src/Encina/Pipeline/Behaviors/` | The activity the observer tags |
| `Encina.Testing.Examples` | `tests/Encina.Testing.Examples/` | Home of the compiled example |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` core (`ValidationTwoPhase`, new, inside the block 600-649 assigned to the validation-2 family) | 630-639 | 630 used; 631-639 free; sibling plans in the same block: #233 600-609, #234 610-619, #235 620-629 |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, READMEs and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Observer, registration, log, integration | 3 (`DomainValidationObserverBehavior`, `TwoPhaseValidationServiceCollectionExtensions`, `TwoPhaseValidationLog`) | 2 (`EventIdRanges.cs`, PublicAPI) | 7 (observer unit, log, guard, DI, 3 integration) | 1 (`EncinaEventIdAllocationTests.cs`) | 1 | 0 | 14 | ~950 |
| S2 | Architecture rule | 0 | 3 (`EncinaArchitectureRules.cs`, `EncinaArchitectureRulesBuilder.cs`, PublicAPI) | 0 | 2 (`EncinaArchitectureRulesTests.cs`, `EncinaArchitectureRulesBuilderTests.cs`) | 1 | 1 (the package README) | 7 | ~350 |
| S3 | Guidance, ADR, example, finalization | 0 | 0 | 1 (`TwoPhaseValidationExamples.cs`) | 0 | 0 | 11 (feature page, `docs/features/index.md`, ADR, ADR index, `pipeline-behaviors.md`, 3 provider READMEs, changelog fragment, 2 justification `.md`; counted as 12 minus the example) | 12 | ~800 |
| **Total** | | **3** | **5** | **8** | **3** | **2** | **12** | **33** | **~2,100** |

The columns sum to the total (3 + 5 + 8 + 3 + 2 + 12 = 33) and the pull-request rows sum to 33 files (14 + 7 + 12). The S3 "Other" count is 11 (the feature page, the features index entry, the ADR, the ADR index, `pipeline-behaviors.md`, the three provider READMEs, the changelog fragment and the two justification files); S2's "Other" is its package README; the total "Other" is 11 + 1 = 12. No pull request exceeds 14 files or 1,000 lines. S1 and S2 are independent; S3 needs both. Verify caller counts with `git grep` before each pull request.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file:

| File | Unit | Guard | Property | Justification |
|------|------|-------|----------|---------------|
| `DomainValidationObserverBehavior.cs` | 95 | 90 | 0 | Every category branch, the `Right` path and the null-logger path are reachable with fake `nextStep` delegates; the guard tests reach the three argument checks; the logic is a set membership test, so no property adds coverage |
| `TwoPhaseValidationServiceCollectionExtensions.cs` | 90 | 90 | 0 | Registration is fully exercised by the DI test; the null check is guard-tested |
| `TwoPhaseValidationLog.cs` | 90 | 0 | 0 | One source-generated method exercised by the logging test; no parameters to guard |
| `EncinaArchitectureRules.cs`, `EncinaArchitectureRulesBuilder.cs` (changed members) | 90 | 0 | 0 | The new rule and builder method are exercised against fixture types in the existing rule tests; the new members take no parameters, so guard tests cannot reach more |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #236 - the two-phase validation pattern for Encina.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5): phase 1 is ValidationPipelineBehavior with
  one of FluentValidation, DataAnnotations or MiniValidator; phase 2 is handler code using BusinessRule and aggregates.
- Railway Oriented Programming: Either<EncinaError, T>; every error code carries an ErrorCategory (ADR-062).

IMPLEMENTATION OVERVIEW:
1. DomainValidationObserverBehavior (opt-in, AddEncinaTwoPhaseValidation): logs EventId 630 and tags the current
   Activity encina.validation.phase=domain when a handler returns a client-correctable category; read-only.
2. ValidatorsShouldNotDependOnInfrastructure and EnforceTwoPhaseValidation in Encina.Testing.Architecture (opt-in).
3. Guidance page, ADR (number assigned when the ADR pull request opens), compiled CreateOrder example, README updates.
Three pull requests (S1 to S3), see the Estimated File Count table.

KEY PATTERNS:
- Handlers use the categorized EncinaErrors.Create; the issue's EncinaErrors.Validation does not exist.
- Never log or tag EncinaError.Message; DI test with ValidateOnBuild and ValidateScopes; integration tests per provider.
- PublicAPI.Unshipped.txt, manifests and changelog through mechanical-fixer; documentation through docs-writer.

REFERENCE FILES:
- src/Encina/Validation/*.cs, src/Encina/Pipeline/Ordering/PipelineStage.cs
- src/Encina.Testing.Architecture/EncinaArchitectureRules.cs
- docs/plans/two-phase-validation-implementation-plan-236.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Validation is per request, as the issue says |
| 2 | OpenTelemetry | ✅ Phase 1 | Phase 2 is traced here and phase 1 by #726. The observer tags the current activity with `encina.validation.phase=domain` (and the tenant tag when `TenantId` is present, ADR-054 D4); #726 attributes phase 1 in `ValidationOrchestrator` and owns duration and failure-rate metrics |
| 3 | Structured Logging | ✅ Phase 1 | `TwoPhaseValidationLog` EventId 630 in the new range 630-639, by request type, code and category only |
| 4 | Health Checks | ❌ N/A | In-process; no checkable dependency |
| 5 | Validation | ✅ All phases | This is the validation feature; phase 1 is provider-neutral and the three providers are exercised by the integration tests |
| 6 | Resilience | ❌ N/A | No external call; repositories called by phase 2 own their resilience |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Phase 1 runs before the handler; phase 2 reads inside the handler's scope and starts no transaction here |
| 9 | Idempotency | ❌ N/A | The observer is read-only and repeatable |
| 10 | Multi-Tenancy | ⏭️ Follow-up | Tenant-specific rules per phase: draft `artifacts/issues/validation-tenant-module-scoped-resources.md`; today `TenantId` reaches phase 1 through the request context and phase 2 through the handler |
| 11 | Module Isolation | ⏭️ Follow-up | Module-scoped validators per phase: the same draft |
| 12 | Audit Trail | ⏭️ Follow-up | Auditing of rejected requests is a follow-up of the family that accompanies the plan of #230; the observer's log is operational, not an audit record |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Validation orchestrator pattern and `ValidationPipelineBehavior` | ✅ On `main` | Phase 1 exists |
| ADR-062 error category contract (`GetCategory`, categorized `EncinaErrors.Create`) | ✅ Merged | The observer and the guidance depend on it |
| `BusinessRule`, `BusinessRuleError` in `Encina.DomainModeling` | ✅ On `main` | Phase-2 building blocks |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Central validation instrumentation | #726 | Attributes phase 1 and owns the metrics |
| `FailedPrecondition` HTTP row and compliance-style categories | #3420 | May change the category of the state-rule row (Design Choice 2) |
| Code-less validation `Left` never becomes a 400 | #3415 | Independent: phase 1's `Left` is unchanged here |
| Aggregation of several rule failures | #234 | Optional tool for handlers |

---

## Next Steps

1. Link this plan from #236 and ask the maintainer to answer Design Choices 1 to 3 and register row C9 when work starts.
2. Open the follow-up issue when the first pull request opens: tenant and module scoped validators per phase (draft: `artifacts/issues/validation-tenant-module-scoped-resources.md`).
3. Implement S1 to S3 as the pull requests of the Estimated File Count split, `Fixes #236` on S3.
