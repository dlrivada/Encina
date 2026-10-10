# Benchmarks - Dead Letter Store

## Status: Not Implemented

## Justification

Benchmarks of the persistent dead letter stores (`IDeadLetterStore` on the ten database providers) are intentionally not implemented for the following reasons:

### 1. Not a Hot Path

The queue is written only when a message has failed permanently and read by operators. Neither path runs per request, and neither is on the latency of a successful message.

### 2. Cost Is Dominated by the Database

Each operation is one statement (an indexed `INSERT`, a keyed `UPDATE`, a paged `SELECT` ordered by `DeadLetteredAtUtc` then `Id`, a set-based `DELETE`). A micro-benchmark would measure the database round trip and the container, not Encina code.

### 3. The Indexes Are Verified Instead

The six indexes of the `029` scripts (unique source key, paging order, pending, expiry, correlation, tenant) are asserted against real PostgreSQL and MySQL schemas by the schema script integration tests, which protects the access paths a benchmark would exercise.

### 4. Adequate Coverage from Other Test Types

- **Integration Tests**: the contract on the ten stores, including a data set larger than one page.
- **Property Tests**: paging and expiry invariants.

### 5. Recommended Alternative

If a deployment needs capacity numbers, run BenchmarkDotNet on the insert and the first page of `GetMessagesAsync` against the production database engine, with `--filter` verified through `--list flat` first.

## Related Files

- `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`
- `src/Encina.ADO.SqlServer/Scripts/029_CreateDeadLetterMessagesTable.sql`
- `tests/Encina.IntegrationTests/SchemaScripts/`

## Date: 2026-10-09
## Issue: #583
