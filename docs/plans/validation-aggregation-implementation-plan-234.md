# Implementation Plan: Validation Result Aggregation and Validation Groups

> **Issue**: [#234](https://github.com/dlrivada/Encina/issues/234)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, `RequestValidatorRunner` and EventIds 550-559; interplay in Design Choice 4), #233 (owns `ValidationError.ErrorCode`; the aggregator carries it through untouched), #235 and #236 (planned apart; #236 uses the aggregator for its domain phase), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), ADR-001, ADR-006, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (core types, one provider decorator, one registration extension; no persistence)
> **Estimated Scope**: ~900 lines of production code + ~1,700 lines of tests + ~200 lines of documentation, in four pull requests of at most 14 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`)

- `ValidationOrchestrator` calls exactly one `IValidationProvider` (`ValidationOrchestrator.cs:65`) and turns an invalid `ValidationResult` into a code-less `Left` (`:71`). Each provider package registers itself with `TryAdd`, so the first package registered wins and there is no way to run two providers for one request: `AddEncinaFluentValidation` uses `TryAddScoped` (`FluentValidation/ServiceCollectionExtensions.cs:62`), `AddDataAnnotationsValidation` and `AddMiniValidation` use `TryAddSingleton` (`DataAnnotations/ServiceCollectionExtensions.cs:72`, `MiniValidator/ServiceCollectionExtensions.cs:65`).
- `ValidationResult` is an immutable list of `ValidationError(PropertyName, ErrorMessage)` (`ValidationResult.cs:97`) with `Success`, `Failure(...)` and `ToErrorMessage` (`:77`); it has no group, source, priority, ordering rule or merge operation. Inside one provider the only aggregation is `FluentValidationProvider` flattening the failures of its validators (`FluentValidationProvider.cs:73-77`).
- The issue's example is `ValidationAggregator.Add(...)` ... `Either<AggregatedValidationErrors, Unit>`. A left type other than `EncinaError` conflicts with the ROP rule of `AGENTS.md` section 3 (operations return `Either<EncinaError, T>`), and the orchestrator and every host read `EncinaError`.
- Not in `src`: `ValidationAggregator`, `ValidationGroup`, `StopOnFirstGroupFailure`, partial validation, any composite or grouped `IValidationProvider`, any error ordering or de-duplication rule, any EventId range for validation aggregation.

### What this plan delivers

Two related capabilities, both opt-in and in core `Encina.Validation`:

1. **`ValidationAggregator`**, the issue's builder: it collects `ValidationResult`s from several sources, tags each error with its group and source, orders and de-duplicates them deterministically, caps their number, and produces an `AggregatedValidationResult` that converts to a plain `ValidationResult` (what the orchestrator needs) or to `Either<EncinaError, Unit>` (code `validation.aggregated_failed`, category `Validation`, the group counts as details, never the messages).
2. **Validation groups**: `AddEncinaValidationGroups(...)` registers a `GroupedValidationProvider` that runs an ordered list of named groups (`structural`, `business`, `external`), each backed by one or more of the existing providers registered as keyed services, optionally stopping after the first failing group (`StopOnFirstGroupFailure`) and optionally validating only some groups (partial validation, through `IValidationGroupRunner`). Because it is itself an `IValidationProvider`, `ValidationOrchestrator`, `ValidationPipelineBehavior` and all three provider packages are unchanged.

Nothing changes for an application that does not call `AddEncinaValidationGroups` or use the aggregator.

- Affected packages: `Encina` (core). `Encina.FluentValidation`, `Encina.DataAnnotations` and `Encina.MiniValidator` are group members and are not modified.
- Provider category: Validation (3) (`AGENTS.md` section 5): any of the three can back a group; an equivalence unit test proves the same rule gives the same grouped result whichever provider hosts it.
- Shared pieces, owned by this plan: `ValidationAggregator`, `AggregatedValidationResult`, `ValidationGroup`, `IValidationGroupRunner`. #236 reuses the aggregator for domain-phase errors.

---

## Design Choices

<details>
<summary><strong>1. How validators join a group</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A group is one or more keyed `IValidationProvider` registrations (`UseProvider<TProvider>()` registers a keyed service named after the group); the grouped provider resolves them by key and runs them in order** | Reuses the three existing providers unchanged; matches the issue's example, whose three sources are separate validators of different kinds; keyed services are the .NET 8+ idiom | A group is a whole provider, not a subset of one provider's validators; one validator class cannot be assigned to a group |
| **B) A group name on each validator registration (`AddRequestValidator<T, TValidator>(group: "business")`, an attribute on the validator)** | Fine-grained: individual validators belong to groups | Works only for the neutral contract of #230 (FluentValidation `IValidator<T>`, DataAnnotations and MiniValidator have no such registration); needs a metadata store and a change to #230's extension |
| **C) FluentValidation rule sets only (`RuleSet("business", ...)` and `IncludeRuleSets`)** | Native, no Encina code | FluentValidation only; DataAnnotations and MiniValidator have no equivalent, which breaks the provider consistency of `AGENTS.md` section 5 |

### Chosen Option: **A — Keyed providers per group** (recommended, pending the maintainer)

### Rationale

- The issue's groups (`structural`, `business`, `external`) are sources of different kinds; mapping each to a provider is the smallest design that works for all three libraries, and it needs no change to any provider.
- B wins when the same provider must host validators of several groups (every validator is FluentValidation, split into phases); it can be added later on top of A as a provider that filters by metadata. C wins for a FluentValidation-only application.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Result shape and conversion to the ROP result</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AggregatedValidationResult` (errors with group, order and source) converting to `ValidationResult` and to `Either<EncinaError, Unit>` with code `validation.aggregated_failed` and category `Validation`** | Follows ROP; the orchestrator and hosts already read `EncinaError`; the category drives the HTTP status through ADR-062 | The details carry group counts only |
| **B) `Either<AggregatedValidationErrors, Unit>` as in the issue** | Typed left side with full error detail | A second error type that no host, orchestrator or pipeline behavior understands |

### Chosen Option: **A — `EncinaError` on the left**

### Rationale

- Operations return `Either<EncinaError, T>`; a validation rejection is an `EncinaError` with a code and a category (ADR-001, ADR-006, ADR-062 rule R3: field rules are `Validation`).
- The code starts with `validation.`, which the HTTP mapping already routes to 400 until the category replaces the prefix (`ProblemDetailsExtensions.cs:212`), and unlike the orchestrator's code-less `Left` (#3415) it can be rendered.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (Railway Oriented Programming: operations return `Either<EncinaError, T>`), ADR-001, ADR-006 and ADR-062 rule R3; option B violates the first, so A is the only compliant option

</details>

<details>
<summary><strong>3. Execution semantics of groups</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Groups run sequentially in `Order` (ties by registration order); providers inside a group run in registration order; `StopOnFirstGroupFailure` skips the later groups; partial validation names the groups to run** | Deterministic; later groups can assume the earlier ones passed (structural before business before external I/O); skipped groups cost nothing | A request with errors in several groups shows only the first failing group's errors when the flag is on (progressive disclosure, which the issue asks for) |
| **B) All groups run in parallel and the results are merged** | Lowest latency | `Order` and `StopOnFirstGroupFailure` would have no meaning; expensive groups run for requests that cannot pass |

### Chosen Option: **A — Sequential by order**

### Rationale

- The issue's API defines `order:` and `StopOnFirstGroupFailure` per group; both are meaningful only for sequential execution.

**Class:** Trivial
**Settled by:** the issue's own API (`ValidationGroup.Create("structural", order: 1)`, `options.StopOnFirstGroupFailure = true`) and its feature list "support for validation groups/phases", "partial validation support"

</details>

<details>
<summary><strong>4. Interplay with the async validator runner of #230</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Accept it and document it: once #230 has landed, each of the three providers also runs the registered `IRequestValidator<T>` validators, so a grouped configuration that uses two providers runs them twice; the aggregator de-duplicates the identical errors and the documentation says to keep async validators registered once (a group of its own backed by one provider)** | No change to #230; nothing to wait for | Duplicate I/O is possible in a mixed configuration |
| **B) A suppression switch in the runner of #230 that the grouped provider sets for all but one group** | No duplicate I/O | A change to the contract owned by #230, an ambient flag, and an ordering rule between the plans |

### Chosen Option: **A — Document and de-duplicate; revisit when #230 has landed** (recommended, pending the maintainer)

### Rationale

- Before #230 is implemented the interplay does not exist; the equivalence test of Phase 3 pins today's behavior and the test for the mixed configuration is added when #230's last pull request lands.
- B wins when an application mixes grouped providers with expensive async validators and cannot restructure its groups.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core `Encina.Validation`, next to `IValidationProvider`** | The three provider packages and the orchestrator already live there; no new project; keyed services need no extra package | Core grows by about eight files |
| **B) A new package `Encina.Validation.Aggregation`** | Core untouched | A package for an opt-in feature that every provider package would not reference; the grouped provider must be an `IValidationProvider`, which core defines |

### Chosen Option: **A — Core `Encina.Validation`**

### Rationale

- The contract, the orchestrator and `ValidationResult` are in `src/Encina/Validation/`; the aggregator and the grouped provider are part of that machinery and the issue's "Affected Packages" ticks `Encina (core)`.

**Class:** Trivial
**Settled by:** the existing layout (`src/Encina/Validation/IValidationProvider.cs`, `ValidationOrchestrator.cs`, `ValidationResult.cs`) and the issue's "Affected Packages" list

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | `ValidationAggregator` is a per-request builder and is not thread-safe (documented); `AggregatedValidationResult` is immutable | Trivial | The builder idiom (`System.Collections.Immutable` builders, `StringBuilder`) documented as not thread-safe |
| C2 | An exception from a group's provider propagates unchanged, exactly as from the single provider today: the request is denied and nothing reports success | Trivial | `ValidationOrchestrator.cs:49-81` (it catches only `OperationCanceledException`); `AGENTS.md` section 3 (errors are never swallowed) |
| C3 | Cancellation: the caller's `OperationCanceledException` propagates from the grouped provider and the orchestrator maps it as it does today | Settled | ADR-062 rule R5; `ValidationOrchestrator.cs:76-80` |
| C4 | An unknown group name in a partial validation call throws `ArgumentException` (a programming error, not a request failure); an empty request-time selection runs nothing and returns `Success` only when the caller passed an explicit empty list | Trivial | .NET argument-validation conventions; the call is made by application code, never from request data |
| C5 | The new error code `validation.aggregated_failed` is created with `EncinaErrors.Create(code, ErrorCategory.Validation, message, details: groupCounts)` (ADR-062 rule R3); its details hold group names and counts only | Settled | ADR-062 (rule R3 and the categorized `EncinaErrors.Create` overload); `AGENTS.md` section 3 (messages and values never reach logs) |
| C6 | Logs and the details carry request type name, group names (configuration constants, not request data) and counts, never an error message, property value or `EncinaError.Message`; the culture of #233 is not involved | Settled | `AGENTS.md` section 3 (#1168, #1173, #1259, #1274) |
| C7 | Registration completeness: `AddEncinaValidationGroups` replaces `IValidationProvider` with the grouped provider and `ValidationOrchestrator` with a scoped registration, in any registration order relative to the provider packages, because a singleton orchestrator (DataAnnotations and MiniValidator today) cannot depend on a scoped grouped provider; a DI test with `ValidateOnBuild` and `ValidateScopes` proves each of the six orders | Settled | `AGENTS.md` section 3 (registration completeness, never a scoped service into a singleton) |
| C8 | The default `MaxErrors` is 100 per aggregation; reaching it truncates the list, sets `Truncated`, and the result stays invalid (a cap can never turn a failure into a success); `0` is rejected at start-up | Dilemma (recommended, pending the maintainer) | numeric default; bounds memory when a hostile payload produces thousands of errors |
| C9 | `DeduplicateErrors` defaults to `true`: an error with the same property name and message already present (from a lower-order source) is dropped; the first in order wins | Dilemma (recommended, pending the maintainer) | public behavior; `false` is the alternative when sources should all be visible |
| C10 | Telemetry: no instrument is added; `AggregatedValidationResult` exposes `SourceCount`, `GroupCount`, `SkippedGroupCount` and `Truncated` so #726's central instrumentation can tag them, and any tenant tag is #726's, only when multi-tenancy is on (ADR-054 D4) | Settled | Issue #726 (open, proposed solution 4); ADR-054 D3, D4 |
| C11 | EventIds 610-611 are packed from the start of the sub-block 610-619 of the block 600-649 assigned to this family; the range `ValidationAggregation = (610, 619)` is registered first | Trivial | `AGENTS.md` section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |

---

## Implementation Phases

### Phase 1: Aggregator and Result Types (pull request S1)

> **Goal**: The issue's aggregator and its result, tested alone; nothing is registered.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/ValidationGroup.cs` (`public sealed record ValidationGroup(string Name, int Order)` with `Create(string name, int order)` validating a non-empty name), `AggregatedValidationError.cs` (`record AggregatedValidationError(ValidationError Error, string Group, int Order, string? Source)`), `AggregatedValidationResult.cs` (immutable; `Errors`, `IsValid`, `Truncated`, `SourceCount`, `GroupCount`, `SkippedGroupCount`, `ErrorsByGroup`, `ToValidationResult()`, `ToEither(string requestTypeName)` returning `Either<EncinaError, Unit>`), `AggregatedValidationErrorCodes.cs` (the constant `validation.aggregated_failed`) and `ValidationAggregator.cs` (sealed; `Add(ValidationResult result, ValidationGroup? group = null, string? source = null)`, `Add(ValidationError error, ...)`, `ToResult()`; constructor options `(int maxErrors = 100, bool deduplicate = true)`; ordering by group order, then insertion order; register rows C8, C9).
2. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
3. Tests (new): `ValidationAggregatorTests` (merge, group order, de-duplication on and off, truncation keeps the result invalid, source tagging), `AggregatedValidationResultTests` (`ToValidationResult`, `ToEither` code, category `Validation` and details without messages), `ValidationAggregationGuardTests` (null `result`/`error`/`name`, an empty group name, a non-positive `maxErrors`), `ValidationAggregatorProperties` (FsCheck: for any list of results the aggregated error set equals the de-duplicated union, the order is stable, and `IsValid` equals "every input was valid").

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #234 plan docs/plans/validation-aggregation-implementation-plan-234.md,
phase 1 of 4. Core Encina.Validation lives in src/Encina/Validation/ (ValidationResult.cs, ValidationOrchestrator.cs).

TASK:
1. Create ValidationGroup, AggregatedValidationError, AggregatedValidationResult, AggregatedValidationErrorCodes and
   ValidationAggregator. The result converts to ValidationResult and to Either<EncinaError, Unit>.
2. The Left uses EncinaErrors.Create("validation.aggregated_failed", ErrorCategory.Validation, message, details:...).

KEY RULES:
- Left is EncinaError, never a new error type. Details hold group names and counts only, never messages or values.
- A cap on errors truncates but never makes a failed result valid. No thread-safety promise on the aggregator.
- XML docs on every public member; PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina/Validation/ValidationResult.cs, ValidationOrchestrator.cs
- src/Encina/Results/EncinaErrors.cs (categorized Create overload)
```

</details>

---

### Phase 2: Grouped Provider, Options and Logging (pull request S2)

> **Goal**: The grouped `IValidationProvider`, its options and its EventIds; resolution of keyed providers is tested with hand-built service providers (registration comes in S3).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/ValidationGroupsOptions.cs` (`Groups` as an ordered list of `ValidationGroupDefinition(string Name, int Order, IReadOnlyList<string> ProviderKeys)`, `StopOnFirstGroupFailure`, `MaxErrors`, `DeduplicateErrors`) and `ValidationGroupsOptionsValidator.cs : IValidateOptions<ValidationGroupsOptions>` (unique non-empty names, at least one provider key per group, `MaxErrors` above zero).
2. **Create** `src/Encina/Validation/IValidationGroupRunner.cs` (`ValueTask<AggregatedValidationResult> ValidateAsync<TRequest>(TRequest request, IRequestContext context, IReadOnlyCollection<string>? groups, CancellationToken ct)`; `null` runs every group) and `GroupedValidationProvider.cs` (sealed; implements `IValidationProvider` and `IValidationGroupRunner`; constructor `(IServiceProvider serviceProvider, IOptions<ValidationGroupsOptions> options, ILogger<GroupedValidationProvider>? logger = null)`; resolves each group's providers with `GetRequiredKeyedService<IValidationProvider>(key)`; executes per Design Choice 3; `IValidationProvider.ValidateAsync` returns `ToValidationResult()`).
3. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationAggregation = (610, 619)`; **create** `src/Encina/Validation/ValidationAggregationLog.cs` (`[LoggerMessage]`, packed): 610 later groups skipped because a group failed (debug: request type, group name, error count, skipped group count), 611 the error cap was reached and the list truncated (warning: request type, limit). 612-619 stay free. XML docs name the range.
4. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina` also maps to the new range), `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
5. Tests (new): `GroupedValidationProviderTests` (order, stop on first failing group, partial selection, unknown group name throws, a throwing provider propagates, cancellation, two providers in one group), `ValidationGroupsOptionsValidatorTests`, `ValidationAggregationLogTests` (EventIds, no messages in the rendered text), `ValidationGroupsGuardTests` (null request/context, null logger is a valid case), `GroupedValidationProperties` (FsCheck: the grouped result equals the sequential merge of the members for any group order and selection).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #234 plan docs/plans/validation-aggregation-implementation-plan-234.md, phase 2 of 4. Depends on S1.
ADR-021: register the EventId range in src/Encina/Diagnostics/EventIdRanges.cs before using it.

TASK:
1. ValidationGroupsOptions + validator; IValidationGroupRunner; GroupedValidationProvider that runs ordered groups of
   keyed IValidationProvider services sequentially with StopOnFirstGroupFailure and partial selection.
2. Register ValidationAggregation (610, 619) and ValidationAggregationLog 610-611.

KEY RULES:
- A provider exception propagates; an unknown group name is an ArgumentException; never swallow a failure.
- Logs carry request type name, group names and counts only. A null logger is valid.
- EncinaEventIdAllocationTests must map the Encina assembly to the new range.

REFERENCE FILES:
- src/Encina/Validation/ValidationOrchestrator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 3: Registration and Provider Equivalence (pull request S3)

> **Goal**: `AddEncinaValidationGroups` wins in any registration order and the three providers behave the same inside a group.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/ValidationGroupsBuilder.cs` (`Group(string name, int order, Action<ValidationGroupBuilder> configure)`; `ValidationGroupBuilder.UseProvider<TProvider>() where TProvider : class, IValidationProvider` registers `AddKeyedScoped<IValidationProvider, TProvider>(key)` with a key built from the group name and a position, so the same provider type can serve two groups) and `ValidationGroupsServiceCollectionExtensions.cs` (`AddEncinaValidationGroups(this IServiceCollection, Action<ValidationGroupsBuilder> configure)`: options with `ValidateOnStart`, `Replace` of `IValidationProvider` with `GroupedValidationProvider` (scoped), `Replace` of `ValidationOrchestrator` with a scoped registration, `TryAddScoped<IValidationGroupRunner>` forwarding to the same instance, the behavior registration untouched).
2. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
3. Tests (new): `ValidationGroupsRegistrationTests` (DI: the six orders of `AddEncinaFluentValidation`, `AddDataAnnotationsValidation`, `AddEncinaValidationGroups` with `ValidateOnBuild` and `ValidateScopes`; the grouped provider is the resolved `IValidationProvider` in every order; the orchestrator resolves without a captive dependency), `ValidationGroupsExtensionsGuardTests` (null `services`, null `configure`, a null builder callback), `ValidationGroupsProviderEquivalenceTests` in `tests/Encina.UnitTests/Validation/` (the same required-field rule hosted in a DataAnnotations group, a FluentValidation group and a MiniValidator group gives the same aggregated errors in the same order; the three project references already exist in `Encina.UnitTests`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #234 plan docs/plans/validation-aggregation-implementation-plan-234.md, phase 3 of 4. Depends on S1 and S2.
AGENTS.md section 3: registration completeness is proven by a ValidateOnBuild + ValidateScopes DI test.

TASK:
1. ValidationGroupsBuilder with UseProvider<TProvider>() (keyed scoped registrations) and AddEncinaValidationGroups
   that replaces IValidationProvider and ValidationOrchestrator with scoped registrations in any order.
2. DI test for all six registration orders and the three-provider equivalence unit test.

KEY RULES:
- A singleton orchestrator must never capture the scoped grouped provider: the extension re-registers it as scoped.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer; XML docs on every public member.

REFERENCE FILES:
- src/Encina.FluentValidation/ServiceCollectionExtensions.cs (:62-64)
- src/Encina.DataAnnotations/ServiceCollectionExtensions.cs, src/Encina.MiniValidator/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/FluentValidation/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 4: Documentation and Finalization (pull request S4)

> **Goal**: Usage guide, changelog and justification files.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/validation-aggregation.md` through `docs-writer` (the aggregator, groups, stop on first failure, partial validation, the error code and category, the interplay note of Design Choice 4, a table of which provider fits which group).
2. **Create** `changelog.d/234-validation-aggregation.added.md` through `mechanical-fixer`.
3. **Create** the justification files for Load and Benchmark tests (`AGENTS.md` section 9): in-memory merge of small lists with no shared state; the existing `ValidationBenchmarks.cs` stays the provider benchmark.
4. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of the touched files against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #234 plan docs/plans/validation-aggregation-implementation-plan-234.md, phase 4 of 4.

TASK:
1. Document the aggregator and validation groups; add the changelog fragment and the two justification files.
2. Build and measure per-flag coverage against the manifest targets.

KEY RULES:
- Pages go through docs-writer; fragments through mechanical-fixer. Never type coverage figures by hand: cite cov: markers.
- Link ADRs by number; link sibling plans by issue number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-001, ADR-006, `AGENTS.md` section 3 | Operations return `Either<EncinaError, T>`; no exceptions for business logic | Result shape (Design Choice 2) |
| ADR-062 | Category contract; rule R3 field rules are `Validation`; R5 cancellation | Code `validation.aggregated_failed` (register row C5) |
| ADR-054 D3, D4 | One meter per package; tenant tag only when multi-tenancy is on | Why telemetry stays with #726 (row C10) |
| Keyed services in `Microsoft.Extensions.DependencyInjection` (.NET 8 and later) | `AddKeyedScoped`, `GetRequiredKeyedService` | Group membership (Design Choice 1) |
| FluentValidation 12.1.1 | Rule sets and severities exist but are FluentValidation-only | Why they are not the group mechanism |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IValidationProvider`, `ValidationOrchestrator`, `ValidationResult`, `ValidationError` | `src/Encina/Validation/` | The grouped provider is one more `IValidationProvider`; nothing else changes |
| The three provider registrations | `FluentValidation/ServiceCollectionExtensions.cs:62`, `DataAnnotations/ServiceCollectionExtensions.cs:72`, `MiniValidator/ServiceCollectionExtensions.cs:65` | The lifetimes the replacement must reconcile (row C7) |
| `EncinaErrors.Create` (categorized overload) | `src/Encina/Results/EncinaErrors.cs` | The aggregated `Left` |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | Range registration |
| Provider unit, guard, property folders | `tests/Encina.UnitTests/{FluentValidation,DataAnnotations,MiniValidator,Validation}`, `tests/Encina.GuardTests/Validation`, `tests/Encina.PropertyTests/Validation` | Where the new tests sit |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` core (`ValidationAggregation`, new, inside the block 600-649 assigned to the validation-2 family) | 610-619 | 610-611 used (two messages); 612-619 free; sibling plans in the same block: #233 600-609, #235 620-629, #236 630-639 |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Aggregator and result types | 5 (`ValidationGroup`, `AggregatedValidationError`, `AggregatedValidationResult`, `AggregatedValidationErrorCodes`, `ValidationAggregator`) | 1 (PublicAPI) | 4 (aggregator, result, guard, property) | 0 | 1 | 0 | 11 | ~900 |
| S2 | Grouped provider, options, logging | 5 (`ValidationGroupsOptions`, its validator, `IValidationGroupRunner`, `GroupedValidationProvider`, `ValidationAggregationLog`) | 2 (`EventIdRanges.cs`, PublicAPI) | 5 (provider, options validator, log, guard, property) | 1 (`EncinaEventIdAllocationTests.cs`) | 1 | 0 | 14 | ~1,000 |
| S3 | Registration and equivalence | 2 (`ValidationGroupsBuilder`, `ValidationGroupsServiceCollectionExtensions`) | 1 (PublicAPI) | 3 (DI, guard, equivalence) | 0 | 1 | 0 | 7 | ~600 |
| S4 | Documentation | 0 | 0 | 0 | 0 | 0 | 4 (feature page, changelog fragment, 2 justification `.md`) | 4 | ~250 |
| **Total** | | **12** | **4** | **12** | **1** | **3** | **4** | **36** | **~2,750** |

The columns sum to the total (12 + 4 + 12 + 1 + 3 + 4 = 36) and the pull-request rows sum to 36 files (11 + 14 + 7 + 4). The largest is S2 with 14 files and about 1,000 lines, at the limit: if the property test grows, move it to its own pull request. S1 first; S2 needs S1; S3 needs S1 and S2; S4 needs all. Verify caller counts with `git grep` before each pull request.

Per-file coverage targets (per flag, with a one-sentence justification), written into the manifest by the pull request that adds the file:

| File | Unit | Guard | Property | Justification |
|------|------|-------|----------|---------------|
| `ValidationAggregator.cs` | 95 | 90 | 70 | Merge, ordering, de-duplication and truncation are all reachable with plain results; the guard tests reach every argument check; the property test covers the union and order invariants |
| `AggregatedValidationResult.cs`, `AggregatedValidationError.cs`, `ValidationGroup.cs` | 95 | 85 | 0 | Immutable types with conversions and a factory that validates its name; no random structure beyond the aggregator's property test |
| `AggregatedValidationErrorCodes.cs` | 100 | 0 | 0 | One constant; no parameters to guard |
| `GroupedValidationProvider.cs` | 90 | 85 | 60 | Order, stop, partial, throwing and cancellation branches are reachable with fake keyed providers; the guard tests reach the argument checks and the null logger case; the property test compares with the sequential merge |
| `ValidationGroupsOptionsValidator.cs` | 95 | 0 | 0 | Pure rule checks on one options object; no further parameters for guard tests |
| `ValidationGroupsBuilder.cs`, `ValidationGroupsServiceCollectionExtensions.cs` | 90 | 90 | 0 | Registration is fully exercised by the six-order DI test; the null checks are guard-tested |
| `ValidationAggregationLog.cs` | 90 | 0 | 0 | Source-generated methods exercised by the logging test; no parameters to guard |
| `IValidationGroupRunner.cs` | 0 | 0 | 0 | Interface with no executable code; its implementation is covered above |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #234 - validation result aggregation and validation groups for Encina.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5): IValidationProvider, ValidationOrchestrator,
  ValidationPipelineBehavior live in core; FluentValidation, DataAnnotations and MiniValidator stay consistent.
- Railway Oriented Programming: Either<EncinaError, T>; every new error code carries an ErrorCategory (ADR-062).

IMPLEMENTATION OVERVIEW:
1. ValidationAggregator and AggregatedValidationResult in core Encina.Validation; ToEither returns
   validation.aggregated_failed with ErrorCategory.Validation and group counts only.
2. GroupedValidationProvider (an IValidationProvider) runs ordered groups of keyed providers, with StopOnFirstGroupFailure
   and partial validation through IValidationGroupRunner; EventIds 610-611 in a range registered first.
3. AddEncinaValidationGroups replaces provider and orchestrator with scoped registrations in any order.
Four pull requests (S1 to S4), see the Estimated File Count table.

KEY PATTERNS:
- Optional logger parameters; DI test with ValidateOnBuild and ValidateScopes over all registration orders.
- Never swallow a provider failure; a cap on errors never turns a failure into a success.
- PublicAPI.Unshipped.txt, manifests and changelog through mechanical-fixer; documentation through docs-writer.

REFERENCE FILES:
- src/Encina/Validation/*.cs
- src/Encina.FluentValidation/ServiceCollectionExtensions.cs
- docs/plans/validation-aggregation-implementation-plan-234.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Results are per request, as the issue says |
| 2 | OpenTelemetry | ⏭️ #726 | The issue asks for the source count on the trace: the result exposes `SourceCount`, `GroupCount`, `SkippedGroupCount` and `Truncated`, and #726's central instrumentation of `ValidationOrchestrator` tags them (tenant tag only when multi-tenancy is on, ADR-054 D4); this plan adds no instrument |
| 3 | Structured Logging | ✅ Phase 2 | `ValidationAggregationLog` EventIds 610-611 in the new range 610-619, by request type, group name and count only |
| 4 | Health Checks | ❌ N/A | In-process aggregation, no checkable dependency |
| 5 | Validation | ✅ All phases | This is the validation feature; the three providers of `AGENTS.md` section 5 can all back a group and an equivalence test pins it |
| 6 | Resilience | ❌ N/A | No external call; a provider that calls a service owns its resilience |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Validation runs before the handler; a scoped provider inside a group sees the request scope as before |
| 9 | Idempotency | ❌ N/A | Aggregation is deterministic and read-only |
| 10 | Multi-Tenancy | ❌ N/A | Aggregation is tenant-agnostic as the issue states; `TenantId` reaches member providers through the context; no tenant id is stored or keyed |
| 11 | Module Isolation | ❌ N/A | Per-request aggregation; module-scoped selection of providers is covered by the family follow-up that accompanies the plan of #230 |
| 12 | Audit Trail | ❌ N/A | Validation failures are not audit records; auditing of rejected requests is the follow-up of the family that accompanies the plan of #230 |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Validation orchestrator pattern (`IValidationProvider`, `ValidationOrchestrator`) | ✅ On `main` | Integration point exists |
| ADR-062 error category contract (`EncinaErrors.Create` with a category) | ✅ Merged | Code of register row C5 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| `IRequestValidator<T>` and the runner | #230 | Interplay of Design Choice 4; independent for implementation |
| Coded `ValidationError` | #233 | The aggregator carries `ErrorCode` and `Arguments` through unchanged; it does not need them |
| Code-less validation `Left` never becomes a 400 | #3415 | This plan's `Left` has a code; the orchestrator's own `Left` stays as is |
| Central validation instrumentation | #726 | Consumes the counts of register row C10 |
| Domain-phase consumer | #236 | Uses the aggregator for handler-side errors |

---

## Next Steps

1. Link this plan from #234 and ask the maintainer to answer Design Choices 1 and 4 and register rows C8 and C9 when work starts.
2. Implement S1 to S4 as the pull requests of the Estimated File Count split, `Fixes #234` on S4.
