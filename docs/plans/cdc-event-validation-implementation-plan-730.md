# Implementation Plan: Validation of CDC Event Payloads

> **Issue**: [#730](https://github.com/dlrivada/Encina/issues/730)
> **Type**: Feature (validation, CDC)
> **Related**: #729 (owns `ValidationOrchestrator.EvaluateMessageAsync` and `ValidateMessageAsync`; open its S1 first), #2343 (open bug: a `Left` from the dispatcher is neither retried nor dead-lettered; a hard prerequisite), #2344 (the CDC dead letter queue moves into the unified queue; this plan stays independent of the store), #1602, #636 (adds a dispatch context to `ICdcDispatcher`; rebase order below), #2393, #726 (central validation instrumentation), ADR-001, ADR-050 (CDC requires a reachable dead letter queue), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one dispatcher stage, one contract, one processor branch; no persistence of its own)
> **Estimated Scope**: ~2,300 lines in total (~380 production, ~1,500 tests, ~420 documentation), in four pull requests of at most 12 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-17, main `dc54ca2c5`)

- The issue names `Encina.Messaging` and `Encina` as the affected packages; the CDC code is `Encina.Cdc` (`src/Encina.Cdc`). `ChangeEvent(TableName, Operation, Before, After, Metadata)` carries `object?` payloads (`ChangeEvent.cs`).
- `CdcDispatcher.DispatchAsync` (`src/Encina.Cdc/Processing/CdcDispatcher.cs:73`) finds the entity type of the table, deserializes `Before` and `After` to it (`DispatchInsertAsync`, `DispatchUpdateAsync`, `DispatchDeleteAsync`, `:157-226`; a deserialization failure is already a `Left`, `CdcErrors.DeserializationFailed`) and invokes the typed `IChangeEventHandler<TEntity>`. Nothing checks whether a successfully deserialized entity is acceptable: a missing required value, an out-of-range number or an unknown enum member that survived a migration reaches the handler.
- The dispatcher is registered as a singleton (`ServiceCollectionExtensions.cs:80`) and resolves handlers from its root `IServiceProvider` (`:126`); it is constructed with the `CdcConfiguration` that lists the table and handler registrations (`:58-70`).
- `CdcProcessor.ProcessChangesAsync` counts a `Left` from the dispatcher (`failureCount++`, `lastFailedEvent`, `CdcProcessor.cs:174-175`) and moves on; the next success saves a later position. The retry and dead letter branch is reached only after exceptions (`:94-102`). This is #2343 (open, settled by elimination: a `Left` is retried up to `MaxRetries`, then dead-lettered with the real event, and the position never moves past an undispatched, undead-lettered event). `CdcDeadLetterEntry` holds the exception type name or an error code in `ErrorMessage`, never a message (`CdcDeadLetterEntry.cs`).
- ADR-050 lists for CDC: "a reachable dead letter queue (#2344)" as required, with startup validation; this plan does not decide that, it relies on it.
- The three validation providers validate any object: `IValidationProvider.ValidateAsync<TRequest>` is unconstrained (`src/Encina/Validation/IValidationProvider.cs`); the core orchestrator method `EvaluateMessageAsync<TMessage>` that this plan calls is delivered by #729 S1.
- Not in `src`: any validator contract for CDC, any JSON Schema library (`Directory.Packages.props` lists `FluentValidation` and `MiniValidation` only), any EventId range for CDC validation (`Cdc = (4800, 4899)` is claimed up to 4891 by #2393, #634, #635 and #636).

### What this plan delivers

1. **`ICdcEventValidator<TEntity>`** (`Encina.Cdc`): validates the typed change (`Before`, `After`, operation, table, position metadata) and returns the provider-neutral `ValidationResult`; registered per entity type with `CdcConfiguration.AddEventValidator<TEntity, TValidator>()`.
2. **A provider adapter, opt-in per entity**: `CdcConfiguration.AddProviderEventValidation<TEntity>() where TEntity : class` (the constraint of `EvaluateMessageAsync`; a struct entity such as the `JsonElement` handler of cache invalidation uses a custom validator) validates the `After` state of Insert, Snapshot and Update events with the registered `IValidationProvider` (FluentValidation, DataAnnotations or MiniValidator) through `ValidationOrchestrator.EvaluateMessageAsync` (#729).
3. **A dispatcher stage**: after deserialization and before the handler, every validator of the entity type runs; any failure returns a non-retryable `Left` with the new code `encina.cdc.validation_failed`.
4. **A processor branch**: a `Left` with that code is dead-lettered at once (no retries, the real event, the code as the reason) and the position then moves on, through the dead letter path that #2343 and #2344 define.

Nothing changes for an application that registers no validator: an entity type with no validator is not touched by the stage.

- Affected packages: `Encina.Cdc` (production), `Encina` (one range registration in `EventIdRanges.cs`). The three validation packages need no code change.
- Provider category: Validation (3) for the adapter, one integration test per provider; the CDC connectors (SqlServer, PostgreSQL, MySql, MongoDb, Debezium) are untouched because validation sits after the connector in the dispatcher (`AGENTS.md` section 5: no provider-specific code).
- Shared pieces: consumes #729's `EvaluateMessageAsync` (it does not use the `validation.message_*` codes: the CDC stage builds its own coded error from the merged results of all validators); owns `encina.cdc.validation_failed`, which #636's observers may see as an ordinary `Left`.

---

## Design Choices

<details>
<summary><strong>1. The validator contract</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A CDC contract `ICdcEventValidator<TEntity>` with a context (table, operation, `Before`, `After`, metadata), plus an opt-in per-entity adapter onto the three providers** | Matches the issue's interface; the validator sees the operation and both states; an application's command validators are never applied to a persisted entity by accident, because the adapter is registered per entity | One new public interface and one record |
| **B) No new interface: any registered `IValidator<TEntity>` (or provider equivalent) validates `After` automatically** | Least API; "same discovery as command validators" | A DTO that is both a command and a CDC entity would silently get the command rules applied to database rows; Delete and Update semantics cannot be expressed; the stage would run for every entity that has any validator |
| **C) A schema contract (JSON Schema) as the only mechanism** | Works without entity types, for raw payloads | Needs a new third-party dependency; the dispatcher validates typed entities today; see Design Choice 2 |

### Chosen Option: **A — `ICdcEventValidator<TEntity>` and an opt-in adapter** (recommended, pending the maintainer)

### Rationale

- A keeps CDC validation an explicit, per-entity decision and gives validators what they need (operation and both states); the adapter makes the three providers available with one registration line.
- B wins for an application whose CDC entities are exactly its validated DTOs and wants zero registration; the adapter plus one line per entity gives the same result with an explicit choice.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Schema-based validation (JSON Schema)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Out of this plan: typed-entity validation now; JSON Schema validation as a follow-up issue (draft written with this plan)** | No new third-party dependency decision inside a validation plan; the typed path covers every handler Encina dispatches today | The issue's "schema-based validation (JSON Schema or model validation)" is met by "model validation" only |
| **B) Add a JSON Schema validator package (`Encina.Cdc.JsonSchema`) in this feature** | Covers raw `JsonElement` payloads such as the Debezium connector's | A dependency choice (library, draft version, licence) and a new package the maintainer has not been asked about; the `IChangeEventHandler<JsonElement>` handler of cache invalidation is the only untyped consumer today |

### Chosen Option: **A — Model validation now, JSON Schema as a follow-up** (recommended, pending the maintainer)

### Rationale

- The issue allows "JSON Schema or model validation"; model validation needs no dependency and fits the typed dispatcher.
- B wins when the maintainer needs to validate untyped payloads (for example Debezium events routed to a `JsonElement` handler) before 1.0; the contract of Design Choice 1 does not prevent adding a schema-backed validator later.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How an invalid event reaches the dead letter queue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The dispatcher returns a non-retryable `Left` (`encina.cdc.validation_failed`); the processor dead-letters it at once through the path #2343 and #2344 define** | One owner of dead-lettering (the processor), which already owns the retry and position rules; works unchanged when the queue moves to the unified one (#2344); no duplicate store access | Depends on #2343 being merged first; the processor gains a branch |
| **B) The dispatcher dead-letters the event itself and returns `Right`** | Works before #2343 | The dispatcher has no connector id until #636's dispatch context lands, writes to a store that #2344 is replacing, and a `Right` hides the failure from the processor's counters |

### Chosen Option: **A — Non-retryable `Left`, the processor dead-letters** (recommended, pending the maintainer)

### Rationale

- Retrying a deterministic validation failure only delays the dead letter, so the processor treats the code as non-retryable; everything else about the loop stays #2343's.
- B wins only if the maintainer wants validation shipped before #2343 and accepts the temporary duplicate path.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Behavior on an invalid event: always dead-letter, no skip mode</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Always fail the event: dead-letter and log an error; no mode that drops or ignores it** | "Never silently drop" (the issue); consistent with the rule that a `Left` in a processor fails the operation | An application cannot choose to ignore invalid rows (it can register no validator) |
| **B) A mode option (`DeadLetter`, `LogAndSkip`)** | Flexible | `LogAndSkip` is the silent drop the issue forbids; the position would move past the event |

### Chosen Option: **A — Always dead-letter**

### Rationale

- The issue states "never silently drop"; `AGENTS.md` section 3 requires that a `Left` inside a processor fails the operation (retry, dead-letter or exception) and never reports success.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 ("Errors are NEVER swallowed in background infrastructure") and issue #730 ("never silently drop"); only A complies

</details>

<details>
<summary><strong>5. Where validators are resolved from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A scope created per validation from `IServiceScopeFactory` (taken from the dispatcher's provider), validators resolved from it** | Singleton, scoped and transient validators all work; no scoped service is captured by the singleton dispatcher | One scope per validated event (only for entity types that have a validator) |
| **B) Resolve from the dispatcher's root provider, as handlers are today (`CdcDispatcher.cs:126`)** | No scope | A scoped validator (one that uses a `DbContext`) fails under `ValidateScopes`, the case the three providers document |

### Chosen Option: **A — Scope per validation**

### Rationale

- The dispatcher is a singleton; a scoped service must never be resolved from the root provider.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (registration completeness: proven by a provider built with `ValidateScopes`) and `src/Encina.Cdc/ServiceCollectionExtensions.cs:80` (singleton dispatcher); the validators' lifetime is the application's, so only a scope works for every lifetime

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | New codes (carried by the `Left` of the dispatcher stage, or by whatever carries the failure if Design Choice 3 B is chosen instead) created with the categorized `EncinaErrors.Create`: `encina.cdc.validation_failed` (`ErrorCategory.Validation`, ADR-062 rule R3: field rules) and `encina.cdc.validator_failed` (`ErrorCategory.Internal`, rule R4: a validator that throws is a provider failure, retried like a handler exception) | Settled | ADR-062 rules R3 and R4 |
| C2 | The failure details (the `Left` of the dispatcher stage under Design Choice 3 A, or the dead letter entry and the log under 3 B) carry only the table name, the operation, the error count and at most 20 distinct property names in its details; never a validator message, a property value or the `Before` and `After` payloads (CDC rows are personal data) | Settled | `AGENTS.md` section 3 (#1168, #1173, #1259, #1274); ADR-054 settled point 2 |
| C3 | The error log (EventId 660) carries the table name, the operation, the position's string form, the error count and the property names; never the payload | Settled | `AGENTS.md` section 3 (same rule as C2) |
| C4 | The dead letter entry for a validation failure uses `ErrorMessage` = the code, `RetryCount` = 0 and the real `ChangeEvent`; the code is the whole reason (no free text); `StackTrace` is the empty string (the field is non-null) and the XML documentation of `ErrorMessage` ("the exception type name") is updated to "an exception type name or an error code" | Dilemma (follow-on of Design Choice 3 A, recommended, pending the maintainer) | `CdcDeadLetterEntry.cs` (the field holds a type name or code, never a message); `AGENTS.md` section 3; applies only if the processor dead-letters the event |
| C5 | A validator exception returns a `Left` (`encina.cdc.validator_failed`) and the exception is logged through `ForLogging()` (EventId 661); the processor retries it like any dispatcher failure once #2343 defines that retry; the `Left` is built with a constant `message:` (`EncinaErrors.FromException(code, category, ex, message: "A CDC event validator failed.")`), because the overload otherwise copies `ex.Message` into the `Left` | Dilemma (follow-on of Design Choice 3 A, recommended, pending the maintainer) | `AGENTS.md` section 3 (exceptions through `ForLogging()`; gates fail closed; `EncinaError.Message` never reaches logs) |
| C6 | The validator receives an anonymous `IRequestContext` created per event (`RequestContext.Create()`, correlation id only): a change event carries no user or tenant, so a validator cannot depend on either, and a missing context is never replaced by a guessed one | Trivial | `SagaRunner.cs` (anonymous fallback context for work outside a dispatch); `ChangeMetadata.cs` has no tenant or user field |
| C7 | The adapter validates the state the handler acts on: `After` for Insert, Snapshot and Update; a Delete is not validated by the adapter (a custom `ICdcEventValidator<TEntity>` sees every operation) | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `IChangeEventHandler.cs` (insert and update handlers receive the new state; delete receives the removed one); applies only if the adapter is chosen |
| C8 | The stage is skipped for an entity type with no validator; the set of validated entity types and the per-type invokers are built once in the dispatcher's constructor from `CdcConfiguration` (a `FrozenDictionary`), so the hot path is one lookup; the constructor signature does not change (12 `new CdcDispatcher(` sites stay valid) | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `CdcDispatcher.cs:58-70` (the table and handler maps are frozen the same way); applies only if the registration shape of Design Choice 1 is chosen |
| C9 | EventIds 660-661 in the new range `ValidationCdc = (660, 669)` registered first in `EventIdRanges.cs`, packed from the start of the block 650-699 assigned to this family; 662-669 stay free | Trivial | `AGENTS.md` section 7 (ADR-021); `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C10 | Tenancy and modules: the stage composes no tenant-qualified id and the event has no tenant, so the reserved `default` segment and ADR-054 D4 tags do not arise; tenant-aware validation of CDC events is the follow-up shared with #729 and #732 (draft `artifacts/issues/validation-message-gates-tenant-module-scoped.md`) | Trivial | ADR-054 D4 (tenant tag only when a tenant is present) |
| C11 | Cancellation during validation propagates as the cancellation of the dispatch, never as a validation failure. Today `DispatchAsync` wraps the routing in `catch (Exception ex)` and returns `CdcErrors.HandlerFailed` (`CdcDispatcher.cs:89-107`), so a cancellation raised by a validator would become a `Left`; S2 adds the exception filter `when (ex is not OperationCanceledException \|\| !cancellationToken.IsCancellationRequested)` to that catch (which also stops a cancelled handler from being reported as a handler failure) and a test of both cases | Trivial | `CdcDispatcher.cs:73` (the dispatcher takes the processor's token); ADR-062 rule R5 (a cancellation is not a fault) |
| C12 | Logging versus the issue's "validation errors logged with full event context": the error log (EventId 660) carries the table, operation, position, error count and property names; the full `ChangeEvent` (with its `Before` and `After` payloads) is held by the dead letter entry (`OriginalEvent`), not by the log. This narrows the criterion's "full event context" to the identifying context plus the dead letter, because CDC rows are personal data | Dilemma (recommended, pending the maintainer) | `AGENTS.md` section 3 (`EncinaError.Message` and user data never reach logs); issue #730 acceptance criterion. Alternative that wins when the maintainer wants payload fields in the log: a redacted payload allow-list per table, at the cost of a configuration surface |

---

## Implementation Phases

### Phase 1: Contract, Codes and Log (pull request S1)

> **Goal**: The public contract, the error and the log exist and are proven in isolation; no behavior changes.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Cdc/Validation/ICdcEventValidator.cs`: `public interface ICdcEventValidator<TEntity>` with `ValueTask<ValidationResult> ValidateAsync(CdcEventValidationContext<TEntity> context, CancellationToken cancellationToken)` (`ValidationResult` is `Encina.Validation.ValidationResult`), XML docs with an example.
2. **Create** `src/Encina.Cdc/Validation/CdcEventValidationContext.cs`: `public sealed record CdcEventValidationContext<TEntity>(string TableName, ChangeOperation Operation, TEntity? Before, TEntity? After, ChangeMetadata Metadata, IRequestContext RequestContext)`.
3. **Modify** `src/Encina.Cdc/Errors/CdcErrorCodes.cs` (`ValidationFailed = "encina.cdc.validation_failed"`, `ValidatorFailed = "encina.cdc.validator_failed"`) and `src/Encina.Cdc/Errors/CdcErrors.cs`: `ValidationFailed(string tableName, ChangeOperation operation, int errorCount, IReadOnlyList<string> propertyNames)` through `EncinaErrors.Create(code, ErrorCategory.Validation, constantText, details: ...)` and `ValidatorFailed(string tableName, Exception exception)` through `EncinaErrors.FromException(code, ErrorCategory.Internal, exception, message: "A CDC event validator failed.")` with a constant `message:` (the overload otherwise copies `exception.Message` into the `Left`; the test asserts it does not) (register rows C1, C2, C5). `ValidationFailed` takes its count and names from `ValidationFailureSummary` (#729 S1). Both use the categorized overloads, unlike the older code-less factories of the same class, so `GetCode()` and `GetCategory()` work.
4. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationCdc = (660, 669)`; **create** `src/Encina.Cdc/Validation/CdcValidationLog.cs` (`internal static partial class`, `[LoggerMessage]`, packed): 660 an event failed validation (Error: table, operation, position, error count, property names), 661 a validator threw (Error: table, exception through `ForLogging()`). XML docs name the range.
5. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina.Cdc` also maps to `ValidationCdc`), `src/Encina/PublicAPI.Unshipped.txt` (the new public field `EventIdRanges.ValidationCdc`), `src/Encina.Cdc/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Cdc.json` through `mechanical-fixer`.
6. Tests (new): `tests/Encina.UnitTests/Cdc/CdcValidationLogTests.cs` (EventIds; no payload or message in the rendered text) and `tests/Encina.UnitTests/Cdc/CdcEventValidationContextTests.cs` (record equality and the null `Before` or `After` cases). (edited) `tests/Encina.UnitTests/Cdc/CdcErrorsTests.cs` (code, category `Validation` and `Internal`, details with names and counts only, the constant message of `ValidatorFailed`). (new) `tests/Encina.GuardTests/Cdc/CdcErrorsGuardTests.cs`: `ValidationFailed` with a null table name and a null name list, `ValidatorFailed` with a null table name and a null exception, each throwing `ArgumentNullException` (the factories validate these parameters with `ArgumentNullException.ThrowIfNull`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #730 plan docs/plans/cdc-event-validation-implementation-plan-730.md, phase 1
of 4. The CDC package is src/Encina.Cdc; ADR-062 requires every EncinaError to carry a category
(EncinaErrors.Create(code, category, message, ...)).

TASK:
1. Create ICdcEventValidator<TEntity> and CdcEventValidationContext<TEntity> in src/Encina.Cdc/Validation/.
2. Add CdcErrorCodes.ValidationFailed and ValidatorFailed and the CdcErrors factories with categories.
3. Register ValidationCdc (660, 669) in EventIdRanges.cs and write CdcValidationLog with ids 660 and 661.

KEY RULES:
- Details and logs carry the table, operation, error count and property names only; never a validator message,
  a property value or the Before/After payload.
- Use the categorized EncinaErrors.Create (the older CdcErrors factories are code-less; do not copy them).
- PublicAPI.Unshipped.txt, the manifest and EncinaEventIdAllocationTests (assembly mapping) as described.

REFERENCE FILES:
- src/Encina.Cdc/Errors/CdcErrors.cs, CdcErrorCodes.cs, src/Encina.Cdc/CdcLog.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/architecture/adr/062-error-category-contract.md
```

</details>

---

### Phase 2: Registration, Adapter and Dispatcher Stage (pull request S2)

> **Goal**: A registered validator rejects an invalid change before the handler, proven with each of the three providers. Open after #2343 and #729 S1 have merged.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Cdc/CdcConfiguration.cs`: `AddEventValidator<TEntity, TValidator>() where TValidator : class, ICdcEventValidator<TEntity>` and `AddProviderEventValidation<TEntity>() where TEntity : class`, each recording a registration (the shape of `AddHandler`, `:82`).
2. **Create** `src/Encina.Cdc/Validation/ProviderCdcEventValidator.cs`: `internal sealed class ProviderCdcEventValidator<TEntity> : ICdcEventValidator<TEntity> where TEntity : class`; constructor `(ValidationOrchestrator orchestrator)`; validates `After` for Insert, Snapshot and Update through `ValidationOrchestrator.EvaluateMessageAsync` (#729 S1, which returns the provider's `ValidationResult` unchanged) and returns it; a Delete returns `ValidationResult.Success` (register row C7). A provider exception or a cancelled token propagates to the dispatcher stage, which handles it (register rows C5 and C11).
3. **Modify** `src/Encina.Cdc/ServiceCollectionExtensions.cs`: register each validator scoped (`AddScoped(typeof(ICdcEventValidator<>).MakeGenericType(entity), validatorType)`), the adapter scoped; the application registers the provider and the orchestrator itself (the package adds none). Startup: a provider built with `ValidateOnBuild` fails when the adapter is registered without a validation provider.
4. **Modify** `src/Encina.Cdc/Processing/CdcDispatcher.cs`: build the frozen entity-to-invoker map from the configuration's validator registrations (register row C8); in `DispatchInsertAsync`, `DispatchUpdateAsync` and `DispatchDeleteAsync`, after the existing deserialization checks and before the handler call, run `ValidateAsync(entityType, changeEvent, before, after, cancellationToken)`: create a scope from `IServiceScopeFactory` (Design Choice 5), resolve `IEnumerable<ICdcEventValidator<TEntity>>`, run them in order, merge the results through `ValidationFailureSummary` (#729 S1: error count and at most 20 distinct names), and on a failure log EventId 660 and return `CdcErrors.ValidationFailed(...)`; an exception is logged (661) and returned as `CdcErrors.ValidatorFailed`. Add the exception filter of register row C11 to the existing `catch (Exception ex)` of `DispatchAsync` (`:89-107`), so a cancellation propagates instead of becoming `CdcErrors.HandlerFailed`.
5. **Modify** `src/Encina.Cdc/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Cdc.json` through `mechanical-fixer`.
6. Tests (new): `tests/Encina.UnitTests/Cdc/CdcDispatcherValidationTests.cs` (valid event reaches the handler; invalid Insert, Update and Delete never reach it and return `encina.cdc.validation_failed` with names and counts only; two validators merge; no validator for the type means no scope; a scoped validator with a `DbContext`-like dependency resolves; a throwing validator returns `validator_failed` and its message is absent from the `Left`; a cancelled token propagates as an `OperationCanceledException` from a validator and from a handler; the payload and messages are absent from the rendered logs), `ProviderCdcEventValidatorTests.cs`, `tests/Encina.GuardTests/Cdc/CdcEventValidationGuardTests.cs` (null services, null configuration arguments, the adapter constructor), `CdcEventValidationRegistrationTests.cs` (DI with `ValidateOnBuild` and `ValidateScopes`, with and without a provider) and `tests/Encina.IntegrationTests/Cdc/CdcEventValidationIntegrationTests.cs` (the real dispatcher with the real FluentValidation, DataAnnotations and MiniValidator registrations through the adapter, the same invalid `Order` event rejected by each provider and a valid one handled, no container). (edited) `tests/Encina.UnitTests/Cdc/CdcConfigurationTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #730 plan docs/plans/cdc-event-validation-implementation-plan-730.md, phase 2 of 4. Depends on S1 of this plan,
on #729 S1 (ValidationOrchestrator.EvaluateMessageAsync) and on #2343 (a Left from the dispatcher is handled by the
processor). CdcDispatcher is an internal singleton built with CdcConfiguration.

TASK:
1. Add AddEventValidator<TEntity,TValidator>() and AddProviderEventValidation<TEntity>() to CdcConfiguration and register
   the validators scoped in ServiceCollectionExtensions.
2. Add ProviderCdcEventValidator<TEntity> (After for Insert/Snapshot/Update through EvaluateMessageAsync).
3. In CdcDispatcher, after deserialization and before the handler, run the validators of the entity type from a scope
   created with IServiceScopeFactory; return CdcErrors.ValidationFailed on failure.

KEY RULES:
- The stage is skipped for entity types without a validator (frozen map built in the constructor; constructor unchanged).
- Never log or return a validator message, a property value or the payload; names and counts only.
- Tests run the real dispatcher with real provider registrations; no reflection-only tests.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Cdc/Processing/CdcDispatcher.cs (:58-70, :157-226), src/Encina.Cdc/CdcConfiguration.cs (:82)
- src/Encina.Cdc/ServiceCollectionExtensions.cs (:80-90), src/Encina/Validation/ValidationOrchestrator.cs
- tests/Encina.UnitTests/Cdc/CdcDispatcherTests.cs
```

</details>

---

### Phase 3: Non-Retryable Dead Lettering in the Processors (pull request S3)

> **Goal**: An event rejected by validation is dead-lettered at once with its real event and the code, and never retried or lost. Open after S2.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Cdc/Processing/CdcProcessor.cs` and `src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs`: in the `Left` handling that #2343 introduces, an error whose `GetCode()` equals `CdcErrorCodes.ValidationFailed` skips the retry loop and goes straight to the dead letter path with `RetryCount = 0` and `ErrorMessage` = the code (register row C4); the position then moves past the event because the dead letter holds it. Any other `Left` keeps #2343's retry rules. When #2343's change has a different structure, the branch moves with it; the rule above is what this plan fixes.
2. **Modify** `src/Encina.Cdc/PublicAPI.Unshipped.txt` only if a public member is added (none is planned) and `.github/coverage-manifest/Encina.Cdc.json` through `mechanical-fixer`.
3. Tests: **edit** `tests/Encina.UnitTests/Cdc/CdcProcessorDeadLetterTests.cs` and `tests/Encina.UnitTests/Cdc/Sharding/ShardedCdcProcessorFailureTests.cs` (a validation `Left` is dead-lettered once with `RetryCount = 0`, the code and the real event, never retried; the position is saved after the dead letter; a validator exception is retried up to `MaxRetries`; a dead letter store failure keeps the event from advancing the position; with no `ICdcDeadLetterStore` registered (`UseDeadLetterQueue` defaults to false, `CdcOptions.cs:90`, and `PersistToDeadLetterAsync` only logs when the store is null, `CdcProcessor.cs:203-207`) a validation `Left` must not let the position move past the event: the branch follows #2343's rule for an event that can be neither retried nor dead-lettered, and a test pins it, because ADR-050 requires a reachable dead letter queue for CDC) and **create** `tests/Encina.UnitTests/Cdc/CdcProcessorValidationTests.cs` (the end-to-end loop with the real dispatcher and a fake connector, store and position store).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #730 plan docs/plans/cdc-event-validation-implementation-plan-730.md, phase 3 of 4. Depends on S2 and on #2343.
CdcProcessor and ShardedCdcProcessor handle a Left from ICdcDispatcher as #2343 defines (retry up to MaxRetries, then
dead letter with the real event; the position never moves past an undispatched, undead-lettered event).

TASK:
1. For a Left whose code is CdcErrorCodes.ValidationFailed, dead-letter immediately (RetryCount 0, ErrorMessage = the
   code, the real ChangeEvent), then continue; do not retry.
2. Tests of both processors and an end-to-end loop test.

KEY RULES:
- Never put an error message in the dead letter entry; the code only. Use TimeProvider for any new timestamp (the
  processor reads DateTime.UtcNow today, #2344 and #3414 track it; do not add new reads).
- A failing dead letter store must not let the position move past the event.

REFERENCE FILES:
- src/Encina.Cdc/Processing/CdcProcessor.cs, src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs
- src/Encina.Cdc/DeadLetter/CdcDeadLetterEntry.cs
- tests/Encina.UnitTests/Cdc/CdcProcessorDeadLetterTests.cs
```

</details>

---

### Phase 4: Documentation and Finalization (pull request S4)

> **Goal**: The feature is documented and ready to close the issue.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `docs/features/cdc.md` (a section: the validator contract, the per-entity adapter, the dead letter outcome, the codes and categories, what is never logged) and `src/Encina.Cdc/README.md` through `docs-writer`.
2. **Create** `changelog.d/730-cdc-event-validation.added.md` through `mechanical-fixer`.
3. **Create** the justification files for Load and Benchmark tests (`AGENTS.md` section 9): `tests/Encina.LoadTests/Cdc/EventValidation.md` and `tests/Encina.BenchmarkTests/Cdc/EventValidation.md` (the stage adds one dictionary lookup per event and one scope per validated event; a benchmark of the dispatcher with and without validators is left to #726's instrumentation, and the dispatcher holds no shared mutable state).
4. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of the touched files against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #730 plan docs/plans/cdc-event-validation-implementation-plan-730.md, phase 4 of 4. Depends on S1 to S3.

TASK:
1. Add the validation section to docs/features/cdc.md and src/Encina.Cdc/README.md (docs-writer).
2. Add the changelog fragment (mechanical-fixer) and the two justification files for Load and Benchmark tests.

KEY RULES:
- Pages go through docs-writer; fragments through mechanical-fixer. Never type coverage figures by hand.
- Name no private client application: write "the reference application" if one is needed.
- Link sibling plans by issue number only. No ADR is created by this plan.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/cdc.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-001, ADR-006 | Railway Oriented Programming; no exceptions for business logic | A failed validation is a `Left`; a validator exception is converted to a `Left` |
| ADR-050 (closed list of module requirements) | CDC requires a reachable dead letter queue, validated at startup | The dead letter is not a new requirement of this feature |
| ADR-062 | Category contract; rules R3, R4, R5 | Categories of the two codes (register row C1) |
| ADR-054 D4 and settled point 2 | Tenant tag only when a tenant is present; code or type only in telemetry | Log and detail shape (rows C2, C3, C10) |
| `AGENTS.md` section 3 | Errors never swallowed in background infrastructure; gates fail closed; no scoped service in a singleton | Design Choices 4 and 5 |
| Enterprise Integration Patterns: Invalid Message Channel, Dead Letter Channel | Messages that cannot be processed are routed to a channel for inspection | The dead letter outcome |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `CdcDispatcher`, `CdcConfiguration` | `src/Encina.Cdc/Processing/CdcDispatcher.cs`, `src/Encina.Cdc/CdcConfiguration.cs` | The stage and the registrations |
| `CdcProcessor`, `ShardedCdcProcessor` | `src/Encina.Cdc/Processing/`, `src/Encina.Cdc/Sharding/` | The non-retryable dead letter branch |
| `ICdcDeadLetterStore`, `CdcDeadLetterEntry` | `src/Encina.Cdc/DeadLetter/` | The destination (shape owned by #2343 and #2344) |
| `CdcErrors`, `CdcErrorCodes` | `src/Encina.Cdc/Errors/` | New coded errors |
| `ValidationOrchestrator.EvaluateMessageAsync` | `src/Encina/Validation/` (delivered by #729 S1) | The provider call of the adapter |
| `CdcDeadLetterMetrics`, `CdcDeadLetterHealthCheck` | `src/Encina.Cdc/DeadLetter/Diagnostics/`, `src/Encina.Cdc/Health/` | Existing counters and health of dead letters that now include rejected events |
| `RequestContext.Create()` | `src/Encina/Core/RequestContext.cs` | Anonymous context for validators |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | New range registration and mapping |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc` (`ValidationCdc`, new, inside the block 650-699 assigned to the validation-3 family) | 660-669 | 660-661 used (two messages); 662-669 free; sibling ranges of the family: #729 uses `ValidationNotification` 650-659, #732 uses `ValidationSaga` 670-679. The existing `Cdc` range 4800-4899 is claimed to 4891 by other plans and is not used here |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, READMEs and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Contract, context, codes, log | 3 (`ICdcEventValidator`, `CdcEventValidationContext`, `CdcValidationLog`) | 5 (`CdcErrorCodes`, `CdcErrors`, `EventIdRanges.cs`, PublicAPI of `Encina` and of `Encina.Cdc`) | 3 (log, context, `CdcErrorsGuardTests`) | 2 (`CdcErrorsTests`, `EncinaEventIdAllocationTests`) | 1 | 0 | 14 | ~620 |
| S2 | Registration, adapter, dispatcher stage | 1 (`ProviderCdcEventValidator`) | 4 (`CdcConfiguration`, `ServiceCollectionExtensions`, `CdcDispatcher`, PublicAPI) | 5 (dispatcher, adapter, guard, DI, integration) | 1 (`CdcConfigurationTests`) | 1 | 0 | 12 | ~950 |
| S3 | Processor branch | 0 | 2 (`CdcProcessor`, `ShardedCdcProcessor`) | 1 (`CdcProcessorValidationTests`) | 2 (`CdcProcessorDeadLetterTests`, `ShardedCdcProcessorFailureTests`) | 1 | 0 | 6 | ~450 |
| S4 | Documentation and finalization | 0 | 0 | 0 | 0 | 0 | 5 (`docs/features/cdc.md`, `src/Encina.Cdc/README.md`, changelog fragment, 2 justification `.md`) | 5 | ~330 |
| **Total** | | **4** | **11** | **9** | **5** | **3** | **5** | **37** | **~2,350** |

The columns sum to the total (4 + 11 + 9 + 5 + 3 + 5 = 37) and the pull-request rows sum to 37 files (14 + 12 + 6 + 5). The three integration test projects reference the validation packages and `Encina.Cdc` already (`Encina.IntegrationTests.csproj`), so no csproj edit is needed. Verify caller counts with `git grep` before each pull request: `new CdcDispatcher(` has 12 sites and `new CdcProcessor(` 11, none changes because the validator registrations come through `CdcConfiguration` and S3 adds no constructor parameter. Rebase order with #636: both change `CdcDispatcher.DispatchAsync` and `ICdcDispatcher`; whichever merges second adapts its call sites, and S2's stage lives inside the Insert, Update and Delete methods, which #636's context change does not touch.

Per-file coverage targets (per flag, with a one-sentence justification), written into `.github/coverage-manifest/Encina.Cdc.json` by the pull request whose tests reach them. Manifest targets are whole-file, and `CdcDispatcher.cs`, `CdcProcessor.cs` and `ShardedCdcProcessor.cs` hold defaults only today, so the PR that edits each first measures the file's per-flag baseline and sets a whole-file target not below it (the figures below are for the changed members and are confirmed against that baseline); no existing target is lowered:

| File | Unit | Guard | Property | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `Processing/CdcDispatcher.cs` (changed members) | 90 | 60 | 0 | 50 | Valid, invalid, multiple-validator, scoped-validator and throwing-validator paths run against fake validators for Insert, Update and Delete; the guard tests reach the existing constructor and argument checks; the integration test runs the stage with the real providers (the Delete and exception paths are covered by the unit flag only) |
| `Processing/CdcProcessor.cs`, `Sharding/ShardedCdcProcessor.cs` (changed block) | 90 | 0 | 0 | 0 | The new branch is reachable with fake dispatchers, stores and position stores; it adds no parameter, so guard tests cannot reach more, and the loop tests need no database |
| `Validation/ProviderCdcEventValidator.cs` | 95 | 80 | 0 | 80 | Every operation and each provider-result kind runs in unit tests; the guard tests reach the constructor and the context check; the integration test runs it with the three real providers |
| `Validation/ICdcEventValidator.cs` | 0 | 0 | 0 | 0 | Interface, no executable lines |
| `Validation/CdcEventValidationContext.cs` | 100 | 0 | 0 | 0 | A positional record with no logic; guard tests have nothing to check |
| `Validation/CdcValidationLog.cs` | 90 | 0 | 0 | 0 | Two source-generated methods exercised by the logging test; no parameters to guard |
| `Errors/CdcErrors.cs`, `Errors/CdcErrorCodes.cs` (changed members) | 95 | 80 | 0 | 0 | The two factories run in `CdcErrorsTests`; the new `CdcErrorsGuardTests` reaches the null table name, null name list and null exception checks |
| `CdcConfiguration.cs`, `ServiceCollectionExtensions.cs` (changed members) | 90 | 90 | 0 | 0 | Registration is exercised by the DI test; the guard tests cover the null checks |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #730 - validation of CDC event payloads in Encina.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- The CDC package is src/Encina.Cdc: CdcProcessor streams ChangeEvent values to CdcDispatcher, which deserializes the
  payload to the entity type and calls IChangeEventHandler<TEntity>. A Left from the dispatcher is handled by the
  processor as defined by #2343 (retry, then dead letter; the position never passes an undispatched, undead-lettered event).
- Validation follows the orchestrator pattern: ValidationOrchestrator.EvaluateMessageAsync (delivered by #729 S1) calls
  the registered IValidationProvider (FluentValidation, DataAnnotations or MiniValidator).
- Railway Oriented Programming: Either<EncinaError, T>; every error code carries an ErrorCategory (ADR-062).

IMPLEMENTATION OVERVIEW:
1. ICdcEventValidator<TEntity>, CdcEventValidationContext<TEntity>, codes encina.cdc.validation_failed (Validation) and
   encina.cdc.validator_failed (Internal), log 660-661 in the new range ValidationCdc (S1).
2. AddEventValidator and AddProviderEventValidation registrations, ProviderCdcEventValidator, and the dispatcher stage
   (scope per validation, frozen per-entity map) (S2).
3. The processors dead-letter a validation Left at once, without retries (S3).
4. docs/features/cdc.md, README, changelog fragment, justification files (S4).
Four pull requests, see the Estimated File Count table.

KEY PATTERNS:
- Never log or store a validator message, a property value or the Before/After payload; names, counts, table, operation.
- Validators resolve from a scope created with IServiceScopeFactory; the dispatcher is a singleton.
- Tests run the real dispatcher and processors with real provider registrations; PublicAPI.Unshipped.txt, manifests and
  changelog through mechanical-fixer; documentation through docs-writer.

REFERENCE FILES:
- src/Encina.Cdc/Processing/CdcDispatcher.cs, CdcProcessor.cs, src/Encina.Cdc/CdcConfiguration.cs
- src/Encina.Cdc/Errors/CdcErrors.cs, src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Validation is per event and depends on the payload; the validator resolution cache of #702 speeds the provider call |
| 2 | OpenTelemetry | ✅ Phase 3 | Rejected events enter the existing dead letter counters (`CdcDeadLetterMetrics`); no new instrument is added, central validation instrumentation is #726 |
| 3 | Structured Logging | ✅ Phase 1 | `CdcValidationLog` EventIds 660-661 in the new range 660-669: table, operation, position, error count and property names |
| 4 | Health Checks | ✅ Phase 3 | Dead-lettered events are counted by the existing `CdcDeadLetterHealthCheck`; no new check |
| 5 | Validation | ✅ All phases | This is the validation feature; the three providers are exercised through the adapter |
| 6 | Resilience | ❌ N/A | No external call; a validation failure is deterministic and not retried, a validator exception follows the processor's retry policy |
| 7 | Distributed Locks | ❌ N/A | No shared state; processor coordination is #1251 and #2393 |
| 8 | Transactions | ❌ N/A | The stage runs before the handler and starts no transaction; the dead letter write follows #2343's rules and the orchestrator never calls `SaveChangesAsync` |
| 9 | Idempotency | ❌ N/A | Validation is repeatable and side-effect free; replay of a dead letter re-runs it |
| 10 | Multi-Tenancy | ⏭️ Follow-up | A change event carries no tenant today (`ChangeMetadata`), so validators run with an anonymous context; tenant-aware validation of CDC events is the follow-up draft `artifacts/issues/validation-message-gates-tenant-module-scoped.md` (shared with #729 and #732) |
| 11 | Module Isolation | ⏭️ Follow-up | Module-scoped validators are the same follow-up as row 10 |
| 12 | Audit Trail | ⏭️ Follow-up | Auditing of rejected events belongs to the audit family (#636 streams CDC changes to audit sinks); the error log is operational, not an audit record |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #2343: a `Left` from the dispatcher is retried and dead-lettered, the position never passes it | Open (settled by elimination, no maintainer decision pending) | S2 opens after it merges; without it a rejected event would be counted and lost |
| #729 S1: `ValidationOrchestrator.EvaluateMessageAsync` | Planned (plan #729) | Needed by the adapter in S2 |
| ADR-062 error category contract | ✅ Merged | The two codes depend on it |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| CDC dead letters move into the unified queue; the entry gets the unified shape | #2344 | S3 uses whatever dead letter path exists when it opens |
| Dispatch context on `ICdcDispatcher` | #636 | Rebase order above |
| Persistent position store | #2393 | Independent |
| Central validation instrumentation | #726 | Instruments the orchestrator, including `EvaluateMessageAsync` |

---

## Next Steps

1. Link this plan from #730 and ask the maintainer to answer Design Choices 1 to 3 when work starts.
2. Open the follow-up issue when the first pull request opens: tenant and module scoped validation of message gates (draft: `artifacts/issues/validation-message-gates-tenant-module-scoped.md`), and the JSON Schema follow-up if the maintainer confirms option A of Design Choice 2 (draft: `artifacts/issues/cdc-json-schema-event-validation.md`).
3. Implement S1 to S4 as the pull requests of the Estimated File Count split, `Fixes #730` on S4; S2 waits for #2343 and #729 S1.
