<!-- issue
title: [BUG] Encina.ADO.PostgreSQL audit and anonymization stores interpolate raw exception messages into EncinaError
labels: bug, area-database, area-security
milestone: v0.14.0 — Hardening
-->

## Description

`Auditing/AuditStoreADO.cs`, `Auditing/ReadAuditStoreADO.cs` and
`Anonymization/TokenMappingStoreADO.cs` build `EncinaError` (and, for anonymization,
`AnonymizationErrors.StoreError`) by interpolating the raw `ex.Message` of a caught exception.
`CLAUDE.md`/#856/SPEC-002 REQ-034 require that no `EncinaError`, log message, activity attribute
or metric tag carry a payload, a secret or a direct identifier of a data subject. Queries in
these classes are parameterized (no raw SQL text with values is interpolated), so this is not a
proven direct leak of row data, but the underlying Npgsql exception message is never sanitized
before it is surfaced, and it can include constraint or column names. `TokenMappingStoreADO` is
the most sensitive of the three, since it stores the token↔original-value mapping for
anonymization.

## Steps to Reproduce

1. Open `src/Encina.ADO.PostgreSQL/Auditing/AuditStoreADO.cs:143`.
2. Observe `EncinaError.New($"Failed to record audit entry: {ex.Message}")` (same pattern at
   lines 178, 215, 248, 326, 350).
3. Open `src/Encina.ADO.PostgreSQL/Auditing/ReadAuditStoreADO.cs:120,155,192,251,275` — same pattern.
4. Open `src/Encina.ADO.PostgreSQL/Anonymization/TokenMappingStoreADO.cs:66` —
   `AnonymizationErrors.StoreError("Store", ex.Message)`.

## Expected Behavior

`EncinaError` factories in security- and compliance-relevant stores never carry an unsanitized
exception message; the message is either a fixed, generic string, or the exception detail is
logged separately (not returned to the caller) with the identifying fields redacted.

## Actual Behavior

`ex.Message` from Npgsql is passed straight into the error the caller receives, in 12 call sites
across 3 files.

## Environment

- **Encina Version**: pre-1.0, main as of 2026-09-24
- **.NET Version**: .NET 10.0
- **OS**: any
- **Package(s) Affected**: Encina.ADO.PostgreSQL

## Code Sample

```csharp
// AuditStoreADO.cs:143 — current:
return Either<EncinaError, Unit>.Left(EncinaError.New($"Failed to record audit entry: {ex.Message}"));

// proposed:
_logger.LogAuditWriteFailed(ex); // full detail to logs only
return Either<EncinaError, Unit>.Left(EncinaError.New("Failed to record audit entry."));
```

## Stack Trace

N/A — found by static audit.

## Additional Context

Found during the SPEC-003 pilot-2 deep quality audit of `Encina.ADO.PostgreSQL` (AUD-13).
Interim result at `artifacts/audit/encina-ado-postgresql.md` in worktree `waud-adopg`.

## Root Cause

The `catch (Exception ex)` blocks in these stores were written for developer-friendly debugging
before the no-PII-in-errors rule (#856) was established project-wide; no test asserts the
absence of `ex.Message` in these particular error paths.
