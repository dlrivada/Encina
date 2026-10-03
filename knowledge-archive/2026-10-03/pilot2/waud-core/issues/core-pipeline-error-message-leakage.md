<!-- issue
title: [BUG] Core mediator pipeline writes EncinaError.Message and exception messages into OpenTelemetry activity tags and logs
labels: bug, area-core
milestone: v0.14.0 — Hardening
-->

## Description

The core request pipeline (`src/Encina/Pipeline/**`, `src/Encina/Core/**`, `src/Encina/Dispatchers/**`) writes `EncinaError.Message` and raw exception messages into OpenTelemetry activity tags and structured logs on every failed `Send`/`Publish`. This repeats, inside the most central package of the codebase, the defect class the maintainer already tracked for the outbox/dead-letter path (#1168, #1173, #1259, #1274) and that `CLAUDE.md` now states as a mandatory rule: "`EncinaError.Message` never reaches logs, activity tags, health-check results or plaintext storage: only the error code or exception type is recorded."

Found during the SPEC-003 pilot 2 deep quality audit of the core request pipeline audit unit (checklist item AUD-13).

## Steps to Reproduce

1. Register a pipeline behavior or handler that returns a `Left(EncinaErrors.Create(..., message: "<data that must not leave the process>"))`, or one that throws with a message containing such data.
2. Call `IEncina.Send`/`Publish` for a command or query with `CommandActivityPipelineBehavior<,>`/`QueryActivityPipelineBehavior<,>` registered (the pattern shown in each behavior's own XML `<example>`).
3. Inspect the emitted `Activity` (e.g., via an `ActivityListener`) or the application log.

## Expected Behavior

Only the error code (`EncinaError.GetEncinaCode()`) or the exception's type name reaches the `Activity` status, its tags, and the log message. The free-text `Message` never leaves the process through either channel.

## Actual Behavior

- `src/Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs:113` and `:164`, and the identical code in `src/Encina/Pipeline/Behaviors/QueryActivityPipelineBehavior.cs:108` and `:159`, call `activity?.SetStatus(ActivityStatusCode.Error, ex.Message)` / `activity?.SetStatus(ActivityStatusCode.Error, error.Message)`.
- The same two files also call `activity?.SetTag(ActivityTagNames.ExceptionMessage, ex.Message)` (`CommandActivityPipelineBehavior.cs:115`, `QueryActivityPipelineBehavior.cs:110`).
- `src/Encina/Core/Encina.cs`'s `Log.RequestFailed` (`EventId = 120`, message template `"...: {Message}"`) is called from `LogSendOutcomeCore` (`Core/Encina.cs:137`) with `effectiveError.Message`.
- `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`'s `Log.NotificationHandlerFailure` (`EventId = 117`, message template `"...: {Message}"`) is called at `NotificationDispatcher.cs:134` with `error.Message`.

By contrast, `CommandMetricsPipelineBehavior.cs` and `QueryMetricsPipelineBehavior.cs` in the same folder only ever record `error.GetEncinaCode()`, showing the correct pattern already exists side by side with the defect.

## Environment

- **Encina Version**: pre-1.0 (main, 2026-09-24)
- **.NET Version**: .NET 10.0
- **OS**: Windows 11
- **Package(s) Affected**: `Encina` (core)

## Code Sample

```csharp
// Pipeline/Behaviors/CommandActivityPipelineBehavior.cs
private static void RecordException(Activity? activity, Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);           // leaks
    activity?.SetTag(ActivityTagNames.ExceptionType, ex.GetType().FullName);
    activity?.SetTag(ActivityTagNames.ExceptionMessage, ex.Message);     // leaks
}
```

## Stack Trace

N/A — not an exception, a data-flow defect.

## Additional Context

- Related: #1168, #1173, #1259, #1274 (the same defect class, already fixed for `Encina.Messaging`'s outbox/dead-letter path; this issue is the core mediator's instance of it).
- SPEC-003 audit unit: `encina-core-pipeline`; result file: `docs/knowledge/audits/encina-core-pipeline.md` (once promoted from `artifacts/audit/encina-core-pipeline.md`).
- Proposed fix: replace `ex.Message`/`error.Message` with `ex.GetType().Name`/`error.GetEncinaCode()` in `RecordException`/`RecordErrorOutcome` of both behaviors, and drop the `{Message}` placeholder from `Log.RequestFailed` and `Log.NotificationHandlerFailure` (or replace it with the error code). Add a regression test per AUD-06 that asserts the activity/log never receives the literal message text.
