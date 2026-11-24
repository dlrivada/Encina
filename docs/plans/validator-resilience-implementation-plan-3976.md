# Implementation Plan: Retry and Circuit Breaking for Async Request Validators

> **Issue**: [#3976](https://github.com/dlrivada/Encina/issues/3976)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, `RequestValidatorRunner`, the per-validator timeout, the codes `validation.async_validator_failed` and `validation.async_validator_timeout` and the range `ValidationAsync` 550-559; a blocking prerequisite of S1), #726 (central validation instrumentation: retry counters stay there), #728 (resilience telemetry), #740 (also adds Polly log messages, 3922-3924), #3982 (tenant- and module-scoped validator selection, edits the same runner), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one seam in core, two adapters, no persistence)
> **Estimated Scope**: ~800 lines of production code + ~1,750 lines of tests + ~450 lines of documentation (~3,000 lines in total, as the Estimated File Count table), in four pull requests of at most 14 files and 1,000 lines each
> **Note on #230**: every statement about `IRequestValidator<T>`, the runner, its options and its codes follows the plan of #230 (a plan pull request, not code on `main`); it is re-verified against `main` when S1 starts

---

## Summary

### What exists today (read on 2026-11-24, main `278c164f1`)

- No async validator contract exists in `src` (`git grep -n -E "IRequestValidator|RequestValidatorRunner" -- src` is empty). #230 plans it: a core runner that runs every `IRequestValidator<TRequest>` in parallel, each under its own timeout, and maps a validator exception to `RequestValidatorException`, which `ValidationOrchestrator` turns into a typed `Left` (`validation.async_validator_failed`, `Internal`; `validation.async_validator_timeout`, `Timeout`). A transient failure of a validator (a connection reset in a customer lookup) therefore rejects the request on the first attempt.
- Two resilience integrations exist in `src`, both on Polly 8 (`Directory.Packages.props`: `Polly` 8.6.6, `Microsoft.Extensions.Resilience` 10.10.0): `Encina.Extensions.Resilience` (`AddEncinaStandardResilience`, `StandardResiliencePipelineBehavior`, a `ResiliencePipelineProvider<string>`, `src/Encina.Extensions.Resilience/ServiceCollectionExtensions.cs:35`) and `Encina.Polly` (`AddEncinaPolly`, `RetryPipelineBehavior`, `CircuitBreakerPipelineBehavior`, which builds a `ResiliencePipeline<Either<EncinaError, TResponse>>` with `TimeProvider`, `CircuitBreakerPipelineBehavior.cs:103-108`). Both wrap the request handler as pipeline behaviors; neither can wrap one validator call, because the validator is called by the runner, not by a behavior.
- Core `Encina` references neither Polly nor `Microsoft.Extensions.Resilience` (`src/Encina/Encina.csproj`), so the runner cannot build a pipeline itself; it needs a seam that an adapter package fills.
- `Encina.Extensions.Resilience` has the EventId range `ExtensionsResilience` 3950-3999 with 3950-3955 in use; `Encina.Polly` has `Polly` 3900-3949 with 3900-3921 in use and 3922-3924 claimed by the plan of #740 (`EventIdRanges.cs`; `EncinaEventIdAllocationTests.cs` already maps both assemblies to their ranges).
- Not in `src`: any retry or circuit breaker around a validator, any option named `Resilience` on the validation options, any error code for an open circuit.

### What this plan delivers

An opt-in resilience policy for `IRequestValidator<T>` validators that call databases or external APIs:

1. **A core seam**: `IRequestValidatorExecutor` with one method that runs one validator attempt delegate under a policy, a pass-through default, and `RequestValidatorOptions.Resilience` (`Retry`, `CircuitBreaker`), so the example of the issue `services.AddEncinaRequestValidators(o => o.Resilience.Retry.MaxAttempts = 2)` works once an adapter is registered. The runner (#230) calls the executor once per validator, so the per-validator timeout of #230 applies to each attempt.
2. **Two adapters, one per resilience integration** (the Resilience category of `AGENTS.md` section 5): `Encina.Extensions.Resilience` (`AddEncinaRequestValidatorResilience()`) and `Encina.Polly` (`AddEncinaPollyRequestValidatorResilience()`), both building a Polly 8 `ResiliencePipeline` per validator type with retry, backoff with jitter on the injected `TimeProvider`, and a circuit breaker.
3. **A start-up check**: configuring retry or a breaker while only the pass-through executor is registered fails `ValidateOnStart`, so a configured policy is never silently ignored.

Failure semantics (Settled and Dilemma rows of the register): a validator exception that the policy classifies as transient is retried up to `MaxAttempts`; a timeout is not retried unless configured; after the last attempt the failure is #230's `Left`, never a pass; an open circuit denies the request with a new `Unavailable` code; the caller's cancellation is never retried and never a `Left`. Nothing changes for an application that sets no `Resilience` option.

- Affected packages: `Encina` (core seam), `Encina.Extensions.Resilience`, `Encina.Polly`. The three validation provider packages need no change: the runner is shared (#230).
- Provider category: Validation (3) (`AGENTS.md` section 5): behavior is in the shared runner, so FluentValidation, DataAnnotations and MiniValidator behave the same; an integration test per provider proves it (S4). Resilience (3): Polly and Extensions.Resilience get an adapter each; `Encina.Extensions.Http.Resilience` has no adapter because a validator is not necessarily an HTTP call: a validator that uses an `HttpClient` configures the resilience of that named client itself (register row C12).
- Shared pieces: consumes everything of #230; owns `IRequestValidatorExecutor`, `ValidatorResilienceOptions` and the code `validation.async_validator_circuit_open`. #3982 (scoped selection) changes the runner at another point; the two plans touch `RequestValidatorRunner.cs`, so their pull requests merge one after the other (no shared type).

---

## Design Choices

<details>
<summary><strong>1. Which resilience engine: Microsoft.Extensions.Resilience, Encina.Polly, or both</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Extensions.Resilience` first (platform standard), `Encina.Polly` as a second adapter on the same seam** | Both integrations of the category stay consistent; each application uses the package it already configures; the platform standard is the documented default | Two adapters to maintain; about 80 lines of pipeline-building code exist twice |
| **B) Only `Encina.Polly` (the repository's existing retry integration)** | One adapter; matches the issue's ticked package | Applications on `Microsoft.Extensions.Resilience` get nothing; the category rule asks for consistency across its providers |
| **C) Only `Encina.Extensions.Resilience`** | One adapter; platform standard; AOT-friendly | Applications configured with `Encina.Polly` must also take the other package |

### Chosen Option: **A — Both adapters, Extensions.Resilience documented as the default** (recommended, pending the maintainer)

### Rationale

- The issue's own Dilemma recommends exactly this; `AGENTS.md` section 5 (Resilience: Polly, Extensions.Resilience, Extensions.Http.Resilience) asks a feature that touches a category to stay consistent across it.
- B wins when the maintainer wants the smallest first version; C wins when `Encina.Polly` is going to be merged into the platform integration before 1.0.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the policy is applied: a seam in the runner, or a decorator around each validator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IRequestValidatorExecutor` called by the runner once per validator; the runner's timeout wraps each attempt** | One attempt is bounded by the #230 timeout, so a hung dependency cannot hold a request for `MaxAttempts` times the timeout unbounded; the runner stays the single owner of ordering, parallelism and failure mapping | A new public interface in core; the runner (#230) is changed |
| **B) A decorator `ResilientRequestValidator<T>` registered around each validator (`Scrutor`-style, at `AddRequestValidator`)** | The runner is untouched; policy per registration is natural | The runner's timeout then covers all attempts together (the retries share one budget, a slow first attempt leaves no time to retry); validators registered by #227's generated extension are not wrapped unless that code also wraps; a validator registered without `AddRequestValidator` escapes the policy |

### Chosen Option: **A — A seam called by the runner** (recommended, pending the maintainer)

### Rationale

- With A every validator that the runner runs goes through the policy, whatever registered it (hand-written, generated by #227, a schema adapter of #235), and the timeout of #230 keeps its meaning (one attempt).
- B wins when the application wants a policy on a single validator and never uses the runner's other options, and the shared timeout budget is acceptable.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which failures are retried by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An allow-list of transient exception types: `HttpRequestException`, `IOException`, `TimeoutException` raised by the validator's dependency, `System.Data.Common.DbException` with `IsTransient`, `SocketException`; extendable with `ShouldRetry` on the options** | A bug in a validator (a `NullReferenceException`, an `ArgumentException`) fails at once instead of being repeated; predictable | A transient type outside the list is not retried until the application adds it |
| **B) Retry every exception except cancellation** | Nothing to configure | A deterministic defect is repeated `MaxAttempts` times, multiplying load on the dependency and latency of every request |

### Chosen Option: **A — Allow-list of transient types, extendable** (recommended, pending the maintainer)

### Rationale

- Retrying is only useful when the next attempt can succeed; a gate repeated on a programming error multiplies latency for no benefit. The runner's own timeout expiry (`RequestValidatorException` with the timeout code) is never part of the list (register row C2).
- B wins when the application's validators wrap every dependency failure in its own exception type and cannot extend the list.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What a circuit breaker is keyed on</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One breaker per validator type, shared by every tenant and request** | The dependency's health is not tenant specific, so every tenant is protected at once; the key set is bounded by the registered validator types | A tenant-specific dependency (a customer's own API) opens the circuit for the other tenants of the same validator type |
| **B) One breaker per (validator type, tenant id), built as a tuple key (validator type, `string?` tenant), not as a concatenated string** | A bad tenant endpoint isolates that tenant; the tuple is injective (no two pairs map to one key) and a missing tenant is its own key, never the reserved `default` segment | Unbounded key growth with tenant count: needs an eviction bound and a per-tenant state to test; more state to keep correct |

### Chosen Option: **A — Per validator type** (recommended, pending the maintainer)

### Rationale

- The breaker protects a dependency; the validators of the issue (a customer lookup, a database) are shared infrastructure. B wins when a validator calls an endpoint chosen by the tenant, which #3982 makes possible through tenant-scoped validators; if B is chosen, the key is the tuple above, bounded by an LRU of a configured size, and the reserved `default` tenant segment is the segment of a null tenant only in logs, never in the key.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Global policy or a per-validator override</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One global `Resilience` policy for all validators, plus an optional per-validator override on `AddRequestValidator<TRequest, TValidator>(..., Action<ValidatorResilienceOptions>? configure)`** | Simple default; the override covers a validator with a slower dependency; the issue says "per-validator or per-request-type" | A second configuration path to document and test |
| **B) Global only** | Smallest surface | One slow validator forces the policy of every validator |

### Chosen Option: **A — Global default with a per-validator override** (recommended, pending the maintainer)

### Rationale

- The override is an optional parameter of the registration #230 already defines, so no new method name appears.
- B wins when a first version must ship with the smallest public surface.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Package placement of the seam and options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Seam and options in core `Encina.Validation`, next to the runner; adapters in the two resilience packages** | Core stays free of Polly; the runner needs only the interface; the options are plain data | Core gains three files |
| **B) A new package `Encina.Validation.Resilience`** | Core untouched | The runner must still call something, so the seam stays in core; a fourth package for two small adapters |

### Chosen Option: **A — Core seam, adapters in the existing packages**

### Rationale

- The runner is in core (#230), and core cannot reference Polly; the adapters belong to the packages that already own the dependency.

**Class:** Trivial
**Settled by:** the existing layout (`src/Encina/Encina.csproj` has no Polly reference; `src/Encina.Extensions.Resilience` and `src/Encina.Polly` reference core and Polly) and the issue's "Affected Packages" list (`Encina (core)`, `Encina.Polly`)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | After the last attempt the failure is #230's `Left` (`validation.async_validator_failed`, `Internal`), never a pass. The contract of the attempt delegate and of the executor: the delegate (the runner's timed call of one validator) throws the validator's raw exception, or `RequestValidatorException` with the timeout code when the runner's per-attempt timeout fires; the executor classifies the raw exception, retries or not, and after the last attempt rethrows it unchanged (an open circuit is the one exception the executor creates: `RequestValidatorException` with the circuit-open code); the runner then wraps any raw exception that leaves the executor in `RequestValidatorException` (`validation.async_validator_failed`, inner exception kept), exactly where #230 wraps it today, so the wrapping stays in one place and `ValidationOrchestrator` maps it unchanged | Settled | `AGENTS.md` section 3 (compliance and security gates fail closed; errors are never swallowed): every path of the contract ends in a `Left` or in the caller's cancellation, under every option of Design Choices 1 to 5 (the decorator of option B of Design Choice 2 wraps the same delegate) |
| C2 | A timeout expiry (the runner's per-validator timeout, code `validation.async_validator_timeout`) is not retried unless `Retry.RetryOnTimeout = true`. Option B: retry timeouts by default | Dilemma (recommended, pending the maintainer): not retried | not retried bounds the latency a request can cost (a validator that already ran for the full timeout would multiply it); retried wins when a dependency has cold-start latency spikes. The issue lists this as Settled citing a denial-of-service argument from `AGENTS.md` section 3 that the section does not state, and a hung dependency is exactly the scenario where option B wins, so the plan records it as a Dilemma |
| C15 | What the circuit breaker counts as a failure: every classified transient exception and every runner timeout (a hung dependency is the main reason for a breaker); never a returned invalid result, never a caller cancellation, never an open-circuit rejection. Option B: the breaker counts exactly what retry handles (so a timeout counts only with `RetryOnTimeout = true`) | Dilemma (recommended, pending the maintainer): timeouts count | with option B a dependency that hangs never opens the circuit while retry is off for timeouts; B wins when a timeout is judged a slow answer rather than a failing dependency. Tested in S2 and S3 (a run of timeouts opens the circuit) |
| C3 | A caller cancellation is never retried, never counted as a breaker failure and never a `Left` of this feature: it propagates as `OperationCanceledException` to the orchestrator, which already handles it | Settled | ADR-062 rule R5; verified in #230 register row C2 |
| C4 | A returned invalid `ValidationResult` is a verdict, not a failure: it is never retried and never opens the circuit | Settled | `IRequestValidator<T>` returns `ValueTask<ValidationResult>` (#230 Phase 1); retrying a deterministic verdict cannot change it |
| C5 | An open circuit denies the request: a new code `validation.async_validator_circuit_open`, category `ErrorCategory.Unavailable` (HTTP 503, `docs/architecture/adr/062-error-category-contract.md`, row `Unavailable`), created with `EncinaErrors.Create(code, category, message)` and a constant message (the code and the request type name only), carried by `RequestValidatorException` like the two codes of #230. Option B: reuse `validation.async_validator_failed` (`Internal`) for an open circuit | Dilemma (recommended, pending the maintainer): new code | a dependency that is down is `Unavailable` (503, retryable by the client) not `Internal` (500); reuse wins when the application maps by code and wants no new public code |
| C6 | Retry defaults: `MaxAttempts = 1` (no retry; the issue's example sets 2), exponential backoff `BaseDelay = 200 ms`, `MaxDelay = 2 s`, jitter on; breaker defaults (off unless `CircuitBreaker.Enabled = true`): `FailureRatio = 0.5`, `MinimumThroughput = 10`, `SamplingDuration = 30 s`, `BreakDuration = 15 s`. Options validate at start-up (attempts 1 to 10, delays positive and `BaseDelay <= MaxDelay`, ratio in (0, 1], throughput at least 2) | Dilemma (recommended, pending the maintainer) | numeric defaults |
| C7 | Backoff, the breaker's sampling and break windows use the injected `TimeProvider` (`ResiliencePipelineBuilder.TimeProvider`, as `CircuitBreakerPipelineBehavior.cs:108`), never the system clock | Settled | `AGENTS.md` section 3 (time comes from `TimeProvider`) |
| C8 | Retried validators must be idempotent: validators are read-only by contract (the issue: cross-cutting row 9), and the documentation says a validator with a side effect must not be configured for retry | Trivial | the issue's Cross-Cutting table (row 9, "Validators are read-only"); documented in S4 |
| C9 | Breaker state never lives in a scoped object: under option A of Design Choice 2 it is a singleton executor (a `ConcurrentDictionary` of pipelines per validator type, as `CircuitBreakerPipelineBehavior.cs:85` caches) registered with `TryAddSingleton` and resolved by the scoped runner; under option B it is a singleton pipeline registry used by the decorators | Settled | `AGENTS.md` section 3 (registration completeness: never inject a scoped service into a singleton; breaker state must outlive a request scope to count failures across requests); a singleton may be injected into a scoped service |
| C10 | Logs and errors carry the code, the validator type name, the attempt number and the exception type through `ForLogging()` (type and stack trace, never the message); the `Left` carries the code and request type only; no tenant id is logged and no request value | Settled | `AGENTS.md` section 3 (`EncinaError.Message` and exception messages never reach logs; #1168, #1173, #1259, #1274) |
| C11 | EventIds of whichever adapter is built (Design Choice 1): `Encina.Extensions.Resilience` 3956-3958 (retry attempt, circuit opened, request rejected by an open circuit) inside its range `ExtensionsResilience` 3950-3999; `Encina.Polly` 3925-3927 inside `Polly` 3900-3949 (3922-3924 are claimed by #740). Packed, no new range | Trivial | `AGENTS.md` section 7 (ids inside a package's existing range; the registry `docs/engineering/eventid-block-registry.md` lists both ranges as historical, moving in #4007 OP-2) |
| C12 | `Encina.Extensions.Http.Resilience` gets no adapter: it configures `HttpClient` handlers, and a validator that calls HTTP gets resilience from the named client it injects | Trivial | `src/Encina.Extensions.Resilience/Encina.Extensions.Resilience.csproj` references `Microsoft.Extensions.Http.Resilience` for client pipelines, the layer that owns HTTP calls |
| C13 | Telemetry: no instrument is added; retry and breaker counters follow #726, which instruments the validation pipeline centrally. The ADR-054 D4 tenant tag is then #726's, because the tag is added where a metric is emitted; this plan adds log messages only | Trivial | the issue's Cross-Cutting row 2 ("Retry counters follow #726"); `AGENTS.md` section 6 allows "Defer (open an issue and reference it)": #726 is open |
| C14 | The start-up check: `RequestValidatorOptionsValidator` (#230) fails when `Resilience.Retry.MaxAttempts > 1` or `CircuitBreaker.Enabled` is set and the registered executor is the public pass-through `DirectRequestValidatorExecutor` (public so the adapters can recognize and replace exactly it). It reads the executor through an optional constructor parameter, so the #230 tests that build the validator with `new` compile unchanged | Settled | `AGENTS.md` section 3 (registration completeness: an option the registration cannot honor must fail at start-up, proven by a `ValidateOnBuild` test) |

---

## Implementation Phases

### Phase 1: Core Seam, Options and Runner Integration

> **Goal**: The seam, the options, the override marker and the runner call, tested with a fake executor; no adapter yet (pull request S1).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/IRequestValidatorExecutor.cs`: `public interface IRequestValidatorExecutor { ValueTask<ValidationResult> ExecuteAsync(Type validatorType, ValidatorResilienceOptions options, Func<CancellationToken, ValueTask<ValidationResult>> attempt, CancellationToken cancellationToken); }`, XML docs naming the contract of register row C1: the executor calls `attempt` one or more times with the effective options the runner resolved for that validator type, rethrows the last raw exception unchanged, creates only the circuit-open `RequestValidatorException`, and rethrows a cancellation unchanged.
2. **Create** `src/Encina/Validation/DirectRequestValidatorExecutor.cs` (`public sealed`, singleton; calls `attempt` once). It is the default and is public so the adapter packages can recognize it by type and replace exactly that descriptor (register row C14); listed in `PublicAPI.Unshipped.txt`.
3. **Create** `src/Encina/Validation/ValidatorResilienceOptions.cs`: `ValidatorResilienceOptions` with `Retry` (`MaxAttempts`, `BaseDelay`, `MaxDelay`, `UseJitter`, `RetryOnTimeout`, `ShouldRetry` of type `Func<Exception, bool>?`) and `CircuitBreaker` (`Enabled`, `FailureRatio`, `MinimumThroughput`, `SamplingDuration`, `BreakDuration`) as two nested public sealed classes with the defaults of register row C6, and `bool IsEnabled`.
4. **Modify** `RequestValidatorOptions.cs` (#230): `public ValidatorResilienceOptions Resilience { get; } = new();`. **Modify** `RequestValidatorOptionsValidator.cs`: validate the ranges of C6 and the start-up check of C14 (optional constructor parameter `IRequestValidatorExecutor? executor = null`).
5. **Create** `src/Encina/Validation/ValidatorResilienceOverrideMarker.cs` (`internal sealed record ValidatorResilienceOverrideMarker(Type ValidatorType)`): the per-validator override of Design Choice 5 option A is stored as a named options entry (`services.Configure<ValidatorResilienceOptions>(validatorType.FullName, configure)`) plus one marker singleton, so the runner can tell an overridden validator from one that uses the global policy (a named options entry that was never configured would otherwise read as defaults). The circuit-open code and the orchestrator mapping are not in S1: they ship in S2, the first pull request that throws it (so no unused code lands here).
6. **Modify** `RequestValidatorRunner.cs`: optional trailing constructor parameters `IRequestValidatorExecutor? executor = null` (the #230 tests that construct it with `new` compile unchanged; `null` means the pass-through); per validator the runner resolves the effective options (the named entry when a marker exists for the validator type, otherwise `RequestValidatorOptions.Resilience`) and calls `executor.ExecuteAsync(validator.GetType(), effectiveOptions, ct => RunOneWithTimeoutAsync(validator, request, context, ct), cancellationToken)` inside the try block that wraps a raw exception in `RequestValidatorException` (register row C1), so the #230 timeout applies per attempt.
7. **Modify** `RequestValidatorServiceCollectionExtensions.cs` (#230): `TryAddSingleton<IRequestValidatorExecutor, DirectRequestValidatorExecutor>()` inside `AddEncinaRequestValidators`; add the optional `Action<ValidatorResilienceOptions>? configure` parameter to `AddRequestValidator<TRequest, TValidator>` (Design Choice 5) that registers the named entry and the marker.
8. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).
9. Tests (S1): runner unit tests with a recording fake executor (called once per validator, the attempt delegate is the timed call, a thrown `RequestValidatorException` passes through, cancellation passes through); a case that a validator with an override receives its own options and one without receives the global ones; options validator unit tests (each range, the C14 failure and its pass case with a fake non-pass-through executor); a guard test file for the extension's optional delegate (a null `configure` is the valid case), the runner's optional executor and the options setters; a DI test with `ValidateOnBuild` and `ValidateScopes` that resolves the runner, the executor and the options and proves the C14 failure.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #3976 adds retry and circuit breaking for
IRequestValidator<T> validators. Read docs/plans/validator-resilience-implementation-plan-3976.md (Design Choices
1-6, register rows C1-C14) and, for the runner it extends, docs/plans/async-cross-field-validation-implementation-plan-230.md.
#230 is implemented first; verify its types on main before you start.

TASK:
1. Create IRequestValidatorExecutor, DirectRequestValidatorExecutor (internal, calls the attempt once) and
   ValidatorResilienceOptions (Retry, CircuitBreaker) under src/Encina/Validation/.
2. Add RequestValidatorOptions.Resilience, the range and start-up checks in RequestValidatorOptionsValidator, the code
   the override marker and the runner call through the executor (the circuit-open code ships in S2).
3. Register the pass-through executor with TryAddSingleton and add the optional per-validator configure delegate.

KEY RULES:
- Optional trailing constructor parameters keep existing tests compiling; never inject a scoped service into a singleton.
- Errors and logs carry codes and type names only; caller cancellation is never retried and never a Left.
- XML docs on every public member; PublicAPI.Unshipped.txt and the manifest through mechanical-fixer; no [Obsolete].

REFERENCE FILES:
- src/Encina/Validation/ (the files of #230), src/Encina/Errors/ErrorCategory.cs
- docs/architecture/adr/062-error-category-contract.md
```

</details>

---

### Phase 2: Extensions.Resilience Adapter

> **Goal**: The platform-standard adapter with retry and breaker, logging by code (pull request S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Extensions.Resilience/Validation/ResilienceRequestValidatorExecutor.cs` (`public sealed partial class : IRequestValidatorExecutor`; constructor `(ILogger<ResilienceRequestValidatorExecutor> logger, TimeProvider? timeProvider = null)`; the options arrive per call, register row C1): per validator type it builds and caches (`ConcurrentDictionary<Type, ResiliencePipeline>`) a pipeline from the options of the first call for that type: retry (exponential backoff with jitter, `ShouldHandle` from register rows C2 and Design Choice 3, never `OperationCanceledException`), then the circuit breaker when enabled, counting failures as register row C15 says; an open circuit throws `RequestValidatorException` with `AsyncValidatorCircuitOpen`; an exception after the last attempt is rethrown unchanged (the runner wraps it). Private `[LoggerMessage]` methods EventIds 3956 (retry attempt, warning: validator type, attempt), 3957 (circuit opened, warning: validator type, break duration), 3958 (rejected by an open circuit, warning: validator type), each with the range named in its XML docs.
2. **Create** `src/Encina.Extensions.Resilience/Validation/RequestValidatorResilienceServiceCollectionExtensions.cs`: `AddEncinaRequestValidatorResilience(this IServiceCollection, Action<ValidatorResilienceOptions>? configure = null)` registers the executor as a singleton: it removes only the descriptors whose `ImplementationType` is `DirectRequestValidatorExecutor` (never every descriptor of the service type, so an application's own executor is never removed) and adds itself unless a descriptor of another implementation already exists; both registration orders (before and after `AddEncinaRequestValidators`) end with the adapter active, and an application executor registered first or last stays.
3. **Modify** `RequestValidationErrorCodes.cs` (#230): add `AsyncValidatorCircuitOpen = "validation.async_validator_circuit_open"` created with `ErrorCategory.Unavailable` (register row C5); **modify** `ValidationOrchestrator.cs` (#230 S3): the `RequestValidatorException` catch maps the new code with its category (the mapping is a lookup by code in #230's plan; if it carries the category on the exception, only the new constant changes); **modify** `src/Encina/PublicAPI.Unshipped.txt`, `src/Encina.Extensions.Resilience/PublicAPI.Unshipped.txt` and the manifests `.github/coverage-manifest/Encina.json` and `Encina.Extensions.Resilience.json` (through `mechanical-fixer`).
4. Tests (S2): `ResilienceRequestValidatorExecutorTests` (retries a transient exception up to the attempts with a `FakeTimeProvider` and no sleeping, does not retry a deterministic exception, does not retry a timeout code unless configured, a verdict is never retried, the circuit opens after the configured ratio and rejects, closes after the break duration, cancellation passes through unretried and uncounted, log EventIds 3956 to 3958 carry no request value); `ResilienceRequestValidatorExecutorProperties` (FsCheck: attempts never exceed `MaxAttempts` for any failure sequence); a guard test file (null arguments of the constructor and of `ExecuteAsync`, the null `configure` and null `TimeProvider` valid cases); a DI test (`ValidateOnBuild`, `ValidateScopes`, resolves runner and executor, the C14 check passes once the adapter is registered, both registration orders, and an application executor registered first or last is not removed); cases for the per-validator override (two validator types with different options get different pipelines) and for register row C15 (a run of timeouts opens the circuit); an edit of `ValidationOrchestratorAsyncFailureTests` (#230 S3) for the circuit-open code (`Unavailable`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #3976, plan docs/plans/validator-resilience-implementation-plan-3976.md, phase 2 of 4. Depends on S1.

TASK:
1. Create ResilienceRequestValidatorExecutor in src/Encina.Extensions.Resilience/Validation/: a cached
   ResiliencePipeline per validator type (retry with backoff and jitter, optional circuit breaker, TimeProvider),
   mapping an open circuit to RequestValidatorException(validation.async_validator_circuit_open).
2. Add AddEncinaRequestValidatorResilience replacing only the core pass-through executor.
3. Log with private [LoggerMessage] methods 3956-3958 (inside ExtensionsResilience 3950-3999), by validator type and attempt.

KEY RULES:
- Time from TimeProvider; never retry cancellation or a returned invalid result; exceptions through ForLogging().
- The executor is a singleton; the runner is scoped. Tests use FakeTimeProvider and no sleeps.

REFERENCE FILES:
- src/Encina.Extensions.Resilience/Behaviors/StandardResiliencePipelineBehavior.cs, ServiceCollectionExtensions.cs
- src/Encina.Polly/Behaviors/CircuitBreakerPipelineBehavior.cs (:77-110, the caching pattern)
```

</details>

---

### Phase 3: Polly Adapter and Equivalence

> **Goal**: The second adapter on the same seam and a contract test that both behave alike (pull request S3).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Polly/Validation/PollyRequestValidatorExecutor.cs` (same contract and constructor shape as the S2 executor; EventIds 3925-3927, `LoggerMessage` methods inside `Polly` 3900-3949) and `src/Encina.Polly/Validation/PollyRequestValidatorServiceCollectionExtensions.cs` (`AddEncinaPollyRequestValidatorResilience(this IServiceCollection, Action<ValidatorResilienceOptions>? configure = null)`).
2. **Modify** `src/Encina.Polly/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Polly.json` and, for the Contract targets of the three executors that this pull request's contract test reaches (`DirectRequestValidatorExecutor`, `ResilienceRequestValidatorExecutor`, `PollyRequestValidatorExecutor`), `.github/coverage-manifest/Encina.json` and `Encina.Extensions.Resilience.json` (through `mechanical-fixer`); **modify** `tests/Encina.ContractTests/Encina.ContractTests.csproj` (ProjectReferences to `Encina.Polly` and `Encina.Extensions.Resilience`, which it does not reference today).
3. **Create** `tests/Encina.ContractTests/Core/Validation/RequestValidatorExecutorContractTests.cs`: one scenario set (transient failure retried, deterministic failure not, open circuit code, cancellation passes through, attempts bounded) run against both real executors with the same options and a `FakeTimeProvider`; the pass-through executor is the third case.
4. Tests (S3): `PollyRequestValidatorExecutorTests`, a guard test file and a DI test as in S2.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #3976, plan docs/plans/validator-resilience-implementation-plan-3976.md, phase 3 of 4. Depends on S1.

TASK:
1. Create PollyRequestValidatorExecutor and AddEncinaPollyRequestValidatorResilience in src/Encina.Polly/Validation/,
   mirroring the S2 executor; log EventIds 3925-3927 (3922-3924 belong to #740).
2. Write the contract test that runs the same scenarios against both executors.

KEY RULES:
- Same behavior as the Extensions.Resilience executor: codes, attempts, cancellation, no retry of a verdict.
- Real executors in the contract test; no reflection-only tests; Shouldly through Encina.Testing.Shouldly.

REFERENCE FILES:
- src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs, CircuitBreakerPipelineBehavior.cs
- src/Encina.Extensions.Resilience/Validation/ (S2)
```

</details>

---

### Phase 4: Integration Through the Pipeline, Documentation and Finalization

> **Goal**: The three providers are proven end to end and the feature is documented (pull request S4).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/ResilientAsyncValidationPipelineTests.cs`: a request through `IEncina.Send` with the real pipeline behavior, a scoped fake dependency that fails twice and then succeeds, `AddEncinaRequestValidators(o => o.Resilience.Retry.MaxAttempts = 3)` plus the Extensions.Resilience adapter; asserts success after the retries, the typed `Left` when the failures exceed the attempts, and the 503-category `Left` once the circuit is open. Not a database feature: no container.
2. **Modify** `docs/features/async-and-cross-field-validation.md` (owned by #230 S6; a section "Resilient validators", through `docs-writer`), `src/Encina.Extensions.Resilience/README.md` and `src/Encina.Polly/README.md` (through `docs-writer`): the options, the two registration methods, the codes, the validators-must-be-read-only rule (register row C8).
3. **Create** `changelog.d/3976-validator-resilience.added.md` (through `mechanical-fixer`).
4. **Create** the justification files `tests/Encina.LoadTests/Validation/ValidatorResilience.md` and `tests/Encina.BenchmarkTests/Validation/ValidatorResilience.md` (`AGENTS.md` section 9 format): the executor adds one delegate call and a dictionary lookup per validator and is bounded by the runner's options; the existing `ValidationBenchmarks.cs` stays the provider benchmark.
5. Build verification: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the touched projects; per-flag coverage of the touched files against the targets of the Estimated File Count section.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #3976, plan docs/plans/validator-resilience-implementation-plan-3976.md, phase 4 of 4. Depends on S1 and S2.

TASK:
1. Write the three integration tests (one per validation provider) that send a request through IEncina.Send with a
   flaky scoped fake dependency and the resilience policy configured.
2. Document the feature (docs-writer), add the changelog fragment (mechanical-fixer) and the two justification files.

KEY RULES:
- Never type coverage figures by hand: cite cov: markers (SPEC-001). Link ADRs and sibling plans by number only.
- The integration tests use no container; tests execute real provider and executor code.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
- tests/Encina.IntegrationTests/Validation/ (the #230 S5 tests)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-062 | Error category contract; the `Unavailable` and `Timeout` rows; R4 and R5 | Codes and categories of register rows C1 to C5 |
| ADR-054 D3, D4 | One meter and source per package; tenant tag only when multi-tenancy is on | Why this plan adds log messages only (register row C13) |
| Polly 8 documentation: retry and circuit breaker strategies | `RetryStrategyOptions` (backoff, jitter), `CircuitBreakerStrategyOptions` (failure ratio, minimum throughput, sampling and break durations) | Defaults and pipeline shape (register row C6) |
| Microsoft "Build resilient HTTP apps" and `Microsoft.Extensions.Resilience` | `AddResiliencePipeline`, `ResiliencePipelineProvider`, `TimeProvider` support | The platform-standard adapter |
| `AGENTS.md` sections 3 and 5 | Fail-closed gates, time from `TimeProvider`, Resilience category | Register rows C1, C7 and Design Choice 1 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestValidator<T>`, `RequestValidatorRunner`, `RequestValidatorOptions`, `RequestValidatorException`, `RequestValidationErrorCodes` (#230) | `src/Encina/Validation/` (after #230) | Extended; the runner calls the new seam |
| `CircuitBreakerPipelineBehavior` (pipeline cache, `TimeProvider`) | `src/Encina.Polly/Behaviors/CircuitBreakerPipelineBehavior.cs:77-110` | Pattern for the cached per-type pipeline |
| `StandardResiliencePipelineBehavior`, `AddEncinaStandardResilience` | `src/Encina.Extensions.Resilience/` | Pattern for the platform adapter and its registration |
| `ErrorCategory.Unavailable` and `EncinaErrors.Create(code, category, message)` | `src/Encina/Errors/ErrorCategory.cs`, `src/Encina/Results/EncinaErrors.cs` | The open-circuit code |
| `FakeTimeProvider` | `Microsoft.Extensions.TimeProvider.Testing` (central package version) | Backoff and break windows without sleeping |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Both assemblies already map to their ranges (no edit) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Extensions.Resilience` (`ExtensionsResilience`, existing) | 3956-3958 | 3950-3955 in use; 3956-3958 used by this plan; 3959-3999 free |
| `Encina.Polly` (`Polly`, existing) | 3925-3927 | 3900-3921 in use; 3922-3924 claimed by #740; 3925-3927 used by this plan; 3928-3949 free |
| `Encina` core | none | The seam logs nothing; no new range |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, READMEs and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests. The numbers of "edited tests" are the #230 test files S1 extends, re-counted with `git grep -c "new RequestValidatorRunner(" -- tests` when S1 starts (the new constructor parameter is optional, so no call site changes).

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Seam, options, override marker, runner call | 4 (`IRequestValidatorExecutor`, `DirectRequestValidatorExecutor`, `ValidatorResilienceOptions`, `ValidatorResilienceOverrideMarker`) | 5 (`RequestValidatorOptions`, `RequestValidatorOptionsValidator`, `RequestValidatorRunner`, `RequestValidatorServiceCollectionExtensions`, PublicAPI) | 4 (runner-executor unit, options unit, guard, DI) | 0 | 1 (`Encina.json`) | 0 | 14 | ~900 |
| S2 | Extensions.Resilience adapter, circuit-open code | 2 (executor, extensions) | 4 (`RequestValidationErrorCodes`, `ValidationOrchestrator`, core PublicAPI, package PublicAPI) | 4 (unit, property, guard, DI) | 1 (`ValidationOrchestratorAsyncFailureTests`) | 2 (`Encina.json`, `Encina.Extensions.Resilience.json`) | 0 | 13 | ~900 |
| S3 | Polly adapter, equivalence | 2 (executor, extensions) | 1 (PublicAPI) | 4 (unit, guard, DI, contract) | 1 (`Encina.ContractTests.csproj`) | 3 (`Encina.Polly.json`, `Encina.json`, `Encina.Extensions.Resilience.json`: the Contract targets) | 0 | 11 | ~850 |
| S4 | Integration, docs | 0 | 0 | 3 (one per provider) | 0 | 0 | 6 (feature page, 2 READMEs, fragment, 2 justification `.md`) | 9 | ~450 |
| **Total** | | **8** | **10** | **15** | **2** | **6** | **6** | **47** | **~3,100** |

The columns sum to the total (8 + 10 + 15 + 2 + 6 + 6 = 47) and the rows sum to 47 files (14 + 13 + 11 + 9) and about 3,100 lines (900 + 900 + 850 + 450), within the header's estimate. The largest pull request is S1 at 14 files; none exceeds 1,500 lines. S1 comes first; S2 and S3 need only S1 (S3 does not use the circuit-open code from S2 beyond the contract test, so S3 needs S2 for that code; the order is S1, S2, S3); S4 needs S1 and S2.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file:

| File (entered by) | Unit | Guard | Property | Contract | Justification |
|------|------|-------|----------|----------|---------------|
| `ValidatorResilienceOptions.cs` (S1) | 95 | 70 | 0 | 0 | Property setters and defaults, reached by the options unit tests; the guard tests reach the argument checks of the setters; a property test adds nothing over the range cases |
| `IRequestValidatorExecutor.cs` (S1) | 0 | 0 | 0 | 0 | An interface with no executable code; its behavior is covered through the implementations |
| `DirectRequestValidatorExecutor.cs` (S1; its Contract 40 is entered by S3) | 90 | 0 | 0 | 0 | One call to the delegate, covered by the runner unit tests; guard 0 because `ExecuteAsync` only forwards its arguments to the delegate and the runner guards them; the Contract 40 is written by S3, whose contract test runs it as the third case |
| `RequestValidatorRunner.cs`, `RequestValidatorOptionsValidator.cs`, `RequestValidatorServiceCollectionExtensions.cs` (S1, changed methods) | 90 | 85 | 0 | 0 | The new branches are covered by the S1 runner, options and DI tests; the guard tests reach the optional-parameter cases |
| `ResilienceRequestValidatorExecutor.cs` (S2; its Contract 40 is entered by S3) | 90 | 85 | 60 | 0 | Every retry, breaker and cancellation branch is reachable with fakes and `FakeTimeProvider`; the S2 property bounds attempts for any failure sequence; the Contract 40 is written by S3, whose contract test runs the main path |
| `RequestValidatorResilienceServiceCollectionExtensions.cs` (S2) | 90 | 90 | 0 | 0 | Replacing only the pass-through descriptor, in both registration orders and with an application executor present, is exercised by the S2 DI test; the null checks are guard-tested |
| `PollyRequestValidatorExecutor.cs` (S3) | 90 | 85 | 0 | 40 | The same branches as the S2 executor with the same fakes; no property test of its own because the S2 property covers the shared pipeline shape; the contract test is in S3 |
| `PollyRequestValidatorServiceCollectionExtensions.cs` (S3) | 90 | 90 | 0 | 0 | As the S2 extensions |

Integration targets are not entered per file: the S4 tests run through `IEncina.Send` and reach the main path of the runner and the Extensions.Resilience executor, whose manifest entries keep the integration flag at the package default.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #3976 - retry and circuit breaking for async request validators that call external systems.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5); the runner of #230 is shared by the three
  providers. Railway Oriented Programming: Either<EncinaError, T>; every new error code carries an ErrorCategory (ADR-062).
- Resilience adapters live in Encina.Extensions.Resilience and Encina.Polly; core references neither.

IMPLEMENTATION OVERVIEW:
1. Core: IRequestValidatorExecutor (pass-through default), ValidatorResilienceOptions, the override marker, the runner
   call, a start-up check (S1).
2. Extensions.Resilience adapter with retry, backoff and breaker per validator type, the code
   validation.async_validator_circuit_open (Unavailable) and its orchestrator mapping, EventIds 3956-3958 (S2).
3. Polly adapter, EventIds 3925-3927, and a contract test over both executors (S3).
4. Integration tests per provider through IEncina.Send, docs, changelog, justification files (S4).
Four pull requests (S1 to S4), see the Estimated File Count table.

KEY PATTERNS:
- Optional trailing constructor parameters keep existing tests compiling.
- Never inject a scoped service into a singleton; the executor is a singleton, the runner scoped.
- Time from TimeProvider; codes and type names only in logs; ForLogging() for exceptions.
- Cancellation is never retried and never a Left; a returned invalid result is never retried.

REFERENCE FILES:
- docs/plans/validator-resilience-implementation-plan-3976.md, docs/plans/async-cross-field-validation-implementation-plan-230.md
- src/Encina.Polly/Behaviors/CircuitBreakerPipelineBehavior.cs, src/Encina.Extensions.Resilience/
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A retry decision is per call; the breaker pipelines are cached per validator type (register row C9), which is not `ICacheProvider` data |
| 2 | OpenTelemetry | ⏭️ #726 | Retry and breaker counters follow #726 (open), which instruments the validation pipeline centrally; the ADR-054 D4 tenant tag is added there (register row C13) |
| 3 | Structured Logging | ✅ Phases 2, 3 | `[LoggerMessage]` EventIds 3956-3958 and 3925-3927 inside the existing package ranges; validator type, attempt and exception type only |
| 4 | Health Checks | ❌ N/A | The dependency a validator calls owns its health check; the circuit state is not a health result of this feature |
| 5 | Validation | ✅ All phases | This is validation; the three providers behave the same through the shared runner (S4 tests) |
| 6 | Resilience | ✅ All phases | The purpose of the issue: retry, backoff, circuit breaker; timeout is #230's |
| 7 | Distributed Locks | ❌ N/A | Breaker state is per process on purpose; no shared state is written |
| 8 | Transactions | ❌ N/A | Validation runs before the handler and starts no transaction |
| 9 | Idempotency | ❌ N/A | Validators are read-only and repeatable (register row C8); a retried validator is documented as such |
| 10 | Multi-Tenancy | ❌ N/A | The default policy is not tenant specific (Design Choice 4, option A); option B keys a breaker on a tuple of validator type and tenant, with an injective key. Tenant-scoped validator selection is #3982 |
| 11 | Module Isolation | ❌ N/A | Not module specific |
| 12 | Audit Trail | ❌ N/A | Retries are operational, not auditable events; a denied request is a `Left` the caller audits |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `IRequestValidator<T>`, `RequestValidatorRunner`, options, codes (#230) | Open (plan only) | Blocking for S1; its S1 and S3 must be merged first |
| ADR-062 error category contract (`EncinaErrors.Create` with a category) | ✅ Merged | Code of register row C5 |
| `Encina.Extensions.Resilience` and `Encina.Polly` packages | ✅ On `main` | Adapters live there |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Central validation instrumentation | #726 (open) | Retry and breaker metrics land there |
| Tenant- and module-scoped validator selection | #3982 (open) | Edits `RequestValidatorRunner.cs` too; merge order, no shared type |
| Polly log messages 3922-3924 | #740 (open) | This plan starts at 3925 |

---

## Next Steps

1. Link this plan from #3976 and ask the maintainer to answer Design Choices 1 to 5 and the Dilemma rows C2, C5 and C6 of the register when work starts.
2. Implement S1 to S4 as the pull requests of the Estimated File Count split once #230 S1 to S3 are merged, `Fixes #3976` on S4.
3. Re-verify against `main` when S1 starts: the runner's constructor, the options validator and `RequestValidationErrorCodes.cs` from #230, and the free EventIds of both resilience ranges.
