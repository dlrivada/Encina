# Benchmarks - Transactional Outbox (ADO.NET and Dapper PostgreSQL and MySQL)

## Status: Not Implemented (for PostgreSQL and MySQL)

## Justification

The skip-path benchmark of the transactional outbox is implemented once, for SQL Server, in
`Encina.ADO.SqlServer.Benchmarks/Benchmarks/TransactionalOutboxSkipPathBenchmarks.cs`. It is not repeated for the
PostgreSQL and MySQL benchmark projects, ADO.NET or Dapper:

### 1. The measured code is provider-neutral

`TransactionalOutboxPipelineBehavior<TRequest, TResponse>` lives in `Encina.Messaging` and is identical for every
provider. On the skip path (a request without `IHasNotifications`) it runs a static per-`TRequest` flag check and
calls the next step; it never reaches the accessor, the store or the connection, so the provider is irrelevant to
the number the benchmark measures. A PostgreSQL or MySQL copy would produce the same result with a different
connection type that is never used.

### 2. The database-bound path is not a hot path of this slice

The path that does touch the database (begin, write n rows, commit) is dominated by the engine's round trips, which
the existing provider benchmarks (`OutboxStoreBenchmarks`) already track per provider. The slice adds one
transaction per request that has notifications and removes a second, non-atomic write; it adds no new loop or
algorithm to measure.

### 3. Adequate coverage from other test types

- **Integration tests** run the full path on SQL Server, PostgreSQL and MySQL.
- **Property and contract tests** cover the commit and rollback invariants.
- **Unit tests** assert that the skip path does not touch the transaction source.

### 4. Recommended alternative

If a database-bound regression is suspected, run `OutboxStoreBenchmarks` for the provider and compare the add path
with and without an active transaction.

## Related Files

- `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs`
- `tests/Encina.BenchmarkTests/Encina.ADO.SqlServer.Benchmarks/Benchmarks/TransactionalOutboxSkipPathBenchmarks.cs`

## Date: 2026-10-20

## Issue: #718
