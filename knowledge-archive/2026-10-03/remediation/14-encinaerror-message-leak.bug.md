<!-- issue
title: [BUG] Personal data leakage via EncinaError.Message in the validation pipeline
labels: bug, area-validation
milestone: v0.14.0 — Hardening
-->

## Description

Found by the SPEC-003 audit of #14 (Encina.Validation orchestrator refactor), verified by an `adversarial-reviewer` pass on 2026-09-25.

`ValidationOrchestrator.ValidateAsync` (`src/Encina/Validation/ValidationOrchestrator.cs:70-71`) builds `EncinaError.New(errorMessage)` where `errorMessage = result.ToErrorMessage(typeof(TRequest).Name)`. `ValidationResult.ToErrorMessage` (`src/Encina/Validation/ValidationResult.cs:77-89`) concatenates every `PropertyName: ErrorMessage` pair from whichever provider ran — FluentValidation `.WithMessage(...)` templates, DataAnnotations `ErrorMessage` strings, or MiniValidation output — any of which can embed the submitted value (e.g. a custom message that echoes the invalid email).

That `EncinaError` is returned as `Left` from `ValidationPipelineBehavior<,>.Handle` (`src/Encina/Validation/ValidationPipelineBehavior.cs:61-66`) and propagates as the `Send`/`Query`/`Command` outcome. Two pre-existing core sinks then unconditionally record `error.Message` for any such `Left`:

1. `Log.RequestFailed` — `[LoggerMessage(EventId = 120, Level = LogLevel.Error, ...: {Message}")]` at `src/Encina/Core/Encina.cs:243`, invoked from `LogSendOutcomeCore` at `src/Encina/Core/Encina.cs:137`.
2. `EncinaDiagnostics.SendCompleted(activity, ..., errorMessage: outcomeError?.Message)` at `src/Encina/Dispatchers/Encina.RequestDispatcher.cs:140-144`, which calls `activity.SetStatus(ActivityStatusCode.Error, errorMessage)` at `src/Encina/Diagnostics/EncinaDiagnostics.cs:39` — the raw message lands in the OTel activity status description, exported to tracing backends.

`src/Encina/Results/EncinaErrors.cs:124` itself documents that `EncinaError.Message` "may contain personal data" — exactly the case CLAUDE.md's rule (project history #1168, #1173, #1259, #1274) exists to prevent: "`EncinaError.Message` never reaches logs, activity tags, health-check results or plaintext storage — only the error code or exception type is recorded." Issue #14's refactor is what routes free-form, per-property validation text into the one field those sinks always record for every `Left`, not just validation ones.

Note: `ProblemDetailsExtensions.ToProblemDetails` (`src/Encina.AspNetCore/ProblemDetailsExtensions.cs:75`, `Detail = error.Message`) also exposes `Message` to the HTTP client, but that is by design for validation responses (the client needs to see its own validation errors) and is not part of this defect — the defect is the logs/traces path, which is meant for operators, not the request's own author.

## Steps to Reproduce

1. Register a command whose validator's error message embeds the submitted value (e.g. a FluentValidation rule with `.WithMessage(x => $"Email '{x.Email}' is not valid.")`, or a DataAnnotations `[EmailAddress(ErrorMessage = "...")]`).
2. Register any of the three validation providers (`AddEncinaFluentValidation`, `AddDataAnnotationsValidation`, `AddMiniValidation`).
3. Send a command that fails that rule via `IEncina.Send`.
4. Inspect the structured logs for the `Log.RequestFailed` entry (EventId 120) — the submitted value appears in the message text.
5. Inspect the exported OpenTelemetry span for that request — the same text appears as the `ActivityStatusCode.Error` description.

## Expected Behavior

`EncinaError.Message` produced by a failed validation never appears in application logs or OpenTelemetry activity status/tags; only the error code or a generic, value-free description is recorded there.

## Actual Behavior

The full per-field validation text, including any submitted value a validator's message template echoes, is written to `Log.RequestFailed` and to the OpenTelemetry activity status description for every validation failure.

## Environment

- **Encina Version**: main (pre-1.0)
- **.NET Version**: .NET 10.0
- **OS**: any
- **Package(s) Affected**: Encina (core: Validation, Core, Dispatchers, Diagnostics)

## Code Sample

```csharp
// Validator with a message template that echoes the submitted value
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage(x => $"Email '{x.Email}' is not valid.");
    }
}

// src/Encina/Validation/ValidationOrchestrator.cs:70-71
var errorMessage = result.ToErrorMessage(typeof(TRequest).Name);
// -> "Validation failed for CreateUserCommand with 1 error(s): Email: Email 'user@example.com' is not valid."
return Left<EncinaError, Unit>(EncinaError.New(errorMessage));

// src/Encina/Core/Encina.cs:137/243 and
// src/Encina/Dispatchers/Encina.RequestDispatcher.cs:140-144 both then record error.Message verbatim.
```

## Stack Trace

```
N/A — no exception; this is a silent data-exposure path through structured logging and OpenTelemetry, not a crash.
```

## Additional Context

**Root Cause**: `ValidationOrchestrator` packs full per-field validation detail — including anything a validator's message template embeds — into `EncinaError.Message`, the one field the core dispatcher and diagnostics sinks always record for a failed `Send`/`Query`/`Command`, with no distinction between "safe to log" and "may contain personal data" errors.

**Proposed Fix** (either direction closes the gap; pick one during triage):
1. `ValidationOrchestrator` stops putting concatenated per-property text into `EncinaError.Message` for validation failures (e.g. keep `Message` to a generic, value-free summary such as `"Validation failed for CreateUserCommand"` and carry the detailed per-field errors in a structured, non-logged place the ASP.NET Core problem-details mapping can still read); or
2. `Log.RequestFailed` and `EncinaDiagnostics.SendCompleted` stop recording `error.Message` unconditionally and instead record only the error code or exception type, consistent with how the rule is already enforced elsewhere (#1168, #1173, #1259, #1274).

This is compliance-sensitive for any request payload containing personal data validated via FluentValidation, DataAnnotations or MiniValidator, since Encina's own reference use case (ConsultaPsicologica) handles health data.

**Related Issues**: found during the SPEC-003 audit of #14; distinct from #1330 (README documents `error.Exception` behaviour the code does not implement — a docs-only accuracy gap) and from #1337/#1338 (missing DI/guard/cancellation tests for the same three packages).
