# Implementation Plan: Enhanced Async and Cross-Field Validation

> **Issue**: [#230](https://github.com/dlrivada/Encina/issues/230)
> **Type**: Feature
> **Related**: #227 (generated validators, consumes the contract of this plan), #228 (domain validation), #234 (validation result aggregation, planned apart), #236 (two-phase validation, planned apart), #726 (OpenTelemetry for the validation providers, owns the central instrumentation), #3415 (a code-less validation `Left` never becomes a 400), #702 (validator resolution cache), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (core plus the three validation providers; no provider-dependent persistence code)
> **Estimated Scope**: ~900 lines of production code + ~2,650 lines of tests + ~400 lines of documentation (~3,950 lines in total, as the Estimated File Count table), in six pull requests of at most 15 files and 1,500 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`)

- `IValidationProvider.ValidateAsync<TRequest>(request, context, ct)` returns a `ValueTask<ValidationResult>` (`src/Encina/Validation/IValidationProvider.cs:29`); `ValidationOrchestrator` calls the one registered provider and turns an invalid result into a `Left` built with `EncinaError.New(message)`, a code-less error (`ValidationOrchestrator.cs:68-72`). It catches only an `OperationCanceledException` raised while the token is cancelled (`:76-80`); any other exception leaves the pipeline as an exception.
- `FluentValidationProvider` already resolves every `IValidator<TRequest>`, runs them with `Task.WhenAll` and copies `CorrelationId`, `UserId` and `TenantId` into `RootContextData` (`FluentValidationProvider.cs:44-70`). `MustAsync`, `When`/`Unless` and `DependentRules` therefore work today for FluentValidation users. It is registered with `TryAddScoped` (`ServiceCollectionExtensions.cs:62-63`).
- `DataAnnotationsValidationProvider` is synchronous: `Validator.TryValidateObject(..., validateAllProperties: true)` with `new ValidationContext(request, serviceProvider: null, items: null)` (`DataAnnotationsValidationProvider.cs:31,45`). `IValidatableObject.Validate` (cross-field) runs inside `TryValidateObject`, but with no service provider and no async. `MiniValidationProvider` calls `MiniValidator.TryValidate(request, out var errors)` (`MiniValidationProvider.cs:31`) and ignores the request context. Both are registered with `TryAddSingleton` and have a public parameterless constructor (`DataAnnotations/ServiceCollectionExtensions.cs:72-73`, `MiniValidator/ServiceCollectionExtensions.cs:65-66`), so a validator that needs a scoped `DbContext` or repository cannot be resolved by them.
- Not in `src`: any provider-neutral async validator contract, any parallel runner outside FluentValidation, any timeout on a validator, any EventId range for validation (`EventIdRanges.cs` has none).
- The issue's examples (`MustAsync`, `GreaterThan(x => x.StartDate)`, `When`, `Unless`, `SetValidator`) are FluentValidation API and already work; the gap is DataAnnotations and MiniValidator (two of the three providers of `AGENTS.md` section 5), plus the runner properties the acceptance criteria name (parallel execution, cancellation, failure handling).

### What this plan delivers

A provider-neutral request validator contract in core, `IRequestValidator<in TRequest>`, and a `RequestValidatorRunner` that runs every registered validator of a request type in parallel, bounded and with a per-validator timeout, under the request's `CancellationToken`. `DataAnnotationsValidationProvider`, `MiniValidationProvider` and `FluentValidationProvider` all call the runner and merge its result with their own, so an async, DI-backed or cross-field rule written once as an `IRequestValidator<T>` behaves the same under all three providers. The rules native to a library keep that library's failure semantics: an exception thrown inside a FluentValidation `MustAsync` rule stays an unmapped exception (register row D9). Cross-field rules without I/O stay `IValidatableObject` (DataAnnotations and MiniValidator) or native FluentValidation. Nothing changes for an application that registers no `IRequestValidator<T>`.

- Affected packages: `Encina` (core), `Encina.DataAnnotations`, `Encina.MiniValidator`, `Encina.FluentValidation`.
- Provider category: Validation (3) (`AGENTS.md` section 5): the three providers are changed in lock-step (pull requests S4 and S5) and an equivalence contract test pins that the same rule gives the same `ValidationResult` under each.
- Shared piece, owned by this plan: `IRequestValidator<T>`, `RequestValidatorRunner`, `RequestValidatorOptions` and `AddEncinaRequestValidators` in core `Encina.Validation`. #227 builds its generated validators on this contract (its plan names it as a prerequisite).
- Behavior changes: `AddDataAnnotationsValidation` and `AddMiniValidation` register the provider and the orchestrator as scoped instead of singleton (Design Choice 2), and a non-cancellation exception from a validator becomes a `Left` instead of an exception (Design Choice 3).

---

## Design Choices

<details>
<summary><strong>1. How do DataAnnotations and MiniValidator run async and DI-backed rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A core `IRequestValidator<in TRequest>` resolved from DI and run by a shared `RequestValidatorRunner` that every provider calls** | One rule, same behavior under the three providers; the validator can take a scoped repository; the request context (`TenantId`, `UserId`, `CorrelationId`) is a parameter; #227 reuses it | A new public contract in core; FluentValidation users have a second way to write an async rule |
| **B) An `IAsyncValidatableObject` interface the request type implements** (the async twin of `IValidatableObject`) | No DI registration; mirrors a BCL shape | The request object has no services, so the issue's own example (`customers.ExistsAsync`) cannot be written; the request DTO would depend on infrastructure |
| **C) A provider-neutral rule builder DSL (`RuleFor(...).MustAsync(...)`) in core** | One syntax for all providers | Re-implements FluentValidation inside Encina; large public surface; two DSLs for FluentValidation users |

### Chosen Option: **A — `IRequestValidator<T>` plus a shared runner** (recommended, pending the maintainer)

### Rationale

- The issue's example needs a repository inside the validator (`ICustomerRepository`); only an object resolved from the container can have one. A is the smallest contract that allows it and keeps the request type a plain DTO.
- `FluentValidationProvider` already has `IValidator<T>`; A adds a neutral sibling instead of a second DSL. B wins when validation must travel with the request type and never touch services; C wins when the maintainer wants FluentValidation removed as a dependency.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Provider and orchestrator lifetime for DataAnnotations and MiniValidator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Scoped provider and orchestrator (as `FluentValidationProvider` is today); the provider takes the runner through its constructor** | A validator sees the same scoped services (DbContext, tenant services) as the handler; no scope created per call; one lifetime for all three providers | Behavior change: a provider resolved from the root provider with scope validation on now fails; the tests that build the provider with `new` still compile because the new constructor parameter is optional |
| **B) Keep both singleton; the runner creates a scope per validation through `IServiceScopeFactory`** | No lifetime change | The validator runs in a different scope than the handler (a second `DbContext`, a second tenant resolution); one scope allocation per request |

### Chosen Option: **A — Scoped provider and orchestrator** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 3 (registration completeness) forbids injecting a scoped service into a singleton; both options comply (A by changing the lifetime, B through a factory), so the choice is a Dilemma. `TryAddScoped` for the same two types is what `AddEncinaFluentValidation` does today (`FluentValidation/ServiceCollectionExtensions.cs:62-63`), so A makes the three registrations equal.
- B wins when an application resolves the provider from the root provider outside a request (a hosted service that validates a message by hand).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How a validator that throws or times out reaches the caller</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The runner throws a `RequestValidatorException` carrying a code; `ValidationOrchestrator` catches it and returns a `Left` (`validation.async_validator_failed`, `Internal`; `validation.async_validator_timeout`, `Timeout`)** | The pipeline answers with a typed error; the request is denied (fail closed); ADR-062 maps the category to a status | One more exception type; the orchestrator gains a catch |
| **B) Let the exception propagate (today's behavior for any provider exception)** | No new code | The caller sees an unhandled exception for an infrastructure failure inside validation; no category |

### Chosen Option: **A — Typed `Left` from the orchestrator** (recommended, pending the maintainer)

### Rationale

- Both options deny the request, so fail closed does not decide it. A follows ADR-062 rule R4 (provider and store failures are `Internal`) and AGENTS.md section 3 (a failure in a gate is an operation failure, never a success).
- The exception carries the validator type and the inner exception; the log line uses `ForLogging()` (type and stack trace, never the message); the `Left` carries only the code.
- B wins when the application has its own exception middleware that already maps every exception.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Do async validators run when the synchronous pass already failed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only when the provider's own synchronous pass is valid (`RunAsyncValidatorsAfterSyncPasses = true`)** | No database lookup for an obviously malformed request; bounds the cost an attacker can force with garbage input | The client sees the async errors only after fixing the sync ones; DataAnnotations and MiniValidator differ from FluentValidation, whose `MustAsync` rules run with the rest |
| **B) Always run everything and aggregate** | One round trip shows every error; equals FluentValidation's behavior | Runs I/O for requests that cannot pass |

### Chosen Option: **A — Run async validators after the synchronous pass is valid** (recommended, pending the maintainer)

### Rationale

- The option is a property of `RequestValidatorOptions`, default `true`, and applies to the validators run by the runner under all three providers; the FluentValidation provider applies it to `IRequestValidator<T>` only (its own `MustAsync` rules keep running with the rest).
- B wins when a form shows all errors at once and the lookups are cheap and cached.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Cross-field and conditional syntax</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No new DSL: cross-field rules are `IValidatableObject` (DataAnnotations, MiniValidator), native FluentValidation (`GreaterThan(x => x.StartDate)`, `When`, `Unless`, `DependentRules`) or plain C# in an `IRequestValidator<T>`; declarative cross-field attributes come from #227** | Nothing to learn or maintain; every example of the issue is covered; the equivalence contract test proves the three providers agree | The conditional and dependent syntax differs per provider |
| **B) A neutral builder in core for conditional and dependent rules** | One syntax | Duplicates FluentValidation; large public API |

### Chosen Option: **A — No new DSL** (recommended, pending the maintainer)

### Rationale

- The acceptance criteria "cross-field validation support" and "conditional validation (When/Unless)" are met by existing library features; this plan adds the test evidence (S5) and the documentation (S6), not new syntax.
- B wins when the maintainer wants application code independent of the validation library.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The contract and runner in core `Encina.Validation`, next to `IValidationProvider`** | The three provider packages already reference core; no new project | Core grows by about six files |
| **B) A new package `Encina.Validation.Async`** | Core untouched | Three providers would reference a fourth package; the orchestrator mapping (Design Choice 3) lives in core anyway |

### Chosen Option: **A — Core `Encina.Validation`**

### Rationale

- The orchestrator, the provider contract and `ValidationResult` are in core (`src/Encina/Validation/`); the runner is part of that machinery.

**Class:** Trivial
**Settled by:** the existing layout, `src/Encina/Validation/IValidationProvider.cs`, `ValidationOrchestrator.cs` and `ValidationPipelineBehavior.cs`, and the issue's "Affected Packages" list, which ticks `Encina (core)`

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A `Left` from the runner or a validator failure denies the request; nothing reports success | Settled | AGENTS.md section 3 (errors are never swallowed; gates fail closed) |
| C2 | The caller's cancellation is not a validator failure: it stays `OperationCanceledException` to the orchestrator, which already turns it into a `Left` (`ValidationOrchestrator.cs:76-80`); a validator timeout is a different token and a different code | Settled | ADR-062 rule R5; verified `ValidationOrchestrator.cs:76-80` |
| C3 | Logs, tags and errors carry codes, the validator type and the request type name, never the request values or `EncinaError.Message`; exceptions go through `ForLogging()` | Settled | AGENTS.md section 3 (#1168, #1173, #1259, #1274) |
| C4 | Timeouts use the injected `TimeProvider` (`new CancellationTokenSource(timeout, timeProvider)`), never the system clock directly | Settled | AGENTS.md section 3 (time comes from `TimeProvider`) |
| C5 | No `IRequestValidator<T>` registered means the runner returns `Success` without work; registering one is the opt-in | Settled | AGENTS.md section 1 (pay-for-what-you-use) |
| C6 | OpenTelemetry for the validation operation (duration, failure rate) is deferred to #726, which instruments `ValidationOrchestrator` centrally and so covers every provider and this runner's call | Trivial | `AGENTS.md` section 6 allows "Defer (open an issue and reference it)"; the deferral is recorded as #726 (open) |
| C7 | EventIds 550-552 are packed from the start of the block assigned to this family | Trivial | AGENTS.md section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C8 | New error codes and categories (ADR-062), created with the category overload: `validation.async_validator_failed` (`ErrorCategory.Internal`, rule R4), `validation.async_validator_timeout` (`ErrorCategory.Timeout`, the `Timeout` row of the ADR-062 category table, HTTP 504, `docs/architecture/adr/062-error-category-contract.md:52`) | Dilemma (recommended, pending the maintainer) | public codes; follows Design Choice 3 |
| D1 | Defaults `MaxDegreeOfParallelism = 4`, `ValidatorTimeout = 5 seconds`, `RunAsyncValidatorsAfterSyncPasses = true`; options validated at start-up (parallelism above 0, a finite timeout above zero; a hung validator must never hang a request) | Dilemma (recommended, pending the maintainer) | numeric defaults |
| D2 | The runner returns the merged errors ordered by validator registration order then error order, so the result is deterministic although execution is parallel | Dilemma (recommended, pending the maintainer) | public behavior |
| D3 | `DataAnnotationsValidationProvider` passes its `IServiceProvider` into the `ValidationContext` (so attributes and `IValidatableObject` can call `GetService`) and `MiniValidationProvider` uses the `TryValidate(target, serviceProvider, out errors)` overload that MiniValidation 0.9.2 offers; #232 relies on it | Dilemma (recommended, pending the maintainer) | changes what attributes can see |
| D4 | The `FluentValidationProvider` shares one `ValidationContext<TRequest>` across validators that run in parallel (`FluentValidationProvider.cs:52,66-68`), a thread-safety defect in a file S5 touches. Option A: fix it in S5 (one context per validator; S5 grows by one test file and stays at 13 files). Option B: leave it and open a follow-up issue (draft: `artifacts/issues/fluentvalidation-provider-shared-context.md`), keeping S5 to the runner call. `AGENTS.md` section 11 allows both (fix now, or open an issue and continue) | Dilemma (recommended, pending the maintainer): B | B wins when the maintainer wants the race fixed with its own regression test and review; A wins when a release must not ship a known race in code this plan edits |
| D5 | When several validators fail or time out at once: the runner waits for every validator (wait-all, no sibling cancellation except the caller's token), merges all failures, and when any validator threw or timed out the result is the exception, the timeout code taking precedence over the failure code if both occur. Option B: fail-fast, the first failure cancels the siblings | Dilemma (recommended, pending the maintainer): wait-all | wait-all gives a deterministic result and every error; fail-fast wins when validators are expensive and one failure already decides the outcome |
| D6 | Order inside the merged result: the provider's own synchronous errors first, then the runner's errors (register row D2 order). Option B: sort all errors by property name | Dilemma (recommended, pending the maintainer): sync first | public behavior; sorting wins when a client diffs results across providers |
| D7 | `AddRequestValidator<TRequest, TValidator>` defaults to `ServiceLifetime.Scoped` (a validator may take a scoped repository) and registers with `TryAddEnumerable`, so registering the same validator type twice runs it once. Option B: `Transient` default; Option C: always add (duplicates run twice) | Dilemma (recommended, pending the maintainer): Scoped plus `TryAddEnumerable` | Scoped matches the provider lifetime (Design Choice 2); Transient wins for stateless validators with no scoped dependency |
| D8 | A provider constructed without a runner (the optional constructor parameter) runs no `IRequestValidator<T>`: that is a construction by hand with no container, where no validator can exist; the DI path always passes the runner, proven by the S4 and S5 DI tests that resolve the provider from a provider with a registered validator and observe it run. Option B: a required constructor parameter (fails closed by construction; changes the 19, 25 and 6 `new` sites of existing tests, two more test files in S4 and one in S5) | Dilemma (recommended, pending the maintainer): optional | A keeps existing tests unchanged; B wins when the maintainer wants a hand-built provider never to skip validators silently |
| D9 | An exception thrown inside a FluentValidation-native rule (`MustAsync`) is not mapped by the plan: it stays an exception as today, while an `IRequestValidator<T>` failure becomes a `Left` (Design Choice 3). The three providers therefore differ on native-rule exceptions; documented in S6. Option B: the Fluent provider wraps its own validators' exceptions in `RequestValidatorException` too | Dilemma (recommended, pending the maintainer): unmapped | unmapped keeps library semantics and the Fluent change small; B wins when uniform failure semantics matter more than the size of S5 |

---

## Implementation Phases

### Phase 1: Core Contract, Options and Runner

> **Goal**: The neutral contract and the runner, tested alone; no provider uses it yet (pull request S1).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/IRequestValidator.cs`: `public interface IRequestValidator<in TRequest> { ValueTask<ValidationResult> ValidateAsync(TRequest request, IRequestContext context, CancellationToken cancellationToken); }`.
2. **Create** `RequestValidatorOptions.cs` (`MaxDegreeOfParallelism`, `ValidatorTimeout`, `RunAsyncValidatorsAfterSyncPasses`, defaults of register row D1) and `RequestValidatorOptionsValidator.cs : IValidateOptions<RequestValidatorOptions>`.
3. **Create** `RequestValidationErrorCodes.cs` (the two codes of register row C8, created through `EncinaErrors.Create(code, ErrorCategory.X, message)`) and `RequestValidatorException.cs` (sealed; `Code`, `ValidatorType`, inner exception; the message holds the code only).
4. **Create** `RequestValidatorRunner.cs` (sealed; constructor `(IServiceProvider serviceProvider, IOptions<RequestValidatorOptions> options, TimeProvider? timeProvider = null)`; `ValueTask<ValidationResult> RunAsync<TRequest>(TRequest request, IRequestContext context, bool syncPassIsValid, CancellationToken ct)`): resolves `IEnumerable<IRequestValidator<TRequest>>` from the injected provider, returns `Success` when none, applies register row D2 ordering, runs at most `MaxDegreeOfParallelism` validators at once (`Parallel.ForEachAsync`), gives each its own linked token with `ValidatorTimeout` (C4), and maps a validator exception or its own timeout to `RequestValidatorException` (register row C8); the caller's cancellation propagates as `OperationCanceledException`.
5. **Create** `RequestValidatorServiceCollectionExtensions.cs`: `AddEncinaRequestValidators(this IServiceCollection, Action<RequestValidatorOptions>? configure = null)` (`TryAddScoped` runner, `TryAddSingleton(TimeProvider.System)`, options with `ValidateOnStart`) and `AddRequestValidator<TRequest, TValidator>(this IServiceCollection, ServiceLifetime lifetime = ServiceLifetime.Scoped)`.
6. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).
7. Tests of this phase ship in S1: unit (runner: none registered, one, many, ordering, timeout with `FakeTimeProvider`, throwing validator, caller cancellation, parallelism bound), unit (options validator), guard (runner and extension null checks), property (the merged error set equals the union of the validators' errors for any registration order and any degree of parallelism), and a registration DI test of `AddEncinaRequestValidators` and `AddRequestValidator` that builds the provider with `ValidateOnBuild` and `ValidateScopes`, resolves the runner and a registered validator, and proves a duplicate registration of one validator type runs once (register row D7).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #230 adds a provider-neutral async validator
contract to core Encina.Validation (src/Encina/Validation/). Read docs/plans/async-cross-field-validation-
implementation-plan-230.md, Design Choices 1-4 and register rows C1-C8 and D1-D2, for the shape.

TASK:
1. Create IRequestValidator<in TRequest>, RequestValidatorOptions (+ IValidateOptions validator), the two error
   codes with ErrorCategory (ADR-062 category overload of EncinaErrors.Create), RequestValidatorException and
   RequestValidatorRunner under src/Encina/Validation/.
2. The runner resolves validators from the injected IServiceProvider per call, runs them with a bounded degree of
   parallelism, a per-validator timeout from the injected TimeProvider, and the caller's CancellationToken.
3. AddEncinaRequestValidators registers runner, options (ValidateOnStart) and TimeProvider with TryAdd*.

KEY RULES:
- Never inject a scoped service into a singleton; the runner is scoped.
- Errors and logs carry codes and type names only, never request values or EncinaError.Message.
- XML docs on every public member; PublicAPI.Unshipped.txt through mechanical-fixer; no [Obsolete].

REFERENCE FILES:
- src/Encina/Validation/IValidationProvider.cs, ValidationOrchestrator.cs, ValidationResult.cs
- src/Encina/Results/EncinaErrors.cs (category overloads), src/Encina/Errors/ErrorCategory.cs
```

</details>

---

### Phase 2: Logging and EventIds

> **Goal**: The runner logs failures and skips by code (pull request S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationAsync = (550, 559)` (block 550-599 assigned to this family; this plan owns 550-559).
2. **Create** `src/Encina/Validation/ValidationAsyncLog.cs` (`[LoggerMessage]`, packed): 550 an async validator failed (error: request type, validator type, exception through `ForLogging()`), 551 an async validator timed out (warning: request type, validator type, timeout), 552 async validators skipped because the synchronous pass failed (debug: request type, count). 553-559 stay free inside the range.
3. **Modify** `RequestValidatorRunner.cs`: optional trailing `ILogger<RequestValidatorRunner>? logger = null` (the S1 tests that construct it with `new` compile unchanged); **modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina` maps to the new range as well) and the `PublicAPI.Unshipped.txt` and manifest through `mechanical-fixer`.
4. Tests: a logging unit test (EventIds and the absence of request values in the rendered messages) and the null-logger valid-result case added to the S1 guard test file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #230, plan docs/plans/async-cross-field-validation-implementation-plan-230.md, phase 2 of 6. ADR-021: register
the EventId range in src/Encina/Diagnostics/EventIdRanges.cs first.

TASK:
1. Register ValidationAsync (550, 559); write ValidationAsyncLog.cs with 550, 551, 552 as [LoggerMessage] methods.
2. Give RequestValidatorRunner an optional ILogger and log by code and type name; an exception goes through
   ForLogging().
3. Update EncinaEventIdAllocationTests so the Encina assembly maps to the new range.

KEY RULES:
- Pack ids sequentially, no gaps; XML docs name the range; no request values in any message.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 3: Orchestrator Failure Mapping

> **Goal**: A runner failure becomes a typed `Left` (pull request S3, Design Choice 3 option A).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Validation/ValidationOrchestrator.cs`: catch `RequestValidatorException` after the existing `OperationCanceledException` catch and return `Left` built from the exception's code and category (`EncinaErrors.Create(code, category, message)`); the message names the code and the request type only. The existing code-less `Left` for an invalid result is #3415's and is not changed here.
2. **Modify** `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).
3. Tests: new `ValidationOrchestratorAsyncFailureTests` (failure gives `Internal`, timeout gives `Timeout`, caller cancellation still gives the cancelled `Left`, a plain validation failure is unchanged); the existing orchestrator constructor tests are untouched (the constructor does not change).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #230, plan docs/plans/async-cross-field-validation-implementation-plan-230.md, phase 3 of 6. Depends on S1.

TASK:
1. In ValidationOrchestrator.ValidateAsync add a catch for RequestValidatorException that returns a Left with the
   exception's code and ErrorCategory (ADR-062).
2. Keep the existing OperationCanceledException catch first; do not touch the code-less Left of an invalid result.

KEY RULES:
- Message holds the code and request type name, never the inner exception message.
- A new test class proves each mapping; no behavior change for a valid or invalid ValidationResult.

REFERENCE FILES:
- src/Encina/Validation/ValidationOrchestrator.cs (:49-81)
- src/Encina/Validation/RequestValidatorRunner.cs
```

</details>

---

### Phase 4: DataAnnotations and MiniValidator Providers

> **Goal**: The two synchronous providers run the runner and the cross-field rules see the services (pull request S4).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.DataAnnotations/DataAnnotationsValidationProvider.cs`: add an optional trailing constructor parameter `RequestValidatorRunner? runner = null` and an `IServiceProvider? serviceProvider = null` the `ValidationContext` receives (register row D3); after the synchronous pass call `runner.RunAsync` (register row D1 `RunAsyncValidatorsAfterSyncPasses`) and merge the errors. The 19 `new DataAnnotationsValidationProvider()` sites of `DataAnnotationsValidationProviderTests.cs` compile unchanged (verified with `git grep -c "new DataAnnotationsValidationProvider(" -- tests`).
2. **Modify** `src/Encina.MiniValidator/MiniValidationProvider.cs` the same way (the 25 `new MiniValidationProvider()` sites of `MiniValidationProviderTests.cs` compile unchanged) and use the `TryValidate(target, serviceProvider, out errors)` overload when a provider is present.
3. **Modify** both `ServiceCollectionExtensions.cs`: `TryAddScoped` for provider and orchestrator (Design Choice 2) and a call to `AddEncinaRequestValidators()`; the pipeline behavior registration is unchanged.
4. **Modify** both `PublicAPI.Unshipped.txt` and both coverage manifests (through `mechanical-fixer`).
5. Tests (S4): per provider an async-validator unit test (valid, invalid, timeout, merged errors, cancellation) and a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves the provider, the orchestrator and the behavior and runs a registered `IRequestValidator<T>` through the resolved provider (register row D8); one guard test file for the new optional constructor parameters (a null runner is a valid case: the provider behaves as before).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #230, plan docs/plans/async-cross-field-validation-implementation-plan-230.md, phase 4 of 6. Depends on S1-S3.
AGENTS.md section 5: the three validation providers stay consistent; this phase does DataAnnotations and MiniValidator.

TASK:
1. Add optional trailing constructor parameters (runner, service provider) to DataAnnotationsValidationProvider and
   MiniValidationProvider; call the runner after the synchronous pass is valid and merge the errors.
2. Change both AddX registrations to TryAddScoped for provider and orchestrator and call AddEncinaRequestValidators().
3. Prove registration completeness with a ValidateOnBuild + ValidateScopes DI test per package.

KEY RULES:
- Existing tests that construct the providers with new compile unchanged (optional parameters).
- No scoped service into a singleton; XML docs; PublicAPI.Unshipped.txt through mechanical-fixer.

REFERENCE FILES:
- src/Encina.DataAnnotations/DataAnnotationsValidationProvider.cs, ServiceCollectionExtensions.cs
- src/Encina.MiniValidator/MiniValidationProvider.cs, ServiceCollectionExtensions.cs
- src/Encina.FluentValidation/ServiceCollectionExtensions.cs (the scoped precedent)
```

</details>

---

### Phase 5: FluentValidation Provider, Equivalence and Integration Tests

> **Goal**: The third provider calls the runner and the three providers are proven equivalent (pull request S5).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.FluentValidation/FluentValidationProvider.cs`: optional trailing `RequestValidatorRunner? runner = null`; after its own validators call the runner (the 6 `new FluentValidationProvider(` sites of `FluentValidationProviderTests.cs` compile unchanged) and merge. The shared `ValidationContext` is not touched (register row D4). **Modify** its `ServiceCollectionExtensions.cs` to call `AddEncinaRequestValidators()` in both `AddEncinaFluentValidation` overloads, `PublicAPI.Unshipped.txt` and the manifest.
2. **Create** `tests/Encina.ContractTests/Core/Validation/ValidationProviderEquivalenceContractTests.cs`: one set of rules (an end date before the start date, a conditional required field, an async lookup) expressed per provider (`IValidatableObject`, FluentValidation `GreaterThan`/`When`, an `IRequestValidator<T>`) and the same `ValidationResult` expected from each.
3. **Create** `tests/Encina.PropertyTests/Validation/CrossFieldEquivalenceProperties.cs` (FsCheck: for any date pair the three providers agree on validity).
4. **Create** `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/AsyncValidationPipelineTests.cs`: the request goes through `IEncina.Send` with the real pipeline behavior, a scoped fake repository and `TenantId` in the context; the folders hold only `GlobalSuppressions.cs` today.
5. Tests of the Fluent change: a unit test (runner called, errors merged) and a DI test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #230, plan docs/plans/async-cross-field-validation-implementation-plan-230.md, phase 5 of 6. Depends on S1-S4.

TASK:
1. Make FluentValidationProvider call RequestValidatorRunner after its own validators and merge the result; keep the
   shared ValidationContext as is (register row D4).
2. Write the equivalence contract test, the FsCheck property and the three integration tests that run through
   IEncina.Send.

KEY RULES:
- Tests execute real provider code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly.
- The integration tests are not database tests: no container, a scoped fake repository.

REFERENCE FILES:
- src/Encina.FluentValidation/FluentValidationProvider.cs, ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/FluentValidation/FluentValidationProviderTests.cs
- tests/Encina.PropertyTests/Validation/FluentValidation/ValidationInvariantProperties.cs
```

</details>

---

### Phase 6: Documentation and Finalization

> **Goal**: Usage, the three provider READMEs, the changelog and the justification files (pull request S6).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/async-and-cross-field-validation.md` (through `docs-writer`): the contract, the three provider examples of the issue, the options, the failure codes, when to use `IValidatableObject`, FluentValidation or `IRequestValidator<T>`.
2. **Modify** `src/Encina.DataAnnotations/README.md`, `src/Encina.FluentValidation/README.md` and `src/Encina.MiniValidator/README.md` (through `docs-writer`).
3. **Create** `changelog.d/230-async-cross-field-validation.added.md` (through `mechanical-fixer`).
4. **Create** the justification files for Benchmark and Load tests (`.md`, `AGENTS.md` section 9): `tests/Encina.BenchmarkTests/Validation/AsyncCrossFieldValidation.md` and `tests/Encina.LoadTests/Validation/AsyncCrossFieldValidation.md`, each with the sections the justification-file format requires: the runner is bounded and I/O-bound; the existing `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs` stays the provider benchmark of #945.
5. Build verification: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the touched projects; per-flag coverage of the touched files against their manifest targets (table below).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #230, plan docs/plans/async-cross-field-validation-implementation-plan-230.md, phase 6 of 6.

TASK:
1. Document the contract and the three-provider behavior; update the three provider READMEs.
2. Add the changelog fragment and the two justification files; run the build and the per-flag coverage measurement.

KEY RULES:
- Never type coverage figures by hand: cite cov: markers (SPEC-001). Pages go through docs-writer; fragments through
  mechanical-fixer. Link ADRs and sibling plans by number only.

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
| ADR-062 | Error category contract: R4 (provider failures are `Internal`), R5 (cancellation) | Codes and categories of register row C8 |
| ADR-054 D3, D4 | One meter and source per package; tenant tag only when multi-tenancy is on | Why the instrumentation stays with #726 (register row C6) |
| `System.ComponentModel.DataAnnotations.IValidatableObject` | `Validate(ValidationContext)` runs inside `Validator.TryValidateObject` | Cross-field rules without I/O (Design Choice 5) |
| FluentValidation documentation: async validation, dependent rules | `MustAsync`, `When`, `Unless`, `DependentRules` | Behavior the contract test compares against |
| .NET `CancellationTokenSource(TimeSpan, TimeProvider)` | Timeout driven by a `TimeProvider` | Per-validator timeout (register row C4) |
| MiniValidation 0.9.2 | `TryValidate(target, IServiceProvider, out errors)` overload | Passing services to MiniValidator rules (register row D3) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IValidationProvider`, `ValidationOrchestrator`, `ValidationPipelineBehavior`, `ValidationResult` | `src/Encina/Validation/` | Extended, not replaced |
| `FluentValidationProvider` (parallel run, context data) | `src/Encina.FluentValidation/FluentValidationProvider.cs` | Model for the runner and for the lifetime |
| `EncinaErrors.Create(code, category, message)` | `src/Encina/Results/EncinaErrors.cs` | Typed errors with a category |
| `FakeTimeProvider` | `Microsoft.Extensions.TimeProvider.Testing` (central package version) | Timeout tests without sleeping |
| `ValidationBenchmarks` | `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs` | Existing provider benchmark |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` core (`ValidationAsync`, new, inside the block 550-599 assigned to the validation-1 family) | 550-559 | 550-552 used (three messages); 553-559 free; the same block holds the sibling plans' ranges (#227 560-569, #231 580-589, #232 590-599; #228 needs none) |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, the README and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Contract, options, codes, exception, runner, `AddEncinaRequestValidators` | 7 (`IRequestValidator`, `RequestValidatorOptions`, its validator, `RequestValidationErrorCodes`, `RequestValidatorException`, `RequestValidatorRunner`, `RequestValidatorServiceCollectionExtensions`) | 1 (PublicAPI) | 6 (runner unit, options validator unit, runner guard, extensions guard, runner property, registration DI test) | 0 | 1 | 0 | 15 | ~1,100 |
| S2 | Logging and EventIds | 1 (`ValidationAsyncLog`) | 3 (runner, `EventIdRanges.cs`, PublicAPI) | 1 (logging unit) | 2 (`EncinaEventIdAllocationTests`, the S1 guard file) | 1 | 0 | 8 | ~350 |
| S3 | Orchestrator failure mapping | 0 | 1 (`ValidationOrchestrator.cs`) | 1 (`ValidationOrchestratorAsyncFailureTests`) | 0 | 1 | 0 | 3 | ~250 |
| S4 | DataAnnotations and MiniValidator | 0 | 6 (2 providers, 2 `ServiceCollectionExtensions`, 2 PublicAPI) | 5 (2 async unit, 2 DI, 1 guard) | 0 (the 44 `new` sites compile unchanged) | 2 | 0 | 13 | ~900 |
| S5 | FluentValidation, equivalence, integration | 0 | 3 (provider, `ServiceCollectionExtensions`, PublicAPI) | 8 (Fluent unit, Fluent DI, contract, property, 3 integration, 1 Fluent guard) | 0 | 1 | 0 | 12 | ~950 |
| S6 | Documentation | 0 | 3 (3 READMEs) | 0 | 0 | 0 | 4 (feature page, changelog fragment, 2 justification `.md`) | 7 | ~400 |
| **Total** | | **8** | **17** | **21** | **2** | **6** | **4** | **58** | **~3,950** |

The columns sum to the total (8 + 17 + 21 + 2 + 6 + 4 = 58) and every statement of the total in this plan is 58 files and about 3,950 lines (1,100 + 350 + 250 + 900 + 950 + 400). The largest pull request is S1 at 15 files; none exceeds 1,500 lines. S6's two "Other" justification files are the Benchmark and Load `.md` files of Phase 6 and the fragment and feature page make the other two. S1 comes first; S2 and S3 need only S1; S4 and S5 need S1 to S3; S6 needs all.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file:

| File (entered by) | Unit | Guard | Property | Justification |
|------|------|-------|----------|---------------|
| `RequestValidatorRunner.cs` (S1) | 90 | 85 | 60 | Every branch (none, many, timeout, failure, cancellation, ordering) is reachable with fakes and `FakeTimeProvider`; the guard tests reach the argument checks; the property test (also S1) covers the ordering and union invariants |
| `RequestValidatorOptionsValidator.cs` (S1) | 95 | 0 | 0 | Pure validation rules; no parameters beyond the options object, so guard tests cannot reach more than the unit tests do |
| `RequestValidatorServiceCollectionExtensions.cs` (S1) | 90 | 90 | 0 | Registration with `TryAdd*` is fully exercised by the S1 registration DI test (`ValidateOnBuild`, `ValidateScopes`); the null checks are guard-tested in S1 |
| `RequestValidationErrorCodes.cs`, `RequestValidatorException.cs` (S1) | 100 | 0 | 0 | Constants and a sealed exception with no argument validation |
| `ValidationAsyncLog.cs` (S2) | 90 | 0 | 0 | Source-generated log methods exercised by the S2 logging test; no parameters to guard |
| `ValidationOrchestrator.cs` (changed method, S3) | 90 | 85 | 0 | The new catch is covered by the three S3 mapping tests; existing guard tests stay |
| `DataAnnotationsValidationProvider.cs`, `MiniValidationProvider.cs` (S4: Unit and Guard) | 90 | 70 | 0 | Each new branch has an S4 unit test; the S4 guard file covers the optional constructor parameters and the `ValidateAsync` argument checks, a realistic 70 because the remaining lines are validation logic reached by the unit tests |
| `DataAnnotationsValidationProvider.cs`, `MiniValidationProvider.cs` (S5: Property, Contract, Integration) | 90 | 70 | 50 | Property 50 is entered by S5, which adds `CrossFieldEquivalenceProperties` (the merge and validity for any input); Contract 40 and Integration 50 are entered by S5 too: the equivalence contract test and the `IEncina.Send` tests execute the provider, but only its main path |
| `FluentValidationProvider.cs` (S5) | 90 | 70 | 50 | Unit 90 from the S5 unit test of the runner call and merge; Guard 70 from the S5 Fluent guard test; Property 50 from the equivalence property; Contract 40 and Integration 50 from the contract and integration tests, which reach the main path only |

Contract and Integration targets are entered per file only for the three provider files (rows above), in S5, with the tests that meet them; the other files of this plan have no Contract or Integration target because no contract or integration test is written for them beyond what runs through the providers.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #230 - provider-neutral async and cross-field validation for the three Encina validation
providers.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5): IValidationProvider, ValidationOrchestrator,
  ValidationPipelineBehavior live in core; FluentValidation, DataAnnotations and MiniValidator stay consistent.
- Railway Oriented Programming: Either<EncinaError, T>; every new error code carries an ErrorCategory (ADR-062).

IMPLEMENTATION OVERVIEW:
1. Core: IRequestValidator<T>, RequestValidatorRunner (bounded parallelism, per-validator timeout from TimeProvider,
   caller cancellation), options with start-up validation, AddEncinaRequestValidators.
2. EventIds 550-552 in a range registered first (ValidationAsync 550-559).
3. ValidationOrchestrator maps RequestValidatorException to a typed Left.
4. DataAnnotations and MiniValidator become scoped and call the runner; FluentValidation calls it too.
5. Equivalence contract test, property test, integration tests through IEncina.Send; docs and changelog.
Six pull requests (S1 to S6), see the Estimated File Count table.

KEY PATTERNS:
- Optional trailing constructor parameters keep the existing tests compiling.
- No scoped service in a singleton; DI test with ValidateOnBuild and ValidateScopes.
- Codes and type names only in logs and errors; ForLogging() for exceptions.

REFERENCE FILES:
- src/Encina/Validation/*.cs
- src/Encina.FluentValidation/FluentValidationProvider.cs
- docs/plans/async-cross-field-validation-implementation-plan-230.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A per-request decision; the validator resolution cache of Fluent is #702 |
| 2 | OpenTelemetry | ⏭️ #726 | #726 instruments `ValidationOrchestrator` centrally (verified open); the runner's call is inside it. Tenant tag per ADR-054 D4 is #726's, because the tag is added where the metric is emitted |
| 3 | Structured Logging | ✅ Phase 2 | `ValidationAsyncLog` EventIds 550-552 in the new range 550-559 |
| 4 | Health Checks | ❌ N/A | Validators are stateless; a validator's own dependency (a database) is checked by that dependency's health check |
| 5 | Validation | ✅ All phases | This is the validation feature; the three providers of `AGENTS.md` section 5 change together and an equivalence contract test pins them |
| 6 | Resilience | ✅ Phase 1 | Timeout and bounded parallelism are in Phase 1 (deferred part: retry and circuit breaking for validators that call external APIs is a follow-up, draft: `artifacts/issues/async-validator-resilience.md`) |
| 7 | Distributed Locks | ❌ N/A | No shared state is written |
| 8 | Transactions | ❌ N/A | Validation runs before the handler; a validator that reads through the request scope sees the same `DbContext` (Design Choice 2) but starts no transaction |
| 9 | Idempotency | ❌ N/A | Validation is read-only and repeatable |
| 10 | Multi-Tenancy | ✅ Phase 1 | `TenantId` reaches every validator through `IRequestContext` (deferred part: tenant-scoped validator selection, a follow-up, draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) |
| 11 | Module Isolation | ⏭️ | The same follow-up issue covers module-scoped validators |
| 12 | Audit Trail | ⏭️ | Auditing of rejected requests is a follow-up (draft: `artifacts/issues/validation-failure-audit.md`); an infrastructure failure of a validator is logged by code and returned as a `Left` |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Validation orchestrator pattern (`IValidationProvider`, `ValidationOrchestrator`) | ✅ On `main` | Integration point exists |
| ADR-062 error category contract (`EncinaErrors.Create` with a category) | ✅ Merged | Codes of register row C8 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Code-less validation `Left` never becomes a 400 | #3415 | This plan keeps the existing invalid-result `Left` as is; #3415 owns it |
| Central validation instrumentation | #726 | Covers the runner's call; this plan adds no instrument |
| Guard and DI proof tests of the providers | #1338, #1339 | Overlap in test files only; this plan adds new files and does not wait |

---

## Next Steps

1. Link this plan from #230 and ask the maintainer to answer Design Choices 1 to 5 and the Dilemma rows C8 and D1 to D9 of the register when work starts.
2. Open the follow-up issues when the first pull request opens (the four drafts exist in the planning worktree's `artifacts/issues/` and are opened by the orchestrator): the shared `ValidationContext` of `FluentValidationProvider`, unless the maintainer picks option A of register row D4 (draft: `artifacts/issues/fluentvalidation-provider-shared-context.md`), resilience of async validators (draft: `artifacts/issues/async-validator-resilience.md`), tenant and module scoped selection (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) and auditing of validation failures (draft: `artifacts/issues/validation-failure-audit.md`).
3. Implement S1 to S6 as the pull requests of the Estimated File Count split, `Fixes #230` on S6.
