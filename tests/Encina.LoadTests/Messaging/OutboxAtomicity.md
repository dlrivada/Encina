# Load Tests - Transactional Outbox (ADO.NET and Dapper)

## Status: Not Implemented

## Justification

A load test for the transactional outbox on the ADO.NET and Dapper providers (`TransactionalOutboxPipelineBehavior`,
`DbTransactionAccessor`, the four messaging stores joining the scope's transaction) is intentionally not implemented:

### 1. One transaction per scope, no state shared across requests

- The accessor is registered scoped and holds one connection and at most one transaction, owned by exactly one
  request scope. Two concurrent requests never touch the same accessor, lease or transaction.
- `TransactionLeaseState` is created per transaction and is only mutated by the participants of that scope, which
  run sequentially inside one request pipeline. There is no lock, queue or static mutable state to contend on.
- The only statics are the per-`TRequest` skip flag (a read-only boolean computed once) and the diagnostics
  instances (`ActivitySource`, `Meter`), which are thread-safe by design.

### 2. The concurrency risk sits in the database, which the integration tests already cover

- What can go wrong under load is database behavior (lock waits, deadlocks, connection pool pressure). That
  depends on the engine and the business statements, not on the code of this slice, and a load profile of this
  slice would measure the database.
- The integration tests run the real providers (SqlServer, PostgreSQL, MySQL) and prove the atomicity
  invariants: commit, handler failure, outbox write failure, join and rollback-only, inbox plus outbox, module
  isolation and database-per-tenant.

### 3. Why the Unit of Work load rule does not apply to this slice

`AGENTS.md` asks for a load test of the Unit of Work. This slice does not add a Unit of Work or change its
concurrency model: `UnitOfWorkADO` only moved onto the shared lease (`IDbTransactionAccessor.BeginOrJoinAsync`).
Its scope is still one connection per request scope, single-threaded, and it is not shared between requests, so
the concurrent behavior a Unit of Work load test measures (parallel transactions on separate connections, rollback
under load, connection pool pressure) is unchanged by this work. The existing profile
`tests/Encina.LoadTests/profiles/nbomber.database-uow.json` stays the alternative for that: it exercises
concurrent transactions, rollbacks and connection pool pressure. It currently lists only the EF Core providers; the
ADO.NET and Dapper providers are not part of it, and adding them belongs to the Unit of Work load work, not to this slice.

### 4. Adequate coverage from other test types

- **Unit tests**: `DbTransactionAccessor`, `TransactionLeaseState`, `OutboxWriter`,
  `TransactionalOutboxPipelineBehavior`, the transaction behavior and the registration order.
- **Guard tests**: every new or changed public constructor and method.
- **Contract tests**: the lease contract (owner, joiner, rollback-only, dispose) against the real accessor, and
  the behavior contract of the transactional outbox.
- **Property tests**: the transaction commits if and only if the owner commits and no participant rolled back;
  a committed request leaves exactly n outbox messages and any other outcome leaves none.
- **Integration tests**: the three ADO.NET and the three Dapper providers on real databases.
- **Benchmark**: the skip path of the behavior (`ADO.SqlServer` benchmarks project).

### 5. Recommended alternative

If load evidence is needed later, extend `nbomber.database-uow.json` with the ADO.NET and Dapper providers and a scenario
that sends requests carrying notifications, then compare against the same scenario without notifications.

## Related Files

- `src/Encina.Messaging/Transactions/DbTransactionAccessor.cs`
- `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs`
- `src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs`
- `tests/Encina.LoadTests/profiles/nbomber.database-uow.json`
- `tests/Encina.IntegrationTests/Messaging/Outbox/OutboxAtomicityScenario.cs`

## Date: 2026-10-20

## Issue: #718
