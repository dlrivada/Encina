# Implementation Plan: OpenTelemetry for Validation (One Instrumented Orchestrator for All Three Providers, Bounded Per-Rule Error Counts)

> **Issue**: [#726](https://github.com/dlrivada/Encina/issues/726)
> **Type**: Feature (observability, validation)
> **Related**: ADR-054 (D3 names and constants, D4 tenant tag, settled points 2 and 7), #2676 (tenant tag helper), #906 (observability guide), #1787 (every source and meter subscribed), #721, #723, #724, #725 (sibling instrumentation plans), ADR-062
> **Complexity**: Medium (one orchestrator instrumented in core; the care is in cardinality and in what a validation error may reveal)
> **Estimated Scope**: ~200 lines of production code + ~650 lines of tests, in three pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-23, main `38204119d`)

- `ValidationOrchestrator` (`src/Encina/Validation/ValidationOrchestrator.cs`, public sealed, 83 lines) is the single place all three providers go through: `ValidateAsync<TRequest, TResponse>(request, context, cancellationToken)` calls `IValidationProvider.ValidateAsync`, returns `Left(EncinaErrors.Create(EncinaErrorCodes.ValidationFailed, ErrorCategory.Validation, message))` for an invalid result and `Right(Unit)` otherwise, and turns a cancellation into a `Left`. Its constructor has one parameter, `IValidationProvider`. `ValidationPipelineBehavior<,>` calls it. No `ActivitySource`, `Meter` or log exists in `src/Encina/Validation/`.
- The three providers are `FluentValidationProvider` (`Encina.FluentValidation`, resolves `IValidator<TRequest>` from DI and runs them in parallel), `DataAnnotationsValidationProvider` (`Encina.DataAnnotations`, `Validator.TryValidateObject`) and `MiniValidationProvider` (`Encina.MiniValidator`). Each registers itself and `ValidationOrchestrator` with `TryAddSingleton` in its `ServiceCollectionExtensions.cs` (`AddEncinaFluentValidation`, `AddDataAnnotationsValidation`, `AddMiniValidation`). `IValidationProvider` has no name, so the orchestrator cannot ask a provider which one it is.
- A validation failure carries `ValidationError(string? PropertyName, string ErrorMessage)` (`src/Encina/Validation/ValidationResult.cs`). `ErrorMessage` is application or user-supplied text that can echo the submitted value, and `ToErrorMessage` joins all of them into the `EncinaError` message, so neither may reach a tag (`AGENTS.md` section 3). `PropertyName` is a member path: for FluentValidation it includes collection indexes (for example `Items[3].Name`), which would be unbounded as a metric tag.
- `AddEncinaOpenTelemetry`/`WithEncina()` (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, `WithEncina` at line 166) subscribes sources by explicit name (`AddSource("Encina.Messaging.DeadLetter")` and others); no `Encina.Validation` name exists. `ValidationOrchestrator` has 5 `new` sites in `tests/Encina.UnitTests/Validation/ValidationOrchestratorTests.cs`, 7 in `tests/Encina.GuardTests/Core/Validation/ValidationPipelineBehaviorGuardTests.cs`, 6 in `tests/Encina.ContractTests/Core/Validation/ValidationPipelineBehaviorContractTests.cs` and others in the property tests; production creates it only through DI.
- `tests/Encina.UnitTests` references all three validation packages and `Encina.OpenTelemetry`; `tests/Encina.ContractTests` references neither validation package; `src/Encina/Encina.csproj` already has `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests`. The manifest entry of the orchestrator has unit 100 and guard 84 today (`.github/coverage-manifest/Encina.json`).

### What this plan delivers

- An internal `ValidationTelemetry` in core (`src/Encina/Validation/ValidationTelemetry.cs`): `ActivitySource` and `Meter` named `Encina.Validation` (version `1.0`, ADR-054 D3: one per package name), span `encina.validation.validate`, and three instruments: `encina.validation.requests` (counter; tags `encina.validation.provider`, `encina.validation.outcome` = `valid`, `invalid`, `cancelled`, `failed`), `encina.validation.duration` (histogram, unit `s`, same tags) and `encina.validation.errors` (counter, one increment per validation error; tags `encina.validation.provider` and `encina.validation.rule`, Design Choice 3).
- `ValidationOrchestrator` records them around the provider call through two optional trailing constructor parameters, `TimeProvider? timeProvider = null` and `MultiTenancyMarker? multiTenancy = null`. The provider label comes from an explicit table keyed by the full type name of the provider (`Encina.FluentValidation.FluentValidationProvider` gives `fluentvalidation`, `Encina.DataAnnotations.DataAnnotationsValidationProvider` gives `dataannotations`, `Encina.MiniValidator.MiniValidationProvider` gives `minivalidator`, anything else `other`).
- The span carries the request name (the existing core key `Encina.request_name`, `ActivityTagNames.RequestName`), the provider, the outcome and the error count; an invalid result sets the span status to error with the code `encina.validation.failed` only. A thrown provider exception is recorded as the exception type name, the outcome `failed`, and rethrown unchanged.
- `WithEncina()` subscribes `Encina.Validation` for traces and metrics; the tenant tag comes from the helper of #2676 in its own pull request.
- Shared pieces and owner: this plan owns `ValidationTelemetry` and its names; #2676 owns the tenant helper; #906 documents it.
- Affected packages: `Encina` (core), `Encina.OpenTelemetry` (subscription only). `Encina.FluentValidation`, `Encina.DataAnnotations` and `Encina.MiniValidator` need no change: the issue lists them, but central instrumentation covers them through `IValidationProvider`. Provider category: Validation (3), all covered by the orchestrator.

---

## Design Choices

<details>
<summary><strong>1. Where the instrumentation lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside `ValidationOrchestrator` in core, with an internal `ValidationTelemetry`** | One place covers all three providers and any custom `IValidationProvider`; the issue asks for it ("instrument `ValidationOrchestrator` centrally"); the span is on the real validation, not a wrapper | The orchestrator gains two optional constructor parameters; core owns a source named `Encina.Validation` |
| **B) A decorator of `IValidationProvider` in `Encina.OpenTelemetry`, applied by `AddEncinaOpenTelemetry`** | The orchestrator stays unchanged; the provider type is known to the decorator | Decoration order and a second registration; the orchestrator cannot tell a cancelled call from a failed one if the provider swallows it |
| **C) One instrumentation block in each of the three providers** | Provider-specific detail | Three copies and no coverage of a custom provider (ADR-054 D6.1 asks for the Encina operation level) |

### Chosen Option: **A - In the orchestrator** (recommended, pending the maintainer)

### Rationale

- A wins on coverage and on the issue's own text; B wins if the maintainer wants core to stay free of any diagnostics source for validation. Pay-for-what-you-use holds: without a listener or a meter subscriber the orchestrator does not read the clock or build a tag.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What the provider label is</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An explicit table of the three full type names with an `other` fallback (the approach of #723 and #724)** | No public API; a test per row; a custom provider reports `other` | Each future provider adds a row |
| **B) Add `string Name { get; }` to `IValidationProvider`** | Exact for any provider, third-party included | A public interface change: every implementer, mock and fake (3 providers, the test doubles) must implement it, which is a breaking change for any `IValidationProvider` outside the repository |
| **C) `provider.GetType().Name`** | Zero code | A custom provider's name (an application type) becomes a tag value; generic types render with a backtick |

### Chosen Option: **A - Type-name table** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins if third-party providers need their own label and the maintainer accepts the interface change pre-1.0.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Per-rule error counting without unbounded tags or leaked values</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina.validation.rule` is the normalized property name: every bracketed segment is replaced by `[]` (`Items[3].Name` becomes `Items[].Name`), the value is capped at 64 characters, and a null property name (object-level error) is `_object`; the request type is a span attribute only** | Works for all three providers with no API change; cardinality is bounded by the number of distinct member paths of the application; no message or submitted value | Two rules on one property are not told apart; a property name is application text (a code identifier, not data) |
| **B) Add `string? RuleCode` to `ValidationError` (FluentValidation `ErrorCode`, the validator name)** | Distinguishes rules exactly | A public record change with 4 `new ValidationError(` sites in `src` and 11 in tests; DataAnnotations and MiniValidator expose no rule code, so the value would be empty for two of three providers |
| **C) Count errors only (no rule dimension)** | No cardinality risk at all | Fails the acceptance criterion "per-rule error counting" |

### Chosen Option: **A - Normalized property name** (recommended, pending the maintainer)

### Rationale

- A wins because it satisfies the criterion with data all providers already produce; B wins if operators need to alert on a specific rule (for example a `NotEmpty` failure) rather than a field, and accept the record change; C wins for a deployment that does not want any field name in telemetry. The normalization rule is a pure function with a table-driven test, and `ValidationError.ErrorMessage` is never read for telemetry.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Cost, time, errors and names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `HasListeners()` for the span and `Instrument.Enabled` for the instruments; time from the injected `TimeProvider` (optional last parameter, default `TimeProvider.System`); the span status description and `error.type` carry the error code (`encina.validation.failed`) or the exception type name, never a message; instrument names lowercase and dotted, durations in seconds, no `_total`** | Zero cost without a subscriber; follows ADR-054 | One more optional parameter |
| **B) Always measure** | Simpler | Pays per request |

### Chosen Option: **A - Guarded**

### Rationale

- ADR-054 settled points 2, 4 and 7 and D3; `AGENTS.md` section 3 (`TimeProvider`, no error message in telemetry). Tag keys are in the `encina.validation.*` namespace (the issue's `validation.provider`, `validation.type` and `validation.result` are replaced: `validation.type` has no defined meaning in the issue and the provider and outcome tags cover the other two).

**Class:** Trivial
**Settled by:** ADR-054 settled points 2, 4 and 7 and D3; `AGENTS.md` section 3

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code (`encina.validation.failed` with `ErrorCategory.Validation` already exists), so nothing new for ADR-062 | Settled | ADR-062; `src/Encina/Errors/EncinaErrorCodes.cs:138` |
| C2 | No logging and no EventId: spans and metrics only; no range is claimed | Settled | `AGENTS.md` section 7 |
| C3 | Telemetry never changes the result: an invalid request still returns the same `Left`, a provider exception still propagates, a cancellation still returns the same `Left`; a failure inside the recording code is not caught because the recording code does not throw | Settled | `AGENTS.md` section 3 (errors are not swallowed; background semantics unchanged) |
| C4 | The orchestrator never emits `EncinaError.Message`, `ValidationError.ErrorMessage`, the request body or any submitted value | Settled | `AGENTS.md` section 3; ADR-054 settled point 2 |
| C5 | No tenant-qualified id is built, so the reserved `default` segment and injectivity do not apply; the tenant tag, when added, goes through the helper of #2676 and is absent when multi-tenancy is off | Settled | ADR-054 D4; #2676 |

---

## Implementation Phases

### Phase 1: Instrumented Orchestrator (pull request V1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/ValidationTelemetry.cs` (`internal static`): `ActivitySource` and `Meter` named `Encina.Validation`, the three instruments, `ProviderLabel(IValidationProvider provider)` (the explicit table of Design Choice 2), `NormalizeRule(string? propertyName)` (Design Choice 3), `Start(Type requestType, string provider)` and `Complete(...)` helpers.
2. **Modify** `src/Encina/Validation/ValidationOrchestrator.cs`: constructor `(IValidationProvider validationProvider, TimeProvider? timeProvider = null, MultiTenancyMarker? multiTenancy = null)` (optional parameters last, CS1737); wrap the provider call: start the span only when `HasListeners()`, read the clock only when an instrument is enabled, map the result to `valid`, `invalid` (also counts each error by rule), `cancelled` (both cancellation branches) or `failed` (any other exception, recorded as its type name and rethrown). The returned `Either` is built exactly as today.
3. **Edit** `src/Encina/PublicAPI.Unshipped.txt` (the constructor line), the manifest `.github/coverage-manifest/Encina.json` (new entry for `Validation/ValidationTelemetry.cs`; the orchestrator entry keeps unit 100 and guard 84 or rises) and the changelog fragment `changelog.d/726-validation-telemetry.added.md`, through `mechanical-fixer`.
4. Tests: new `tests/Encina.UnitTests/Validation/ValidationTelemetryTests.cs` (the three real providers `FluentValidationProvider`, `DataAnnotationsValidationProvider` and `MiniValidationProvider` behind real `ValidationOrchestrator` instances with a `FakeTimeProvider`, an `ActivityListener` and a `MeterListener`: a valid and an invalid request emit the span, the counter, the histogram and the error counter with the same tag keys for all three; no tag contains an error message or a submitted value; `Items[3].Name` becomes `Items[].Name`; a 200-character path is capped; cancellation and provider exception outcomes; nothing is created without a listener); edit `tests/Encina.UnitTests/Validation/ValidationOrchestratorTests.cs` (existing behavior unchanged); new `tests/Encina.GuardTests/Core/Validation/ValidationOrchestratorTelemetryGuardTests.cs` (null `timeProvider` and null `multiTenancy` are valid and yield the system clock and no tenant tag; the existing null checks).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #726, plan docs/plans/validation-telemetry-implementation-plan-726.md, phase 1.
All three validation providers go through ValidationOrchestrator (src/Encina/Validation/ValidationOrchestrator.cs).
ADR-054: lowercase dotted names, seconds, no _total, never messages or data in tags.

TASK:
1. Create internal ValidationTelemetry (source and meter "Encina.Validation"; requests, duration, errors; ProviderLabel table; NormalizeRule).
2. ValidationOrchestrator: optional last parameters TimeProvider? and MultiTenancyMarker?; record valid, invalid, cancelled and failed.
3. Unit and guard tests with the real providers; PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- Never emit ValidationError.ErrorMessage, EncinaError.Message, request values or exception messages; the result is returned unchanged.
- Zero cost without a listener (HasListeners, Instrument.Enabled); time from TimeProvider.
- Existing "new ValidationOrchestrator(provider)" sites must still compile; null optional parameters get valid-result guard tests.

REFERENCE FILES:
- src/Encina/Validation/ValidationOrchestrator.cs, src/Encina/Validation/ValidationResult.cs, src/Encina/Diagnostics/EncinaDiagnostics.cs
- tests/Encina.UnitTests/Validation/ValidationOrchestratorTests.cs
```

</details>

---

### Phase 2: Subscription and Documentation (pull request V2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` (`WithEncina`): `tracing.AddSource("Encina.Validation")` and `metrics.AddMeter("Encina.Validation")` next to the other explicit names (a wildcard subscription and the architecture test of every source and meter belong to #1787 and the ADR-054 D3 follow-up).
2. Tests: edit `tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs` (the source and meter are subscribed); the manifest `.github/coverage-manifest/Encina.OpenTelemetry.json` entry for the file keeps unit 90 and guard 35 or rises; changelog `changelog.d/726-validation-telemetry-subscription.added.md` through `mechanical-fixer`.
3. A validation section in the observability guide (#906) and a paragraph in each of the three validation package READMEs naming the source, the instruments and the rule normalization, through `docs-writer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #726 plan docs/plans/validation-telemetry-implementation-plan-726.md, phase 2. Phase 1 merged.

TASK:
1. Subscribe "Encina.Validation" in WithEncina (traces and metrics) and extend the unit test of the registration.
2. Documentation goes to docs-writer; PublicAPI is unchanged; manifest and changelog through mechanical-fixer.

KEY RULES:
- Explicit names next to the existing ones; do not introduce a wildcard here (#1787 owns it).

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (WithEncina), tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 3: Tenant Tag (pull request V3, after #2676)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `ValidationOrchestrator.cs` and `ValidationTelemetry.cs`: add the tenant tag to the span and to the requests and duration instruments through `TenantTelemetry.ValueFor(context, _multiTenant)` of #2676; the error counter does not carry the tenant (its cardinality is already rule times provider).
2. Tests: edit `ValidationTelemetryTests.cs` (tenant on with the marker, absent without it, blank tenant); changelog `changelog.d/726-validation-telemetry-tenant.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #726 plan docs/plans/validation-telemetry-implementation-plan-726.md, phase 3. #2676 merged (TenantTelemetry in core).

TASK:
1. Add the tenant tag to the span, requests and duration through TenantTelemetry.ValueFor; the marker is read in the constructor.
2. Tests for tenant on, off and blank.

KEY RULES:
- Only when multi-tenancy is enabled; never baggage; never on the error counter.

REFERENCE FILES:
- src/Encina/Diagnostics/TenantTelemetry.cs (from #2676), src/Encina/Validation/ValidationTelemetry.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | Settled points 2, 4, 7; D3 names; D4 tenant | Names, errors, cost, tenant |
| `AGENTS.md` section 5 | Validation category: all 3 providers go through `ValidationOrchestrator` | Central instrumentation covers the category |
| `AGENTS.md` section 3 | No `EncinaError.Message` in telemetry; `TimeProvider` | Tag and clock rules |
| .NET metrics guidance | Bounded tag cardinality | Rule normalization and cap |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ValidationOrchestrator` | `src/Encina/Validation/ValidationOrchestrator.cs` | Instrumented |
| `EncinaDiagnostics`, `ActivityTagNames` | `src/Encina/Diagnostics/` | Pattern and the `RequestName` key |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenant flag (phase 3) |
| `WithEncina` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:166` | Subscription |
| The three providers | `src/Encina.FluentValidation/`, `src/Encina.DataAnnotations/`, `src/Encina.MiniValidator/` | Exercised by the proof tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (core) | none | No logging (register row C2); no range is claimed and `EventIdRanges.cs` is untouched |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| V1 | Phase 1 | 1 | 2 (`ValidationOrchestrator.cs`, `PublicAPI.Unshipped.txt`) | 2 | 1 | 1 | 1 (changelog) | 8 | ~720 |
| V2 | Phase 2 | 0 | 1 | 0 | 1 | 1 | 5 (guide page, 3 READMEs, changelog) | 8 | ~160 |
| V3 | Phase 3 | 0 | 2 | 0 | 1 | 0 | 1 (changelog) | 4 | ~110 |
| **Total** | | **1** | **5** | **2** | **3** | **2** | **7** | **20** | **~990** |

Columns sum: 1 + 5 + 2 + 3 + 2 + 7 = 20 = 8 + 8 + 4. Test doubles: no interface changes (the orchestrator is a class), so no fake needs an edit; the `new ValidationOrchestrator(` sites of the unit, guard, contract and property tests compile unchanged because both new parameters are optional. Per-file coverage targets:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `src/Encina/Validation/ValidationTelemetry.cs` | 95 | 0 | 0 | 0 | 0 | Internal static with no validated parameter (guard 0 with that reason); every label row, the normalization cases, and the four outcomes are unit cases with the three real providers |
| `src/Encina/Validation/ValidationOrchestrator.cs` | 100 | 90 | 0 | 0 | 0 | Today's targets are unit 100 and guard 84 (a rise only): the new constructor parameters and the null-valid cases are guard tests; contract is 0 because the orchestrator implements no interface |
| `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | 90 | 35 | 0 | 0 | 0 | The two new subscriptions are driven by the registration unit tests; the current targets are kept |

The pull requests report the measured per-flag values against these targets; no target is lowered (`target-ratchet`).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #726 - OpenTelemetry for validation - in three small pull requests (V1 orchestrator instrumentation,
V2 subscription and documentation, V3 tenant tag after #2676).

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0. Edit with Edit/Write only; PublicAPI, manifest and changelog through mechanical-fixer; documentation
  through docs-writer.
- ADR-054 names and privacy rules. All three validation providers go through ValidationOrchestrator, so no provider package changes.

IMPLEMENTATION OVERVIEW:
1. V1: internal ValidationTelemetry in core and ValidationOrchestrator instrumentation (provider label table, outcomes, normalized rule).
2. V2: WithEncina subscribes "Encina.Validation"; guide and README paragraphs.
3. V3: tenant tag through TenantTelemetry (#2676).

KEY PATTERNS:
- Never emit ErrorMessage, EncinaError.Message, request values or exception messages; return the result unchanged.
- Zero cost without a listener; optional constructor parameters last so existing "new ValidationOrchestrator(" sites compile.
- Tests use the real FluentValidation, DataAnnotations and MiniValidator providers.

REFERENCE FILES:
- src/Encina/Validation/, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Validation results are not cached; the instrumentation adds no read |
| 2 | OpenTelemetry | ✅ Phases 1-3 | This is the feature |
| 3 | Structured Logging | ❌ N/A | Spans and metrics only; no EventId (register row C2) |
| 4 | Health Checks | ❌ N/A | Observes operations; no checkable dependency |
| 5 | Validation | ✅ Phase 1 | The feature instruments the validation orchestrator itself |
| 6 | Resilience | ❌ N/A | No external call; never retries |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Never commits or rolls back |
| 9 | Idempotency | ❌ N/A | Recording twice is harmless |
| 10 | Multi-Tenancy | ✅ Phase 3 | Tenant tag only when multi-tenancy is on, through the helper of #2676 |
| 11 | Module Isolation | ❌ N/A | No module data |
| 12 | Audit Trail | ❌ N/A | No personal data may reach telemetry; validation errors are never recorded |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 | Accepted | Conventions |
| #2676 tenant helper | Open | Only V3 waits |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Observability guide | #906 | V2 adds the validation section |
| Every source and meter subscribed (architecture test) | #1787 | Will replace the explicit names with a wildcard |

---

## Next Steps

1. Link this plan from #726 and ask the maintainer to answer Design Choices 1 to 3 when work starts.
2. Implement V1, then V2; V3 after #2676.
3. Maintainer action: amend the acceptance text of #726 that names `validation.provider`, `validation.type`, `validation.result` and "affected packages" for the three providers to this plan's `encina.validation.*` keys and central instrumentation (this plan never comments on an issue).
