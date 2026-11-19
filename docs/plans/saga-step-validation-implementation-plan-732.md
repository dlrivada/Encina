# Implementation Plan: Validation of Saga Step Input Data

> **Issue**: [#732](https://github.com/dlrivada/Encina/issues/732)
> **Type**: Feature (validation, saga)
> **Related**: #729 (owns `ValidationOrchestrator.EvaluateMessageAsync`, which this plan calls; open its S1 first), #730 (the CDC counterpart, same design), #720 (saga compensation transaction: changes `SagaRunner`'s constructor and `CommitAsync`), #736 (compensation idempotency), #1237 (correlation wait steps), #750 (saga audit), #738 (saga tenant), #1581 (the runner discards the `Left` of the orchestrator's persisting calls), #726 (central validation instrumentation), ADR-001, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one runner stage, one contract, one fluent marker; no persistence change)
> **Estimated Scope**: ~2,500 lines in total (~340 production, ~1,700 tests, ~460 documentation), in four pull requests of at most 13 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-17, main `822c310c7`)

- The issue names `ISagaStepValidator<TStep, TData>`. The saga API has no step types: a saga is a `BuiltSagaDefinition<TData>` of named steps (`SagaStepDefinition<TData>(Name, Execute, Compensate)`, `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs`), each `Execute` a delegate `Func<TData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TData>>>` built by `SagaStepBuilder<TData>` (`SagaStepBuilder.cs:145` is the only `new SagaStepDefinition<`). The step input is the saga data `TData` produced by the previous step.
- `SagaRunner.ExecuteStepsAsync` (`src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs:124-126`) logs the step and calls `step.Execute(progress.Data, requestContext, cancellationToken)`; nothing checks `progress.Data` first. A step `Left` goes to `HandleStepFailedAsync` (`:160`), which persists only the error code (`StartCompensationAsync(sagaId, errorCode)`, `:183`; the orchestrator's contract says the persisted `ISagaState.ErrorMessage` is "never free text"), compensates the completed steps in reverse, and ends the saga `Compensated` (or `Failed` when a compensation fails); the caller receives the original step error.
- `SagaRunner` is registered scoped (`MessagingServiceCollectionExtensions.cs:369`) with `(SagaOrchestrator, IRequestContextAccessor, ILogger<SagaRunner>)` (`SagaRunner.cs:36-39`); `new SagaRunner(` appears in 14 places (src and tests). `Encina.Messaging` has no `InternalsVisibleTo` for `Encina.GuardTests`, so a new type with guarded parameters must be public to be guard-tested.
- `ErrorMessage` is an unbounded text column in the six ADO.NET and Dapper scripts (`NVARCHAR(MAX)`, `TEXT`, `LONGTEXT`), so the saga store needs no schema or provider change for this feature.
- The three providers validate any object (`IValidationProvider.ValidateAsync<TRequest>` is unconstrained); #729 S1 adds `ValidationOrchestrator.EvaluateMessageAsync<TMessage>`, which returns the provider's `ValidationResult` unchanged.
- `SagaErrorCodes` (`SagaErrorCodes.cs`) has no validation code, and its factories use the uncategorized `EncinaErrors.Create`; the saga log class `Log` in `SagaRunner.cs` uses 2888-2898 inside the `Messaging` range (2800-2999), where #736 claims 2971-2972.

### What this plan delivers

1. **`ISagaStepValidator<TData>`** (`Encina.Messaging`): `StepName` plus `ValidateAsync(TData data, IRequestContext context, CancellationToken)` returning the provider-neutral `ValidationResult`; registered per step name with `AddSagaStepValidator<TData, TValidator>()`.
2. **A provider adapter, opt-in per step**: `AddSagaStepProviderValidation<TData>(string stepName)` registers a validator that runs the registered `IValidationProvider` (FluentValidation, DataAnnotations or MiniValidator) on the saga data through `ValidationOrchestrator.EvaluateMessageAsync` (#729).
3. **`SagaStepBuilder<TData>.ValidateInput()`**: marks a step as requiring validation of its input. A marked step with no validator registered for its name fails closed.
4. **A runner stage**: before `step.Execute`, a marked step runs every validator registered for its name; any failure takes the existing step-failure path (the step does not execute, the completed steps are compensated, the saga ends `Compensated`, the persisted reason is `saga.step_validation_failed`) and the caller receives the categorized `Left`.

Nothing changes for a saga that marks no step.

- Affected packages: `Encina.Messaging` (production), `Encina` (one range registration in `EventIdRanges.cs`). The saga stores of the ten database providers are untouched; the provider-neutral integration scenarios run on all of them (S3). The three validation packages need no code change.
- Provider category: Database (10) for the persisted outcome (the existing scenarios class runs on all ten stores) and Validation (3) for the adapter (`AGENTS.md` section 5).
- Shared pieces: consumes #729's `EvaluateMessageAsync`; owns `saga.step_validation_failed` and `saga.step_validation_unavailable`.

---

## Design Choices

<details>
<summary><strong>1. The validator contract and where step rules live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A DI contract `ISagaStepValidator<TData>` with a `StepName`, registered per step, plus a per-step provider adapter and the `ValidateInput()` marker on the step** | Rules are classes with injected dependencies (a scoped validator may use a `DbContext`), discoverable and unit-testable; the three providers are available through one registration; matches the issue's interface idea without step types that do not exist | A string join between the definition (`Step("Charge")`) and the registration; one new public interface and two extensions |
| **B) A delegate on the builder: `.Step("Charge").ValidateInput((data, ctx, ct) => ...)`** | Inline, no DI, no name join | Closures cannot take scoped services; every definition repeats its rules; the provider path needs a separate marker anyway |
| **C) The issue's literal `ISagaStepValidator<TStep, TData>` with marker step classes** | Matches the issue text | The low-ceremony API has no step classes; adding them is a second saga authoring model for one feature |

### Chosen Option: **A — `ISagaStepValidator<TData>` by step name, provider adapter, builder marker** (recommended, pending the maintainer)

### Rationale

- A keeps the rules where the application's other validators live (DI, any lifetime) and reuses the validation providers, which is the issue's "same validation providers as command validation".
- B wins for sagas whose definitions are built inline with a few trivial checks and no dependencies; the marker stays and a delegate overload can be added later without changing A.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The saga state after an invalid step input</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The existing step-failure path: the invalid step does not execute, the completed steps are compensated, the saga ends `Compensated` with `ErrorMessage` = `saga.step_validation_failed`** | The effects of the earlier steps are undone, which the invalid data may have caused; one outcome for every step failure; no new code path | The final status reads `Compensated` (as for any failed step), and the failure reason is in `ErrorMessage` |
| **B) Fail the saga at once (`FailAsync`, status `Failed`), without compensating** | An explicit error state; with the dead letter integration on, the saga is captured for inspection | Earlier steps stay applied with nothing undoing them, which is the "expensive to recover" outcome the issue wants to avoid; manual intervention for every invalid input |

### Chosen Option: **A — Compensate through the existing step-failure path** (recommended, pending the maintainer)

### Rationale

- A saga that stops mid-way owes its completed steps a compensation; the runner already does exactly that for a failing step and persists the code only (`SagaRunner.cs:160-195`).
- B wins when the maintainer wants invalid input to stop the saga for an operator without any automatic compensation (for example when the compensation of the earlier steps is itself risky), or when the validation always runs before the first step (nothing to compensate).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What the saga row records about a validation failure</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The code only (`saga.step_validation_failed`); the failing step is `CurrentStep`, and the property names are in the returned `Left` and the log** | Follows the documented rule that the persisted reason is a code, never free text; nothing derived from user data in the row | An operator reading only the saga table sees the code and step index, not the failing properties |
| **B) The code with a bounded suffix: `saga.step_validation_failed:<step name>:<property names>` (at most 200 characters)** | Diagnosable from the row alone; the compensation failure already stores `code:ExceptionTypeName` (`SagaRunner.cs:330`) | The suffix is derived from the data's shape (property names), grows the contract of a column that today holds codes, and needs a truncation rule |

### Chosen Option: **A — Code only** (recommended, pending the maintainer)

### Rationale

- The orchestrator's contract (`SagaOrchestrator.cs`, `StartCompensationAsync` remarks) says the persisted reason "must not carry exception or user-supplied messages"; the code is the safe whole.
- B wins for an operations team that runs sagas without log access and needs the failing field in the row; the suffix would then be limited to names, never values.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. A step marked for validation with no validator available</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: the step does not execute; the runner returns `saga.step_validation_unavailable` (`ErrorCategory.Internal`) through the step-failure path** | A misspelled step name or a missing registration is caught on the first run, not silently skipped | A marked step needs a registered validator |
| **B) Validate when a validator exists, otherwise run the step** | No failure on a missing registration | The gate fails open: a typo in the step name turns validation off without any sign |

### Chosen Option: **A — Fail closed** (recommended, pending the maintainer)

### Rationale

- A gate that cannot find its rules must deny, and the only opt-out is an explicit one: not marking the step (`AGENTS.md` section 3: "gates fail closed: when context is missing ... or a lookup fails, the gate denies").
- B wins only if the maintainer prefers validators to be optional per step; the choice applies only if Design Choice 1 keeps the marker and name-keyed validators.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Where in the run the input is validated</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Immediately before each marked step's `Execute`, on the data the previous step produced** | Validates the actual input of the step, including values computed by earlier steps | One validator call per marked step per run |
| **B) Once, on the initial data, when the saga starts** | One call | Step input depends on earlier steps' output; later steps would be unvalidated |

### Chosen Option: **A — Before each marked step**

### Rationale

- The issue is about "saga step input data", which only exists at the step boundary.

**Class:** Trivial
**Settled by:** issue #732 ("Validate step input data before step execution") and `SagaRunner.cs:124-126` (the step input is `progress.Data` at that point)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | New codes created with the categorized `EncinaErrors.Create`: `saga.step_validation_failed` (`ErrorCategory.Validation`, ADR-062 rule R3: field rules); the second code, `saga.step_validation_unavailable`, is in row C8 | Settled | ADR-062 rules R3 and R4 |
| C2 | The `Left` of a failure carries the step name, the error count and at most 20 distinct property names in its details; never a validator message, a property value or the saga data | Settled | `AGENTS.md` section 3 (#1168, #1173, #1259, #1274); ADR-054 settled point 2 |
| C3 | The log (EventId 670) carries the saga id, step number, step name, error count and property names; never the saga data or a message | Settled | `AGENTS.md` section 3 (same rule as C2) |
| C4 | A validator that throws, and a cancelled token, take the runner's existing handlers (`HandleExceptionAsync`: the completed steps are compensated and the saga fails with `saga.handler.failed`; `HandleCancelledAsync`); the step does not execute | Settled | `SagaRunner.cs:100-109` (the run's existing catch blocks); `AGENTS.md` section 3 (gates fail closed) |
| C5 | Validators of a step run in registration order; their results are merged (errors concatenated, counts and names deduplicated); the first throwing validator ends the stage | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `FluentValidationProvider.cs:44` (several validators of one type run and merge); the merge uses `ValidationFailureSummary` from #729 S1 |
| C6 | `ISagaStepValidator<TData>` instances are resolved from the runner's scoped `IServiceProvider` per run (the runner is scoped), never captured in a singleton; the `IServiceProvider` is an optional trailing constructor parameter, and a marked step with no provider fails closed (row C8). A resolver abstraction (an `ISagaStepValidatorResolver` taking the `IServiceProvider` itself) also complies with the no-singleton rule and avoids a service locator in the runner; it wins if the maintainer wants no `IServiceProvider` in `SagaRunner`, at the cost of one more public type | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `AGENTS.md` section 3 (no scoped service in a singleton); `MessagingServiceCollectionExtensions.cs:369` (scoped runner) |
| C7 | EventId 670 (a step input failed validation) in the new range `ValidationSaga = (670, 679)` registered first in `EventIdRanges.cs`, packed from the start of the block 650-699 assigned to this family (671, for a marked step with no validator, is in row C8); 672-679 stay free; the assembly `Encina.Messaging` is mapped to it in `EncinaEventIdAllocationTests` | Trivial | `AGENTS.md` section 7 (ADR-021); `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C8 | A marked step is validated by the validators registered for its exact (ordinal, case-sensitive) name; zero validators, or no `IServiceProvider`, returns the code `saga.step_validation_unavailable` (`ErrorCategory.Internal`, ADR-062 rule R4: a missing server-side registration) and logs EventId 671 (packed right after 670). Step names are the developer-defined strings of `SagaStepDefinition<TData>.Name`, compared ordinally like the other identifiers of the saga API; several validators may share a name (row C5), but registering the provider adapter twice for the same `TData` and name is rejected at registration, because a second identical adapter would only repeat the same provider call | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | Design Choice 4 (fail closed); `SagaDefinition.cs` (`Name` is a plain string) |
| C9 | The adapter validates the whole saga data with the registered provider; per-step differences come from registering the adapter for some steps only or from step-specific validators | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `IValidationProvider.cs` (validates one object; no step notion) |
| C10 | Tenancy and modules: validators receive the runner's `IRequestContext` (the ambient context, or an anonymous one outside a dispatch, `SagaRunner.cs:69-75`); the feature composes no tenant-qualified id, so the reserved `default` segment does not arise; tenant and module scoped validators are the follow-up shared with #729 and #730 (draft `artifacts/issues/validation-message-gates-tenant-module-scoped.md`) | Trivial | ADR-054 D4 (tenant tag only when a tenant is present) |
| C11 | `SagaStepDefinition<TData>` gains `ValidatesInput` (an optional trailing constructor parameter, default `false`), so the single construction site and its tests compile unchanged | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `SagaStepBuilder.cs:145` (one construction site) |

---

## Implementation Phases

### Phase 1: Contract, Adapter, Codes, Log and Registration (pull request S1)

> **Goal**: The validator contract, the provider adapter, the error codes, the log and the registration exist and are proven in isolation; the runner does not call them yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Sagas/Validation/ISagaStepValidator.cs`: `public interface ISagaStepValidator<in TData> where TData : class` with `string StepName { get; }` and `ValueTask<ValidationResult> ValidateAsync(TData data, IRequestContext context, CancellationToken cancellationToken)` (`ValidationResult` is `Encina.Validation.ValidationResult`), XML docs with an example.
2. **Create** `src/Encina.Messaging/Sagas/Validation/ProviderSagaStepValidator.cs`: `public sealed class ProviderSagaStepValidator<TData> : ISagaStepValidator<TData> where TData : class`; constructor `(string stepName, ValidationOrchestrator orchestrator)` (guards: non-blank name, non-null orchestrator); `ValidateAsync` returns `orchestrator.EvaluateMessageAsync(data, context, cancellationToken)` (#729 S1; register row C9).
3. **Create** `src/Encina.Messaging/Sagas/Validation/SagaStepValidationServiceCollectionExtensions.cs`: `AddSagaStepValidator<TData, TValidator>(this IServiceCollection services)` (scoped, `TryAddEnumerable`) and `AddSagaStepProviderValidation<TData>(this IServiceCollection services, string stepName)` (scoped factory registering the adapter for that name; registering the same name twice for one `TData` is rejected with `ArgumentException` at registration).
4. **Modify** `src/Encina.Messaging/Sagas/SagaErrorCodes.cs`: `StepValidationFailed = "saga.step_validation_failed"` and `StepValidationUnavailable = "saga.step_validation_unavailable"` (register row C1, categories documented in the XML docs).
5. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationSaga = (670, 679)`; **create** `src/Encina.Messaging/Sagas/Validation/SagaStepValidationLog.cs` (`internal static partial class`, `[LoggerMessage]`, packed): 670 a step input failed validation (Warning: saga id, step number, step name, error count, property names), 671 a marked step has no validator (Error: saga id, step number, step name). XML docs name the range.
6. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina.Messaging` also maps to `ValidationSaga`), `src/Encina/PublicAPI.Unshipped.txt` (the new public field `EventIdRanges.ValidationSaga`), `src/Encina.Messaging/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Messaging.json` through `mechanical-fixer`.
7. Tests (new): `tests/Encina.UnitTests/Messaging/Sagas/Validation/ProviderSagaStepValidatorTests.cs` (valid and invalid saga data through a fake provider; the result is returned unchanged), `SagaStepValidationLogTests.cs` (EventIds; no data or message in the rendered text), `SagaStepValidationRegistrationTests.cs` (DI with `ValidateOnBuild` and `ValidateScopes`: validators resolve in a scope, the adapter resolves with a provider registered and the build fails without one, a duplicate step name is rejected) and `tests/Encina.GuardTests/Messaging/Sagas/SagaStepValidationGuardTests.cs` (null or blank step name, null orchestrator, null `services`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #732 plan docs/plans/saga-step-validation-implementation-plan-732.md, phase 1
of 4. Depends on #729 S1 (ValidationOrchestrator.EvaluateMessageAsync). The saga API is the low-ceremony runner in
src/Encina.Messaging/Sagas/LowCeremony/. Encina.Messaging has no InternalsVisibleTo for Encina.GuardTests: new types
with guarded parameters must be public.

TASK:
1. Create ISagaStepValidator<TData>, ProviderSagaStepValidator<TData> and the two registration extensions in
   src/Encina.Messaging/Sagas/Validation/.
2. Add SagaErrorCodes.StepValidationFailed and StepValidationUnavailable (documented categories Validation, Internal).
3. Register ValidationSaga (670, 679) in EventIdRanges.cs, write SagaStepValidationLog (670, 671) and map the
   Encina.Messaging assembly to the range in EncinaEventIdAllocationTests.

KEY RULES:
- Never log a validator message, a property value or the saga data; names and counts only.
- Validators are scoped; DI test with ValidateOnBuild and ValidateScopes.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaErrorCodes.cs, src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina/Validation/ValidationOrchestrator.cs
- tests/Encina.UnitTests/Messaging/Sagas/SagaStatusAndErrorCodesTests.cs
```

</details>

---

### Phase 2: Builder Marker and Runner Stage (pull request S2)

> **Goal**: A marked step is validated before it executes and a failure takes the step-failure path, proven with fake stores.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs` (`SagaStepDefinition<TData>`: `public bool ValidatesInput { get; }` and an optional trailing internal constructor parameter, register row C11) and `src/Encina.Messaging/Sagas/LowCeremony/SagaStepBuilder.cs` (`public SagaStepBuilder<TData> ValidateInput()` sets the flag; `AddStepToParent` passes it at `:145`).
2. **Modify** `src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs`: constructor gains the optional trailing `IServiceProvider? serviceProvider = null` (register row C6; the 14 `new SagaRunner(` sites compile unchanged); in `ExecuteStepsAsync`, before `step.Execute`, a marked step calls the new private `ValidateStepInputAsync<TData>(step, progress.Data, requestContext, sagaId, stepNumber, cancellationToken)`, which resolves the validators of the step name (register row C8), runs them in order, merges the results (row C5) and returns `Either<EncinaError, Unit>`; a `Left` goes to `HandleStepFailedAsync` with the step index (Design Choice 2), so the step does not execute and the completed steps are compensated; the persisted reason is the code only (Design Choice 3). Logs 670 and 671. The method is split so each method keeps CRAP at or below 10.
3. **Modify** `src/Encina.Messaging/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Messaging.json` through `mechanical-fixer`.
4. Tests (new): `tests/Encina.UnitTests/Messaging/Sagas/LowCeremony/SagaRunnerStepValidationTests.cs` (valid input executes the step; invalid input of the second step does not execute it, compensates the first, ends `Compensated` with `ErrorMessage` equal to the code and returns a `Left` with the details of names and counts only; invalid input of the first step ends `Compensated` with nothing to compensate; two validators merge; a marked step with no validator, and with a null provider, return `saga.step_validation_unavailable`; a throwing validator ends `Failed` with `saga.handler.failed`; a cancelled token takes the cancellation path; an unmarked step never calls a validator; the saga data and messages are absent from the logs) `SagaStepBuilderValidationTests.cs` (`ValidateInput()` flags only its step, before or after `Execute`), `tests/Encina.GuardTests/Messaging/Sagas/SagaStepBuilderGuardTests.cs` (the null-delegate checks of the `Execute` and `Compensate` overloads); and one real-provider test each, `tests/Encina.IntegrationTests/Validation/FluentValidation/SagaStepValidationPipelineTests.cs`, `.../DataAnnotations/SagaStepValidationPipelineTests.cs` and `.../MiniValidator/SagaStepValidationPipelineTests.cs` (the real `SagaRunner` with `FakeSagaStore`, the provider's real registration and `AddSagaStepProviderValidation<TData>(stepName)`: the same invalid saga data stops the marked step under each provider and a valid one runs it; no container), because the issue requires all three providers. (edited) `tests/Encina.GuardTests/Messaging/Sagas/SagaRunnerGuardTests.cs` (a null `IServiceProvider` is a valid case, not an exception).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #732 plan docs/plans/saga-step-validation-implementation-plan-732.md, phase 2 of 4. Depends on S1. SagaRunner is a
scoped class; ExecuteStepsAsync runs each step with progress.Data; a step Left goes to HandleStepFailedAsync, which
persists only the error code, compensates the completed steps and ends the saga Compensated (or Failed).

TASK:
1. Add SagaStepDefinition.ValidatesInput (optional trailing constructor parameter) and SagaStepBuilder.ValidateInput().
2. Add the optional IServiceProvider constructor parameter to SagaRunner and, before step.Execute of a marked step,
   run the validators registered for the step name; a failure goes to HandleStepFailedAsync with
   saga.step_validation_failed (Validation) or saga.step_validation_unavailable (Internal).

KEY RULES:
- Fail closed: a marked step with no validators does not execute.
- Persist and log the code only; details of the Left carry the step name, error count and at most 20 property names.
- Keep every method at CRAP <= 10: split ValidateStepInputAsync. Tests use the real runner with fake stores.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs (:36-39, :96-160, :160-195)
- src/Encina.Messaging/Sagas/LowCeremony/SagaStepBuilder.cs (:138-146), SagaDefinition.cs
- tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs
```

</details>

---

### Phase 3: Persisted Outcome on the Ten Stores (pull request S3)

> **Goal**: The persisted outcome of an invalid step input is proven on every provider's real saga store.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs`: add `AssertInvalidStepInputEndsCompensatedAsync(ISagaStore store, ISagaStateFactory factory, Func<Task>? flush = null)`: a two-step saga whose second step is marked with `ValidateInput()` and whose registered validator rejects the data; asserts the second step never ran, the first was compensated, the persisted saga is `Compensated`, completed, not stuck, and `ErrorMessage` equals `saga.step_validation_failed` (never a property name or message).
2. **Modify** the ten provider saga test classes to call it, one line each: `tests/Encina.IntegrationTests/ADO/{MySQL,PostgreSQL,SqlServer}/Sagas/SagaStoreADOTests.cs`, `tests/Encina.IntegrationTests/Dapper/{MySQL,PostgreSQL,SqlServer}/Sagas/SagaStoreDapperTests.cs`, `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{MySQL,PostgreSQL,SqlServer}/Sagas/SagaStoreEF{MySql,PostgreSql,SqlServer}Tests.cs` and `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/SagaStoreMongoDBIntegrationTests.cs` (the same ten files that call `AssertStepFailureEndsCompensatedAsync`, verified by `git grep`). Each already uses its shared `[Collection("<Family>-<Database>")]` fixture.
3. **Modify** `.github/coverage-manifest/Encina.Messaging.json` through `mechanical-fixer`: the integration target of `SagaRunner.cs`, added here because the ten-store scenario that reaches it is in this PR (no existing target is lowered). No production code changes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #732 plan docs/plans/saga-step-validation-implementation-plan-732.md, phase 3 of 4. Depends on S2.
SagaRunnerOutcomeScenarios is a provider-neutral scenarios class that every saga store integration test class calls
against its own real database.

TASK:
1. Add AssertInvalidStepInputEndsCompensatedAsync to SagaRunnerOutcomeScenarios (two steps, the second marked with
   ValidateInput() and rejected by a registered validator).
2. Call it from each of the ten provider test classes that call AssertStepFailureEndsCompensatedAsync.

KEY RULES:
- Use the shared collection fixtures; never create a per-class fixture or dispose the shared one.
- Assert the persisted ErrorMessage is exactly the code; the second step must never have run.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs
- tests/Encina.IntegrationTests/ADO/PostgreSQL/Sagas/SagaStoreADOTests.cs
```

</details>

---

### Phase 4: Documentation and Finalization (pull request S4)

> **Goal**: The feature is documented and ready to close the issue.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `docs/messaging/sagas.md` (a section: when step input is validated, the marker, the validator contract and the three providers, the outcome `Compensated` with the code, fail-closed behavior) and `src/Encina.Messaging/README.md` through `docs-writer`.
2. **Create** `changelog.d/732-saga-step-validation.added.md` through `mechanical-fixer`.
3. **Create** the justification files for Load and Benchmark tests (`AGENTS.md` section 9): `tests/Encina.LoadTests/Messaging/SagaStepValidation.md` and `tests/Encina.BenchmarkTests/Messaging/SagaStepValidation.md` (one validator call per marked step, no shared state; a saga step is dominated by its own work).
4. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of the touched files against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #732 plan docs/plans/saga-step-validation-implementation-plan-732.md, phase 4 of 4. Depends on S1 to S3.

TASK:
1. Add the validation section to docs/messaging/sagas.md and src/Encina.Messaging/README.md (docs-writer).
2. Add the changelog fragment (mechanical-fixer) and the two justification files for Load and Benchmark tests.

KEY RULES:
- Pages go through docs-writer; fragments through mechanical-fixer. Never type coverage figures by hand.
- Name no private client application: write "the reference application" if one is needed.
- Link sibling plans by issue number only. No ADR is created by this plan.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/messaging/sagas.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Saga pattern (Garcia-Molina and Salem, 1987) | A saga that stops mid-way compensates the steps already committed | Design Choice 2 (compensate through the existing path) |
| ADR-001, ADR-006 | Railway Oriented Programming; no exceptions for business logic | A failed validation is a `Left` |
| ADR-062 | Category contract; rules R3 and R4 | Categories of the two codes (register row C1) |
| ADR-054 D4 and settled point 2 | Tenant tag only when a tenant is present; code or type only in telemetry | Log and detail shape (rows C2, C3, C10) |
| `AGENTS.md` sections 3 and 5 | Gates fail closed; no scoped service in a singleton; provider coherence; 10 database providers | Design Choice 4, row C6, S3 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SagaRunner`, `SagaStepBuilder`, `SagaStepDefinition<TData>` | `src/Encina.Messaging/Sagas/LowCeremony/` | The hook site and the marker |
| `SagaOrchestrator.StartCompensationAsync`, `SagaErrorCodes` | `src/Encina.Messaging/Sagas/` | Persisted code, new codes |
| `ValidationOrchestrator.EvaluateMessageAsync` | `src/Encina/Validation/` (delivered by #729 S1) | The provider call of the adapter |
| `SagaRunnerOutcomeScenarios` | `tests/Encina.IntegrationTests/Infrastructure/Sagas/` | Provider-neutral scenarios run on all ten stores |
| `ErrorCategory`, `EncinaErrors.Create` | `src/Encina/Errors/ErrorCategory.cs`, `src/Encina/Results/EncinaErrors.cs` | Categorized errors |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | New range registration and mapping |
| `FakeSagaStore` and the in-memory saga stores | `src/Encina.Testing.Fakes/` | Unit tests of the runner |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`ValidationSaga`, new, inside the block 650-699 assigned to the validation-3 family) | 670-679 | 670-671 used (two messages); 672-679 free; sibling ranges of the family: #729 uses `ValidationNotification` 650-659, #730 uses `ValidationCdc` 660-669. The existing `Messaging` range 2800-2999 is not used here |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, READMEs and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Contract, adapter, codes, log, registration | 4 (`ISagaStepValidator`, `ProviderSagaStepValidator`, `SagaStepValidationServiceCollectionExtensions`, `SagaStepValidationLog`) | 4 (`SagaErrorCodes`, `EventIdRanges.cs`, PublicAPI of `Encina` and of `Encina.Messaging`) | 4 (adapter, log, registration, guard) | 1 (`EncinaEventIdAllocationTests`) | 1 | 0 | 14 | ~860 |
| S2 | Builder marker, runner stage, provider tests | 0 | 4 (`SagaDefinition`, `SagaStepBuilder`, `SagaRunner`, PublicAPI) | 6 (runner stage, builder, builder guard, 3 provider integration) | 1 (`SagaRunnerGuardTests`) | 1 | 0 | 12 | ~1,120 |
| S3 | Persisted outcome on ten stores | 0 | 0 | 0 | 11 (`SagaRunnerOutcomeScenarios` and the ten provider classes) | 1 | 0 | 12 | ~320 |
| S4 | Documentation and finalization | 0 | 0 | 0 | 0 | 0 | 5 (`docs/messaging/sagas.md`, `src/Encina.Messaging/README.md`, changelog fragment, 2 justification `.md`) | 5 | ~320 |
| **Total** | | **4** | **8** | **10** | **13** | **3** | **5** | **43** | **~2,620** |

The columns sum to the total (4 + 8 + 10 + 13 + 3 + 5 = 43) and the pull-request rows sum to 43 files (14 + 12 + 12 + 5). The three integration test projects already reference the validation packages and `Encina.Messaging`, so no csproj edit is needed. Caller counts verified with `git grep` on `822c310c7`: `new SagaRunner(` has 14 sites and `new SagaStepDefinition<` 1; none changes, because the new constructor parameters are optional trailing ones. Rebase order with the sibling saga plans: #720 also adds optional constructor parameters to `SagaRunner` and changes its compensation path, #736 and #1237 edit `CompensateAsync` and `ExecuteStepsAsync`; this plan adds one guarded call in `ExecuteStepsAsync` and one private method, so the plan that merges later rebases its constructor and call site.

Per-file coverage targets (per flag, with a one-sentence justification), written into `.github/coverage-manifest/Encina.Messaging.json` by the pull request whose tests reach them. Manifest targets are whole-file; `SagaRunner.cs` already has unit 95 and guard 10, which are kept, and no existing target is lowered. `SagaStepBuilder.cs` and `SagaDefinition.cs` hold defaults only, so the PR that edits them first measures their per-flag baseline and sets a whole-file target not below it:

| File | Unit | Guard | Property | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `Sagas/LowCeremony/SagaRunner.cs` (changed members) | 95 (existing) | 10 (existing; the only new parameter is optional and null is a valid case, so guard tests add no line) | 0 | 40, added by S3 | Every validation branch (valid, invalid first and later step, merge, unavailable, throwing, cancelled) runs against fake stores; the ten-store scenario in S3 runs the invalid-input path on real databases (the other branches are unit-only) |
| `Sagas/LowCeremony/SagaStepBuilder.cs`, `SagaDefinition.cs` (changed members) | 95 | 40 | 0 | 0 | The marker is exercised in the builder tests; the new `SagaStepBuilderGuardTests.cs` (S2) reaches the four existing argument checks of the builder (`Execute` and `Compensate` overloads), which `Encina.GuardTests` does not cover today |
| `Sagas/Validation/ProviderSagaStepValidator.cs` | 95 | 90 | 0 | 80, written by S2 (the PR that holds the three provider tests; S1 sets unit and guard) | The two paths and the constructor guards are reached with a fake provider; the three real-provider tests of S2 run the adapter under FluentValidation, DataAnnotations and MiniValidator |
| `Sagas/Validation/SagaStepValidationServiceCollectionExtensions.cs` | 90 | 90 | 0 | 0 | The DI test covers every line and the duplicate rejection; the guard test covers the null and blank checks |
| `Sagas/Validation/ISagaStepValidator.cs` | 0 | 0 | 0 | 0 | Interface, no executable lines |
| `Sagas/Validation/SagaStepValidationLog.cs` | 90 | 0 | 0 | 0 | Two source-generated methods exercised by the logging test; no parameters to guard |
| `Sagas/SagaErrorCodes.cs` (changed members) | 100 | 0 | 0 | 0 | Constants; the code test pins their values |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #732 - validation of saga step input data in Encina.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- The saga API is the low-ceremony runner (src/Encina.Messaging/Sagas/LowCeremony): a BuiltSagaDefinition<TData> of named
  steps run by SagaRunner (scoped). A failing step goes to HandleStepFailedAsync, which persists only the error code,
  compensates the completed steps and ends the saga Compensated (or Failed).
- Validation follows the orchestrator pattern: ValidationOrchestrator.EvaluateMessageAsync (delivered by #729 S1) calls
  the registered IValidationProvider (FluentValidation, DataAnnotations or MiniValidator).
- Railway Oriented Programming: Either<EncinaError, T>; every error code carries an ErrorCategory (ADR-062).
- Encina.Messaging has no InternalsVisibleTo for Encina.GuardTests: new types with guarded parameters are public.

IMPLEMENTATION OVERVIEW:
1. ISagaStepValidator<TData> (by step name), ProviderSagaStepValidator<TData>, registration extensions, codes
   saga.step_validation_failed (Validation) and saga.step_validation_unavailable (Internal), log 670-671 (S1).
2. SagaStepBuilder.ValidateInput() and the runner stage before step.Execute, fail closed (S2).
3. The provider-neutral scenario on the ten saga stores (S3).
4. docs/messaging/sagas.md, README, changelog fragment, justification files (S4).
Four pull requests, see the Estimated File Count table.

KEY PATTERNS:
- Never log or persist a validator message, a property value or the saga data; the persisted reason is the code.
- A marked step with no validator does not execute (fail closed).
- Validators are scoped; the runner takes an optional IServiceProvider; constructors only gain optional trailing parameters.
- PublicAPI.Unshipped.txt, manifests and changelog through mechanical-fixer; documentation through docs-writer.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs, SagaStepBuilder.cs, SagaDefinition.cs
- src/Encina.Messaging/Sagas/SagaErrorCodes.cs, src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Validation is per step and depends on the data; the validator resolution cache of #702 speeds the provider call |
| 2 | OpenTelemetry | ⏭️ #726 | The saga runner has no activity today and central validation instrumentation is #726; this plan adds no instrument (ADR-054 settled point 8: one owner per source) |
| 3 | Structured Logging | ✅ Phase 1 | `SagaStepValidationLog` EventIds 670-671 in the new range 670-679: saga id, step, error count and property names |
| 4 | Health Checks | ❌ N/A | In-process; the existing stuck-saga detection covers sagas that do not finish |
| 5 | Validation | ✅ All phases | This is the validation feature; the three providers are exercised through the adapter |
| 6 | Resilience | ❌ N/A | No external call; a validation failure is deterministic |
| 7 | Distributed Locks | ❌ N/A | No shared state; saga locking is #715 |
| 8 | Transactions | ❌ N/A | The stage runs before the step and writes nothing itself; the persisted outcome uses the existing orchestrator calls, and the commit is owned by `SagaOrchestrator.CommitAsync` of #720 (the orchestrator never calls `SaveChangesAsync`) |
| 9 | Idempotency | ❌ N/A | Validation is repeatable and side-effect free; compensation idempotency is #736 |
| 10 | Multi-Tenancy | ⏭️ Follow-up | Validators receive the ambient `IRequestContext` (with `TenantId` when present); tenant-specific rules are the follow-up draft `artifacts/issues/validation-message-gates-tenant-module-scoped.md` (shared with #729 and #730), and saga tenancy is #738 |
| 11 | Module Isolation | ⏭️ Follow-up | Module-scoped validators are the same follow-up as row 10 |
| 12 | Audit Trail | ⏭️ Follow-up | Saga audit is #750; a rejected step input appears there as a step failure with the code; the log is operational, not an audit record |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #729 S1: `ValidationOrchestrator.EvaluateMessageAsync` | Planned (plan #729) | Needed by the adapter in S1 |
| ADR-062 error category contract | ✅ Merged | The two codes depend on it |
| Low-ceremony saga runner and the ten saga stores | ✅ On `main` | S3 runs on all of them |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| The runner discards the `Left` of the orchestrator's persisting calls | #1581 | The failure path of this plan relies on `HandleStepFailedAsync`, which returns persisting failures; the discarded calls are in the success path only |
| Saga compensation transaction | #720 | Rebase order above |
| Central validation instrumentation | #726 | Instruments the orchestrator |

---

## Next Steps

1. Link this plan from #732 and ask the maintainer to answer Design Choices 1 to 3 when work starts.
2. Open the follow-up issue when the first pull request opens: tenant and module scoped validation of message gates (draft: `artifacts/issues/validation-message-gates-tenant-module-scoped.md`).
3. Implement S1 to S4 as the pull requests of the Estimated File Count split, `Fixes #732` on S4; S1 waits for #729 S1.
