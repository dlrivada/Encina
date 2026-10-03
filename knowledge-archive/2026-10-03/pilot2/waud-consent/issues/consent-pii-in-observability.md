<!-- issue
title: [BUG] Encina.Compliance.Consent logs and traces the data subject's own identifier in plain text
labels: bug, area-compliance, ai:claude-required
milestone: v0.14.0 — Hardening
-->

## Description

The `Encina.Compliance.Consent` package violates the project compliance rule (`CLAUDE.md`; project history #856; SPEC-002 REQ-034/REQ-062) that no log message, activity attribute, metric tag, or `EncinaError` may carry a direct identifier of a data subject.

`SubjectId` (the data subject's own identifier, e.g. a user id) is logged in plain text in multiple log templates, exposed as an OpenTelemetry activity tag and metric tag, and embedded in `EncinaError.details` under the key `"subjectId"`. This exposes direct data-subject identifiers in every log aggregator, trace backend and metrics pipeline the host application wires up.

## Steps to Reproduce

1. Read `src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs` (log templates at EventIds 8200, 8201, 8202, 8203, 8204, 8206, 8230, 8260).
2. Read `src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs` (`TagSubjectId = "consent.subject_id"`).
3. Read `src/Encina.Compliance.Consent/ConsentErrors.cs` (`MissingConsent`, `ConsentExpired`, `ConsentWithdrawn`, `RequiresReconsent`, `VersionMismatch` all put `subjectId` into `EncinaError.details["subjectId"]`).

## Expected Behavior

Subject identifiers are hashed, pseudonymized, or omitted from logs, traces, metrics and error metadata, so no direct identifier of a data subject is ever emitted through observability channels.

## Actual Behavior

- `ConsentLogMessages.cs` logs `SubjectId` in plain text in log templates at EventIds 8200, 8201, 8202, 8203, 8204, 8206, 8230, and 8260.
- `ConsentDiagnostics.cs` defines `TagSubjectId = "consent.subject_id"`, used as an OpenTelemetry activity tag and metric tag, set from `ConsentRequiredPipelineBehavior.cs:133,232-233,246-247`.
- `ConsentErrors.cs` puts `subjectId` into `EncinaError.details["subjectId"]` in `MissingConsent`, `ConsentExpired`, `ConsentWithdrawn`, `RequiresReconsent`, and `VersionMismatch`.

## Environment

- **Encina Version**: pre-1.0 (unreleased)
- **.NET Version**: .NET 10.0
- **OS**: N/A — found during a static code audit, not a runtime report
- **Package(s) Affected**: Encina.Compliance.Consent

## Code Sample

```csharp
// src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs
LoggerMessage.Define<string, string>(
    LogLevel.Debug,
    new EventId(8200, nameof(ConsentCheckStarted)),
    "Consent check started. RequestType={RequestType}, SubjectId={SubjectId}");
```

## Stack Trace

```
N/A — this is a static-analysis finding (SPEC-003 audit AUD-13), not an exception.
```

## Additional Context

Found during the SPEC-003 deep quality audit of `Encina.Compliance.Consent` (2026-09-24), checklist item AUD-13. Every occurrence is listed in `artifacts/audit/findings.csv` (finding F1) and `artifacts/audit/encina-compliance-consent.md` in the audit worktree. The fix should hash or drop `subjectId` from every log template, tag and error-metadata entry across the package, and add a regression test asserting its absence (per AUD-13's verification method).
