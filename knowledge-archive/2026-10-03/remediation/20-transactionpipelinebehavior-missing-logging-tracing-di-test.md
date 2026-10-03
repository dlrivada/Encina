<!-- issue
title: [DEBT] Encina.Messaging.TransactionPipelineBehavior has no logging, no tracing, and no DI-resolution test
labels: technical-debt, area-messaging
milestone: v0.14.0 — Hardening
-->

Found by the SPEC-003 audit of #20.

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [x] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

`Encina.Messaging.TransactionPipelineBehavior<TRequest,TResponse>` (`src/Encina.Messaging/TransactionPipelineBehavior.cs`) takes only an `IDbConnection` in its constructor and never logs or traces anything. Yet `src/Encina.Messaging/MessagingLog.cs:203-231` declares three `[LoggerMessage]` methods explicitly for this behavior — `TransactionStarted` (EventId 2834), `TransactionCommitted` (EventId 2835), `TransactionRolledBack` (EventId 2836) — that have zero call sites anywhere in the package. This is a direct consequence of issue #20's centralization: the six Dapper/ADO providers that now share this one behavior (SqlServer, PostgreSQL, MySQL, for both Dapper and ADO) lost the operational visibility their EF Core sibling still has (`src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs` does log begin/commit/reuse). There is also no `ActivitySource`/tracing on the transaction boundary, and no DI `ValidateOnBuild`/`ValidateScopes` test proves `IDbConnection` resolves for this behavior in any Dapper or ADO provider's registration.

## Location

- **File(s)**: `src/Encina.Messaging/TransactionPipelineBehavior.cs`, `src/Encina.Messaging/MessagingLog.cs` (lines 203-231)
- **Package(s)**: Encina.Messaging (consumed by Encina.Dapper.SqlServer, Encina.Dapper.PostgreSQL, Encina.Dapper.MySQL, Encina.ADO.SqlServer, Encina.ADO.PostgreSQL, Encina.ADO.MySQL)

## Current Behavior

`TransactionPipelineBehavior.Handle` opens a connection, begins a transaction, commits on `Right` or rolls back on `Left`/exception, but never calls `MessagingLog.TransactionStarted/Committed/RolledBack` and has no `ActivitySource` span. No test in `tests/**/*Dapper*/**/*.cs` or `tests/**/*ADO*/**/*.cs` uses `ValidateOnBuild` to prove the shared behavior's `IDbConnection` dependency resolves.

## Expected Behavior

The behavior logs transaction start, commit and rollback via the existing `MessagingLog` methods, carries an `ActivitySource` span around `Handle` (cross-cutting functions #2 and #3 per CLAUDE.md), and at least one DI test per affected provider proves the registration is complete with `ValidateOnBuild`/`ValidateScopes` (per CLAUDE.md's "Registration completeness" rule, project history #1260/#1273/#1285/#1289).

## Root Cause

When issue #20 centralized `TransactionPipelineBehavior` into `Encina.Messaging`, the `MessagingLog` entries for transaction events were created but never wired into the new shared class, and no DI completeness test was added for the new shared registration path across the six providers.

## Proposed Fix

Inject `ILogger<TransactionPipelineBehavior<TRequest,TResponse>>` into the constructor (acceptable breaking change pre-1.0). Call `MessagingLog.TransactionStarted` before beginning the transaction, `TransactionCommitted` after a successful commit, and `TransactionRolledBack` in both rollback paths. Add an `ActivitySource` span around `Handle`. Add one DI `ValidateOnBuild` test per Dapper/ADO provider proving `IDbConnection` resolves when `UseTransactions = true`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

Found by the SPEC-003 audit of #20. Related: #1328 (a different, already-tracked `EncinaError.Message` leak in EF Core's sibling `TransactionPipelineBehavior.cs:120`, not this issue's concern).
