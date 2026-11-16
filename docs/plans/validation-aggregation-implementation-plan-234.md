# Implementation Plan: Validation Result Aggregation and Validation Groups

> **Issue**: [#234](https://github.com/dlrivada/Encina/issues/234)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, `RequestValidatorRunner` and EventIds 550-559; interplay in Design Choice 4), #233 (owns `ValidationError.ErrorCode`; the aggregator carries it through untouched), #235 and #236 (planned apart; #236 uses the aggregator for its domain phase), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), ADR-001, ADR-006, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (core types, one provider decorator, one registration extension; no persistence)
> **Estimated Scope**: ~950 lines of production code + ~1,800 lines of tests + ~250 lines of documentation, in four pull requests of at most 15 files and 1,100 lines each

---

## Summary

### What exists today (read on 2026-11-16, main `3c95c4b2c`)

- `ValidationOrchestrator` calls exactly one `IValidationProvider` (`ValidationOrchestrator.cs:65`) and turns an invalid `ValidationResult` into a code-less `Left` (`:71`). Each provider package registers itself with `TryAdd`, so the first package registered wins and there is no way to run two providers for one request: `AddEncinaFluentValidation` uses `TryAddScoped` (`FluentValidation/ServiceCollectionExtensions.cs:62`), `AddDataAnnotationsValidation` and `AddMiniValidation` use `TryAddSingleton` (`DataAnnotations/ServiceCollectionExtensions.cs:72`, `MiniValidator/ServiceCollectionExtensions.cs:65`).
- `ValidationResult` is an immutable list of `ValidationError(PropertyName, ErrorMessage)` (`ValidationResult.cs:97`) with `Success`, `Failure(...)` and `ToErrorMessage` (`:77`); it has no group, source, priority, ordering rule or merge operation. Inside one provider the only aggregation is `FluentValidationProvider` flattening the failures of its validators (`FluentValidationProvider.cs:73-77`).
- The issue's example is `ValidationAggregator.Add(...)` ... `Either<AggregatedValidationErrors, Unit>`. A left type other than `EncinaError` conflicts with the ROP rule of `AGENTS.md` section 3 (operations return `Either<EncinaError, T>`), and the orchestrator and every host read `EncinaError`.
- Not in `src`: `ValidationAggregator`, `ValidationGroup`, `StopOnFirstGroupFailure`, partial validation, any composite or grouped `IValidationProvider`, any error ordering or de-duplication rule, any EventId range for validation aggregation.

### What this plan delivers

Two related capabilities, both opt-in and in core `Encina.Validation`:

1. **`ValidationAggregator`**, the issue's builder: it collects `ValidationResult`s from several sources, tags each error with its group and source, orders and de-duplicates them deterministically, caps their number, and produces an `AggregatedValidationResult` that converts to a plain `ValidationResult` or to `Either<EncinaError, Unit>` (code `validation.aggregated_failed`, category `Validation`, the group counts as details, never the messages).
2. **Validation groups**: `AddEncinaValidationGroups(...)` registers a `GroupedValidationProvider` that runs an ordered list of named groups (`structural`, `business`, `external`), each backed by one or more of the existing providers registered as keyed services, optionally stopping after the first failing group (`StopOnFirstGroupFailure`) and optionally validating only some groups (partial validation, through `IValidationGroupRunner`). It is itself an `IValidationProvider`, so `ValidationPipelineBehavior` and all three provider packages are unchanged.
3. **Reaching the aggregated `Left` through the pipeline** (Design Choice 6): a grouped provider that returns a plain `ValidationResult` would make the unchanged `ValidationOrchestrator` build its own code-less `Left` (`EncinaError.New(errorMessage)`, `ValidationOrchestrator.cs:71-72`), so `validation.aggregated_failed` would never leave the library. The recommended design therefore changes one existing file: `ValidationOrchestrator` asks whether its provider is an `IValidationGroupRunner` and, if so, returns the aggregated `Left`. This edit is counted in S3.

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
- The code starts with `validation.`, which the HTTP mapping already routes to 400 until the category replaces the prefix (`ProblemDetailsExtensions.cs:212`), and unlike the orchestrator's code-less `Left` (#3415) it can be rendered. It reaches the pipeline only through the orchestrator change of Design Choice 6; without that change it is reachable only when application code calls `ToEither` itself.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (Railway Oriented Programming: operations return `Either<EncinaError, T>`), ADR-001, ADR-006 and ADR-062 rule R3; option B violates the first, so A is the only compliant option

</details>

<details>
<summary><strong>3. Execution semantics of groups</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Groups run sequentially in `Order` (ties: register row C15); providers inside a group run in registration order; `StopOnFirstGroupFailure` skips the later groups; partial validation names the groups to run** | Deterministic; later groups can assume the earlier ones passed (structural before business before external I/O); skipped groups cost nothing | A request with errors in several groups shows only the first failing group's errors when the flag is on (progressive disclosure, which the issue asks for) |
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

- Before #230 is implemented the interplay does not exist; the equivalence test of Phase 3 pins today's behavior. The test for the mixed configuration can only be written when #230's last pull request lands: follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/validation-groups-async-validator-duplicate-test.md`; it names `tests/Encina.UnitTests/Validation/ValidationGroupsAsyncValidatorTests.cs`).
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

<details>
<summary><strong>6. How the aggregated error reaches the pipeline</strong></summary>

`ValidationPipelineBehavior` calls the sealed class `ValidationOrchestrator` (`ValidationPipelineBehavior.cs:38,45,61`), which calls `IValidationProvider.ValidateAsync`, gets a `ValidationResult` and builds the `Left` itself with `EncinaError.New(errorMessage)` (`ValidationOrchestrator.cs:66-72`, no code, no category). A provider that returns `ValidationResult` therefore cannot make `validation.aggregated_failed` reach a caller of `IEncina.Send`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValidationOrchestrator` checks `_validationProvider is IValidationGroupRunner` and, when true, calls the runner with every group and returns `aggregated.ToEither(typeof(TRequest).Name)`; otherwise today's path** | One changed file; the behavior, the three provider packages and the 13 `new ValidationOrchestrator(` sites (constructor unchanged) stay as they are; the aggregated `Left` is reachable through `Send` | The orchestrator learns one capability interface; a runner that is wrapped by another decorator is not detected |
| **B) `ValidationPipelineBehavior` (or a new behavior) takes `IValidationGroupRunner` as an optional dependency and bypasses the orchestrator** | The orchestrator stays untouched | A behavior constructor change (its contract and guard tests change), a second validation path that skips the orchestrator's cancellation and null checks, and a second place that owns the `Left` |
| **C) Keep the plain `ValidationResult` path and document that `validation.aggregated_failed` is returned only when application code calls `ToEither`** | No change to existing files | Grouped validation through the pipeline keeps the code-less `Left` of #3415, so the feature's error cannot be rendered in the main use case |

### Chosen Option: **A — The orchestrator recognizes `IValidationGroupRunner`** (recommended, pending the maintainer)

### Rationale

- A is the smallest change that makes the aggregated error reachable through the real path; it is counted in S3 (`ValidationOrchestrator.cs` changed, one new unit test class).
- B wins when the maintainer does not want `ValidationOrchestrator` to change; C wins when #3415's rendering of code-less validation errors is going to be fixed in the orchestrator and the aggregated code adds nothing.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | `ValidationAggregator` is a per-request builder and is not thread-safe (documented); `AggregatedValidationResult` is immutable | Trivial | The builder idiom (`System.Collections.Immutable` builders, `StringBuilder`) documented as not thread-safe |
| C2 | An exception from a group's provider propagates unchanged, exactly as from the single provider today: the request is denied and nothing reports success | Trivial | `ValidationOrchestrator.cs:49-81` (it catches only `OperationCanceledException`); `AGENTS.md` section 3 (errors are never swallowed) |
| C3 | Cancellation: the caller's `OperationCanceledException` propagates from the grouped provider and the orchestrator maps it as it does today (`ValidationOrchestrator.cs:76-80`); the alternative is a runner that returns a `Left` of category `Cancelled` | Dilemma (recommended, pending the maintainer) | ADR-062 rule R5 is satisfied by both; propagation keeps one cancellation path, the `Left` suits callers of the runner that never see the orchestrator |
| C4 | Partial validation input errors: `groups = null` runs every group. An unknown group name and an empty non-null selection are caller bugs; both must deny and neither may report success (an empty selection that succeeds would run no validation and let the request through). Option A: the runner throws `ArgumentException`; option B: the runner returns a `Left` with category `Internal` (codes `validation.unknown_group`, `validation.no_groups_selected`), which needs the runner to return `Either<EncinaError, AggregatedValidationResult>` | Dilemma (recommended, pending the maintainer) | Recommended: B, because `AGENTS.md` section 3 prefers `Either` over exceptions for failures and both outcomes deny; A wins when the caller bug should surface as an exception in tests and logs |
| C5 | The new error code `validation.aggregated_failed` is created with `EncinaErrors.Create(code, ErrorCategory.Validation, message, details: groupCounts)` (ADR-062 rule R3); its details hold group names and counts only | Settled | ADR-062 (rule R3 and the categorized `EncinaErrors.Create` overload); `AGENTS.md` section 3 (messages and values never reach logs) |
| C6 | Logs and the details carry request type name, group names (configuration constants, not request data) and counts, never an error message, property value or `EncinaError.Message`; the culture of #233 is not involved | Settled | `AGENTS.md` section 3 (#1168, #1173, #1259, #1274) |
| C7 | Registration completeness: `AddEncinaValidationGroups` replaces `IValidationProvider` with the grouped provider and `ValidationOrchestrator` with a scoped registration, in any registration order relative to the provider packages (a singleton orchestrator, as DataAnnotations and MiniValidator register it, cannot depend on a scoped grouped provider); a DI test with `ValidateOnBuild` and `ValidateScopes` proves each of the six orders. Alternatives that also avoid the captive dependency: a singleton grouped provider that creates a scope per call through `IServiceScopeFactory`, or leaving the packages' lifetimes alone and resolving per request | Dilemma (recommended, pending the maintainer) | `AGENTS.md` section 3 (registration completeness, never a scoped service into a singleton) is satisfied by all three; scoped replacement is recommended because it keeps member providers in the request scope (FluentValidation validators are scoped), and the scope-per-call option wins when the application wants to keep the singleton orchestrator |
| C8 | The default `MaxErrors` is 100 per aggregation; reaching it truncates the list, sets `Truncated`, and the result stays invalid (a cap can never turn a failure into a success); `0` is rejected at start-up | Dilemma (recommended, pending the maintainer) | numeric default; bounds memory when a hostile payload produces thousands of errors |
| C9 | `DeduplicateErrors` defaults to `true`: an error with the same property name and message already present (from a lower-order source) is dropped; the first in order wins. The key is `(PropertyName, ErrorMessage)` today; when #233 lands `ErrorCode`, the key becomes `(PropertyName, ErrorCode, ErrorMessage)` so two errors with different codes and the same message are both kept (and `Arguments` are not part of the key, because they only parametrize the message) | Dilemma (recommended, pending the maintainer) | public behavior; `false` is the alternative when sources should all be visible |
| C10 | Telemetry (the issue's cross-cutting table marks OpenTelemetry "Included"; this plan changes that to deferred to #726): no instrument is added; `AggregatedValidationResult` exposes `SourceCount`, `GroupCount`, `SkippedGroupCount` and `Truncated` so #726's central instrumentation can tag them, and any tenant tag is #726's, only when multi-tenancy is on (ADR-054 D4) | Settled | Issue #726 (open, proposed solution 4); ADR-054 D3, D4 |
| C11 | EventIds 610-611 are packed from the start of the sub-block 610-619 of the block 600-649 assigned to this family; the range `ValidationAggregation = (610, 619)` is registered first | Trivial | `AGENTS.md` section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C12 | Public API shape against the issue: the issue shows `AddEncinaValidation(options => options.Groups = ...)` and `Either<AggregatedValidationErrors, Unit>`; the plan names `AddEncinaValidationGroups(Action<ValidationGroupsBuilder>)` with `Group(name, order, ...)` and `UseProvider<T>()`, `ValidationAggregator`, `AggregatedValidationResult.ToEither(string requestTypeName)` and `IValidationGroupRunner`. The alternative is the issue's single options-object form with a nested group list (no builder type), keeping the issue's names for the extension | Dilemma (recommended, pending the maintainer) | The builder is recommended because a group needs a generic `UseProvider<T>()` registration that an options property cannot express; the options form wins when the maintainer wants the issue's API verbatim. The `Either` left type is not part of this Dilemma: it is Design Choice 2 |
| C13 | Keyed-service key of a group member: an internal `readonly record struct ValidationGroupProviderKey(string Group, int Position)` (value equality on both fields, so two different group and position pairs never share a key and the same provider type can serve two groups); `ValidationGroupDefinition` carries the number of members, not strings | Trivial | Internal, no public behavior; keyed DI compares keys with `Equals`, which a record struct defines per field |
| C14 | `ValidationAggregator.Add(result, group: null)` puts the errors in an implicit group named `ungrouped` with order `int.MaxValue` (last); the name is reserved and the options validator rejects a configured group with that name | Dilemma (recommended, pending the maintainer) | Alternatives: a required group argument (no default), or an implicit first group (order `int.MinValue`); last is recommended because ungrouped errors are the least specific, first wins when they are structural |
| C15 | Ties between groups with the same `Order`: broken by registration order, documented (the aggregator breaks ties by insertion order, so the behavior is the same); the alternative is rejecting duplicate `Order` values at start-up in the options validator | Dilemma (recommended, pending the maintainer) | Registration order is recommended (no start-up failure for a harmless tie); rejection wins when an exact order matters to the application |
| C16 | `AggregatedValidationError.Source` is a constant string, never request data: the grouped provider sets it to `provider.GetType().Name` of the member; a caller of `Add` passes its own constant. The alternative is the group name only, with no source | Dilemma (recommended, pending the maintainer) | Both satisfy `AGENTS.md` section 3 (no request data in logs or tags); the type name is recommended because two members of one group stay distinguishable |
| C17 | Log levels: 610 (later groups skipped) is `Debug`, 611 (error cap reached) is `Warning`; neither carries a message or value | Trivial | `Debug` is routine progressive disclosure on every failing request; `Warning` marks dropped information; same pattern as the existing source-generated logs in `src/Encina/Diagnostics/RequestIdentityLog.cs` |
| C18 | Issue criterion "error categorization and prioritization": categorization is the group and source of each error plus the single `Validation` category of the aggregated `Left` (ADR-062 rule R3), prioritization is the group order (`Order`, then insertion order); per-error severity or custom categories are not added | Trivial | Maps the issue's wording onto types that exist in this plan; severity does not exist in `ValidationError` today (`ValidationResult.cs:97`) |
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

1. **Create** `src/Encina/Validation/ValidationGroupsOptions.cs` (`Groups` as an ordered list of `ValidationGroupDefinition` with a validating constructor `(string name, int order, int providerCount)`: non-blank name, at least one provider; `StopOnFirstGroupFailure`, `MaxErrors`, `DeduplicateErrors`) and `ValidationGroupsOptionsValidator.cs : IValidateOptions<ValidationGroupsOptions>` (unique non-empty names, the name `ungrouped` reserved (row C14), at least one provider per group, `MaxErrors` above zero; duplicate `Order` values allowed unless row C15 resolves the other way).
2. **Create** `src/Encina/Validation/IValidationGroupRunner.cs` (`ValueTask<Either<EncinaError, AggregatedValidationResult>> ValidateAsync<TRequest>(TRequest request, IRequestContext context, IReadOnlyCollection<string>? groups, CancellationToken ct)`; `null` runs every group; an unknown group or an empty selection denies, per register row C4 option B; option A returns `ValueTask<AggregatedValidationResult>` and throws) and `GroupedValidationProvider.cs` (sealed; implements `IValidationProvider` and `IValidationGroupRunner`; constructor `(IServiceProvider serviceProvider, IOptions<ValidationGroupsOptions> options, ILogger<GroupedValidationProvider>? logger = null)`; resolves each group's providers with `GetRequiredKeyedService<IValidationProvider>(new ValidationGroupProviderKey(group, position))` (row C13); executes per Design Choice 3; `Source` per row C16; `IValidationProvider.ValidateAsync` runs every group and returns `ToValidationResult()` for callers that use the provider directly; when the runner returns a `Left` the provider throws `InvalidOperationException`, because that can only be a configuration bug and a plain `ValidationResult` cannot carry it). The internal `ValidationGroupProviderKey.cs` is created here.
3. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationAggregation = (610, 619)` (with its `PublicAPI.Unshipped.txt` line `Encina.Diagnostics.EventIdRanges.ValidationAggregation`); **create** `src/Encina/Diagnostics/ValidationAggregationLog.cs` (internal, `[LoggerMessage]`, packed; in `Diagnostics/` like `RequestIdentityLog.cs`): 610 later groups skipped because a group failed (`Debug`: request type, group name, error count, skipped group count), 611 the error cap was reached and the list truncated (`Warning`: request type, limit), levels per row C17. 612-619 stay free. XML docs name the range.
4. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina` also maps to the new range), `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
5. Tests (new): `GroupedValidationProviderTests` (order, stop on first failing group, partial selection, unknown group and empty selection deny, a throwing provider propagates, cancellation per row C3, two providers in one group), `ValidationGroupsOptionsValidatorTests`, `ValidationAggregationLogTests` (EventIds, no messages in the rendered text), `ValidationGroupsGuardTests` (null request/context on the provider and the runner, null logger is a valid case, `ValidationGroupsOptionsValidator.Validate(string? name, ValidationGroupsOptions options)` with a null `options` and a null `name` as a valid case, the `ValidationGroupDefinition` constructor with a null/blank name and a non-positive provider count), `GroupedValidationProperties` (FsCheck: the grouped result equals the sequential merge of the members for any group order and selection).

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
- A provider exception propagates; an unknown group name or an empty selection denies (register row C4); never swallow a failure.
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

1. **Create** `src/Encina/Validation/ValidationGroupsBuilder.cs` (`Group(string name, int order, Action<ValidationGroupBuilder> configure)`; `ValidationGroupBuilder.UseProvider<TProvider>() where TProvider : class, IValidationProvider` registers `AddKeyedScoped<IValidationProvider, TProvider>(new ValidationGroupProviderKey(group, position))` (row C13), so the same provider type can serve two groups) and `ValidationGroupsServiceCollectionExtensions.cs` (`AddEncinaValidationGroups(this IServiceCollection, Action<ValidationGroupsBuilder> configure)`: options with `ValidateOnStart`, `Replace` of `IValidationProvider` with `GroupedValidationProvider` (scoped), `Replace` of `ValidationOrchestrator` with a scoped registration (row C7), `TryAddScoped<IValidationGroupRunner>` forwarding to the same instance, the behavior registration untouched).
2. **Modify** `src/Encina/Validation/ValidationOrchestrator.cs` (Design Choice 6, option A): after the null and cancellation checks, when `_validationProvider is IValidationGroupRunner runner` it calls `runner.ValidateAsync(request, context, null, cancellationToken)` and returns `aggregated.ToEither(typeof(TRequest).Name)` for an invalid result, `Right(Unit.Default)` for a valid one, and the runner's `Left` as is; otherwise today's code path. The constructor does not change, so the 13 `new ValidationOrchestrator(` sites in `tests/` (verified on `3c95c4b2c`) need no edit; the existing `OperationCanceledException` `catch` keeps mapping cancellation.
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer` (the manifest edit enters the targets of the two S3 files and raises the Contract target of `GroupedValidationProvider.cs` from 0 to 60, because `ValidationGroupRunnerContractTests` below is its only contract test).
4. Tests (new): `ValidationGroupsRegistrationTests` (DI: the six orders of `AddEncinaFluentValidation`, `AddDataAnnotationsValidation`, `AddEncinaValidationGroups` with `ValidateOnBuild` and `ValidateScopes`; the grouped provider is the resolved `IValidationProvider` in every order; the orchestrator resolves without a captive dependency), `ValidationGroupsExtensionsGuardTests` (null `services`, null `configure`, a null builder callback), `ValidationGroupsProviderEquivalenceTests` in `tests/Encina.UnitTests/Validation/` (the same required-field rule hosted in a DataAnnotations group, a FluentValidation group and a MiniValidator group gives the same aggregated errors in the same order; the three project references already exist in `Encina.UnitTests`), `ValidationOrchestratorGroupedTests` in `tests/Encina.UnitTests/Validation/` (a runner-backed orchestrator returns the `validation.aggregated_failed` `Left` with category `Validation`, a valid result returns `Right`, a plain provider keeps today's code-less `Left`), and `ValidationGroupRunnerContractTests` in `tests/Encina.ContractTests/Core/Validation/` (next to `ValidationPipelineBehaviorContractTests.cs`; it builds a real `GroupedValidationProvider` over hand-built keyed providers and pins the public contract of `IValidationGroupRunner`: `null` selects every group, group order and `StopOnFirstGroupFailure` hold, the runner and `IValidationProvider.ValidateAsync` agree on the errors, and the result converts to the coded `Left`).

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
2. ValidationOrchestrator returns the aggregated Left when its provider is an IValidationGroupRunner (Design Choice 6).
3. DI test for all six registration orders, the three-provider equivalence unit test, the orchestrator test and the
   contract test of IValidationGroupRunner.

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
3. **Create** the three justification files of `AGENTS.md` section 9 (never for unit, guard or contract tests, which the plan implements): `tests/Encina.LoadTests/Validation/ValidationAggregation.md` and `tests/Encina.BenchmarkTests/Validation/ValidationAggregation.md` (in-memory merge of small lists with no shared state; the existing `ValidationBenchmarks.cs` stays the provider benchmark) and `tests/Encina.IntegrationTests/Validation/ValidationAggregation.md` (a non-database feature with no external dependency; the real-DI path with the three real providers is covered by `ValidationGroupsRegistrationTests` and `ValidationGroupsProviderEquivalenceTests` of S3).
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
| `IValidationProvider`, `ValidationOrchestrator`, `ValidationResult`, `ValidationError` | `src/Encina/Validation/` | The grouped provider is one more `IValidationProvider`; only `ValidationOrchestrator` changes (Design Choice 6) |
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
| S2 | Grouped provider, options, logging | 6 (`ValidationGroupsOptions`, its validator, `IValidationGroupRunner`, `GroupedValidationProvider`, internal `ValidationGroupProviderKey`, `ValidationAggregationLog` in `Diagnostics/`) | 2 (`EventIdRanges.cs`, PublicAPI) | 5 (provider, options validator, log, guard, property) | 1 (`EncinaEventIdAllocationTests.cs`) | 1 | 0 | 15 | ~1,050 |
| S3 | Registration, orchestrator change, equivalence, contract | 2 (`ValidationGroupsBuilder`, `ValidationGroupsServiceCollectionExtensions`) | 2 (`ValidationOrchestrator.cs`, PublicAPI) | 5 (DI, guard, equivalence, orchestrator, contract) | 0 | 1 | 0 | 10 | ~850 |
| S4 | Documentation | 0 | 0 | 0 | 0 | 0 | 5 (feature page, changelog fragment, 3 justification `.md`: load, benchmark, integration) | 5 | ~280 |
| **Total** | | **13** | **5** | **14** | **1** | **3** | **5** | **41** | **~3,100** |

The columns sum to the total (13 + 5 + 14 + 1 + 3 + 5 = 41) and the pull-request rows sum to 41 files (11 + 15 + 10 + 5). The changed source files are the PublicAPI file (S1, S2 and S3 each), `EventIdRanges.cs` (S2) and `ValidationOrchestrator.cs` (S3). The largest is S2 with 15 files and about 1,050 lines, at the limit: if the property test grows, move it to its own pull request. S1 first; S2 needs S1; S3 needs S1 and S2; S4 needs all. Verify caller counts with `git grep` before each pull request.

Per-file coverage targets (per flag, with a one-sentence justification), written into the manifest by the pull request that adds the file:

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `ValidationAggregator.cs` | 95 | 90 | 0 | 70 | Merge, ordering, de-duplication and truncation are all reachable with plain results; the guard tests reach every argument check; the property test covers the union and order invariants; a concrete builder, not a public interface or abstract class, so no contract test |
| `AggregatedValidationResult.cs`, `AggregatedValidationError.cs`, `ValidationGroup.cs` | 95 | 85 | 0 | 0 | Immutable types with conversions and a factory that validates its name; no random structure beyond the aggregator's property test; the coded `Left` is pinned by the contract test of the runner |
| `AggregatedValidationErrorCodes.cs` | 100 | 0 | 0 | 0 | One constant; no parameters to guard |
| `GroupedValidationProvider.cs` | 90 | 85 | 0 (S2), raised to 60 in S3 | 60 | Order, stop, partial, throwing and cancellation branches are reachable with fake keyed providers; the guard tests reach the argument checks and the null logger case; the Contract target is 0 in S2 because the only contract test (the real provider through the public runner contract) lands in S3, which raises it to 60 in its manifest edit; the property test compares with the sequential merge |
| `ValidationGroupsOptions.cs` (includes `ValidationGroupDefinition`) | 90 | 85 | 0 | 0 | The definition's constructor validates name and provider count and is guard-tested; the options class is plain properties |
| `ValidationGroupsOptionsValidator.cs` | 95 | 85 | 0 | 0 | Pure rule checks on one options object; `Validate(string? name, options)` has a reference-type parameter, so the guard test covers its null `options` and the null `name` as a valid case |
| `ValidationGroupProviderKey.cs` | 100 | 0 | 0 | 0 | Internal record struct without parameters to validate; used by every registration and resolution test |
| `ValidationGroupsBuilder.cs`, `ValidationGroupsServiceCollectionExtensions.cs` | 90 | 90 | 0 | 0 | Registration is fully exercised by the six-order DI test; the null checks are guard-tested |
| `ValidationOrchestrator.cs` (changed, S3) | 95 | 85 | 0 | 0 | The new runner branch and the unchanged branch are both reachable with a fake runner and a fake provider; existing guard tests keep the null checks (no new parameter) |
| `ValidationAggregationLog.cs` | 90 | 0 | 0 | 0 | Internal source-generated methods exercised by the logging test; no parameters to guard that the guard project can reach (internal type) |
| `IValidationGroupRunner.cs` | 0 | 0 | 0 | 0 | Interface with no executable code, so no flag has a line to cover; the contract it declares is exercised through `GroupedValidationProvider.cs` (contract row above) |

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
3. AddEncinaValidationGroups replaces provider and orchestrator with scoped registrations in any order, and
   ValidationOrchestrator returns the aggregated Left when its provider is an IValidationGroupRunner.
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

1. Link this plan from #234 and ask the maintainer to answer Design Choices 1, 4 and 6 and register rows C3, C4, C7, C8, C9, C12, C14, C15 and C16 when work starts.
2. Implement S1 to S4 as the pull requests of the Estimated File Count split, `Fixes #234` on S4.
