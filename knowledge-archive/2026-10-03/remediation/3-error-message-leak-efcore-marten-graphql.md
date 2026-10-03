<!-- issue
title: [BUG] EF Core, Marten and GraphQL log EncinaError.Message through their Log.cs templates
labels: bug, area-security
milestone: v0.14.0 — Hardening
-->

## Description

Found by the SPEC-003 audit of #3 (logging-concern deep pass). `CLAUDE.md`'s "Code Quality Standards" rule ("`EncinaError.Message` never reaches logs, activity tags, health-check results or plaintext storage: only the error code or exception type is recorded", project history #1168, #1173, #1259, #1274) is violated in four call sites across three packages that #3 gave a `Log.cs`. This is the same defect class already tracked for the core mediator pipeline (#1319) and for `Encina.ADO.PostgreSQL`'s audit/anonymization stores (#1322), but in different packages not covered by either issue's stated scope.

## Steps to Reproduce

1. Register `Encina.EntityFrameworkCore`'s transaction pipeline behavior and cause a rollback with a `Left(EncinaError)` whose `Message` contains data that must not leave the process.
2. Observe the emitted log via `TransactionPipelineBehavior.cs:120`.
3. Similarly, cause a domain-event dispatch failure (`DomainEventDispatcherInterceptor.cs:292`), a Marten domain-event publish failure (`EventPublishingPipelineBehavior.cs:87`), or a GraphQL query/mutation failure (`GraphQLMediatorBridge.cs:58` and `:100`).

## Expected Behavior

Only `error.GetEncinaCode()` (or the exception's type name) reaches the log message; the free-text `Message` never leaves the process through structured logs.

## Actual Behavior

- `src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs:120` — `Log.RollingBackTransactionDueToError(_logger, typeof(TRequest).Name, error.Message, context.CorrelationId)` (Warning).
- `src/Encina.EntityFrameworkCore/DomainEvents/DomainEventDispatcherInterceptor.cs:292` — `Log.DomainEventPublishFailed(_logger, ..., error.Message)` (Warning).
- `src/Encina.Marten/EventPublishingPipelineBehavior.cs:87` — `Log.FailedToPublishDomainEvent(_logger, domainEvent.GetType().Name, error.Message)` (Error).
- `src/Encina.GraphQL/GraphQLMediatorBridge.cs:58` and `:100` — `Log.QueryFailed`/`Log.MutationFailed(..., error.Message)` (Warning).

## Environment

- **Encina Version**: pre-1.0, main as of 2026-09-25
- **.NET Version**: .NET 10.0
- **OS**: any
- **Package(s) Affected**: Encina.EntityFrameworkCore, Encina.Marten, Encina.GraphQL

## Code Sample

```csharp
// TransactionPipelineBehavior.cs:120 — current:
Log.RollingBackTransactionDueToError(_logger, typeof(TRequest).Name, error.Message, context.CorrelationId);

// proposed:
Log.RollingBackTransactionDueToError(_logger, typeof(TRequest).Name, error.GetEncinaCode(), context.CorrelationId);
```

## Stack Trace

N/A — found by static audit, not an exception.

## Additional Context

- Related: #1319 (same defect class, core mediator pipeline), #1322 (same defect class, `Encina.ADO.PostgreSQL` audit/anonymization stores).
- Proposed fix: replace `error.Message` with `error.GetEncinaCode()` in the four `Log.cs` call sites and their `[LoggerMessage]` template placeholders across the three packages; add a regression test per AUD-06 that asserts the log never receives the literal message text.
- Root Cause: these call sites were written for developer-friendly debugging before the no-message-leak rule was established project-wide (like #1322); no test asserts the absence of `error.Message` in these particular paths.
