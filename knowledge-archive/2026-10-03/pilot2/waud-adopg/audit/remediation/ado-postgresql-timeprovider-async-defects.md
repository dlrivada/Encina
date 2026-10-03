<!-- issue
title: [BUG] Encina.ADO.PostgreSQL reads wall-clock time and calls ADO.NET synchronously in 17+ places
labels: bug, area-database
milestone: v0.14.0 — Hardening
-->

## Description

`Encina.ADO.PostgreSQL` has two related conformance defects found during the SPEC-003 deep
quality audit of the package (checklist AUD-14, AUD-16):

1. Four call sites read `DateTime.UtcNow`/`DateTimeOffset.UtcNow` directly instead of taking
   `TimeProvider` by injection, contradicting the decision recorded in #543 and #667 (which
   already fixed the same defect in `OutboxStoreDapper`/`InboxStoreDapper` for this database).
   No ADR, SPEC or later issue records an exception for this package, so this is drift from a
   recorded decision (SPEC-003 AUD-01, Class B).
2. Seventeen call sites use synchronous ADO.NET methods (`.Open()`, `.ExecuteReader()`,
   `.ExecuteScalar()`, `.ExecuteNonQuery()`, `.Read()`, `.BeginTransaction()`, `.Commit()`)
   instead of their `*Async` overloads with a `CancellationToken`, contradicting the rule in
   `CLAUDE.md` ("Database calls are asynchronous with a `CancellationToken`") established by
   #794/#897 (Sonar S6966) precisely to avoid thread-pool starvation under load.

## Steps to Reproduce

1. Open `src/Encina.ADO.PostgreSQL/Sharding/Migrations/PostgreSqlSchemaIntrospector.cs:111`.
2. Observe `new ShardSchema(shardId, tables, DateTimeOffset.UtcNow)` — no `TimeProvider` parameter exists on this type/method to inject through.
3. Open `src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs:153` and `:175`.
4. Observe `.BeginTransaction()` / `.Commit()` called synchronously on the unit-of-work hot path (once per business transaction).

## Expected Behavior

- Every production code path in this package takes `TimeProvider` (optional parameter,
  defaulting to `TimeProvider.System`) instead of reading `DateTime.UtcNow`/`DateTimeOffset.UtcNow`.
- Every ADO.NET call uses its async overload with a `CancellationToken` propagated from the
  caller.

## Actual Behavior

- `Sharding/Migrations/PostgreSqlSchemaIntrospector.cs:111` — `DateTimeOffset.UtcNow` read directly.
- `Sharding/Migrations/AdoMigrationHistoryStore.cs:82,116,191` — `DateTime.UtcNow` read directly (×3).
- `UnitOfWork/UnitOfWorkADO.cs:153,175` — synchronous `.BeginTransaction()`/`.Commit()` on the hot path.
- `BulkOperations/BulkOperationsPostgreSQL.cs:381` — synchronous `.Open()`.
- `ABAC/PolicyStoreADO.cs:435` — synchronous `.Open()`.
- `Modules/SchemaValidatingConnection.cs:81` — synchronous `.Open()` override.
- `Temporal/TemporalRepositoryADO.cs:635,662,672,682` — synchronous `.Open()`/`.ExecuteReader()`/`.ExecuteScalar()`/`.Read()`.
- `Anonymization/TokenMappingStoreADO.cs:252` — synchronous `.Open()`.
- `Sharding/ReferenceTables/ReferenceTableStoreFactoryADO.cs:21` — synchronous `.Open()`.
- `Sharding/ReferenceTables/ReferenceTableStoreADO.cs:96,98` — synchronous `.ExecuteReader()`/`.Read()`.
- `Sharding/Migrations/AdoHelper.cs:18,28,38` — synchronous `.ExecuteReader()`/`.ExecuteNonQuery()`/`.Read()`.
- `Sharding/Migrations/AdoMigrationExecutor.cs:53` — synchronous `.ExecuteNonQuery()`.
- `Modules/SchemaValidatingCommand.cs:118,132` — synchronous `.ExecuteNonQuery()`/`.ExecuteScalar()`.

## Environment

- **Encina Version**: pre-1.0, main as of 2026-09-24
- **.NET Version**: .NET 10.0
- **OS**: Windows 11 (audit machine); applies on any OS
- **Package(s) Affected**: Encina.ADO.PostgreSQL

## Code Sample

```csharp
// UnitOfWorkADO.cs:153,175 — should be:
await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
...
await transaction.CommitAsync(cancellationToken);
```

## Stack Trace

N/A — found by static audit, not a runtime failure report.

## Additional Context

Found during the SPEC-003 pilot-2 deep quality audit of `Encina.ADO.PostgreSQL`
(`docs/knowledge/audits/encina-ado-postgresql.md` when the record lands; interim result at
`artifacts/audit/encina-ado-postgresql.md` in worktree `waud-adopg`). `UnitOfWorkADO.cs` is the
highest-priority fix since it runs on every business transaction, matching the exact pattern
#794/#897 already fixed for `IDbConnection.Open()`. The `Sharding/Migrations/*` and
`Temporal/TemporalRepositoryADO.cs` call sites are lower-traffic (schema introspection,
migration execution, temporal history queries) but still contradict the mandatory rule.

## Root Cause

These call sites predate the TimeProvider and async-only rules being enforced project-wide, or
were added without following them; no test currently fails on their presence (no architecture
test scans for `DateTime.UtcNow` or synchronous ADO.NET calls yet, which is itself a
SPEC-003 REQ-034 gap: AUD-14/AUD-16 are checklist items that "a machine can check" but currently
run only as this manual audit).
