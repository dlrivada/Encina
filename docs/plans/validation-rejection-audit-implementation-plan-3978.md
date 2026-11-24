# Implementation Plan: Optional Audit of Rejected Requests in the Validation Pipeline

> **Issue**: [#3978](https://github.com/dlrivada/Encina/issues/3978)
> **Type**: Feature
> **Related**: #230, #227, #228, #231 (their plans defer "audit of validation failures" here), #232 (audits security violations itself), #233 (owns `ValidationError.ErrorCode`, which this plan reads), #726 (central validation instrumentation), #2676 (tenant tag helper), #2442 (typed `ModuleId` on the request context), #3981 (tenant- and module-scoped validation resources, planned apart), ADR-036, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one core hook, one adapter in `Encina.Security.Audit`, no persistence of its own)
> **Estimated Scope**: ~1,700 lines in total (~420 production, ~1,000 tests and manifests, ~280 documentation), in three pull requests of at most 13 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-24, main `db34f789b`)

- `ValidationOrchestrator.ValidateAsync<TRequest, TResponse>` turns an invalid `ValidationResult` into `Left(EncinaErrors.Create(EncinaErrorCodes.ValidationFailed, ErrorCategory.Validation, message))` and records nothing (`src/Encina/Validation/ValidationOrchestrator.cs:68`). `ValidationPipelineBehavior` passes that `Left` back without calling `nextStep()` (`ValidationPipelineBehavior.cs:61`).
- The orchestrator has one constructor parameter, `IValidationProvider` (`ValidationOrchestrator.cs:33`). It is registered as scoped by `AddEncinaFluentValidation` (`Encina.FluentValidation/ServiceCollectionExtensions.cs:63` and `:105`) and as a **singleton** by `AddDataAnnotationsValidation` (`:73`) and `AddMiniValidation` (`:66`). Any dependency this plan adds to the orchestrator must therefore be safe in a singleton.
- `ValidationError` is `(string? PropertyName, string ErrorMessage)` (`ValidationResult.cs:97`); the machine-readable `ErrorCode` is planned by #233 and does not exist yet. The provider messages can embed the rejected value (for example `'abc' is not a valid email`), which is why the audit entry never uses them.
- `Encina.Security.Audit` has `IOperationAuditStore`, `OperationAuditEntry` (with `CorrelationId`, `UserId`, `TenantId`, `Action`, `EntityType`, `Outcome`, `ErrorMessage`, `Metadata`), `AuditOutcome.Failure` ("validation errors or business rule violations") and `AuditPipelineBehavior`. That behavior records any `Left` of the request it wraps with the error **code** in `ErrorMessage` and writes through a fresh scope with `TransactionScopeOption.Suppress` (`AuditPipelineBehavior.cs:306-307`). It audits commands by default and queries only when asked, and it only sees a rejection when it is the outer behavior; a rejected query, an excluded type, or an audit behavior placed inside validation records nothing today.
- `DefaultOperationAuditEntryFactory` cannot be reused for a rejection: it reads the entity id out of the request (`DefaultOperationAuditEntryFactory.cs:101`) and hashes the masked request payload, both of which are rejected values.
- `Encina.Security.Audit` references `Encina` (`Encina.Security.Audit.csproj:20`); the validation code lives in the `Encina` assembly, so a direct reference from validation to the audit package would be a project cycle.
- Not in `src`: any port for rejections, any EventId range for validation (`EventIdRanges.cs` has none), any option for auditing validation.

### What this plan delivers

An opt-in audit of every rejected request, with no values in it:

1. **A port in core**: `IValidationRejectionRecorder` and the value `ValidationRejection` (request type, the code `encina.validation.failed`, error count, distinct error codes, failed property names), plus `ValidationRejectionAuditor`, a sealed helper that calls the port, never throws, and logs a failure by code.
2. **A hook in `ValidationOrchestrator`**: one optional trailing constructor parameter `ValidationRejectionAuditor? rejectionAuditor = null`; on an invalid result the orchestrator awaits `AuditAsync` and returns the same `Left` as today. With nothing registered the code path is one null check.
3. **An adapter in `Encina.Security.Audit`**: `AddEncinaValidationAudit(configure)` registers a singleton-safe recorder that builds one `OperationAuditEntry` per rejection (`Action = "ValidationRejected"`, `EntityType` = request type name, `Outcome = AuditOutcome.Failure`, the code in `ErrorMessage`, counts and codes in `Metadata`, the correlation, user and tenant ids of the context) and writes it through `IOperationAuditStore`, with an optional per-subject bound.

Nothing changes for an application that does not call `AddEncinaValidationAudit`.

- Affected packages: `Encina` (core: port, auditor, orchestrator, log, one EventId range), `Encina.Security.Audit` (adapter, options, meter, registration). The three validation provider packages need no code change (the orchestrator constructor parameter is optional and resolved by the container).
- Provider category: Validation (3) (`AGENTS.md` section 5): the hook sits in the orchestrator all three providers share, and an integration test proves one rejected request writes the same entry under FluentValidation, DataAnnotations and MiniValidator.
- Shared pieces: reads `ValidationError.ErrorCode` (owned by #233); the metric tenant tag uses the helper of #2676; the orchestrator constructor gains its parameter after those of #233 and #726 (whichever plan lands later appends its own last, all optional with `null` defaults).

---

## Design Choices

<details>
<summary><strong>1. Where the hook sits and how the packages are laid out</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A hook in `ValidationOrchestrator` calling a port in core; the adapter lives in `Encina.Security.Audit`** | The orchestrator holds the `ValidationResult`, so the entry can carry the error count and the distinct error codes; one hook serves the three providers; the validation code takes no dependency on the audit package | One more optional constructor parameter on a class that #233 and #726 also change; a new public port |
| **B) A separate pipeline behavior in a new package `Encina.Validation.Audit` that references core and `Encina.Security.Audit`** | No change to the orchestrator or to core; the dependency is explicit | The behavior sees only the outer `Left` (code `encina.validation.failed`, whose text is the flattened message), so the entry cannot carry per-error codes; a new package to ship; the behavior must be registered outside validation or it never sees the rejection |

### Chosen Option: **A - hook in the orchestrator, port in core, adapter in `Encina.Security.Audit`** (recommended, pending the maintainer)

### Rationale

- The issue promises "the error codes"; only the orchestrator has them (#233 puts them on `ValidationError`). B wins if the maintainer wants no change at all to core validation and accepts an entry with the request type and the single code only.
- The hook is one null-checked call after the invalid-result test; the constructor parameter is optional, so the 18 `new ValidationOrchestrator(` sites (`ValidationPipelineBehaviorContractTests.cs` 6, `ValidationPipelineBehaviorGuardTests.cs` 7, `ValidationOrchestratorTests.cs` 5; counted by grep on `origin/main`) compile unchanged.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What the entry says about the failed fields</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Count and distinct error codes only; never a property name** | No chance of a caller-controlled string in the store | "Which field is clients sending wrong" is not answerable from the audit alone |
| **B) Count and codes always; failed property names only when `ValidationAuditOptions.IncludePropertyNames` is true (default false), each name capped at 64 characters and at most 20 names** | The field-level signal exists for the application that wants it; the default stays minimal; the cap bounds a hostile dictionary key such as `Items[abc]` | Another option to document and test |
| **C) Count, codes and property names always** | Most useful by default | A property name can embed a caller-supplied key (`Tags[some-user-text]`), so the default would store caller text |

### Chosen Option: **B - names opt-in, capped** (recommended, pending the maintainer)

### Rationale

- A property name is schema, not a value, but collection and dictionary rules produce names with caller-supplied parts; opting in puts that decision with the application. A wins for a deployment that audits for compliance only and never debugs contracts; C wins for an internal API with no hostile callers.
- Whatever the option, `ErrorMessage` of `ValidationError` never enters the entry (AGENTS.md section 3: only codes, never messages).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Whether every rejection is written</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Audit every rejection, no bound** | Complete for compliance reviews; simplest code | A client that floods invalid requests floods the audit store, which has an auto-purge only after `RetentionDays` (default 2555) |
| **B) Audit every rejection by default, with an optional bound `MaxRejectionsPerSubjectPerMinute` (null by default): beyond it the entry is skipped and `operation_audit.validation_rejections_suppressed` is incremented** | Protects the store on a public endpoint; off by default, so compliance deployments keep completeness; the suppression is visible as a metric, never silent | A small in-memory limiter (per process, bounded to 10,000 tracked subjects, beyond which new subjects are audited, never skipped) |

### Chosen Option: **B - every rejection, optional bound** (recommended, pending the maintainer)

### Rationale

- B is the issue's own recommendation ("audit every rejection with a documented bounded option"). A wins when the maintainer prefers no state in the adapter and handles floods at the edge (a gateway rate limit).
- The limiter is per process by design (AGENTS.md section 6, function 7: no shared state is introduced, so no distributed lock).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. A request already audited by `AuditPipelineBehavior`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Write both entries: the generic one of `AuditPipelineBehavior` and the rejection entry; distinguish them by `Action = "ValidationRejected"` and document the pipeline order** | No change to `AuditPipelineBehavior`; each entry answers its own question (the operation failed with a code; why it was rejected: counts and codes) | A rejected command that the audit behavior wraps has two entries |
| **B) `AuditPipelineBehavior` skips its entry when the failure code is `encina.validation.failed` and the rejection recorder is registered** | One entry per rejection | Changes `AuditPipelineBehavior` (a changed method under the CRAP gate, a new dependency on the port); an application that wants the generic entry loses it silently |

### Chosen Option: **A - both entries, documented** (recommended, pending the maintainer)

### Rationale

- A keeps the two audit paths independent and each testable alone; the integration test pins the order of the default registrations and counts the entries for a rejected command and a rejected query. B wins if the maintainer wants exactly one entry per rejection whatever the registration order.
- Which behavior is outer follows the pipeline order resolver (`PipelineStage`, then registration order); the page of the documentation phase states it so an application can choose.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C5 | The port lives in core and the adapter in `Encina.Security.Audit`; validation does not reference the audit package. The issue's alternative (a direct reference from validation to the audit package) does not build: `Encina.Security.Audit.csproj:20` references `Encina`, and `Encina.Validation` is part of `Encina`, so the reference would be a cycle | Settled | `src/Encina.Security.Audit/Encina.Security.Audit.csproj:20`; `src/Encina/Validation/` |
| C6 | The entry carries the request type name, the code, counts, distinct error codes, and the correlation, user and tenant ids; never a `ValidationError.ErrorMessage`, the request, an entity id read from the request, or a payload hash. The adapter builds the entry itself instead of calling `IOperationAuditEntryFactory` | Settled | AGENTS.md section 3 (`EncinaError.Message` never reaches plaintext storage); `DefaultOperationAuditEntryFactory.cs:101` reads values of the request |
| C7 | A failed write never changes the result: the orchestrator returns the same `Left` it would return without the audit. The auditor catches every exception except `OutOfMemoryException`, logs the exception type through `ForLogging()` and the cause by code, and returns | Settled | AGENTS.md section 3 (errors never swallowed in background infrastructure: here the operation, the validation, has already failed and keeps failing with its own code; `AuditPipelineBehavior.Reconcile`, `AuditPipelineBehavior.cs:131`, returns a `Left` unchanged) |
| C8 | The recorder resolves `IOperationAuditStore` from an `IServiceScopeFactory` scope per write, never from its constructor, and writes with `CancellationToken.None` under `TransactionScopeOption.Suppress`, as `AuditPipelineBehavior` does (`:306-307`); two of the three providers register the orchestrator as a singleton, so a scoped dependency would be captive | Settled | AGENTS.md section 3 (registration completeness, `ValidateScopes`); `AuditPipelineBehavior.cs:306-307`; `AddDataAnnotationsValidation` `:73`, `AddMiniValidation` `:66` |
| C9 | The new error is not a new code: the adapter returns the existing `OperationAuditErrors.WriteFailed(requestType, cause)` (`audit.write_failed`), whose category is `Internal` (ADR-062 R4: store failures are `Internal`); the `Left` never reaches the caller. The rejection keeps `encina.validation.failed` with `ErrorCategory.Validation` (ADR-062 R3) | Settled | ADR-062 R3, R4; `OperationAuditErrors.cs` |
| C10 | Time comes from an injected `TimeProvider` (default `TimeProvider.System`) for `StartedAtUtc`, `CompletedAtUtc` and `TimestampUtc` | Trivial | AGENTS.md section 3 (time comes from `TimeProvider`) |
| C11 | Missing context denies nothing here (the audit never blocks a request): a missing `UserId` or `TenantId` is written as null, and the metric tenant tag is omitted when multi-tenancy is off (ADR-054 D4). The limiter key separates "no tenant" from a tenant literally named `default` with a length-prefixed form (`n\|<user>` versus `t<length>:<tenant>\|<user>`), so no two tenant and user pairs share a key | Trivial | ADR-054 D4; the reserved-segment rule of the 1.0 batch (tenant-qualified keys are injective) |
| C12 | The module is read with `RequestContextModuleExtensions.GetModuleName` today and `IRequestContext.ModuleId` once #2442 lands; it is written to `Metadata["moduleId"]` only when present | Trivial | `src/Encina/Modules/RequestContextModuleExtensions.cs`; the plan of #2442 |
| C13 | `ValidationRejection.ErrorCodes` is filled from `ValidationError.ErrorCode` once #233 S1 exists; until then it is empty. The one-line mapping is written by whichever of the two plans lands second | Trivial | The plan of #233 (owner of `ValidationError.ErrorCode`) |
| C14 | The entry has no `IpAddress` and no `UserAgent`: the issue names correlation, user and tenant only, and an address is personal data the rejection does not need | Trivial | AGENTS.md section 3 (minimal data); GDPR Art. 5(1)(c) |

---

## Implementation Phases

### Phase 1: Port, Auditor, Orchestrator Hook and Log (pull request S1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/IValidationRejectionRecorder.cs`: `public interface IValidationRejectionRecorder { ValueTask<Either<EncinaError, Unit>> RecordAsync(ValidationRejection rejection, IRequestContext context, CancellationToken cancellationToken); }` with XML docs that state the contract: never read a message, never store a value.
2. **Create** `src/Encina/Validation/ValidationRejection.cs`: `public sealed record ValidationRejection(Type RequestType, string ErrorCode, int ErrorCount, ImmutableArray<string> ErrorCodes, ImmutableArray<string> PropertyNames)` and `public static ValidationRejection From(Type requestType, ValidationResult result)` (distinct, ordinal-sorted codes and property names; a null `PropertyName` is skipped; `ErrorCode` is `EncinaErrorCodes.ValidationFailed`).
3. **Create** `src/Encina/Validation/ValidationRejectionAuditor.cs`: `public sealed class ValidationRejectionAuditor`, constructor `(IValidationRejectionRecorder recorder, ILogger<ValidationRejectionAuditor> logger)` (both checked), `public async ValueTask AuditAsync(Type requestType, ValidationResult result, IRequestContext context, CancellationToken cancellationToken)`: builds the rejection, awaits the port; a `Left` is logged (event 12300: request type name and the cause code) and an exception except `OutOfMemoryException` is logged (event 12301: type name and `ex.ForLogging()`), neither rethrown (register row C7).
4. **Create** `src/Encina/Validation/Diagnostics/ValidationAuditLog.cs`: `internal static partial class` with `[LoggerMessage]` 12300 (Warning, port returned a Left), 12301 (Warning, port threw), 12302 (Debug, rejection recorded); packed, XML docs name the range.
5. **Modify** `src/Encina/Validation/ValidationOrchestrator.cs`: private field `_rejectionAuditor`; constructor `(IValidationProvider validationProvider, ..., ValidationRejectionAuditor? rejectionAuditor = null)` where `...` are the optional parameters of #233 and #726 when they have landed; in the `result.IsInvalid` branch, after building the `Left`, `if (_rejectionAuditor is not null) await _rejectionAuditor.AuditAsync(typeof(TRequest), result, context, cancellationToken).ConfigureAwait(false);` then return the same `Left`.
6. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: `public static readonly (int Min, int Max) ValidationAudit = (12300, 12349);` (area Core and messaging; the first free 50-id slot after the reservations in the registry and the claims of the plans of #340 (12200-12249) and the process-manager family (12250-12299)); **modify** `src/Encina/PublicAPI.Unshipped.txt` (`mechanical-fixer`: the new field, the two new types and members, the changed orchestrator constructor: remove the old line, add the new one); **modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the `Encina` entry maps `ValidationAudit`); **modify** `docs/engineering/eventid-block-registry.md` (a `reserved` row in "Blocks" and a `zone` row in "Registered ranges"; `docs-writer` rule: the page is read by `EventIdRegistryAgreementTests`).
7. **Tests**: new `tests/Encina.UnitTests/Validation/ValidationRejectionAuditorTests.cs` (the three providers' real results through a real `ValidationOrchestrator`: the port receives the code, count, sorted distinct codes and names; a `Left` from the port is logged by code and the orchestrator returns the original `Left`; a throwing port is contained; cancellation of the caller token is passed to the port; the rendered log text carries no message); edit `tests/Encina.UnitTests/Validation/ValidationOrchestratorTests.cs` (null auditor keeps today's behavior; valid request never calls the port); new `tests/Encina.GuardTests/Core/Validation/ValidationRejectionAuditorGuardTests.cs` (constructor and `AuditAsync` null checks, `ValidationRejection.From` null checks).
8. **Manifest** `.github/coverage-manifest/Encina.json` through `mechanical-fixer`: per-file targets of the table "Estimated File Count" with the one-sentence justifications.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina (.NET 10, C# 14, pre-1.0). Issue #3978: opt-in audit of requests rejected by the validation pipeline. Validation lives in src/Encina/Validation. ValidationOrchestrator is a singleton under AddDataAnnotationsValidation and AddMiniValidation, scoped under AddEncinaFluentValidation.
TASK: Add the port IValidationRejectionRecorder, the record ValidationRejection, the sealed ValidationRejectionAuditor and the log class; add one optional trailing constructor parameter to ValidationOrchestrator and call the auditor in the invalid-result branch; register EventIdRanges.ValidationAudit (12300, 12349) with ids 12300-12302; write the tests of the plan.
KEY RULES: The rejection result is returned unchanged whatever the auditor does. No ValidationError.ErrorMessage reaches the port, a log or a tag. Never swallow OutOfMemoryException. Log exceptions through ForLogging(). Register the EventId range in EventIdRanges.cs before using the ids, map the assembly in EncinaEventIdAllocationTests, add the PublicAPI.Unshipped.txt lines and the registry rows. Optional constructor parameter last (CS1737). Tests use Shouldly through Encina.Testing.Shouldly and real providers, never reflection-only tests.
REFERENCE FILES: src/Encina/Validation/ValidationOrchestrator.cs, src/Encina/Validation/ValidationResult.cs, src/Encina.Security.Audit/AuditPipelineBehavior.cs (the contained-failure precedent), src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, tests/Encina.UnitTests/Validation/ValidationOrchestratorTests.cs.
```

</details>

### Phase 2: Adapter, Options, Meter and Registration in `Encina.Security.Audit` (pull request S2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Security.Audit/ValidationAuditOptions.cs`: `public sealed class ValidationAuditOptions { bool IncludePropertyNames { get; set; } = false; int? MaxRejectionsPerSubjectPerMinute { get; set; } }`; the setter of the bound rejects a value below 1 with `ArgumentOutOfRangeException`.
2. **Create** `src/Encina.Security.Audit/ValidationRejectionAuditRecorder.cs`: `internal sealed class ValidationRejectionAuditRecorder : IValidationRejectionRecorder`, constructor `(IServiceScopeFactory scopeFactory, IOptions<ValidationAuditOptions> options, TimeProvider? timeProvider = null)`; `RecordAsync` builds the entry of register row C6 (`Id = Guid.NewGuid()`, `CorrelationId`, `UserId`, `TenantId` from the context, `Action = "ValidationRejected"`, `EntityType = rejection.RequestType.Name`, `Outcome = AuditOutcome.Failure`, `ErrorMessage = rejection.ErrorCode`, `Metadata` with `errorCount`, `errorCodes`, optional `propertyNames` and `moduleId`), asks the limiter, then writes through a scope with `TransactionScopeOption.Suppress`, mapping a `Left` or an exception to `OperationAuditErrors.WriteFailed(requestType, causeCode)`.
3. **Create** `src/Encina.Security.Audit/SubjectRateLimiter.cs` (`internal sealed`): a fixed-window counter per key (register row C11), bounded to 10,000 tracked keys (beyond it a new key is admitted), windows read from `TimeProvider`, entries of past windows removed on access.
4. **Modify** `src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs`: counters `operation_audit.validation_rejection_write_failures` and `operation_audit.validation_rejections_suppressed` (no `_total` suffix) with tags `error_code`, `request_type` and the tenant tag through the helper of #2676 only when multi-tenancy is on; the raw tenant id is never a log value.
5. **Modify** `src/Encina.Security.Audit/ServiceCollectionExtensions.cs`: `AddEncinaValidationAudit(this IServiceCollection services, Action<ValidationAuditOptions>? configure = null)` registers the options, `TryAddSingleton<IValidationRejectionRecorder, ValidationRejectionAuditRecorder>()` and `TryAddSingleton<ValidationRejectionAuditor>()`, and `TryAddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>()` as `AddEncinaAudit` does, so any call order works and an application store wins; it does not register `AuditPipelineBehavior` (that stays `AddEncinaAudit`).
6. **Modify** `src/Encina.Security.Audit/PublicAPI.Unshipped.txt` (`mechanical-fixer`): `ValidationAuditOptions` members and the extension method; the recorder and the limiter are internal.
7. **Tests**: new `tests/Encina.UnitTests/Security/Audit/ValidationRejectionAuditRecorderTests.cs` (entry content for each provider's real result, no message and no entity id in the entry, a failing store returns `audit.write_failed` and is counted, property names only with the option and capped at 64 characters and 20 names, tenant null and module absent cases); new `tests/Encina.UnitTests/Security/Audit/SubjectRateLimiterTests.cs` (`FakeTimeProvider`: the N+1th in a window is refused, the next window admits, the cap admits new keys, tenant `default` and no tenant never share a key); new `tests/Encina.UnitTests/Security/Audit/ValidationAuditRegistrationTests.cs` (the three providers' registrations plus `AddEncinaValidationAudit` in both orders build with `ValidateOnBuild` and `ValidateScopes`, the singleton orchestrator resolves, an application `IOperationAuditStore` wins); new `tests/Encina.GuardTests/Security/Audit/ValidationAuditGuardTests.cs` (extension null check, options setter range, recorder constructor and `RecordAsync` nulls, a null `TimeProvider` accepted as a valid result); new `tests/Encina.ContractTests/Security/Audit/ValidationRejectionRecorderContractTests.cs` (the real recorder and a fake keep the port contract: Right on success, Left and no throw on a failing store).
8. **Manifest** `.github/coverage-manifest/Encina.Security.Audit.json` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina issue #3978, phase 2. Phase 1 added IValidationRejectionRecorder, ValidationRejection and ValidationRejectionAuditor to core. Encina.Security.Audit has IOperationAuditStore, OperationAuditEntry, AuditOutcome.Failure and AuditPipelineBehavior, whose write path (new scope, TransactionScopeOption.Suppress, CancellationToken.None) you reuse in spirit.
TASK: Implement ValidationAuditOptions, the internal ValidationRejectionAuditRecorder, the internal SubjectRateLimiter, the two meter counters and AddEncinaValidationAudit with TryAdd registrations; write unit, registration, guard and contract tests.
KEY RULES: Never use IOperationAuditEntryFactory (it reads the entity id and hashes the payload). The entry never carries ValidationError.ErrorMessage. The recorder is a singleton and resolves IOperationAuditStore from IServiceScopeFactory per write. TimeProvider for every timestamp and every window. Metric names have no _total suffix; the tenant tag only when multi-tenancy is on (ADR-054 D4); never the tenant or user id in a log. The limiter key is injective (no tenant versus tenant "default" collision). Existing AddEncinaAudit behavior is unchanged. Update PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.
REFERENCE FILES: src/Encina.Security.Audit/AuditPipelineBehavior.cs, src/Encina.Security.Audit/OperationAuditEntry.cs, src/Encina.Security.Audit/OperationAuditErrors.cs, src/Encina.Security.Audit/ServiceCollectionExtensions.cs, src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs, tests/Encina.UnitTests/Security/Audit/AuditPipelineBehaviorRegistrationTests.cs.
```

</details>

### Phase 3: Integration Tests, Documentation and Finalization (pull request S3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/Security/Audit/ValidationRejectionAuditIntegrationTests.cs`: `IEncina.Send` through each of the three real providers with `AddEncinaAudit` and `AddEncinaValidationAudit` and the in-memory store (no container: the adapter calls only `IOperationAuditStore.RecordAsync`, which the existing store integration tests cover for the SQL and MongoDB stores): a rejected command and a rejected query write one rejection entry with codes and ids and no value; a valid request writes none; with the audit behavior outer a rejected command has two entries as Design Choice 4 says; a failing store still returns the validation `Left`; the bound suppresses beyond the limit.
2. **Create** `docs/features/validation-rejection-audit.md` through `docs-writer` (Diátaxis how-to and reference: registration, what an entry contains and never contains, the options, the pipeline order and the two-entry case, the metrics); **modify** `docs/INVENTORY.md` (one row, `docs-writer`).
3. **Create** `changelog.d/3978-validation-rejection-audit.added.md` through `mechanical-fixer`.
4. Build verification: `dotnet build --configuration Release` with 0 warnings; every flag of the touched files reaches the targets of "Estimated File Count".

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Encina issue #3978, phase 3. The core hook and the Security.Audit adapter exist. The integration test needs no container because the adapter only calls IOperationAuditStore.RecordAsync.
TASK: Write the integration tests of the plan, hand the documentation page, the INVENTORY row and the changelog fragment to docs-writer and mechanical-fixer, then run the build, the tests and the coverage commands.
KEY RULES: Tests drive IEncina.Send with the real providers and the real orchestrator. No message of a ValidationError in any assertion text of the stored entry. A test never swallows an exception. Documentation never types coverage figures by hand (covref markers). Never edit CHANGELOG.md.
REFERENCE FILES: tests/Encina.IntegrationTests/Security/Audit/AuditPipelineBehaviorAwaitedWriteTests.cs, tests/Encina.ContractTests/Security/Audit/AuditPipelineBehaviorContractTests.cs, docs/features/audit-tracking.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How the plan meets it |
|----------|-------------|-----------------------|
| GDPR Art. 5(1)(c) | Data minimisation | The entry has codes, counts and ids; no values, no addresses (register row C14) |
| GDPR Art. 5(2), Art. 32 | Accountability and security of processing | Rejections of repeated requests are recorded when the application opts in |
| ADR-036 | Operation audit store | The adapter writes `OperationAuditEntry` through `IOperationAuditStore` |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on | The meter tag follows the helper of #2676 |
| ADR-062 R3, R4 | Validation is `Validation`, store failures are `Internal` | Register row C9 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ValidationOrchestrator`, `ValidationResult`, `ValidationError` | `src/Encina/Validation/` | The hook and the source of counts, codes and names |
| `IOperationAuditStore`, `OperationAuditEntry`, `AuditOutcome` | `src/Encina.Security.Audit/` | The destination and the entry shape |
| `AuditPipelineBehavior` write path | `src/Encina.Security.Audit/AuditPipelineBehavior.cs:306` | Scope per write, suppressed ambient transaction |
| `OperationAuditErrors.WriteFailed` | `src/Encina.Security.Audit/OperationAuditErrors.cs` | The failure of the adapter |
| `OperationAuditMeter` | `src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs` | Home of the two counters |
| `RequestContextModuleExtensions` | `src/Encina/Modules/RequestContextModuleExtensions.cs` | Module name for the metadata |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (core) | `12300-12349` (`ValidationAudit`), ids 12300-12302 used | New range, area Core and messaging, first free 50-id slot after the reservations of the registry on `origin/main` and the claims of the plans of #340 (12200-12249) and #128, #296, #317, #392 (12250-12299); to be registered in S1 with its registry rows |
| `Encina.Security.Audit` | none | The adapter logs nothing: failures return `audit.write_failed` to the auditor, which logs them in core |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production, S1 (core) | 5 new or changed (`IValidationRejectionRecorder`, `ValidationRejection`, `ValidationRejectionAuditor`, `ValidationAuditLog`, `ValidationOrchestrator`) | Targets: `IValidationRejectionRecorder.cs` unit 0 guard 0 (an interface has no executable lines); `ValidationRejection.cs` unit 95 guard 80 (one factory with branches, tested through real results); `ValidationRejectionAuditor.cs` unit 95 guard 90 (every branch and every checked parameter); `ValidationAuditLog.cs` unit 90 guard 0 (generated bodies reached through the auditor tests; no validated parameters); `ValidationOrchestrator.cs` unit 100 guard 84 (today's targets, never lowered) |
| Registration and registry, S1 | 4 (`EventIdRanges.cs`, `PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, the registry page) | Mechanical |
| Tests, S1 | 3 (auditor unit, orchestrator unit edit, auditor guard) | One manifest `Encina.json` makes S1 13 files |
| Production, S2 (`Encina.Security.Audit`) | 5 (`ValidationAuditOptions`, `ValidationRejectionAuditRecorder`, `SubjectRateLimiter`, `OperationAuditMeter`, `ServiceCollectionExtensions`) | Targets: `ValidationAuditOptions.cs` unit 100 guard 100; `ValidationRejectionAuditRecorder.cs` unit 95 guard 85 contract 80 integration 80; `SubjectRateLimiter.cs` unit 95 guard 0 (internal type with no validated parameters; windows driven by `FakeTimeProvider`); `OperationAuditMeter.cs` unit 100 guard 0 (today's); `ServiceCollectionExtensions.cs` unit 92 guard 82 (today's, never lowered) |
| Registration and manifest, S2 | 2 (`PublicAPI.Unshipped.txt`, `Encina.Security.Audit.json`) | Mechanical |
| Tests, S2 | 5 (recorder unit, limiter unit, registration unit, guard, contract) | S2 is 12 files |
| S3 | 4 (integration test, feature page, `INVENTORY.md` row, changelog fragment) | The integration test is `Encina.IntegrationTests`, no container |
| Total | 33 files over three pull requests (13, 12, 4) | At most 15 files and 900 lines each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina (.NET 10, C# 14, pre-1.0). Issue #3978: an opt-in audit of requests rejected by the validation pipeline. The validation code is src/Encina/Validation (ValidationOrchestrator is a singleton under DataAnnotations and MiniValidator, scoped under FluentValidation). Encina.Security.Audit references Encina, so validation cannot reference it: a port in core and an adapter in Security.Audit.
IMPLEMENTATION OVERVIEW: (1) Core: IValidationRejectionRecorder, ValidationRejection, ValidationRejectionAuditor, ValidationAuditLog (12300-12302 in the new range ValidationAudit 12300-12349), one optional trailing ValidationOrchestrator constructor parameter and a call in the invalid-result branch. (2) Security.Audit: ValidationAuditOptions, internal ValidationRejectionAuditRecorder and SubjectRateLimiter, two counters, AddEncinaValidationAudit. (3) Integration tests with the three real providers, a documentation page, the changelog fragment.
KEY PATTERNS: The rejection Left is returned unchanged whatever the audit does. The entry never has a ValidationError message, a request value, an entity id read from the request, a payload hash, an IP address or a user agent. The recorder is singleton-safe (IServiceScopeFactory per write, TransactionScopeOption.Suppress, CancellationToken.None). TimeProvider everywhere. Metric names without _total; tenant tag only when multi-tenancy is on. Errors are codes; exceptions are logged through ForLogging(). The orchestrator parameter is optional and last (CS1737). Design Choices 1 to 4 are Dilemmas recommended and pending the maintainer: hook in the orchestrator, property names opt-in, optional bound, both entries documented.
REFERENCE FILES: src/Encina/Validation/ValidationOrchestrator.cs, src/Encina.Security.Audit/AuditPipelineBehavior.cs, src/Encina.Security.Audit/OperationAuditEntry.cs, src/Encina.Security.Audit/ServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, tests/Encina.UnitTests/Security/Audit/AuditPipelineBehaviorRegistrationTests.cs, docs/plans/validation-localization-implementation-plan-233.md (sibling by issue number #233).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Per-request audit write; there is no read path to cache. |
| 2 | OpenTelemetry | ✅ | Two counters in the existing `Encina.OperationAudit` meter (write failures, rejections suppressed by the optional limit); tenant tag only when multi-tenancy is on (ADR-054 D4, helper of #2676). |
| 3 | Structured Logging | ✅ | Three `[LoggerMessage]` ids in the new core range `ValidationAudit` (12300-12349): recorder returned a Left, recorder threw, entry written at Debug; codes and exception type only. |
| 4 | Health Checks | ❌ | The audit store has its own `OperationAuditStoreHealthCheck`; the hook adds no dependency of its own. |
| 5 | Validation | ✅ | The hook sits in `ValidationOrchestrator`, shared by the three validation providers. |
| 6 | Resilience | ❌ | Store resilience belongs to the store; the hook never retries, a failed write is logged and the rejection is returned unchanged. |
| 7 | Distributed Locks | ❌ | No shared mutable state across instances; the optional limiter is per process. |
| 8 | Transactions | ❌ | The audit write suppresses any ambient `TransactionScope`, like `AuditPipelineBehavior`, and is not atomic with anything. |
| 9 | Idempotency | ❌ | A repeated request is a repeated rejection; each one is an entry. |
| 10 | Multi-Tenancy | ✅ | The entry carries the `TenantId` of the request context; the metric tenant tag follows ADR-054 D4. |
| 11 | Module Isolation | ✅ | The entry carries the module name in its metadata when the request context has one (`RequestContextModuleExtensions.GetModuleName`, `IRequestContext.ModuleId` after #2442). |
| 12 | Audit Trail | ✅ | This is the feature: one `OperationAuditEntry` per rejected request through `IOperationAuditStore`. |

---

## Prerequisites & Dependencies

| Item | Kind | Why |
|------|------|-----|
| #233 S1 (coded `ValidationError`) | Soft | `ErrorCodes` stays empty until it lands (register row C13); nothing else waits |
| #2676 (tenant tag helper) | Soft | The metric tenant tag; until then the counters carry no tenant tag |
| #726 | None (sibling) | Instruments the orchestrator centrally; both add optional constructor parameters, appended in landing order |
| Prior plans' EventIds | Checked | 12200-12299 are claimed by other plans; 12300-12349 is first free on `origin/main` on 2026-11-24 |

---

## Next Steps

1. Open S1, then S2, then S3; each builds and passes alone (S1 adds an optional parameter and a port nobody implements yet; S2 adds the only implementer and its registration).
2. A bounded, durable limiter (shared between instances) is not in this plan: a deployment that needs one limits at the edge or asks in a new issue when a measured need exists.
3. Auditing of other pipeline rejections (authorization, tenant gates) belongs to their own packages; #232 audits security violations found by `Encina.Validation.Security`.
