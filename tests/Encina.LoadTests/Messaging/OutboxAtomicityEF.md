# Load Tests - Transactional Outbox (EF Core)

## Status: Not Implemented

## Justification

A load test for the transactional outbox on EF Core (`TransactionalOutboxPipelineBehavior`,
`DbContextTransactionAccessor`, EF's `TransactionPipelineBehavior` and `UnitOfWorkEF` sharing one lease) is
intentionally not implemented:

### 1. One transaction per scope, no state shared across requests

- `DbContextTransactionAccessor` is registered scoped and is bound to the scope's single `DbContext`, which EF Core
  itself does not allow to be used concurrently. Two concurrent requests never touch the same accessor, lease or
  `IDbContextTransaction`.
- `TransactionLeaseState` is created per transaction and mutated only by the participants of that scope, which run
  sequentially inside one request pipeline. There is no lock, queue or static mutable state to contend on.
- The only statics are the per-`TRequest` caches of the EF transaction behavior (read-only, computed once) and the
  diagnostics instances (`ActivitySource`, `Meter`), which are thread-safe by design.

### 2. The concurrency risk sits in the database

- Under load the risks are lock waits, deadlocks and connection pool pressure. They depend on the engine and on
  the business statements, not on the code of this slice.
- The integration tests run the real providers (SqlServer and PostgreSQL; MySQL is skipped until Pomelo supports
  EF Core 10, #2086) and prove the atomicity invariants: commit, handler failure, outbox write failure, join and
  rollback-only, inbox plus outbox, retrying strategy, application-begun transaction and a transaction that ended
  mid-request.

### 3. Why the Unit of Work load rule does not apply to `UnitOfWorkEF`

`AGENTS.md` asks for a load test of the Unit of Work. This slice does not add a Unit of Work or change its
concurrency model: `UnitOfWorkEF` only moved onto the shared lease (`IDbTransactionAccessor.BeginOrJoinAsync`).
Its scope is still one `DbContext` per request scope, used by one request at a time, so the concurrent behavior a
Unit of Work load test measures (parallel transactions on separate contexts, rollback under load, connection pool
pressure) is unchanged. The existing profile `tests/Encina.LoadTests/profiles/nbomber.database-uow.json`, which
already lists the EF Core providers, stays the alternative.

### 4. Adequate coverage from other test types

- **Unit tests**: `DbContextTransactionAccessor`, EF's `TransactionPipelineBehavior`, `UnitOfWorkEF` through the
  lease, the registration and its order.
- **Guard tests**: every new or changed public constructor and method.
- **Contract tests**: the lease contract against `DbContextTransactionAccessor` over SQLite in memory.
- **Property tests**: the transaction commits if and only if the owner commits and no participant rolled back.
- **Integration tests**: EF Core on real SqlServer and PostgreSQL.

### 5. Recommended alternative

If load evidence is needed later, extend `nbomber.database-uow.json` with a scenario that sends requests carrying
notifications through EF Core, then compare against the same scenario without notifications.

## Related Files

- `src/Encina.EntityFrameworkCore/Transactions/DbContextTransactionAccessor.cs`
- `src/Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs`
- `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs`
- `tests/Encina.LoadTests/profiles/nbomber.database-uow.json`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Outbox/OutboxAtomicityEFTestsBase.cs`

## Date: 2026-10-23

## Issue: #718
