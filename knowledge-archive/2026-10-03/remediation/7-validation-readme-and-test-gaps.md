<!-- issue
title: [BUG] Encina.DataAnnotations and Encina.FluentValidation READMEs document EncinaError.Exception behaviour the code does not implement
labels: bug, area-validation
milestone: v0.14.0 — Hardening
-->

Found by the SPEC-003 audit of #7.

## Description

The READMEs for `Encina.DataAnnotations` and `Encina.FluentValidation` document that a validation failure produces `EncinaError.Exception = Some(ValidationException)` with structured per-field errors. However, the actual implementation never sets the `Exception` property on a validation failure; it remains `None`. This creates a silent contract mismatch: a consumer following the README literally, expecting structured exception data, receives only a flattened `error.Message` string. Additionally, two related minor coverage gaps exist:

1. No test anywhere in the repository exercises `CustomValidationAttribute` + `ValidationContext` (DataAnnotations).
2. No test locks in the invariant that `Exception` stays `None` for an ordinary validation failure.

## Steps to Reproduce

1. Register `Encina.DataAnnotations` or `Encina.FluentValidation` in an application (`services.AddDataAnnotationsValidation();` or `services.AddEncinaFluentValidation(...)`).
2. Send a request that fails validation (e.g., a required field left empty).
3. Inspect the returned `EncinaError.Exception` in the `Left` branch of the `Either<EncinaError, TResponse>` result.

## Expected Behavior

Per both READMEs' "Validation Failure Structure" sections and their "Handle Validation Errors Functionally" Quick Start examples, `EncinaError.Exception` should be `Some(ValidationException)` carrying structured per-field errors (`ex.Data["ValidationResults"]` for DataAnnotations, `validationEx.Errors` for FluentValidation), and the documented `error.Exception.IfSome(ex => ...)` pattern should execute and extract that data.

## Actual Behavior

`Exception` is always `None` for a validation failure. `src/Encina/Validation/ValidationOrchestrator.cs:71` returns `Left<EncinaError, Unit>(EncinaError.New(errorMessage))` — message-only, no exception attached — on every validation failure. `Exception` is only set on the unrelated cancellation branch (`ValidationOrchestrator.cs:78-79`, `EncinaError.New(ex, ...)`). The documented `IfSome` code path in both READMEs never executes.

## Environment

- **Encina Version**: N/A (documentation/code consistency defect, not version- or environment-specific)
- **.NET Version**: N/A
- **OS**: N/A
- **Package(s) Affected**: Encina.DataAnnotations, Encina.FluentValidation

## Code Sample

```csharp
// README Quick Start pattern (src/Encina.DataAnnotations/README.md, src/Encina.FluentValidation/README.md):
result.Match(
    Right: userId => Console.WriteLine($"User created: {userId}"),
    Left: error =>
    {
        error.Exception.IfSome(ex =>
        {
            // Documented: extract per-field errors from ex (ValidationException).
            // Actual: this block never runs — error.Exception is always None
            // for a validation failure produced by ValidationOrchestrator.
        });
    });
```

## Stack Trace

N/A — no exception is thrown; this is a silent contract mismatch, not a crash.

## Root Cause

`ValidationOrchestrator` has built a message-only `EncinaError` on the validation-failure path since its creation (commit `fc36f4df`, "apply Orchestrator pattern to validation packages", issue #14). The two package READMEs describe an earlier or aspirational design (structured `ValidationException` attached to `Exception`) that was never updated to match the orchestrator's actual, simpler contract.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #7 — the closed issue whose SPEC-003 audit found this defect (the mismatch predates #7 and is not caused by it; #14 introduced the orchestrator that has always behaved this way)

## Additional Context

The fix direction is a decision for the maintainer, not this audit:

- (a) restore structured `Exception` attachment on validation failure in `ValidationOrchestrator.ValidateAsync`, making sure any attached exception's fields do not leak raw user-submitted values into logs, activity tags or metrics per the "`EncinaError.Message` never reaches logs" rule (`CLAUDE.md`, project history #1168, #1173, #1259, #1274); or
- (b) correct both READMEs ("Validation Failure Structure" and the "Handle Validation Errors Functionally" Quick Start examples) to describe the message-only contract that exists today, and drop the `IfSome` example that never fires.

Whichever direction is chosen should also close these two related coverage gaps found during the same audit:

- [ ] Add a property or unit test exercising `CustomValidationAttribute` + `ValidationContext` for `Encina.DataAnnotations` (the original regression scenario fixed in #7, lost when `tests/Encina.DataAnnotations.PropertyTests/DataAnnotationsValidationBehaviorPropertyTests.cs` was consolidated into `tests/Encina.PropertyTests/ValidationProviderProperties.cs`).
- [ ] Add a test that asserts `error.Exception.IsNone` (or the corrected documented behavior) for an ordinary validation failure, so the actual contract — whichever direction is chosen — is regression-guarded.
