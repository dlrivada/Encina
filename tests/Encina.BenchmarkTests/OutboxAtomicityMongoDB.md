# Benchmarks - Transactional Outbox (MongoDB)

## Status: Not Implemented

## Justification

1. The only hot path this slice adds is the skip path of `TransactionalOutboxPipelineBehavior` (a request without
   `IHasNotifications`). That behavior is provider-neutral and lives in `Encina.Messaging`; its skip-path benchmark
   already exists for the ADO.NET provider (see `OutboxAtomicity.md`), and the code measured is identical for MongoDB.
2. The transaction work that MongoDB adds is a server round trip per operation (`StartSession`, the writes, commit).
   Its cost is the cost of the server, not of Encina code, so a benchmark of it would measure the engine.
3. `MongoSessionAccessor` and `UnitOfWorkMongoDB` only moved onto the shared lease; they add no loop, allocation hot
   spot or serialization of their own. One session per scope, nothing shared.

### Adequate coverage from other test types

- **Unit, guard, contract and property tests** exercise the accessor, the lease contract and the
  commit-iff-owner-commits invariant over a mocked client.
- **Integration tests** prove the behavior on a real replica set and a real standalone server.

### Recommended alternative

Extend the skip-path benchmark only if MongoDB ever gets its own variant of the behavior.

## Related Files

- `tests/Encina.BenchmarkTests/OutboxAtomicity.md`
- `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs`
- `src/Encina.MongoDB/Transactions/MongoSessionAccessor.cs`

## Date: 2026-10-28

## Issue: #718
