# Load Tests - Transactional Outbox (MongoDB)

## Status: Not Implemented

## Justification

A load test for the transactional outbox on MongoDB (`TransactionalOutboxPipelineBehavior`, `MongoSessionAccessor`
and `UnitOfWorkMongoDB` sharing one lease) is intentionally not implemented:

### 1. One session and one transaction per scope, nothing shared

- `MongoSessionAccessor` is registered scoped. Each scope starts its own `IClientSessionHandle`; a session is not
  safe for concurrent use and the accessor never hands it to another scope. Two concurrent requests never touch the
  same accessor, lease or transaction.
- `TransactionLeaseState` is created per transaction and mutated only by the participants of that scope, which run
  sequentially in one request pipeline. There is no lock, queue or static mutable state to contend on.
- The only statics are the per-`TRequest` skip flag of the behavior (read-only) and the diagnostics instances
  (`ActivitySource`, `Meter`), thread-safe by design.

### 2. The concurrency risk sits in the server

- Under load the risks are write conflicts (`TransientTransactionError`), the transaction lifetime limit
  (60 s by default) and the driver connection pool. They depend on the server and on the business writes, not on the
  code of this slice. The accessor deliberately does not retry (the application decides).
- The integration tests run a real single-node replica set and a real standalone server and prove the atomicity
  invariants: commit, handler failure and exception, outbox write failure, unit of work join and rollback-only,
  session disposal abort, abort after a failed commit, inbox plus outbox, saga and scheduled message enlistment,
  read/write separation coexistence, and the standalone fail-closed result.

### 3. Why the Unit of Work load rule does not apply to `UnitOfWorkMongoDB`

`AGENTS.md` asks for a load test of the Unit of Work. This slice does not add a Unit of Work or change its
concurrency model: `UnitOfWorkMongoDB` only moved onto the shared lease. Its scope is still one session per request
scope, used by one request at a time, so what a Unit of Work load test measures (parallel transactions, rollback
under load, pool pressure) is unchanged.

### 4. Adequate coverage from other test types

- **Unit tests**: `MongoSessionAccessor`, the four stores' session passing, `UnitOfWorkMongoDB` through the lease,
  the health check, the registration.
- **Guard tests**: every new or changed public constructor and method.
- **Contract tests**: the lease contract against `MongoSessionAccessor` over a mocked client.
- **Property tests**: the transaction commits if and only if the owner commits and no participant rolled back.
- **Integration tests**: MongoDB on a real replica set and a real standalone server.

### 5. Recommended alternative

If load evidence is needed later, add a MongoDB scenario to `tests/Encina.LoadTests/profiles/nbomber.database-uow.json`
that sends requests carrying notifications, and compare against the same scenario without notifications.

## Related Files

- `src/Encina.MongoDB/Transactions/MongoSessionAccessor.cs`
- `src/Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs`
- `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs`
- `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Outbox/OutboxAtomicityMongoDBTests.cs`

## Date: 2026-10-28

## Issue: #718
